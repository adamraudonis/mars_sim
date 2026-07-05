using System;
using System.Collections.Generic;
using System.IO;
using MarsSim.Core;
using MarsSim.Core.Params;
using MarsSim.Core.Scenario;
using UnityEngine;

namespace MarsSim.UnityApp
{
    /// <summary>
    /// Owns the simulation: loads the sourced parameter database and a scenario, builds the
    /// engine, and advances it in timelapse (decoupled from render rate). Everything visual
    /// reads from <see cref="Engine"/>; nothing visual writes to it except through the
    /// parameter registry and scenario reloads.
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        public static SimRunner Instance { get; private set; }

        public SimulationEngine Engine { get; private set; }
        public ParameterRegistry Params { get; private set; }
        public Scenario CurrentScenario { get; private set; }
        public IReadOnlyList<(string file, string name)> AvailableScenarios => _available;

        /// <summary>Timelapse speed in sols per real second (0 = paused).</summary>
        public double SolsPerSecond { get; set; } = 0.5;
        public bool Paused { get; set; }

        public static readonly double[] SpeedPresets = { 0.02, 0.1, 0.5, 2, 10, 50 };

        /// <summary>Raised after the engine is (re)built — views rebuild themselves.</summary>
        public event Action EngineRebuilt;

        private readonly List<(string file, string name)> _available = new();
        private double _stepDebt;
        private string _dbWarnings = "";

        private void Awake()
        {
            Instance = this;
            Params = new ParameterRegistry();
            LoadParameterDatabase();
            DiscoverScenarios();
            var first = _available.Count > 0 ? _available[0].file : null;
            LoadScenario(first);
        }

        private string StreamingPath(string rel) => Path.Combine(Application.streamingAssetsPath, rel);

        private void LoadParameterDatabase()
        {
            string path = StreamingPath("parameters_master.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("parameters_master.json not found — running on code-default parameters (still sourced, see GetOrRegister calls).");
                return;
            }
            try
            {
                int n = Params.LoadDatabaseJson(File.ReadAllText(path), out var warnings);
                _dbWarnings = string.Join("\n", warnings);
                Debug.Log($"Loaded {n} sourced parameters from research database.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load parameter database: {e.Message}");
            }
        }

        private void DiscoverScenarios()
        {
            _available.Clear();
            string dir = StreamingPath("scenarios");
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var s = Scenario.FromJson(File.ReadAllText(f));
                    _available.Add((f, s.Name));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Bad scenario {Path.GetFileName(f)}: {e.Message}");
                }
            }
            _available.Sort((a, b) => string.CompareOrdinal(a.file, b.file));
        }

        public void LoadScenario(string file)
        {
            if (file == null)
            {
                CurrentScenario = new Scenario();       // empty fallback
            }
            else
            {
                CurrentScenario = Scenario.FromJson(File.ReadAllText(file));
            }
            RebuildEngine();
        }

        public void RebuildEngine()
        {
            Engine = SimulationBuilder.Build(CurrentScenario, Params);
            _stepDebt = 0;
            EngineRebuilt?.Invoke();
        }

        private void Update()
        {
            if (Engine == null || Paused || SolsPerSecond <= 0) return;

            _stepDebt += Time.deltaTime * SolsPerSecond / Engine.Clock.DtSols;
            int steps = (int)_stepDebt;
            if (steps <= 0) return;
            // Cap per frame so the app never hitches more than ~30 ms.
            steps = Mathf.Min(steps, 2000);
            _stepDebt -= steps;

            var t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < steps; i++)
            {
                Engine.Step();
                if (Time.realtimeSinceStartup - t0 > 0.03) { _stepDebt = 0; break; }
            }
        }
    }
}
