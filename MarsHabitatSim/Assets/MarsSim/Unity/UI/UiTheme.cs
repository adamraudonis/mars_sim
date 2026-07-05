using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>
    /// Code-driven design system for the HUD — mission-control dark theme.
    /// No USS/asset dependencies: colors, spacing, and typography live here; fonts are
    /// created at runtime from OS fonts (SF/Helvetica for text, Menlo for telemetry digits)
    /// with graceful fallback to Unity's default.
    /// </summary>
    public static class UiTheme
    {
        // ---------- Palette ----------
        public static readonly Color PanelBg = new(0.043f, 0.055f, 0.078f, 0.92f);
        public static readonly Color CardBg = new(1f, 1f, 1f, 0.035f);
        public static readonly Color CardBgHover = new(1f, 1f, 1f, 0.06f);
        public static readonly Color InputBg = new(0f, 0f, 0f, 0.35f);
        public static readonly Color Hairline = new(1f, 1f, 1f, 0.08f);
        public static readonly Color HairlineBright = new(1f, 1f, 1f, 0.16f);

        public static readonly Color Accent = new(1.00f, 0.48f, 0.24f);   // mars orange
        public static readonly Color Accent2 = new(0.33f, 0.78f, 0.91f);  // ice cyan
        public static readonly Color Good = new(0.29f, 0.87f, 0.50f);
        public static readonly Color Warn = new(0.98f, 0.75f, 0.14f);
        public static readonly Color Bad = new(0.97f, 0.44f, 0.44f);

        public static readonly Color TextHi = new(0.95f, 0.94f, 0.91f);
        public static readonly Color Text = new(0.79f, 0.77f, 0.74f);
        public static readonly Color TextDim = new(0.55f, 0.53f, 0.50f);
        public static readonly Color TextFaint = new(0.36f, 0.35f, 0.33f);

        // Chart palette — distinct, luminous on near-black.
        public static readonly Color[] Palette =
        {
            new(0.38f, 0.78f, 0.98f), new(1.00f, 0.58f, 0.30f), new(0.45f, 0.88f, 0.48f),
            new(0.93f, 0.48f, 0.80f), new(0.98f, 0.87f, 0.38f), new(0.65f, 0.62f, 0.98f),
            new(0.36f, 0.88f, 0.82f), new(0.92f, 0.66f, 0.55f),
        };

        // ---------- Fonts (runtime, from OS; cached) ----------
        private static FontDefinition _uiFont, _monoFont;
        private static bool _fontsLoaded;

        public static FontDefinition UiFont { get { EnsureFonts(); return _uiFont; } }
        public static FontDefinition MonoFont { get { EnsureFonts(); return _monoFont; } }

        private static void EnsureFonts()
        {
            if (_fontsLoaded) return;
            _fontsLoaded = true;
            _uiFont = LoadOsFont("Helvetica Neue", "SF Pro Text", "Segoe UI", "Arial");
            _monoFont = LoadOsFont("Menlo", "SF Mono", "Consolas", "Courier New");
        }

        private static FontDefinition LoadOsFont(params string[] names)
        {
            foreach (var name in names)
            {
                try
                {
                    var font = Font.CreateDynamicFontFromOSFont(name, 14);
                    if (font == null) continue;
                    var asset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font);
                    if (asset != null) return FontDefinition.FromSDFFont(asset);
                }
                catch (Exception)
                {
                    // fall through to next candidate / default font
                }
            }
            return default;
        }

        public static void ApplyMono(VisualElement e)
        {
            if (!MonoFont.Equals(default(FontDefinition))) e.style.unityFontDefinition = MonoFont;
        }

        public static void ApplyUi(VisualElement e)
        {
            if (!UiFont.Equals(default(FontDefinition))) e.style.unityFontDefinition = UiFont;
        }

        // ---------- Primitives ----------

        public static void Rounded(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        public static void Border(VisualElement e, Color c, float w = 1)
        {
            e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w;
            e.style.borderLeftColor = c; e.style.borderRightColor = c;
            e.style.borderTopColor = c; e.style.borderBottomColor = c;
        }

        public static void Pad(VisualElement e, float v, float h)
        {
            e.style.paddingTop = v; e.style.paddingBottom = v;
            e.style.paddingLeft = h; e.style.paddingRight = h;
        }

        /// <summary>Floating glass panel (the three dock surfaces).</summary>
        public static VisualElement Panel(float width = 0)
        {
            var p = new VisualElement();
            p.style.backgroundColor = PanelBg;
            Rounded(p, 10);
            Border(p, Hairline);
            Pad(p, 10, 12);
            if (width > 0) p.style.width = width;
            return p;
        }

        /// <summary>Inner card (module entries, chart groups, parameter rows).</summary>
        public static VisualElement Card()
        {
            var c = new VisualElement();
            c.style.backgroundColor = CardBg;
            Rounded(c, 8);
            Border(c, new Color(1, 1, 1, 0.05f));
            Pad(c, 8, 10);
            c.style.marginBottom = 8;
            return c;
        }

        /// <summary>Uppercase, letter-spaced section header with a hairline rule.</summary>
        public static VisualElement SectionHeader(string text, Color? color = null)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            var l = new Label(text.ToUpperInvariant());
            l.style.color = color ?? TextDim;
            l.style.fontSize = 10;
            l.style.letterSpacing = 3;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(l);
            var rule = new VisualElement { style = { height = 1, flexGrow = 1, marginLeft = 8, backgroundColor = Hairline } };
            row.Add(rule);
            return row;
        }

        public static Label Caption(string text, Color? color = null)
        {
            var l = new Label(text);
            l.style.color = color ?? TextDim;
            l.style.fontSize = 10;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        public static Label MonoLabel(string text, float size, Color? color = null)
        {
            var l = new Label(text);
            ApplyMono(l);
            l.style.fontSize = size;
            l.style.color = color ?? TextHi;
            return l;
        }

        /// <summary>Tiny rounded status chip.</summary>
        public static Label Chip(string text, Color color)
        {
            var l = new Label(text);
            ApplyMono(l);
            l.style.fontSize = 9;
            l.style.color = color;
            l.style.backgroundColor = new Color(color.r, color.g, color.b, 0.12f);
            Rounded(l, 4);
            Pad(l, 1, 5);
            l.style.marginRight = 4;
            return l;
        }

        public static VisualElement Dot(Color color, float size = 8)
        {
            var d = new VisualElement();
            d.style.width = size; d.style.height = size;
            Rounded(d, size / 2);
            d.style.backgroundColor = color;
            d.style.marginRight = 7;
            d.style.flexShrink = 0;
            return d;
        }

        // ---------- Controls ----------

        public static Button GhostButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            ApplyUi(b);
            b.style.backgroundColor = new Color(1, 1, 1, 0.05f);
            b.style.color = Text;
            Border(b, Hairline);
            Rounded(b, 6);
            Pad(b, 3, 10);
            b.style.marginLeft = 3; b.style.marginRight = 0;
            b.style.fontSize = 11;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            HoverFx(b, new Color(1, 1, 1, 0.05f), new Color(1, 1, 1, 0.11f));
            return b;
        }

        public static void SetActive(Button b, bool active)
        {
            b.style.backgroundColor = active ? Accent : new Color(1, 1, 1, 0.05f);
            b.style.color = active ? new Color(0.07f, 0.05f, 0.03f) : Text;
            b.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            b.userData = active;
        }

        private static void HoverFx(VisualElement e, Color normal, Color hover)
        {
            e.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (e is Button b && b.userData is bool on && on) return;
                e.style.backgroundColor = hover;
            });
            e.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (e is Button b && b.userData is bool on && on) return;
                e.style.backgroundColor = normal;
            });
        }

        /// <summary>
        /// Segmented control: one pill-group of exclusive options. Returns the container;
        /// onSelect receives the option index. Call the returned setter to sync state.
        /// </summary>
        public static (VisualElement root, Action<int> setIndex) Segmented(
            string[] options, Action<int> onSelect, float fontSize = 10)
        {
            var root = new VisualElement { style = { flexDirection = FlexDirection.Row, flexShrink = 0 } };
            root.style.backgroundColor = new Color(0, 0, 0, 0.30f);
            Rounded(root, 6);
            Border(root, Hairline);
            Pad(root, 2, 2);

            var buttons = new Button[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var b = new Button(() => onSelect(idx)) { text = options[i] };
                ApplyUi(b);
                b.style.fontSize = fontSize;
                b.style.color = TextDim;
                b.style.backgroundColor = Color.clear;
                Border(b, Color.clear, 0);
                Rounded(b, 4);
                Pad(b, 2, 8);
                b.style.marginLeft = 0; b.style.marginRight = 0;
                b.style.unityTextAlign = TextAnchor.MiddleCenter;
                buttons[i] = b;
                root.Add(b);
            }

            void SetIndex(int active)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    bool on = i == active;
                    buttons[i].style.backgroundColor = on ? Accent : Color.clear;
                    buttons[i].style.color = on ? new Color(0.07f, 0.05f, 0.03f) : TextDim;
                    buttons[i].style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            return (root, SetIndex);
        }

        /// <summary>label: VALUE unit — the standard telemetry row.</summary>
        public static (VisualElement root, Label value) KeyValue(string label, string unit, float valueSize = 11)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 1, flexGrow = 1 },
            };
            var l = new Label(label);
            l.style.color = TextDim;
            l.style.fontSize = 10;
            l.style.flexGrow = 1;
            l.style.unityTextOverflowPosition = TextOverflowPosition.End;
            l.style.overflow = Overflow.Hidden;
            row.Add(l);
            var v = MonoLabel("—", valueSize, TextHi);
            v.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(v);
            if (!string.IsNullOrEmpty(unit))
            {
                var u = new Label(unit);
                u.style.color = TextFaint;
                u.style.fontSize = 9;
                u.style.marginLeft = 3;
                u.style.width = 34;
                row.Add(u);
            }
            return (row, v);
        }

        /// <summary>Restyle a UITK TextField into the theme (dark input, mono text).</summary>
        public static void StyleTextField(TextField f, bool mono = true, float fontSize = 11)
        {
            f.style.marginLeft = 0; f.style.marginRight = 0;
            var input = f.Q("unity-text-input");
            if (input != null)
            {
                input.style.backgroundColor = InputBg;
                Border(input, Hairline);
                Rounded(input, 5);
                Pad(input, 1, 6);
                input.style.color = TextHi;
                input.style.fontSize = fontSize;
                if (mono) ApplyMono(input);
            }
        }

        // ---------- Formatting ----------

        public static string Compact(double v)
        {
            double a = Math.Abs(v);
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            if (a >= 1e9) return (v / 1e9).ToString("0.0") + "B";
            if (a >= 1e6) return (v / 1e6).ToString("0.0") + "M";
            if (a >= 1e4) return (v / 1e3).ToString("0.0") + "k";
            if (a >= 100) return v.ToString("0");
            if (a >= 10) return v.ToString("0.0");
            if (a >= 1) return v.ToString("0.00");
            if (a == 0) return "0";
            return v.ToString("0.00");
        }
    }
}
