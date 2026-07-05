using System;
using System.Collections.Generic;
using MarsSim.Core.Modules;
using MarsSim.Core.Params;

namespace MarsSim.Core.Study
{
    public sealed class DistillationResult
    {
        public string ModuleId;
        public string Summary;
        public Dictionary<string, double> FittedParams = new();
        public double FitErrorPercent;
    }

    /// <summary>
    /// Runs a subsystem at its highest fidelity in an isolated sub-simulation, then fits the
    /// distilled (L0) coefficients and installs them as parameter overrides. This is the
    /// "go deep, then average" workflow: explore solar at L2 over a full Mars year, distill
    /// to kWh/sol/kW, and run 20-year campaign studies cheaply — drilling back down whenever
    /// a distilled number turns out to matter.
    /// </summary>
    public static class Distiller
    {
        /// <summary>Generic labor source for isolated sub-sims (a real base has workers; the
        /// module under test shouldn't starve just because its harness has no crew).</summary>
        private sealed class LaborSupplier : SimModule
        {
            public double HoursPerSol = 100;
            public override string DisplayName => "Harness labor supply";
            public override void PreTick(SimContext ctx)
                => ctx.Labor.SupplyCrewHours(HoursPerSol * ctx.DtSols);
            public override void Tick(SimContext ctx) { }
        }
        /// <summary>
        /// Distill the solar farm: L2 run over one Mars year (with the real dust/storm process)
        /// -> mean specific yield installed as power_solar.l0_kwh_per_sol_per_kw.
        /// </summary>
        public static DistillationResult DistillSolar(ParameterRegistry parameters, double latitudeDeg,
            ulong seed = 1234, double sols = Units.SolsPerMarsYear)
        {
            var engine = new SimulationEngine(parameters, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, seed);
            var env = engine.Add(new MarsEnvironment { LatitudeDeg = latitudeDeg }, "environment");
            env.Fidelity = FidelityLevel.L2_Physics;
            var farm = engine.Add(new SolarFarm { ArrayAreaM2 = 1000 }, "solar");
            farm.Fidelity = FidelityLevel.L2_Physics;
            engine.Add(new LaborSupplier(), "harness_labor");
            engine.Initialize();
            engine.RunToSol(sols);

            var series = engine.History.Get("solar.output");
            double sum = 0;
            for (int i = 0; i < series.Count; i++) sum += series[i];
            double meanKw = sum / Math.Max(1, series.Count);
            double kwhPerSol = meanKw * Units.SolHours;
            double perKwInstalled = kwhPerSol / Math.Max(1e-9, farm.InstalledKwRating);

            // Fit error: L0 reproduces the mean exactly; report the seasonal variability the
            // distillation throws away so the user knows what they gave up.
            double variance = 0;
            for (int i = 0; i < series.Count; i++) variance += Math.Pow(series[i] - meanKw, 2);
            double cv = meanKw > 0 ? Math.Sqrt(variance / Math.Max(1, series.Count)) / meanKw : 0;

            parameters.SetDistilledOverride("power_solar.l0_kwh_per_sol_per_kw", perKwInstalled);
            return new DistillationResult
            {
                ModuleId = "solar",
                Summary = $"L2 run over {sols:F0} sols at {latitudeDeg:F0}°N: {perKwInstalled:F2} kWh/sol per installed kW "
                          + $"(discarded variability CV={cv:P0} — night/storms move into the battery question)",
                FittedParams = { ["power_solar.l0_kwh_per_sol_per_kw"] = perKwInstalled },
                FitErrorPercent = cv * 100,
            };
        }

        /// <summary>
        /// Distill the greenhouse batch model into continuous L0 coefficients
        /// (kcal/m²/sol actually achieved, including batch losses and staggering gaps).
        /// </summary>
        public static DistillationResult DistillGreenhouse(ParameterRegistry parameters,
            ulong seed = 1234, double sols = 500)
        {
            var engine = new SimulationEngine(parameters, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, seed);
            engine.Add(new MarsEnvironment(), "environment");
            var hab = engine.Add(new Habitat(), "habitat");
            var gh = engine.Add(new Greenhouse { GrowingAreaM2 = 200 }, "greenhouse");
            gh.Fidelity = FidelityLevel.L1_Analytic;
            // Unlimited support so we measure the crop model, not the power/labor systems.
            engine.Add(new NuclearPlant { Units = 100 }, "nuclear");
            engine.Add(new LaborSupplier(), "harness_labor");
            engine.Initialize();
            hab.AddVolume(engine.Context, 1000, arrivesPressurized: true);
            var water = engine.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            water.AddCapacity(200000);
            water.Deposit(200000);
            engine.RunToSol(sols);

            var kcalSeries = engine.History.Get("greenhouse.kcal_per_sol");
            // Skip the first crop cycle (ramp-up) when averaging.
            int skip = (int)(kcalSeries.Count * (100.0 / sols));
            double sum = 0; int n = 0;
            for (int i = skip; i < kcalSeries.Count; i++) { sum += kcalSeries[i]; n++; }
            double kcalPerM2Sol = (sum / Math.Max(1, n)) / 200.0;

            // Fit the UTILIZATION (achieved / nominal), not the nominal rate itself — the
            // nominal kcal/m²/sol is the batch model's own input, and overriding it would
            // make repeated distillations self-referential.
            double nominal = parameters.V("food.kcal_per_m2_sol");
            double utilization = nominal > 0 ? Math.Clamp(kcalPerM2Sol / nominal, 0, 1.2) : 1;
            parameters.SetDistilledOverride("food.l0_utilization", utilization);
            return new DistillationResult
            {
                ModuleId = "greenhouse",
                Summary = $"L1 batch run over {sols:F0} sols: sustained {kcalPerM2Sol:F1} of nominal "
                          + $"{nominal:F1} kcal/m²/sol → L0 utilization {utilization:P0} "
                          + "(staggering gaps + batch losses)",
                FittedParams = { ["food.l0_utilization"] = utilization },
                FitErrorPercent = 0,
            };
        }
    }
}
