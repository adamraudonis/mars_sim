using System.Collections.Generic;
using MarsSim.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>
    /// Multi-series telemetry chart (Painter2D, x = mission sol): translucent area fills
    /// under each line, sol-gridlines at round intervals, inset min/max labels, and a legend
    /// of live-value chips. Refreshed by the HUD a few times per second.
    /// </summary>
    public sealed class ChartView : VisualElement
    {
        public struct SeriesSpec
        {
            public string Id;
            public Color Color;
            public string LabelOverride;
        }

        private sealed class LegendEntry
        {
            public Label Name;
            public Label Value;
        }

        private readonly List<SeriesSpec> _series = new();
        private readonly List<LegendEntry> _legendEntries = new();
        private readonly Label _title, _unitLabel, _yMax, _yMin, _xMin, _xMax, _empty;
        private readonly VisualElement _legend, _plot;

        private const float PadL = 8, PadR = 8, PadT = 8, PadB = 8;

        public ChartView(string title)
        {
            style.flexShrink = 0;
            style.marginBottom = 14;

            var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 3 } };
            _title = new Label(title);
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.style.fontSize = 12;
            _title.style.color = UiTheme.TextHi;
            _title.style.flexGrow = 1;
            titleRow.Add(_title);
            _unitLabel = UiTheme.Caption("", UiTheme.TextFaint);
            _unitLabel.style.fontSize = 9;
            titleRow.Add(_unitLabel);
            Add(titleRow);

            _legend = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };
            Add(_legend);

            _plot = new VisualElement
            {
                style =
                {
                    height = 118,
                    backgroundColor = new Color(0.024f, 0.031f, 0.047f, 0.95f),
                },
            };
            UiTheme.Rounded(_plot, 7);
            UiTheme.Border(_plot, new Color(1, 1, 1, 0.055f));
            _plot.generateVisualContent += DrawPlot;
            Add(_plot);

            _yMax = InsetLabel(TextAnchor.UpperLeft);
            _yMin = InsetLabel(TextAnchor.LowerLeft);
            _xMax = InsetLabel(TextAnchor.LowerRight);
            _xMin = null; // reserved
            _plot.Add(_yMax);
            _plot.Add(_yMin);
            _plot.Add(_xMax);

            _empty = new Label("awaiting telemetry");
            _empty.style.position = Position.Absolute;
            _empty.style.left = 0; _empty.style.right = 0; _empty.style.top = 0; _empty.style.bottom = 0;
            _empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            _empty.style.color = UiTheme.TextFaint;
            _empty.style.fontSize = 10;
            _empty.style.letterSpacing = 2;
            _plot.Add(_empty);
        }

        private static Label InsetLabel(TextAnchor anchor)
        {
            var l = UiTheme.MonoLabel("", 9, UiTheme.TextFaint);
            l.style.position = Position.Absolute;
            switch (anchor)
            {
                case TextAnchor.UpperLeft: l.style.top = 4; l.style.left = 7; break;
                case TextAnchor.LowerLeft: l.style.bottom = 3; l.style.left = 7; break;
                case TextAnchor.LowerRight: l.style.bottom = 3; l.style.right = 7; break;
            }
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        public void SetSeries(params SeriesSpec[] specs)
        {
            _series.Clear();
            _series.AddRange(specs);
            RebuildLegend();
        }

        private void RebuildLegend()
        {
            _legend.Clear();
            _legendEntries.Clear();
            var engine = SimRunner.Instance?.Engine;
            foreach (var s in _series)
            {
                var ts = engine?.History.Get(s.Id);
                var chip = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 10, marginBottom = 2 },
                };
                chip.Add(UiTheme.Dot(s.Color, 6));
                var name = new Label(s.LabelOverride ?? ts?.DisplayName ?? s.Id);
                name.style.color = UiTheme.TextDim;
                name.style.fontSize = 10;
                name.style.marginRight = 4;
                chip.Add(name);
                var value = UiTheme.MonoLabel("", 10, s.Color);
                chip.Add(value);
                _legend.Add(chip);
                _legendEntries.Add(new LegendEntry { Name = name, Value = value });
            }
        }

        public void Refresh()
        {
            var engine = SimRunner.Instance?.Engine;
            if (_legendEntries.Count != _series.Count) RebuildLegend();

            bool any = false;
            string unit = "";
            for (int i = 0; i < _series.Count; i++)
            {
                var ts = engine?.History.Get(_series[i].Id);
                if (ts != null && ts.Count > 1)
                {
                    any = true;
                    _legendEntries[i].Value.text = UiTheme.Compact(ts.Latest);
                    if (unit == "" && !string.IsNullOrEmpty(ts.Unit)) unit = ts.Unit;
                    if (_legendEntries[i].Name.text != (_series[i].LabelOverride ?? ts.DisplayName))
                        _legendEntries[i].Name.text = _series[i].LabelOverride ?? ts.DisplayName;
                }
                else
                {
                    _legendEntries[i].Value.text = "";
                }
            }

            _unitLabel.text = unit;
            _empty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;

            if (any)
            {
                var (mn, mx, _) = ComputeRange();
                _yMax.text = UiTheme.Compact(mx);
                _yMin.text = UiTheme.Compact(mn);
                _xMax.text = $"SOL {engine.Clock.Sol:0}";
            }
            else
            {
                _yMax.text = _yMin.text = _xMax.text = "";
            }

            _plot.MarkDirtyRepaint();
        }

        private (float min, float max, int count) ComputeRange()
        {
            var engine = SimRunner.Instance?.Engine;
            float mn = float.MaxValue, mx = float.MinValue;
            int count = 0;
            if (engine != null)
            {
                foreach (var s in _series)
                {
                    var ts = engine.History.Get(s.Id);
                    if (ts == null || ts.Count == 0) continue;
                    count = Mathf.Max(count, ts.Count);
                    var (a, b) = ts.Range(0, ts.Count);
                    mn = Mathf.Min(mn, a);
                    mx = Mathf.Max(mx, b);
                }
            }
            if (mn > mx) { mn = 0; mx = 1; }
            if (mx - mn < 1e-9f)
            {
                float pad0 = Mathf.Max(1e-6f, Mathf.Abs(mx) * 0.1f + 0.5f);
                mn -= pad0; mx += pad0;
            }
            float pad = (mx - mn) * 0.07f;
            return (mn - pad, mx + pad, count);
        }

        private static float NiceStep(float span)
        {
            float raw = span / 4f;
            float mag = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(Mathf.Max(1e-6f, raw))));
            float norm = raw / mag;
            float step = norm < 1.5f ? 1 : norm < 3.5f ? 2 : norm < 7.5f ? 5 : 10;
            return step * mag;
        }

        private void DrawPlot(MeshGenerationContext ctx)
        {
            var engine = SimRunner.Instance?.Engine;
            if (engine == null) return;
            var rect = _plot.contentRect;
            float w = rect.width - PadL - PadR;
            float h = rect.height - PadT - PadB;
            if (w < 20 || h < 20) return;

            var (mn, mx, maxCount) = ComputeRange();
            if (maxCount < 2) return;

            var p = ctx.painter2D;
            double lastSol = engine.Clock.Sol;

            // --- Grid: horizontal quarters + vertical sol lines at a round step ---
            p.strokeColor = new Color(1, 1, 1, 0.045f);
            p.lineWidth = 1;
            for (int g = 1; g < 4; g++)
            {
                float y = PadT + h * g / 4f;
                p.BeginPath();
                p.MoveTo(new Vector2(PadL, y));
                p.LineTo(new Vector2(PadL + w, y));
                p.Stroke();
            }
            if (lastSol > 1)
            {
                float step = NiceStep((float)lastSol);
                for (float sol = step; sol < lastSol; sol += step)
                {
                    float x = PadL + (float)(sol / lastSol) * w;
                    p.BeginPath();
                    p.MoveTo(new Vector2(x, PadT));
                    p.LineTo(new Vector2(x, PadT + h));
                    p.Stroke();
                }
            }

            // --- Series: fill then stroke, first series on top ---
            for (int si = _series.Count - 1; si >= 0; si--)
            {
                var s = _series[si];
                var ts = engine.History.Get(s.Id);
                if (ts == null || ts.Count < 2) continue;

                int stride = Mathf.Max(1, ts.Count / Mathf.Max(64, (int)w));
                var pts = new List<Vector2>(ts.Count / stride + 2);
                for (int i = 0; i < ts.Count; i += stride)
                {
                    // Downsample preserving extremes within each bucket.
                    float v = ts[i];
                    int end = Mathf.Min(ts.Count, i + stride);
                    for (int k = i + 1; k < end; k++)
                        if (Mathf.Abs(ts[k] - mn) > Mathf.Abs(v - mn)) v = ts[k];

                    float x = PadL + (float)i / (maxCount - 1) * w;
                    float y = PadT + h - (v - mn) / (mx - mn) * h;
                    pts.Add(new Vector2(x, Mathf.Clamp(y, PadT, PadT + h)));
                }
                if (pts.Count < 2) continue;

                // Area fill.
                p.fillColor = new Color(s.Color.r, s.Color.g, s.Color.b, 0.10f);
                p.BeginPath();
                p.MoveTo(new Vector2(pts[0].x, PadT + h));
                foreach (var pt in pts) p.LineTo(pt);
                p.LineTo(new Vector2(pts[^1].x, PadT + h));
                p.ClosePath();
                p.Fill();

                // Line.
                p.strokeColor = s.Color;
                p.lineWidth = 1.7f;
                p.lineJoin = LineJoin.Round;
                p.BeginPath();
                p.MoveTo(pts[0]);
                for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]);
                p.Stroke();

                // Endpoint marker on the primary series.
                if (si == 0)
                {
                    p.fillColor = s.Color;
                    p.BeginPath();
                    p.Arc(pts[^1], 2.4f, 0, 360);
                    p.Fill();
                }
            }
        }
    }
}
