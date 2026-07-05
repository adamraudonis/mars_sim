using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MarsSim.Core.Modules;
using MarsSim.Core.Params;

namespace MarsSim.Core.Study
{
    /// <summary>One axis of a sweep: a parameter id and the values to try.</summary>
    public sealed class SweepAxis
    {
        public string ParamId;
        public double[] Values;

        public static SweepAxis Linear(string id, double from, double to, int steps)
        {
            var v = new double[steps];
            for (int i = 0; i < steps; i++) v[i] = steps == 1 ? from : from + (to - from) * i / (steps - 1.0);
            return new SweepAxis { ParamId = id, Values = v };
        }

        public static SweepAxis List(string id, params double[] values)
            => new SweepAxis { ParamId = id, Values = values };
    }

    public sealed class StudyConfig
    {
        public string Name = "study";
        public Scenario.Scenario Scenario;
        public List<SweepAxis> Axes = new();
        public int MonteCarloSeeds = 1;
        public double? DurationSolsOverride;
    }

    /// <summary>Objective metrics extracted from a finished run — the columns of the study CSV.</summary>
    public static class Metrics
    {
        public static Dictionary<string, double> Extract(SimulationEngine e)
        {
            var m = new Dictionary<string, double>();
            var crew = e.Find<Crew>();
            var fleet = e.Find<StarshipFleet>();

            m["final_sol"] = e.Clock.Sol;
            m["crew_final"] = crew?.Count ?? 0;
            m["fatalities"] = crew?.Fatalities ?? 0;
            m["crew_health_final"] = crew?.HealthIndex ?? 0;
            m["crew_dose_msv"] = crew?.CumulativeDoseMsv ?? 0;
            m["return_prop_fraction"] = fleet?.ReturnPropellantFraction ?? 0;
            m["food_kg_final"] = e.Stores.Get("food")?.AmountKg ?? 0;
            m["water_kg_final"] = e.Stores.Get("water_potable")?.AmountKg ?? 0;
            m["o2_reserve_kg_final"] = e.Stores.Get("o2_reserve")?.AmountKg ?? 0;
            m["spares_kg_final"] = e.Stores.Get("spares")?.AmountKg ?? 0;
            m["ch4_t"] = (e.Stores.Get("depot_ch4")?.AmountKg ?? 0) / 1000.0;
            m["lox_t"] = (e.Stores.Get("depot_lox")?.AmountKg ?? 0) / 1000.0;

            m["mean_unmet_power_kw"] = SeriesMean(e, "power.unmet");
            m["mean_power_used_kw"] = SeriesMean(e, "power.used");
            m["mean_unmet_labor_h"] = SeriesMean(e, "labor.unmet");
            m["min_ppo2_kpa"] = SeriesMin(e, "hab.ppo2");
            m["max_ppco2_kpa"] = SeriesMax(e, "hab.ppco2");
            m["min_food_days"] = SeriesMin(e, "crew.food_days", skipZeroPrefix: true);
            m["repairs_stuck_final"] = e.History.Get("maint.awaiting_spares")?.Latest ?? 0;

            // Sol when return propellant hit 100% (NaN -> -1 if never).
            var rp = e.History.Get("fleet.return_prop");
            m["sol_return_prop_full"] = -1;
            if (rp != null)
                for (int i = 0; i < rp.Count; i++)
                    if (rp[i] >= 99.999) { m["sol_return_prop_full"] = rp.SolAt(i); break; }

            return m;
        }

        private static double SeriesMean(SimulationEngine e, string id)
        {
            var s = e.History.Get(id);
            if (s == null || s.Count == 0) return 0;
            double sum = 0;
            for (int i = 0; i < s.Count; i++) sum += s[i];
            return sum / s.Count;
        }

        private static double SeriesMin(SimulationEngine e, string id, bool skipZeroPrefix = false)
        {
            var s = e.History.Get(id);
            if (s == null || s.Count == 0) return 0;
            double min = double.MaxValue;
            bool seenNonZero = false;
            for (int i = 0; i < s.Count; i++)
            {
                if (skipZeroPrefix && !seenNonZero)
                {
                    if (s[i] == 0) continue;
                    seenNonZero = true;
                }
                if (s[i] < min) min = s[i];
            }
            return min == double.MaxValue ? 0 : min;
        }

        private static double SeriesMax(SimulationEngine e, string id)
        {
            var s = e.History.Get(id);
            if (s == null || s.Count == 0) return 0;
            double max = double.MinValue;
            for (int i = 0; i < s.Count; i++) if (s[i] > max) max = s[i];
            return max == double.MinValue ? 0 : max;
        }
    }

    /// <summary>
    /// Full-factorial sweep x Monte Carlo seeds, one complete engine run per point, tidy CSV
    /// out. Pure C#: runs headless in -batchmode, in EditMode tests, or from the in-app UI.
    /// </summary>
    public static class TradeStudyRunner
    {
        public static string RunToCsv(StudyConfig config, ParameterRegistry parameters,
            Action<string> progress = null)
        {
            var rows = new List<Dictionary<string, double>>();
            var axisIndices = new int[config.Axes.Count];
            int totalPoints = config.Axes.Aggregate(1, (a, ax) => a * ax.Values.Length);
            int point = 0;

            do
            {
                for (int seed = 0; seed < config.MonteCarloSeeds; seed++)
                {
                    var scenario = config.Scenario;
                    var savedSeed = scenario.Seed;
                    scenario.Seed = savedSeed + (ulong)seed * 7919;

                    var engine = Scenario.SimulationBuilder.Build(scenario, parameters);

                    // Apply axis values as user overrides for this run.
                    for (int a = 0; a < config.Axes.Count; a++)
                    {
                        var ax = config.Axes[a];
                        if (parameters.Find(ax.ParamId) == null)
                            parameters.GetOrRegister(ax.ParamId, ax.ParamId, ax.Values[axisIndices[a]], "", "study axis");
                        parameters.SetUserOverride(ax.ParamId, ax.Values[axisIndices[a]]);
                    }

                    double duration = config.DurationSolsOverride ?? scenario.DurationSols;
                    engine.RunToSol(duration);

                    var row = Metrics.Extract(engine);
                    for (int a = 0; a < config.Axes.Count; a++)
                        row["axis:" + config.Axes[a].ParamId] = config.Axes[a].Values[axisIndices[a]];
                    row["seed"] = seed;
                    rows.Add(row);

                    // Clean up overrides so the next run starts fresh.
                    for (int a = 0; a < config.Axes.Count; a++)
                        parameters.SetUserOverride(config.Axes[a].ParamId, null);
                    scenario.Seed = savedSeed;
                }
                point++;
                progress?.Invoke($"{config.Name}: {point}/{totalPoints} points done");
            }
            while (NextIndex(axisIndices, config.Axes));

            return ToCsv(rows);
        }

        private static bool NextIndex(int[] idx, List<SweepAxis> axes)
        {
            for (int a = 0; a < idx.Length; a++)
            {
                idx[a]++;
                if (idx[a] < axes[a].Values.Length) return true;
                idx[a] = 0;
            }
            return false;
        }

        private static string ToCsv(List<Dictionary<string, double>> rows)
        {
            if (rows.Count == 0) return "";
            var cols = rows.SelectMany(r => r.Keys).Distinct()
                .OrderBy(c => c.StartsWith("axis:") ? 0 : c == "seed" ? 1 : 2)
                .ThenBy(c => c).ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", cols));
            foreach (var r in rows)
                sb.AppendLine(string.Join(",", cols.Select(c =>
                    r.TryGetValue(c, out double v) ? v.ToString("G9", CultureInfo.InvariantCulture) : "")));
            return sb.ToString();
        }
    }
}
