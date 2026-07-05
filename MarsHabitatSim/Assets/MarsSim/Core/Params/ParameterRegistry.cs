using System;
using System.Collections.Generic;
using MarsSim.Core.Json;

namespace MarsSim.Core.Params
{
    public enum Confidence { High, Medium, Low, Speculative }

    /// <summary>
    /// A single tunable quantity with provenance. The simulation NEVER hard-codes numbers;
    /// modules resolve parameters by id so that every value is user-tunable and carries its
    /// citation into the UI.
    /// </summary>
    public sealed class Param
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Unit { get; internal set; }
        public string Domain { get; internal set; }

        /// <summary>Baseline (sourced) value from the research database.</summary>
        public double BaseValue { get; internal set; }

        public double? RangeMin { get; internal set; }
        public double? RangeMax { get; internal set; }
        public string Source { get; internal set; }
        public string SourceUrl { get; internal set; }
        public Confidence Confidence { get; internal set; }
        public string Notes { get; internal set; }

        /// <summary>True while only a scenario override exists and no module/database has defined this id yet.</summary>
        public bool IsPlaceholder { get; internal set; }

        // Override layers (null = not overridden): scenario < distilled < user.
        public double? ScenarioOverride { get; internal set; }
        public double? DistilledOverride { get; internal set; }
        public double? UserOverride { get; internal set; }

        public double Value => UserOverride ?? DistilledOverride ?? ScenarioOverride ?? BaseValue;
        public bool IsOverridden => UserOverride.HasValue || DistilledOverride.HasValue || ScenarioOverride.HasValue;

        public event Action<Param> Changed;
        internal void RaiseChanged() => Changed?.Invoke(this);

        public override string ToString() => $"{Id} = {Value} {Unit} [{Source}]";
    }

    /// <summary>
    /// Loads the sourced parameter database (research/parameters_master.json) and manages
    /// override layers: base (sourced) -> scenario -> distilled (fit from L2 runs) -> user (UI).
    /// Missing parameters can be registered by code with an explicit fallback + source so the
    /// sim still runs before/without the research DB, but always with provenance.
    /// </summary>
    public sealed class ParameterRegistry
    {
        private readonly Dictionary<string, Param> _params = new();
        private readonly List<Param> _ordered = new();

        public IReadOnlyList<Param> All => _ordered;
        public event Action<Param> AnyChanged;

        public bool Has(string id) => _params.ContainsKey(id);

        public Param Find(string id) => _params.TryGetValue(id, out var p) ? p : null;

        /// <summary>Resolve a parameter; if absent, register the code fallback (with provenance).</summary>
        public Param GetOrRegister(string id, string name, double fallbackValue, string unit,
            string source, Confidence confidence = Confidence.Medium, string notes = null)
        {
            if (_params.TryGetValue(id, out var p))
            {
                // Upgrade a placeholder created by an early scenario override.
                if (p.IsPlaceholder)
                {
                    p.Name = name;
                    p.Unit = unit;
                    p.BaseValue = fallbackValue;
                    p.Source = source;
                    p.Confidence = confidence;
                    p.Notes = notes;
                    p.IsPlaceholder = false;
                }
                return p;
            }
            p = new Param
            {
                Id = id,
                Name = name,
                Unit = unit,
                Domain = id.Contains('.') ? id.Substring(0, id.IndexOf('.')) : "misc",
                BaseValue = fallbackValue,
                Source = source,
                Confidence = confidence,
                Notes = notes,
            };
            _params[id] = p;
            _ordered.Add(p);
            return p;
        }

        /// <summary>Shorthand used by modules: value of a parameter (must already exist or have fallback registered).</summary>
        public double V(string id)
        {
            if (_params.TryGetValue(id, out var p)) return p.Value;
            throw new KeyNotFoundException($"parameter '{id}' not registered");
        }

        public void SetUserOverride(string id, double? value)
        {
            var p = Find(id);
            if (p == null) throw new KeyNotFoundException(id);
            p.UserOverride = value;
            p.RaiseChanged();
            AnyChanged?.Invoke(p);
        }

        public void SetScenarioOverride(string id, double value)
        {
            var p = Find(id);
            if (p == null)
            {
                // Modules register lazily in Init; hold the override in a placeholder so it is
                // never silently dropped. GetOrRegister upgrades it with real metadata later.
                p = new Param
                {
                    Id = id,
                    Name = id,
                    Unit = "",
                    Domain = id.Contains('.') ? id.Substring(0, id.IndexOf('.')) : "misc",
                    BaseValue = value,
                    Source = "(scenario override)",
                    Confidence = Confidence.Medium,
                    IsPlaceholder = true,
                };
                _params[id] = p;
                _ordered.Add(p);
            }
            p.ScenarioOverride = value;
            p.RaiseChanged();
            AnyChanged?.Invoke(p);
        }

        public void SetDistilledOverride(string id, double? value)
        {
            var p = Find(id);
            if (p == null) throw new KeyNotFoundException(id);
            p.DistilledOverride = value;
            p.RaiseChanged();
            AnyChanged?.Invoke(p);
        }

        public void ClearScenarioOverrides()
        {
            foreach (var p in _ordered)
                if (p.ScenarioOverride.HasValue) { p.ScenarioOverride = null; p.RaiseChanged(); }
        }

        // ---------------- Database loading ----------------

        /// <summary>
        /// Load research/parameters_master.json (shape documented in ARCHITECTURE.md).
        /// Values loaded here become the sourced baseline; code fallbacks registered earlier
        /// are upgraded in place (id keeps object identity so module bindings survive).
        /// </summary>
        public int LoadDatabaseJson(string jsonText, out List<string> warnings)
        {
            warnings = new List<string>();
            if (!JsonValue.TryParse(jsonText, out var root, out string err))
                throw new FormatException($"parameters_master.json: {err}");

            int loaded = 0;
            var domains = root["domains"];
            foreach (var (domainKey, domain) in domains)
            {
                foreach (var pj in domain["parameters"].Items)
                {
                    string id = pj["id"].AsString();
                    if (string.IsNullOrEmpty(id)) { warnings.Add("parameter with no id skipped"); continue; }

                    if (!_params.TryGetValue(id, out var p))
                    {
                        p = new Param { Id = id };
                        _params[id] = p;
                        _ordered.Add(p);
                    }
                    p.Domain = domainKey;
                    p.IsPlaceholder = false;
                    p.Name = pj["name"].AsString(id);
                    p.BaseValue = pj["value"].AsDouble();
                    p.Unit = pj["unit"].AsString("");
                    p.RangeMin = pj["range_min"].IsNull ? null : pj["range_min"].AsDouble();
                    p.RangeMax = pj["range_max"].IsNull ? null : pj["range_max"].AsDouble();
                    p.Source = pj["source"].AsString("(unsourced)");
                    p.SourceUrl = pj["source_url"].AsString();
                    p.Notes = pj["notes"].AsString();
                    p.Confidence = pj["confidence"].AsString("medium") switch
                    {
                        "high" => Confidence.High,
                        "low" => Confidence.Low,
                        "speculative" => Confidence.Speculative,
                        _ => Confidence.Medium,
                    };
                    p.RaiseChanged();
                    loaded++;
                }
            }
            return loaded;
        }

        /// <summary>Export current values (with provenance and override state) for run reproducibility.</summary>
        public JsonValue ExportSnapshot()
        {
            var arr = JsonValue.NewArray();
            foreach (var p in _ordered)
            {
                var o = JsonValue.NewObject();
                o["id"] = JsonValue.Of(p.Id);
                o["value"] = JsonValue.Of(p.Value);
                o["base_value"] = JsonValue.Of(p.BaseValue);
                o["unit"] = JsonValue.Of(p.Unit);
                o["source"] = JsonValue.Of(p.Source);
                if (p.IsOverridden) o["overridden"] = JsonValue.Of(true);
                arr.Add(o);
            }
            return arr;
        }
    }
}
