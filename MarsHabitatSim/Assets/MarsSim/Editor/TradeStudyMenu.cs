using System;
using System.IO;
using System.Linq;
using MarsSim.Core.Params;
using MarsSim.Core.Scenario;
using MarsSim.Core.Study;
using UnityEditor;
using UnityEngine;

namespace MarsSim.EditorTools
{
    /// <summary>
    /// Canned trade studies runnable from the menu or headless:
    ///   Unity -batchmode -nographics -quit -projectPath &lt;proj&gt;
    ///         -executeMethod MarsSim.EditorTools.TradeStudyMenu.RunHeadless -study &lt;name&gt;
    /// CSVs land in &lt;repo&gt;/studies/out/. Each row = one full mission run.
    /// </summary>
    public static class TradeStudyMenu
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string OutDir
        {
            get
            {
                string d = Path.Combine(RepoRoot, "studies", "out");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        private static ParameterRegistry LoadParams()
        {
            var reg = new ParameterRegistry();
            string dbPath = Path.Combine(Application.streamingAssetsPath, "parameters_master.json");
            if (File.Exists(dbPath))
                reg.LoadDatabaseJson(File.ReadAllText(dbPath), out _);
            return reg;
        }

        private static Scenario LoadScenario(string file)
            => Scenario.FromJson(File.ReadAllText(
                Path.Combine(Application.streamingAssetsPath, "scenarios", file)));

        // ---------------- Study definitions ----------------

        public static string RunStudy(string name)
        {
            StudyConfig cfg = name switch
            {
                // Does the solar baseline or the fission variant get the ship fueled sooner,
                // and how much does a bad storm year hurt? 2 architectures x 5 weather seeds.
                "solar_vs_nuclear" => new StudyConfig
                {
                    Name = name,
                    Scenario = LoadScenario("baseline.json"),
                    Axes =
                    {
                        // 0 = baseline solar manifest, 1 = nuclear manifest (swapped below via scenario)
                    },
                    MonteCarloSeeds = 5,
                },

                // Sweep installed solar area: how much array (mass) is actually needed?
                "solar_sizing" => new StudyConfig
                {
                    Name = name,
                    Scenario = LoadScenario("baseline.json"),
                    Axes = { SweepAxis.List("power_solar.l0_kwh_per_sol_per_kw", 2.0, 2.4, 2.8, 3.2) },
                    MonteCarloSeeds = 3,
                },

                // Spares mass vs failure-rate uncertainty: vary the k-factor.
                "spares_vs_kfactor" => new StudyConfig
                {
                    Name = name,
                    Scenario = LoadScenario("baseline.json"),
                    Axes = { SweepAxis.List("reliability.k_factor", 1.0, 2.0, 3.0, 4.0) },
                    MonteCarloSeeds = 8,
                },

                // Food closure: greenhouse yield sensitivity (spans crop mix / lighting quality).
                "food_closure" => new StudyConfig
                {
                    Name = name,
                    Scenario = LoadScenario("baseline.json"),
                    Axes = { SweepAxis.List("food.kcal_per_m2_sol", 40, 80, 120, 160) },
                    MonteCarloSeeds = 3,
                },

                // Robot workforce size: what does the labor market do to deployment + ISRU?
                "robot_count" => new StudyConfig
                {
                    Name = name,
                    Scenario = LoadScenario("baseline.json"),
                    Axes = { SweepAxis.List("robots.availability", 0.0, 0.4, 0.8) },
                    MonteCarloSeeds = 3,
                },

                _ => throw new ArgumentException($"unknown study '{name}'. Known: solar_vs_nuclear, solar_sizing, spares_vs_kfactor, food_closure, robot_count"),
            };

            string csv;
            if (name == "solar_vs_nuclear")
            {
                // Special case: the axis is the scenario itself.
                var reg1 = LoadParams();
                var cfgSolar = new StudyConfig { Name = "solar", Scenario = LoadScenario("baseline.json"), MonteCarloSeeds = 5 };
                string csvSolar = TradeStudyRunner.RunToCsv(cfgSolar, reg1, Debug.Log);
                var reg2 = LoadParams();
                var cfgNuc = new StudyConfig { Name = "nuclear", Scenario = LoadScenario("nuclear.json"), MonteCarloSeeds = 5 };
                string csvNuc = TradeStudyRunner.RunToCsv(cfgNuc, reg2, Debug.Log);
                csv = "architecture,solar\n" + csvSolar + "\narchitecture,nuclear\n" + csvNuc;
            }
            else
            {
                csv = TradeStudyRunner.RunToCsv(cfg, LoadParams(), Debug.Log);
            }

            string path = Path.Combine(OutDir, $"{name}.csv");
            File.WriteAllText(path, csv);
            Debug.Log($"Study '{name}' written to {path}");
            return path;
        }

        [MenuItem("MarsSim/Studies/Solar vs Nuclear (10 runs)")]
        public static void StudySolarVsNuclear() => RunStudy("solar_vs_nuclear");

        [MenuItem("MarsSim/Studies/Solar sizing sweep")]
        public static void StudySolarSizing() => RunStudy("solar_sizing");

        [MenuItem("MarsSim/Studies/Spares vs failure uncertainty")]
        public static void StudySpares() => RunStudy("spares_vs_kfactor");

        [MenuItem("MarsSim/Studies/Food closure sweep")]
        public static void StudyFood() => RunStudy("food_closure");

        [MenuItem("MarsSim/Studies/Robot workforce sweep")]
        public static void StudyRobots() => RunStudy("robot_count");

        /// <summary>Headless entry point: -executeMethod ... RunHeadless -study &lt;name&gt;</summary>
        public static void RunHeadless()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-study");
            string study = i >= 0 && i + 1 < args.Length ? args[i + 1] : "solar_vs_nuclear";
            RunStudy(study);
            EditorApplication.Exit(0);
        }
    }
}
