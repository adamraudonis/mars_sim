using System;
using MarsSim.Core.Modules;
using MarsSim.Core.Params;

namespace MarsSim.Core.Scenario
{
    /// <summary>
    /// Assembles a ready-to-run engine from a Scenario + parameter registry.
    /// The module set is the full composable stack; anything absent from the manifest simply
    /// stays at zero capacity and costs nothing, so one builder serves every architecture
    /// (solar-only, nuclear-only, no-greenhouse, robot-free, ...).
    /// </summary>
    public static class SimulationBuilder
    {
        public static SimulationEngine Build(Scenario scenario, ParameterRegistry parameters)
        {
            var engine = new SimulationEngine(parameters, scenario.EpochUtc, scenario.TimestepSeconds, scenario.Seed);

            parameters.ClearScenarioOverrides();
            foreach (var kv in scenario.Overrides)
                parameters.SetScenarioOverride(kv.Key, kv.Value);

            var env = engine.Add(new MarsEnvironment
            {
                LatitudeDeg = scenario.LatitudeDeg,
                ElevationKm = scenario.ElevationKm,
            }, "environment");

            var hab = engine.Add(new Habitat(), "habitat");
            var crew = engine.Add(new Crew(), "crew");
            var eclss = engine.Add(new Eclss(), "eclss");
            var solar = engine.Add(new SolarFarm(), "solar");
            var nuclear = engine.Add(new NuclearPlant(), "nuclear");
            var battery = engine.Add(new BatteryBank(), "battery");
            var greenhouse = engine.Add(new Greenhouse(), "greenhouse");
            var isru = engine.Add(new IsruPropellantPlant(), "isru_plant");
            var mine = engine.Add(new IceMine(), "ice_mine");
            var depot = engine.Add(new PropellantDepot(), "depot");
            var fleet = engine.Add(new StarshipFleet(), "fleet");
            var robots = engine.Add(new RobotFleet(), "robots");
            var maint = engine.Add(new MaintenanceSystem(), "maintenance");
            var campaign = engine.Add(new LaunchCampaign(), "campaign");

            campaign.LoadFlights(scenario.Flights);
            fleet.ReturnWindowSols.AddRange(scenario.ReturnWindowSols);

            ApplyFidelity(engine, scenario);
            engine.Initialize();
            return engine;
        }

        /// <summary>Fidelity keys are module ids; unknown keys are ignored (forward compatible).</summary>
        public static void ApplyFidelity(SimulationEngine engine, Scenario scenario)
        {
            foreach (var kv in scenario.Fidelity)
            {
                var m = engine.Find(kv.Key);
                if (m != null) m.Fidelity = kv.Value;
            }
        }
    }
}
