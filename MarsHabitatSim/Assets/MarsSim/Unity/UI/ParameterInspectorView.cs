using System.Globalization;
using System.Linq;
using MarsSim.Core.Params;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>
    /// The tunable-parameter browser: search any sourced parameter, read its citation and
    /// confidence, edit the value live (creates an accent-colored override with a reset
    /// control). The front door for "what if".
    /// </summary>
    public sealed class ParameterInspectorView : VisualElement
    {
        private readonly TextField _search;
        private readonly ScrollView _list;
        private readonly Label _count;
        private string _filter = "";
        private const int MaxRows = 80;

        public ParameterInspectorView()
        {
            style.flexGrow = 1;
            Add(UiTheme.SectionHeader("Parameters"));
            var hint = UiTheme.Caption("Every value is sourced. Edit to override — the simulation applies it live.");
            hint.style.marginBottom = 6;
            Add(hint);

            _search = new TextField();
            _search.textEdition.placeholder = "search  ·  solar, o2, mtbf, kwh …";
            UiTheme.StyleTextField(_search, mono: false, fontSize: 12);
            _search.RegisterValueChangedCallback(evt => { _filter = evt.newValue?.ToLowerInvariant() ?? ""; Rebuild(); });
            Add(_search);

            _count = UiTheme.Caption("", UiTheme.TextFaint);
            _count.style.marginTop = 3;
            _count.style.marginBottom = 4;
            Add(_count);

            _list = new ScrollView { style = { flexGrow = 1 } };
            _list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(_list);
            Rebuild();
        }

        public void Rebuild()
        {
            _list.Clear();
            var reg = SimRunner.Instance?.Params;
            if (reg == null) return;

            var matches = reg.All.Where(Match).ToList();
            _count.text = $"{matches.Count} of {reg.All.Count} parameters"
                          + (matches.Count > MaxRows ? $"  ·  showing first {MaxRows}" : "");

            foreach (var p in matches.Take(MaxRows))
                _list.Add(BuildRow(p));
        }

        private bool Match(Param p)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            return (p.Id?.ToLowerInvariant().Contains(_filter) ?? false)
                   || (p.Name?.ToLowerInvariant().Contains(_filter) ?? false)
                   || (p.Domain?.ToLowerInvariant().Contains(_filter) ?? false);
        }

        private VisualElement BuildRow(Param p)
        {
            var row = new VisualElement { style = { paddingTop = 6, paddingBottom = 6 } };
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1, 1, 1, 0.05f);

            var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var name = new Label(p.Name) { tooltip = p.Id };
            name.style.color = UiTheme.Text;
            name.style.fontSize = 11;
            name.style.flexGrow = 1;
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.marginRight = 6;
            head.Add(name);

            bool overridden = p.UserOverride.HasValue || p.DistilledOverride.HasValue;

            if (overridden)
            {
                var reset = new Button(() =>
                {
                    SimRunner.Instance.Params.SetUserOverride(p.Id, null);
                    SimRunner.Instance.Params.SetDistilledOverride(p.Id, null);
                    Rebuild();
                }) { text = "↺", tooltip = $"Reset to sourced value {p.BaseValue:G6}" };
                reset.style.backgroundColor = Color.clear;
                UiTheme.Border(reset, Color.clear, 0);
                reset.style.color = UiTheme.Accent;
                reset.style.fontSize = 12;
                UiTheme.Pad(reset, 0, 4);
                head.Add(reset);
            }

            var field = new TextField { value = p.Value.ToString("G6", CultureInfo.InvariantCulture) };
            field.style.width = 86;
            UiTheme.StyleTextField(field);
            var input = field.Q("unity-text-input");
            if (input != null)
            {
                input.style.unityTextAlign = TextAnchor.MiddleRight;
                if (overridden) input.style.color = UiTheme.Accent;
            }
            field.RegisterCallback<FocusOutEvent>(_ => Commit(p, field));
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return) Commit(p, field);
            });
            head.Add(field);

            var unit = new Label(p.Unit);
            unit.style.color = UiTheme.TextFaint;
            unit.style.fontSize = 9;
            unit.style.width = 58;
            unit.style.marginLeft = 5;
            unit.style.overflow = Overflow.Hidden;
            head.Add(unit);
            row.Add(head);

            var meta = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2 } };
            var (confText, confColor) = p.Confidence switch
            {
                Confidence.High => ("HIGH", UiTheme.Good),
                Confidence.Medium => ("MED", UiTheme.Accent2),
                Confidence.Low => ("LOW", UiTheme.Warn),
                _ => ("SPEC", UiTheme.Bad),
            };
            meta.Add(UiTheme.Chip(confText, confColor));
            if (overridden) meta.Add(UiTheme.Chip("OVERRIDE", UiTheme.Accent));
            var src = new Label(p.Source);
            src.style.color = UiTheme.TextFaint;
            src.style.fontSize = 9;
            src.style.whiteSpace = WhiteSpace.Normal;
            src.style.flexShrink = 1;
            meta.Add(src);
            row.Add(meta);

            return row;
        }

        private static void Commit(Param p, TextField field)
        {
            if (double.TryParse(field.value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            {
                if (System.Math.Abs(v - p.Value) > double.Epsilon)
                    SimRunner.Instance.Params.SetUserOverride(p.Id, v);
                var input = field.Q("unity-text-input");
                if (input != null)
                    input.style.color = SimRunner.Instance.Params.Find(p.Id).UserOverride.HasValue
                        ? UiTheme.Accent : UiTheme.TextHi;
            }
            else
            {
                field.value = p.Value.ToString("G6", CultureInfo.InvariantCulture);
            }
        }
    }
}
