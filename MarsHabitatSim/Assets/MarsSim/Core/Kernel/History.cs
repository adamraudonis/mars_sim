using System;
using System.Collections.Generic;

namespace MarsSim.Core
{
    /// <summary>
    /// One tracked series, sampled every engine step (uniform dt), so the time axis is
    /// implicit: sol(i) = startSol + i * dtSols. Charts downsample on draw.
    /// </summary>
    public sealed class TimeSeries
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Unit { get; }
        public double StartSol { get; internal set; }
        public double DtSols { get; internal set; }

        private float[] _values = new float[1024];
        public int Count { get; private set; }

        public TimeSeries(string id, string displayName, string unit)
        {
            Id = id;
            DisplayName = displayName;
            Unit = unit;
        }

        public void Append(double v)
        {
            if (Count == _values.Length) Array.Resize(ref _values, _values.Length * 2);
            _values[Count++] = (float)v;
        }

        public float this[int i] => _values[i];
        public double SolAt(int i) => StartSol + i * DtSols;
        public float Latest => Count > 0 ? _values[Count - 1] : 0f;

        /// <summary>Min/max over an index range (for chart auto-scale).</summary>
        public (float min, float max) Range(int from, int to)
        {
            float mn = float.MaxValue, mx = float.MinValue;
            for (int i = Math.Max(0, from); i < Math.Min(Count, to); i++)
            {
                if (_values[i] < mn) mn = _values[i];
                if (_values[i] > mx) mx = _values[i];
            }
            if (mn > mx) { mn = 0; mx = 1; }
            return (mn, mx);
        }

        /// <summary>Average of the last N samples (rough daily mean etc.).</summary>
        public double MeanOfLast(int n)
        {
            int from = Math.Max(0, Count - n);
            if (Count - from <= 0) return 0;
            double sum = 0;
            for (int i = from; i < Count; i++) sum += _values[i];
            return sum / (Count - from);
        }
    }

    /// <summary>
    /// All recorded series. Modules record named values each step via SimContext.Record();
    /// series are auto-created on first use and padded with zeros for steps before creation
    /// so every series stays aligned to the engine step index.
    /// </summary>
    public sealed class History
    {
        private readonly Dictionary<string, TimeSeries> _series = new();
        private readonly List<TimeSeries> _ordered = new();
        private double _dtSols;
        private long _step;

        public IReadOnlyList<TimeSeries> All => _ordered;

        internal void Configure(double dtSols) => _dtSols = dtSols;

        public TimeSeries Get(string id) => _series.TryGetValue(id, out var s) ? s : null;

        public TimeSeries GetOrCreate(string id, string displayName, string unit)
        {
            if (_series.TryGetValue(id, out var s)) return s;
            s = new TimeSeries(id, displayName, unit) { StartSol = 0, DtSols = _dtSols };
            // Pad so index i == engine step i for every series regardless of creation time.
            for (long i = 0; i < _step; i++) s.Append(0);
            _series[id] = s;
            _ordered.Add(s);
            return s;
        }

        private readonly Dictionary<string, double> _pending = new();
        private readonly Dictionary<string, (string name, string unit)> _pendingMeta = new();

        /// <summary>Record a value for this step (last write wins within a step).</summary>
        public void Record(string id, string displayName, string unit, double value)
        {
            _pending[id] = value;
            if (!_series.ContainsKey(id) && !_pendingMeta.ContainsKey(id))
                _pendingMeta[id] = (displayName, unit);
        }

        /// <summary>Accumulate into this step's value (for multiple contributors).</summary>
        public void Accumulate(string id, string displayName, string unit, double value)
        {
            _pending.TryGetValue(id, out double cur);
            _pending[id] = cur + value;
            if (!_series.ContainsKey(id) && !_pendingMeta.ContainsKey(id))
                _pendingMeta[id] = (displayName, unit);
        }

        /// <summary>Close the step: flush pending values; series not written this step repeat their last value.</summary>
        internal void CommitStep()
        {
            foreach (var kv in _pendingMeta)
                GetOrCreate(kv.Key, kv.Value.name, kv.Value.unit);
            _pendingMeta.Clear();

            foreach (var s in _ordered)
            {
                if (_pending.TryGetValue(s.Id, out double v)) s.Append(v);
                else s.Append(s.Latest);
            }
            _pending.Clear();
            _step++;
        }
    }
}
