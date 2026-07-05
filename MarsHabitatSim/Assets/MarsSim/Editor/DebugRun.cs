using System.IO;
using System.Text;
using MarsSim.Core;
using MarsSim.Core.Params;
using MarsSim.Core.Scenario;
using UnityEditor;
using UnityEngine;

namespace MarsSim.EditorTools
{
    /// <summary>
    /// Diagnostic mission runner: executes a scenario headless and dumps the full event log
    /// plus per-100-sol vitals to studies/out/debug_&lt;scenario&gt;.txt. Menu or:
    ///   -executeMethod MarsSim.EditorTools.DebugRun.RunHeadless -scenario baseline.json -sols 1000
    /// </summary>
    public static class DebugRun
    {
        [MenuItem("MarsSim/Debug/Dump baseline 1000 sols")]
        public static void Baseline() => Run("baseline.json", 1000);

        public static void RunHeadless()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-scenario");
            string scenario = i >= 0 && i + 1 < args.Length ? args[i + 1] : "baseline.json";
            i = System.Array.IndexOf(args, "-sols");
            double sols = i >= 0 && i + 1 < args.Length ? double.Parse(args[i + 1]) : 1000;
            Run(scenario, sols);
            EditorApplication.Exit(0);
        }

        public static string Run(string scenarioFile, double sols)
        {
            var reg = new ParameterRegistry();
            string db = Path.Combine(Application.streamingAssetsPath, "parameters_master.json");
            if (File.Exists(db)) reg.LoadDatabaseJson(File.ReadAllText(db), out _);

            var scenario = Scenario.FromJson(File.ReadAllText(
                Path.Combine(Application.streamingAssetsPath, "scenarios", scenarioFile)));
            var engine = SimulationBuilder.Build(scenario, reg);

            var vitals = new StringBuilder();
            vitals.AppendLine("sol,ppo2_kpa,ppco2_kpa,water_kg,food_kg,o2res_kg,power_off_kw,power_unmet_kw,batt_soc,crew,health,ch4_t,lox_t,mine_kgsol,isru_kgsol,tau,dust,labor_unmet");
            double nextSample = 0;
            while (engine.Clock.Sol < sols)
            {
                engine.Step();
                if (engine.Clock.Sol >= nextSample)
                {
                    nextSample += 25;
                    var h = engine.History;
                    string S(string id, string fmt = "F1") => (h.Get(id)?.Latest ?? 0).ToString(fmt);
                    vitals.AppendLine($"{engine.Clock.Sol:F0},{S("hab.ppo2","F2")},{S("hab.ppco2","F3")},{S("store.water_potable","F0")},{S("store.food","F0")},{S("store.o2_reserve","F0")}," +
                        $"{S("power.offered","F0")},{S("power.unmet","F0")},{S("power.battery_soc","F0")},{S("crew.count","F0")},{S("crew.health","F2")}," +
                        $"{S("depot.ch4","F1")},{S("depot.lox","F1")},{S("icemine.production","F0")},{S("isru.production","F0")},{S("env.tau","F2")},{S("solar.dust","F1")},{S("labor.unmet","F1")}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== DEBUG RUN {scenarioFile} to sol {sols} ===\n");
            sb.AppendLine("--- VITALS (every 25 sols) ---");
            sb.Append(vitals);
            sb.AppendLine("\n--- EVENTS ---");
            foreach (var e in engine.Events.Events)
                sb.AppendLine($"[{e.Severity,-9}] sol {e.Sol,7:F1}  {e.Source}: {e.Message}");

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "studies", "out"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"debug_{Path.GetFileNameWithoutExtension(scenarioFile)}.txt");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"Debug dump written to {path}");
            return path;
        }
    }
}
