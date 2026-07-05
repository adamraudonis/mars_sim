using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Regenerative life support chain, ISS-heritage architecture scaled by design crew:
    ///   CO2 removal (CDRA-class)  : cabin CO2 -> CO2 buffer
    ///   O2 generation (OGA-class) : potable water -> cabin O2 + H2 buffer
    ///   Sabatier (CRA-class)      : CO2 + 4 H2 -> CH4 + 2 H2O (water recovered; CH4 vented or to depot)
    ///   Water recovery (UPA+WPA)  : wastewater -> potable (recovery fraction; brine loss)
    /// Runs at L1 (rate-based units with power draws). L0 folds the chain into net closure
    /// fractions with the same interfaces. All units feed the MaintenanceSystem.
    /// </summary>
    public sealed class Eclss : SimModule
    {
        public override string DisplayName => "ECLSS";

        /// <summary>Crew count the installed hardware is sized for (grows with cargo).</summary>
        public int DesignCrew { get; set; }

        public double SabatierCh4KgPerSol { get; private set; }

        private Param _co2CapPerCm, _co2KwhPerKg, _ogaCapPerCm, _ogaKwhPerKgO2,
                      _sabConversion, _sabVent, _wrsRecovery, _wrsKwhPerKg, _o2ReserveTargetSols,
                      _baseKwPerCm;

        private Store _co2, _h2, _o2Store, _water, _wasteWater, _ch4Depot;
        private Habitat _hab;
        private Crew _crew;
        private MaintenanceSystem _maint;

        private double Fcap(string tag) => _maint?.FunctionCapacity(this, tag) ?? CapacityFactor;

        /// <summary>ECLSS idles at keep-alive until crew are aboard — dormant racks don't consume MTBF.</summary>
        public override double FailureDutyCycle => DesignCrew > 0 && (_crew?.Count ?? 0) > 0 ? 1.0 : 0.05;

        private double _stepPowerKw;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _co2CapPerCm = p.GetOrRegister("eclss.co2_removal_capacity_kg_cm_day", "CO2 removal capacity per crew", 1.3, "kg/CM-day",
                "CDRA sized ~125% of metabolic CO2 (BVAD)");
            _co2KwhPerKg = p.GetOrRegister("eclss.co2_removal_kwh_per_kg", "CO2 removal specific energy", 4.0, "kWh/kg CO2",
                "CDRA-class: ~0.86 kW per 4-crew (ISS power reports)");
            _ogaCapPerCm = p.GetOrRegister("eclss.oga_capacity_kg_cm_day", "O2 generation capacity per crew", 1.0, "kg/CM-day",
                "ISS OGA up to ~9 kg/day for 6+ crew");
            _ogaKwhPerKgO2 = p.GetOrRegister("eclss.oga_kwh_per_kg_o2", "O2 generation specific energy", 11.0, "kWh/kg O2",
                "ISS OGA ~3.6 kW nominal for ~5 kg O2/day incl. avionics");
            _sabConversion = p.GetOrRegister("eclss.sabatier_conversion", "Sabatier CO2 conversion efficiency", 0.90, "",
                "ISS CRA flight performance");
            _sabVent = p.GetOrRegister("eclss.sabatier_vent_ch4", "Vent Sabatier CH4 (1) or send to depot (0)", 1, "bool",
                "ISS vents; on Mars CH4 is propellant — a trade the sim exposes");
            _wrsRecovery = p.GetOrRegister("eclss.water_recovery_fraction", "Water recovery fraction", 0.98, "",
                "ISS WRS demonstrated 98% (NASA 2023)");
            _wrsKwhPerKg = p.GetOrRegister("eclss.wrs_kwh_per_kg", "Water recovery specific energy", 0.4, "kWh/kg",
                "UPA+WPA ~1.5 kW for 6 crew scale");
            _o2ReserveTargetSols = p.GetOrRegister("eclss.o2_reserve_target_sols", "O2 reserve target", 60, "sols",
                "Ops policy: buffer against OGA outage (tunable)");
            _baseKwPerCm = p.GetOrRegister("eclss.base_kw_per_crew", "ECLSS hotel load per crew (TCC, sensors, fans)", 0.25, "kW",
                "ISS ECLSS secondary loads scaled");

            _co2 = ctx.Stores.GetOrCreate("co2_buffer", Resource.CO2, 500);
            _h2 = ctx.Stores.GetOrCreate("h2_eclss", Resource.H2, 50);
            _o2Store = ctx.Stores.GetOrCreate("o2_reserve", Resource.O2, 0);
            _water = ctx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            _wasteWater = ctx.Stores.GetOrCreate("water_waste", Resource.WaterWaste, 0);
            _ch4Depot = ctx.Stores.GetOrCreate("depot_ch4", Resource.CH4, 0);
            _hab = Engine.Find<Habitat>();
            _crew = Engine.Find<Crew>();
            _maint = Engine.Find<MaintenanceSystem>();
        }

        public override void PreTick(SimContext ctx)
        {
            if (DesignCrew == 0 || _hab == null) return;
            // Estimate this step's power need at full throughput; actual work scales with grant.
            double days = ctx.DtSeconds / 86400.0;
            double co2Max = _co2CapPerCm.Value * DesignCrew * days;
            double o2Max = _ogaCapPerCm.Value * DesignCrew * days;
            double wrsMax = _wasteWater.AmountKg * 0.2; // drain up to 20% of backlog per step
            _stepPowerKw = (co2Max * _co2KwhPerKg.Value + o2Max * _ogaKwhPerKgO2.Value + wrsMax * _wrsKwhPerKg.Value) / ctx.DtHours
                           + _baseKwPerCm.Value * DesignCrew;
            ctx.Power.Request(this, _stepPowerKw, LoadPriority.Critical);
        }

        public override void Tick(SimContext ctx)
        {
            if (DesignCrew == 0 || _hab == null) return;
            double powerGrantOnly = ctx.Power.GrantedFraction(this);
            double days = ctx.DtSeconds / 86400.0;

            // --- CO2 removal: keep cabin ppCO2 near zero backlog; capacity-limited ---
            // Each function degrades with its own hardware (a dead water processor must not
            // stop the CO2 scrubbers).
            double co2Cap = _co2CapPerCm.Value * DesignCrew * days * powerGrantOnly * Fcap("co2");
            double co2Scrubbed = _hab.DrawCO2(Math.Min(co2Cap, _hab.CabinCO2Kg * 0.5));
            _co2.Deposit(co2Scrubbed);

            // --- OGA: close the actual ppO2 deficit (mass-based, no bang-bang overshoot)
            //     and slowly rebuild the O2 reserve while crew are aboard ---
            double o2Cap = _ogaCapPerCm.Value * DesignCrew * days * powerGrantOnly * Fcap("oga");
            double deficitKg = _hab.KgToRaisePpO2(ctx.Params.V("eclss.ppo2_setpoint") - _hab.PpO2Kpa);
            double o2ForCabin = Math.Min(o2Cap, deficitKg);
            double reserveTargetKg = _crew != null && _crew.Count > 0
                ? ctx.Params.V("eclss.o2_consumption_kg_cm_day") * _crew.Count * 1.0275 * _o2ReserveTargetSols.Value
                : 0;
            // Rebuild the reserve only while the tank can actually take it (else the
            // electrolyzed O2 — and the water it cost — would vent at the full store).
            double o2ForReserve = _o2Store.AmountKg < reserveTargetKg && _o2Store.FreeKg > o2Cap
                ? o2Cap * 0.3 : 0;
            double o2ToMake = Math.Min(o2Cap, o2ForCabin + o2ForReserve);

            // Electrolysis stoichiometry: 9 kg H2O -> 8 kg O2 + 1 kg H2.
            double waterNeeded = o2ToMake * 9.0 / 8.0;
            double waterGot = _water.Withdraw(waterNeeded);
            double o2Made = waterGot * 8.0 / 9.0;
            double h2Made = waterGot / 9.0;
            if (o2ForCabin > 0)
            {
                double toCabin = Math.Min(o2Made, o2ForCabin);
                _hab.InjectO2(toCabin);
                _o2Store.Deposit(o2Made - toCabin);
            }
            else _o2Store.Deposit(o2Made);
            _h2.Deposit(h2Made);

            // --- Sabatier: CO2 + 4H2 -> CH4 + 2H2O. Mass basis per kg CH4: 2.75 CO2, 0.5 H2 -> 2.25 H2O.
            //     Throughput-capped to the reactor's rate (sized to the CO2 scrub stream), so
            //     behavior is timestep-independent rather than "90% of inventory per step". ---
            double reactorCapKg = co2Cap / 2.75;
            double h2Avail = _h2.AmountKg;
            double co2Avail = _co2.AmountKg;
            double ch4Possible = Math.Min(reactorCapKg,
                                     Math.Min(h2Avail / 0.5, co2Avail / 2.75))
                                 * _sabConversion.Value * powerGrantOnly * Fcap("oga");
            if (ch4Possible > 1e-9)
            {
                double ch4 = ch4Possible;
                _h2.Withdraw(ch4 * 0.5);
                _co2.Withdraw(ch4 * 2.75);
                _water.Deposit(ch4 * 2.25);
                if (_sabVent.Value < 0.5) _ch4Depot.Deposit(ch4);
                SabatierCh4KgPerSol = ch4 / ctx.DtSols;
            }
            else SabatierCh4KgPerSol = 0;

            // --- Water recovery (drain rate per sol, timestep-independent) ---
            double wrsDrainFrac = Math.Min(1.0, 4.8 * ctx.DtSols) * powerGrantOnly * Fcap("wrs");
            double wrsIn = _wasteWater.Withdraw(_wasteWater.AmountKg * wrsDrainFrac);
            _water.Deposit(wrsIn * _wrsRecovery.Value); // brine remainder is lost mass (tracked by ledger)

            Record(ctx, "eclss.power", "ECLSS power draw", "kW", _stepPowerKw * ctx.Power.GrantedFraction(this));
            Record(ctx, "eclss.o2_production", "O2 generation rate", "kg/sol", o2Made / ctx.DtSols);
            Record(ctx, "eclss.co2_scrubbed", "CO2 removal rate", "kg/sol", co2Scrubbed / ctx.DtSols);
        }

        public override string StatusLine => DesignCrew == 0 ? "Not installed"
            : $"Sized for {DesignCrew} crew ({Health})";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Design crew", DesignCrew, "people");
                yield return ("O₂ reserve", Engine.Stores.Get("o2_reserve")?.AmountKg ?? 0, "kg");
                yield return ("Potable water", Engine.Stores.Get("water_potable")?.AmountKg ?? 0, "kg");
            }
        }
    }
}
