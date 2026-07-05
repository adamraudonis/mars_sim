using System.Collections.Generic;
using MarsSim.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>Mission log: severity-barred entries with mono sol stamps.</summary>
    public sealed class EventsView : VisualElement
    {
        private readonly ScrollView _scroll;
        private readonly Queue<VisualElement> _rows = new();
        private const int MaxShown = 250;
        private SimulationEngine _bound;

        public EventsView()
        {
            style.flexGrow = 1;
            Add(UiTheme.SectionHeader("Mission log"));
            _scroll = new ScrollView { style = { flexGrow = 1 } };
            _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(_scroll);
        }

        public void Bind(SimulationEngine engine)
        {
            if (_bound == engine) return;
            _bound = engine;
            _scroll.Clear();
            _rows.Clear();
            foreach (var e in engine.Events.Events) Append(e);
            engine.Events.OnEvent += Append;
        }

        private void Append(SimEvent e)
        {
            var color = e.Severity switch
            {
                EventSeverity.Critical => UiTheme.Bad,
                EventSeverity.Warning => UiTheme.Warn,
                EventSeverity.Milestone => UiTheme.Good,
                _ => UiTheme.TextFaint,
            };

            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginBottom = 4, alignItems = Align.FlexStart },
            };
            var bar = new VisualElement
            {
                style =
                {
                    width = 2, alignSelf = Align.Stretch, backgroundColor = color,
                    marginRight = 7, marginTop = 2, marginBottom = 2, flexShrink = 0,
                },
            };
            UiTheme.Rounded(bar, 1);
            row.Add(bar);

            var sol = UiTheme.MonoLabel($"{e.Sol,6:F1}", 9, UiTheme.TextFaint);
            sol.style.marginTop = 1;
            sol.style.marginRight = 7;
            sol.style.flexShrink = 0;
            row.Add(sol);

            var msg = new Label($"{e.Source} — {e.Message}");
            msg.style.fontSize = 10;
            msg.style.whiteSpace = WhiteSpace.Normal;
            msg.style.color = e.Severity == EventSeverity.Info ? UiTheme.TextDim : UiTheme.Text;
            msg.style.flexShrink = 1;
            row.Add(msg);

            _scroll.Add(row);
            _rows.Enqueue(row);
            while (_rows.Count > MaxShown)
                _scroll.Remove(_rows.Dequeue());
            _scroll.schedule.Execute(() => _scroll.scrollOffset = new Vector2(0, float.MaxValue));
        }
    }
}
