using System.Collections.Generic;
using System.Linq;
using MarsSim.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>
    /// Live status board: one card per module with a health dot, status line, key telemetry
    /// as label/value rows, and an L0/L1/L2 segmented fidelity switch. Fully generic —
    /// modules self-describe, so new subsystems appear automatically.
    /// </summary>
    public sealed class SystemsPanelView : VisualElement
    {
        private sealed class Row
        {
            public VisualElement Dot;
            public Label Status;
            public List<(string label, Label value, Label unit)> Figures = new();
            public VisualElement FiguresBox;
            public SimModule Module;
        }

        private readonly Dictionary<string, Row> _rows = new();
        private readonly ScrollView _scroll;

        public SystemsPanelView()
        {
            style.flexGrow = 1;
            Add(UiTheme.SectionHeader("Systems"));
            _scroll = new ScrollView { style = { flexGrow = 1 } };
            _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(_scroll);
        }

        public void Rebuild()
        {
            _scroll.Clear();
            _rows.Clear();
            var engine = SimRunner.Instance?.Engine;
            if (engine == null) return;

            foreach (var m in engine.Modules)
            {
                var module = m;
                var card = UiTheme.Card();

                var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var dot = UiTheme.Dot(UiTheme.Good);
                head.Add(dot);

                var name = new Label(module.DisplayName);
                name.style.color = UiTheme.TextHi;
                name.style.fontSize = 12;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.flexGrow = 1;
                head.Add(name);

                if (module.MaxFidelity > FidelityLevel.L0_Distilled)
                {
                    int levels = (int)module.MaxFidelity + 1;
                    var options = new string[levels];
                    for (int f = 0; f < levels; f++) options[f] = $"L{f}";
                    var (seg, setIdx) = UiTheme.Segmented(options, idx =>
                    {
                        module.Fidelity = (FidelityLevel)idx;
                    }, 9);
                    // Keep the segment in sync when fidelity changes elsewhere.
                    seg.schedule.Execute(() => setIdx((int)module.EffectiveFidelity)).Every(300);
                    setIdx((int)module.EffectiveFidelity);
                    head.Add(seg);
                }
                card.Add(head);

                var status = UiTheme.Caption("");
                status.style.marginLeft = 15;
                status.style.marginTop = 1;
                card.Add(status);

                var figures = new VisualElement { style = { marginLeft = 15, marginTop = 4 } };
                card.Add(figures);

                _scroll.Add(card);
                _rows[module.Id] = new Row { Dot = dot, Status = status, FiguresBox = figures, Module = module };
                BuildFigureRows(_rows[module.Id]);
            }
        }

        private static void BuildFigureRows(Row row)
        {
            row.FiguresBox.Clear();
            row.Figures.Clear();
            foreach (var (label, _, unit) in row.Module.KeyFigures)
            {
                var (kv, value) = UiTheme.KeyValue(label, unit);
                row.Figures.Add((label, value, null));
                row.FiguresBox.Add(kv);
            }
        }

        public void Refresh()
        {
            var engine = SimRunner.Instance?.Engine;
            if (engine == null) return;
            if (_rows.Count != engine.Modules.Count) Rebuild();

            foreach (var m in engine.Modules)
            {
                if (!_rows.TryGetValue(m.Id, out var row)) continue;
                row.Dot.style.backgroundColor = m.Health switch
                {
                    ModuleHealth.Nominal => UiTheme.Good,
                    ModuleHealth.Degraded => UiTheme.Warn,
                    _ => UiTheme.Bad,
                };
                row.Status.text = m.StatusLine;

                var figs = m.KeyFigures.ToList();
                if (figs.Count != row.Figures.Count)
                {
                    BuildFigureRows(row);
                    figs = m.KeyFigures.ToList();
                }
                for (int i = 0; i < figs.Count && i < row.Figures.Count; i++)
                    row.Figures[i].value.text = UiTheme.Compact(figs[i].value);
            }
        }
    }
}
