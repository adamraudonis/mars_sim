using System;
using System.Collections.Generic;
using System.Linq;
using MarsSim.Core.Params;
using MarsSim.Core.Scenario;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Executes the scenario's flight schedule. When ships land: stores fill immediately
    /// (unloading is cheap next to everything else), but *hardware only comes online through
    /// a deployment queue that consumes Construction labor* — so a manifest that outpaces the
    /// available crew+robot workforce piles up on the pad, visibly, in the timelapse.
    /// Also registers failure-prone component groups for each hardware lot as it deploys.
    /// </summary>
    public sealed class LaunchCampaign : SimModule
    {
        public override string DisplayName => "Launch campaign";

        private sealed class DeploymentTask
        {
            public string Description;
            public double TotalHours;
            public double HoursDone;
            public Action<SimContext> OnComplete;
        }

        private readonly Queue<Flight> _pending = new();
        private readonly List<DeploymentTask> _deployQueue = new();
        private const int ConcurrentDeployments = 4;
        // Per-task work-party ceiling, crew-eq hours/sol. Farm-scale assembly parallelizes
        // across many workers; the real limit is the labor pool, not this cap.
        private const double MaxHoursPerTaskPerSol = 100;

        private Param _deployHoursSolarPer100m2, _deployHoursPerNuclearUnit, _deployHoursGreenhousePer10m2,
                      _deployHoursPerHabModule, _deployHoursEclssPerCrew, _unloadHoursPerFlight;

        public int FlightsRemaining => _pending.Count;
        public int DeploymentsPending => _deployQueue.Count;

        public void LoadFlights(IEnumerable<Flight> flights)
        {
            foreach (var f in flights.OrderBy(f => f.Sol)) _pending.Enqueue(f);
        }

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _deployHoursSolarPer100m2 = p.GetOrRegister("mission_architecture.deploy_hours_solar_per_100m2", "Deploy labor: solar per 100 m²", 8, "crew-eq h",
                "NASA Mars surface array studies: deployment-days for MW-class farms");
            _deployHoursPerNuclearUnit = p.GetOrRegister("power_nuclear.deploy_crew_hours", "Deploy labor: reactor unit", 40, "crew-eq h",
                "Estimate: robotic emplacement + cabling");
            _deployHoursGreenhousePer10m2 = p.GetOrRegister("mission_architecture.deploy_hours_greenhouse_per_10m2", "Deploy labor: greenhouse per 10 m²", 12, "crew-eq h",
                "Outfitting-heavy installation estimate");
            _deployHoursPerHabModule = p.GetOrRegister("mission_architecture.deploy_hours_hab_per_100m3", "Deploy labor: habitat per 100 m³", 60, "crew-eq h",
                "Estimate: connect, leak-check, outfit");
            _deployHoursEclssPerCrew = p.GetOrRegister("mission_architecture.deploy_hours_eclss_per_crew", "Deploy labor: ECLSS per crew capacity", 10, "crew-eq h",
                "Rack installation + checkout estimate");
            _unloadHoursPerFlight = p.GetOrRegister("mission_architecture.unload_hours_per_ship", "Unload labor per ship", 30, "crew-eq h",
                "Estimate: 100+ t cargo, crane ops, hauling");
        }

        public override void PreTick(SimContext ctx)
        {
            double hours = 0;
            foreach (var t in _deployQueue.Take(ConcurrentDeployments))
                hours += Math.Min(t.TotalHours - t.HoursDone, ctx.DtSols * MaxHoursPerTaskPerSol);
            if (hours > 0)
                ctx.Labor.Request(this, TaskType.Construction, hours, LaborPriority.Normal);
        }

        public override void Tick(SimContext ctx)
        {
            // --- Deployment work first (on the task set PreTick requested labor for) ---
            if (_deployQueue.Count > 0)
            {
                double grant = ctx.Labor.GrantedFraction(this, TaskType.Construction);
                foreach (var t in _deployQueue.Take(ConcurrentDeployments).ToList())
                {
                    double step = Math.Min(t.TotalHours - t.HoursDone, ctx.DtSols * MaxHoursPerTaskPerSol) * grant;
                    t.HoursDone += step;
                    if (t.HoursDone >= t.TotalHours - 1e-6)
                    {
                        _deployQueue.Remove(t);
                        t.OnComplete(ctx);
                        Log(ctx, EventSeverity.Milestone, $"Deployed: {t.Description}");
                    }
                }
            }

            // --- Landings last: new tasks first request labor next PreTick ---
            while (_pending.Count > 0 && _pending.Peek().Sol <= ctx.Clock.Sol)
                ProcessFlight(ctx, _pending.Dequeue());

            Record(ctx, "campaign.flights_remaining", "Flights remaining", "", _pending.Count);
            Record(ctx, "campaign.deploy_queue", "Deployments pending", "", _deployQueue.Count);
        }

        private void ProcessFlight(SimContext ctx, Flight f)
        {
            Log(ctx, EventSeverity.Milestone, $"{f.Label}: {Math.Max(f.Ships.Count, 1)} ship(s) landing");

            var fleet = Engine.Find<StarshipFleet>();
            foreach (var ship in f.Ships)
            {
                var role = ship.Role switch
                {
                    "CrewTransport" => ShipRole.CrewTransport,
                    "TankerDepot" => ShipRole.TankerDepot,
                    _ => ShipRole.Cargo,
                };
                fleet?.Land(ctx, ship.Name, role, ship.ContributesHabitatVolume);
            }

            var c = f.Cargo;
            var maint = Engine.Find<MaintenanceSystem>();

            // Consumables & feedstocks transfer immediately (capacity grows with deliveries).
            DepositWithCapacity(ctx, "food", Resource.Food, c.FoodKg);
            DepositWithCapacity(ctx, "water_potable", Resource.WaterPotable, c.WaterKg);
            DepositWithCapacity(ctx, "o2_reserve", Resource.O2, c.O2Kg);
            DepositWithCapacity(ctx, "n2_reserve", Resource.N2, c.N2Kg);
            DepositWithCapacity(ctx, "h2_import", Resource.H2, c.H2Kg);
            foreach (var lot in c.Spares) maint?.AddSpares(lot.EquipmentClass, lot.Units, lot.UnitMassKg);

            if (c.Robots > 0)
            {
                var robots = Engine.Find<RobotFleet>();
                if (robots != null) robots.Count += c.Robots;
                maint?.Register(Engine.Find<RobotFleet>(), $"robots_wave_{ctx.Clock.SolNumber}", "robotics",
                    c.Robots, mtbfHours: 8000, repairCrewHours: 4, spareMassKg: 15);
            }

            if (f.CrewArriving > 0)
                Engine.Find<Crew>()?.Arrive(ctx, f.CrewArriving);

            // Unloading effort (quick, logistics).
            int shipCount = Math.Max(1, f.Ships.Count);
            EnqueueDeploy($"Unload {f.Label}", _unloadHoursPerFlight.Value * shipCount, _ => { });

            // Hardware goes through the construction queue.
            if (c.SolarAreaM2 > 0)
                EnqueueDeploy($"Solar array {c.SolarAreaM2:F0} m²",
                    c.SolarAreaM2 / 100.0 * _deployHoursSolarPer100m2.Value,
                    cx =>
                    {
                        var farm = Engine.Find<SolarFarm>();
                        if (farm != null) farm.ArrayAreaM2 += c.SolarAreaM2;
                        maint?.Register(farm, $"solar_strings_{cx.Clock.SolNumber}", "power_electronics",
                            Math.Max(1, (int)(c.SolarAreaM2 / 200)), mtbfHours: 150000, repairCrewHours: 2, spareMassKg: 20);
                    });

            if (c.BatteryKwh > 0)
                EnqueueDeploy($"Battery bank {c.BatteryKwh:F0} kWh", c.BatteryKwh / 100.0,
                    _ =>
                    {
                        var bat = Engine.Find<BatteryBank>();
                        if (bat != null) bat.NameplateKwh += c.BatteryKwh;
                    });

            if (c.NuclearUnits > 0)
                EnqueueDeploy($"{c.NuclearUnits} fission unit(s)",
                    c.NuclearUnits * _deployHoursPerNuclearUnit.Value,
                    cx =>
                    {
                        var nuc = Engine.Find<NuclearPlant>();
                        if (nuc != null) nuc.Units += c.NuclearUnits;
                        maint?.Register(nuc, $"reactor_bop_{cx.Clock.SolNumber}", "power_electronics",
                            c.NuclearUnits, mtbfHours: 87600, repairCrewHours: 8, spareMassKg: 100);
                    });

            if (c.IsruCapacityKgPerSol > 0)
                EnqueueDeploy($"ISRU plant {c.IsruCapacityKgPerSol / 1000.0:F1} t/sol", 1, // commissioning handled by the plant itself
                    cx =>
                    {
                        var plant = Engine.Find<IsruPropellantPlant>();
                        if (plant != null) plant.CapacityKgPerSol += c.IsruCapacityKgPerSol;
                        int units = Math.Max(1, (int)(c.IsruCapacityKgPerSol / 500));
                        maint?.Register(plant, $"isru_compressors_{cx.Clock.SolNumber}", "isru_mech",
                            units, mtbfHours: 6000, repairCrewHours: 6, spareMassKg: 80, weibullShape: 1.5);
                        maint?.Register(plant, $"isru_soxe_{cx.Clock.SolNumber}", "isru_stack",
                            units, mtbfHours: 12000, repairCrewHours: 4, spareMassKg: 40);
                    });

            if (c.IceCapacityKgPerSol > 0)
                EnqueueDeploy($"Ice mining rigs {c.IceCapacityKgPerSol / 1000.0:F1} t/sol",
                    c.IceCapacityKgPerSol / 100.0,
                    cx =>
                    {
                        var mine = Engine.Find<IceMine>();
                        if (mine != null) mine.CapacityKgPerSol += c.IceCapacityKgPerSol;
                        // Rigs ship with water tank farm: ~60 sols of production buffer.
                        cx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0)
                            .AddCapacity(c.IceCapacityKgPerSol * 60);
                        int rigs = Math.Max(1, (int)(c.IceCapacityKgPerSol / 1000));
                        maint?.Register(mine, $"excavators_{cx.Clock.SolNumber}", "mining_mech",
                            rigs, mtbfHours: 3000, repairCrewHours: 5, spareMassKg: 120, weibullShape: 1.6);
                    });

            if (c.GreenhouseM2 > 0)
                EnqueueDeploy($"Greenhouse {c.GreenhouseM2:F0} m²",
                    c.GreenhouseM2 / 10.0 * _deployHoursGreenhousePer10m2.Value,
                    cx =>
                    {
                        var gh = Engine.Find<Greenhouse>();
                        if (gh != null) gh.GrowingAreaM2 += c.GreenhouseM2;
                        maint?.Register(gh, $"gh_led_pumps_{cx.Clock.SolNumber}", "greenhouse_mech",
                            Math.Max(1, (int)(c.GreenhouseM2 / 50)), mtbfHours: 20000, repairCrewHours: 3, spareMassKg: 25);
                    });

            if (c.HabVolumeM3 > 0)
                EnqueueDeploy($"Habitat module {c.HabVolumeM3:F0} m³",
                    c.HabVolumeM3 / 100.0 * _deployHoursPerHabModule.Value,
                    cx => Engine.Find<Habitat>()?.AddVolume(cx, c.HabVolumeM3));

            if (c.EclssCrewCapacity > 0)
                EnqueueDeploy($"ECLSS racks for {c.EclssCrewCapacity} crew",
                    c.EclssCrewCapacity * _deployHoursEclssPerCrew.Value,
                    cx =>
                    {
                        var eclss = Engine.Find<Eclss>();
                        if (eclss != null) eclss.DesignCrew += c.EclssCrewCapacity;
                        // ECLSS racks ship with wastewater tankage (~30 sols of crew waste)
                        // and O2 accumulator headroom — without them the recovery loop vents.
                        cx.Stores.GetOrCreate("water_waste", Resource.WaterWaste, 0)
                            .AddCapacity(c.EclssCrewCapacity * 3.8 * 30);
                        cx.Stores.GetOrCreate("o2_reserve", Resource.O2, 0)
                            .AddCapacity(c.EclssCrewCapacity * 250);
                        int racks = Math.Max(1, c.EclssCrewCapacity / 4);
                        maint?.Register(eclss, $"oga_{cx.Clock.SolNumber}", "eclss_oru",
                            racks, mtbfHours: 8760, repairCrewHours: 4, spareMassKg: 50, functionTag: "oga");
                        maint?.Register(eclss, $"co2_removal_{cx.Clock.SolNumber}", "eclss_oru",
                            racks, mtbfHours: 6000, repairCrewHours: 3, spareMassKg: 40, functionTag: "co2");
                        maint?.Register(eclss, $"wrs_{cx.Clock.SolNumber}", "eclss_oru",
                            racks, mtbfHours: 7000, repairCrewHours: 5, spareMassKg: 60, weibullShape: 1.3, functionTag: "wrs");
                    });
        }

        private void DepositWithCapacity(SimContext ctx, string storeId, Resource r, double kg)
        {
            if (kg <= 0) return;
            var store = ctx.Stores.GetOrCreate(storeId, r, 0);
            store.AddCapacity(kg * 1.1); // delivered in its own tankage with margin
            store.Deposit(kg);
        }

        private void EnqueueDeploy(string description, double hours, Action<SimContext> onComplete)
            => _deployQueue.Add(new DeploymentTask { Description = description, TotalHours = Math.Max(1, hours), OnComplete = onComplete });

        public override string StatusLine =>
            $"{_pending.Count} flights scheduled, {_deployQueue.Count} deployments in progress";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Flights remaining", _pending.Count, "");
                yield return ("Deploy queue", _deployQueue.Count, "");
            }
        }
    }
}
