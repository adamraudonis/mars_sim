using System.Collections.Generic;
using System.Linq;
using MarsSim.Core;
using MarsSim.Core.Study;
using MarsSim.UnityApp.BaseView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarsSim.UnityApp.UI
{
    /// <summary>
    /// Builds and drives the whole HUD (UI Toolkit, all code, mission-control theme):
    ///   top bar    — brand, scenario picker, Earth/sol/LTST/Ls clock cluster, timelapse control
    ///   left dock  — systems status cards with live fidelity switching
    ///   right dock — Telemetry | Parameters | Mission log
    ///   bottom     — latest-event strip
    /// Tab (or the ⤢ button) hides the docks for a clean cinematic timelapse.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private UIDocument _doc;

        private Label _earthValue, _solValue, _ltstValue, _lsValue, _tickerSol, _tickerMsg;
        private VisualElement _tickerBar;
        private System.Action<int> _setSpeedIndex;
        private Button _orbitBtn, _panelsBtn;
        private Button _scenarioBtn;
        private VisualElement _scenarioPopup;

        private VisualElement _leftDock, _rightDock, _bottomBar;
        private bool _panelsHidden;

        private SystemsPanelView _systems;
        private ParameterInspectorView _paramsView;
        private EventsView _events;
        private readonly List<ChartView> _charts = new();
        private VisualElement _chartsTab, _paramsTab, _logTab;
        private System.Action<int> _setTabIndex;
        private Label _distillResult;

        private float _mediumRefreshTimer;

        private void Start()
        {
            _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = LoadPanelSettings();
            BuildUi(_doc.rootVisualElement);

            if (SimRunner.Instance != null)
                SimRunner.Instance.EngineRebuilt += OnEngineRebuilt;
            OnEngineRebuilt();
        }

        private static PanelSettings LoadPanelSettings()
        {
            var ps = Resources.Load<PanelSettings>("MarsPanelSettings");
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                var theme = Resources.Load<ThemeStyleSheet>("MarsTheme");
                if (theme != null) ps.themeStyleSheet = theme;
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            }
            return ps;
        }

        // ================= Layout =================

        private void BuildUi(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;
            UiTheme.ApplyUi(root);

            BuildTopBar(root);
            BuildLeftDock(root);
            BuildRightDock(root);
            BuildTicker(root);
        }

        private void BuildTopBar(VisualElement root)
        {
            var top = UiTheme.Panel();
            top.style.position = Position.Absolute;
            top.style.top = 10; top.style.left = 10; top.style.right = 10;
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            top.style.height = 52;
            UiTheme.Pad(top, 6, 14);
            root.Add(top);

            // --- Brand ---
            var brand = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 16 } };
            var mark = new VisualElement { style = { width = 10, height = 10, backgroundColor = UiTheme.Accent, marginRight = 8 } };
            UiTheme.Rounded(mark, 2);
            mark.style.rotate = new Rotate(45);
            brand.Add(mark);
            var title = new Label("MARS HABITAT SIM");
            title.style.color = UiTheme.TextHi;
            title.style.fontSize = 12;
            title.style.letterSpacing = 3;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            brand.Add(title);
            top.Add(brand);

            // --- Scenario picker (custom popup) ---
            _scenarioBtn = UiTheme.GhostButton("scenario ▾", ToggleScenarioPopup);
            _scenarioBtn.style.maxWidth = 320;
            _scenarioBtn.style.overflow = Overflow.Hidden;
            top.Add(_scenarioBtn);

            // --- Clock cluster (centered) ---
            var clock = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, flexGrow = 1,
                    justifyContent = Justify.Center, alignItems = Align.Center,
                },
            };
            _earthValue = AddClockBlock(clock, "EARTH");
            AddClockDivider(clock);
            _solValue = AddClockBlock(clock, "SOL");
            AddClockDivider(clock);
            _ltstValue = AddClockBlock(clock, "LTST");
            AddClockDivider(clock);
            _lsValue = AddClockBlock(clock, "SEASON Ls");
            top.Add(clock);

            // --- Timelapse control ---
            var speedOptions = new[] { "II", "0.02", "0.1", "0.5", "2", "10", "50" };
            var (speedSeg, setSpeed) = UiTheme.Segmented(speedOptions, OnSpeedSegment, 10);
            _setSpeedIndex = setSpeed;
            top.Add(speedSeg);
            var solsCaption = UiTheme.Caption("SOLS/S", UiTheme.TextFaint);
            solsCaption.style.fontSize = 8;
            solsCaption.style.letterSpacing = 1;
            solsCaption.style.marginLeft = 5;
            solsCaption.style.marginRight = 10;
            top.Add(solsCaption);

            _orbitBtn = UiTheme.GhostButton("ORBIT", () =>
            {
                var cam = Camera.main?.GetComponent<CameraOrbit>();
                if (cam != null) { cam.AutoOrbit = !cam.AutoOrbit; UiTheme.SetActive(_orbitBtn, cam.AutoOrbit); }
            });
            _orbitBtn.tooltip = "Slow auto-orbit (cinematic)";
            _orbitBtn.style.fontSize = 9;
            _orbitBtn.style.letterSpacing = 1;
            top.Add(_orbitBtn);

            _panelsBtn = UiTheme.GhostButton("HIDE", TogglePanels);
            _panelsBtn.tooltip = "Hide panels for a clean timelapse (Tab)";
            _panelsBtn.style.fontSize = 9;
            _panelsBtn.style.letterSpacing = 1;
            top.Add(_panelsBtn);

            var reset = UiTheme.GhostButton("RESET", () => SimRunner.Instance.RebuildEngine());
            reset.style.fontSize = 9;
            reset.style.letterSpacing = 1;
            top.Add(reset);
        }

        private static Label AddClockBlock(VisualElement parent, string caption)
        {
            var block = new VisualElement { style = { alignItems = Align.Center, marginLeft = 10, marginRight = 10 } };
            var c = new Label(caption);
            c.style.color = UiTheme.TextFaint;
            c.style.fontSize = 8;
            c.style.letterSpacing = 2;
            block.Add(c);
            var v = UiTheme.MonoLabel("—", 14, UiTheme.TextHi);
            v.style.marginTop = 1;
            block.Add(v);
            parent.Add(block);
            return v;
        }

        private static void AddClockDivider(VisualElement parent)
        {
            var d = new VisualElement { style = { width = 1, height = 22, backgroundColor = UiTheme.Hairline } };
            parent.Add(d);
        }

        private void BuildLeftDock(VisualElement root)
        {
            _leftDock = UiTheme.Panel(330);
            _leftDock.style.position = Position.Absolute;
            _leftDock.style.left = 10; _leftDock.style.top = 72; _leftDock.style.bottom = 46;
            root.Add(_leftDock);
            _systems = new SystemsPanelView();
            _leftDock.Add(_systems);
        }

        private void BuildRightDock(VisualElement root)
        {
            _rightDock = UiTheme.Panel(430);
            _rightDock.style.position = Position.Absolute;
            _rightDock.style.right = 10; _rightDock.style.top = 72; _rightDock.style.bottom = 46;
            root.Add(_rightDock);

            var (tabs, setTab) = UiTheme.Segmented(new[] { "TELEMETRY", "PARAMETERS", "LOG" }, ShowTab, 9);
            tabs.style.marginBottom = 10;
            tabs.style.alignSelf = Align.FlexStart;
            _setTabIndex = setTab;
            _rightDock.Add(tabs);

            var chartsScroll = new ScrollView { style = { flexGrow = 1 } };
            chartsScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _chartsTab = chartsScroll;
            _paramsTab = new VisualElement { style = { flexGrow = 1, display = DisplayStyle.None } };
            _logTab = new VisualElement { style = { flexGrow = 1, display = DisplayStyle.None } };
            _rightDock.Add(_chartsTab);
            _rightDock.Add(_paramsTab);
            _rightDock.Add(_logTab);

            BuildCharts(chartsScroll);
            _paramsView = new ParameterInspectorView();
            _paramsTab.Add(_paramsView);
            _events = new EventsView();
            _logTab.Add(_events);
            ShowTab(0);
        }

        private void BuildTicker(VisualElement root)
        {
            _bottomBar = UiTheme.Panel();
            _bottomBar.style.position = Position.Absolute;
            _bottomBar.style.bottom = 10; _bottomBar.style.left = 10; _bottomBar.style.right = 10;
            _bottomBar.style.flexDirection = FlexDirection.Row;
            _bottomBar.style.alignItems = Align.Center;
            UiTheme.Pad(_bottomBar, 6, 12);
            root.Add(_bottomBar);

            _tickerBar = new VisualElement
            {
                style = { width = 3, height = 16, backgroundColor = UiTheme.TextFaint, marginRight = 9, flexShrink = 0 },
            };
            UiTheme.Rounded(_tickerBar, 1.5f);
            _bottomBar.Add(_tickerBar);

            _tickerSol = UiTheme.MonoLabel("", 10, UiTheme.TextFaint);
            _tickerSol.style.marginRight = 9;
            _tickerSol.style.flexShrink = 0;
            _bottomBar.Add(_tickerSol);

            _tickerMsg = new Label("—");
            _tickerMsg.style.fontSize = 11;
            _tickerMsg.style.color = UiTheme.Text;
            _tickerMsg.style.overflow = Overflow.Hidden;
            _tickerMsg.style.flexShrink = 1;
            _bottomBar.Add(_tickerMsg);
        }

        // ================= Scenario popup =================

        private void ToggleScenarioPopup()
        {
            if (_scenarioPopup != null) { CloseScenarioPopup(); return; }

            var runner = SimRunner.Instance;
            if (runner == null || runner.AvailableScenarios.Count == 0) return;

            _scenarioPopup = UiTheme.Panel(420);
            _scenarioPopup.style.position = Position.Absolute;
            _scenarioPopup.style.top = 66;
            _scenarioPopup.style.left = 200;
            UiTheme.Border(_scenarioPopup, UiTheme.HairlineBright);
            _scenarioPopup.Add(UiTheme.SectionHeader("Scenarios"));

            foreach (var (file, name) in runner.AvailableScenarios)
            {
                string f = file;
                bool current = runner.CurrentScenario.Name == name;
                var b = new Button(() => { CloseScenarioPopup(); runner.LoadScenario(f); }) { text = name };
                b.style.backgroundColor = current ? new Color(1f, 0.48f, 0.24f, 0.14f) : Color.clear;
                UiTheme.Border(b, current ? new Color(1f, 0.48f, 0.24f, 0.4f) : Color.clear, 1);
                UiTheme.Rounded(b, 6);
                b.style.color = current ? UiTheme.Accent : UiTheme.Text;
                b.style.fontSize = 11;
                b.style.unityTextAlign = TextAnchor.MiddleLeft;
                b.style.whiteSpace = WhiteSpace.Normal;
                UiTheme.Pad(b, 6, 9);
                b.style.marginBottom = 3;
                b.style.marginLeft = 0; b.style.marginRight = 0;
                _scenarioPopup.Add(b);
            }
            _doc.rootVisualElement.Add(_scenarioPopup);
        }

        private void CloseScenarioPopup()
        {
            _scenarioPopup?.RemoveFromHierarchy();
            _scenarioPopup = null;
        }

        // ================= Tabs & charts =================

        private void ShowTab(int index)
        {
            _chartsTab.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _paramsTab.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _logTab.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            _setTabIndex?.Invoke(index);
        }

        private void BuildCharts(ScrollView tab)
        {
            var pal = UiTheme.Palette;

            void Make(string chartTitle, params ChartView.SeriesSpec[] specs)
            {
                var c = new ChartView(chartTitle);
                c.SetSeries(specs);
                _charts.Add(c);
                tab.Add(c);
            }

            Make("Cabin atmosphere",
                new ChartView.SeriesSpec { Id = "hab.ppo2", Color = pal[0], LabelOverride = "ppO₂ kPa" },
                new ChartView.SeriesSpec { Id = "hab.ppco2", Color = pal[1], LabelOverride = "ppCO₂ kPa" });

            Make("Consumable reserves",
                new ChartView.SeriesSpec { Id = "crew.food_days", Color = pal[2], LabelOverride = "food sols" },
                new ChartView.SeriesSpec { Id = "crew.water_days", Color = pal[0], LabelOverride = "water sols" });

            Make("Power",
                new ChartView.SeriesSpec { Id = "power.offered", Color = pal[4], LabelOverride = "available" },
                new ChartView.SeriesSpec { Id = "power.demand", Color = pal[1], LabelOverride = "demand" },
                new ChartView.SeriesSpec { Id = "power.unmet", Color = UiTheme.Bad, LabelOverride = "unmet" });

            Make("Battery state of charge",
                new ChartView.SeriesSpec { Id = "power.battery_soc", Color = pal[5], LabelOverride = "SoC %" });

            Make("Return propellant",
                new ChartView.SeriesSpec { Id = "fleet.return_prop", Color = pal[3], LabelOverride = "readiness %" },
                new ChartView.SeriesSpec { Id = "depot.ch4", Color = pal[6], LabelOverride = "CH₄ t" },
                new ChartView.SeriesSpec { Id = "depot.lox", Color = pal[0], LabelOverride = "LOX t" });

            Make("Water & ISRU",
                new ChartView.SeriesSpec { Id = "store.water_potable", Color = pal[0], LabelOverride = "water kg" },
                new ChartView.SeriesSpec { Id = "icemine.production", Color = pal[6], LabelOverride = "mined kg/sol" },
                new ChartView.SeriesSpec { Id = "isru.water_demand", Color = pal[1], LabelOverride = "plant kg/sol" });

            Make("Dust",
                new ChartView.SeriesSpec { Id = "env.tau", Color = pal[1], LabelOverride = "optical depth τ" },
                new ChartView.SeriesSpec { Id = "solar.dust", Color = pal[4], LabelOverride = "panel dust %" });

            Make("Crew",
                new ChartView.SeriesSpec { Id = "crew.count", Color = pal[2], LabelOverride = "crew" },
                new ChartView.SeriesSpec { Id = "crew.health", Color = pal[0], LabelOverride = "health" },
                new ChartView.SeriesSpec { Id = "crew.dose", Color = pal[1], LabelOverride = "dose mSv" });

            Make("Maintenance",
                new ChartView.SeriesSpec { Id = "maint.queue", Color = pal[1], LabelOverride = "open repairs" },
                new ChartView.SeriesSpec { Id = "maint.awaiting_spares", Color = UiTheme.Bad, LabelOverride = "no spares" },
                new ChartView.SeriesSpec { Id = "labor.unmet", Color = pal[4], LabelOverride = "unmet labor h" });

            // ---- Distillation tools ----
            var tools = UiTheme.Card();
            tools.style.marginTop = 4;
            tools.Add(UiTheme.SectionHeader("Distill  L2 → L0", UiTheme.Accent));
            var hint = UiTheme.Caption(
                "Run a subsystem at max fidelity in isolation, fit the averaged coefficients, and install them as overrides for fast campaign-scale runs.");
            hint.style.marginBottom = 6;
            tools.Add(hint);
            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            btnRow.Add(UiTheme.GhostButton("Solar → kWh/sol/kW", () =>
            {
                var r = Distiller.DistillSolar(SimRunner.Instance.Params,
                    SimRunner.Instance.CurrentScenario.LatitudeDeg);
                _distillResult.text = r.Summary;
                _paramsView.Rebuild();
            }));
            btnRow.Add(UiTheme.GhostButton("Greenhouse → utilization", () =>
            {
                var r = Distiller.DistillGreenhouse(SimRunner.Instance.Params);
                _distillResult.text = r.Summary;
                _paramsView.Rebuild();
            }));
            tools.Add(btnRow);
            _distillResult = UiTheme.Caption("");
            _distillResult.style.marginTop = 5;
            tools.Add(_distillResult);
            tab.Add(tools);
        }

        // ================= Behavior =================

        private void OnEngineRebuilt()
        {
            var runner = SimRunner.Instance;
            if (runner == null) return;
            _scenarioBtn.text = Truncate(runner.CurrentScenario.Name, 42) + "  ▾";
            CloseScenarioPopup();
            _systems.Rebuild();
            if (runner.Engine != null) _events.Bind(runner.Engine);
            SyncSpeedSegment();
        }

        private static string Truncate(string s, int n)
            => string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        private void OnSpeedSegment(int idx)
        {
            var runner = SimRunner.Instance;
            if (idx == 0)
            {
                runner.Paused = !runner.Paused;
            }
            else
            {
                runner.SolsPerSecond = SimRunner.SpeedPresets[idx - 1];
                runner.Paused = false;
            }
            SyncSpeedSegment();
        }

        private void SyncSpeedSegment()
        {
            var runner = SimRunner.Instance;
            if (runner.Paused) { _setSpeedIndex?.Invoke(0); return; }
            int best = 0;
            for (int i = 0; i < SimRunner.SpeedPresets.Length; i++)
                if (System.Math.Abs(runner.SolsPerSecond - SimRunner.SpeedPresets[i]) < 1e-9)
                    best = i + 1;
            _setSpeedIndex?.Invoke(best);
        }

        public void TogglePanels()
        {
            _panelsHidden = !_panelsHidden;
            var d = _panelsHidden ? DisplayStyle.None : DisplayStyle.Flex;
            _leftDock.style.display = d;
            _rightDock.style.display = d;
            UiTheme.SetActive(_panelsBtn, _panelsHidden);
        }

        private void Update()
        {
            var runner = SimRunner.Instance;
            if (runner?.Engine == null) return;
            var clock = runner.Engine.Clock;

            if (Input.GetKeyDown(KeyCode.Tab)) TogglePanels();

            _earthValue.text = clock.EarthUtc.ToString("yyyy-MM-dd");
            _solValue.text = clock.SolNumber.ToString("N0").Replace(",", " ");
            _ltstValue.text = $"{(int)clock.LocalSolarHours:00}:{(int)(clock.LocalSolarHours % 1 * 60):00}";
            _lsValue.text = $"{clock.Ls:0}°";

            var events = runner.Engine.Events.Events;
            if (events.Count > 0)
            {
                var last = events[^1];
                _tickerSol.text = $"SOL {last.Sol:0.0}";
                _tickerMsg.text = $"{last.Source} — {last.Message}";
                var color = last.Severity switch
                {
                    EventSeverity.Critical => UiTheme.Bad,
                    EventSeverity.Warning => UiTheme.Warn,
                    EventSeverity.Milestone => UiTheme.Good,
                    _ => UiTheme.TextFaint,
                };
                _tickerBar.style.backgroundColor = color;
                _tickerMsg.style.color = last.Severity == EventSeverity.Info ? UiTheme.TextDim : UiTheme.Text;
            }

            // UI picking for the camera (UI Toolkit has no EventSystem).
            var panel = _doc.rootVisualElement.panel;
            if (panel != null)
            {
                Vector2 screen = Input.mousePosition;
                var panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
                CameraOrbit.PointerOverUI = panel.Pick(panelPos) != null;
            }

            _mediumRefreshTimer += Time.deltaTime;
            if (_mediumRefreshTimer > 0.33f)
            {
                _mediumRefreshTimer = 0;
                if (!_panelsHidden)
                {
                    _systems.Refresh();
                    if (_chartsTab.style.display == DisplayStyle.Flex)
                        foreach (var c in _charts) c.Refresh();
                }
                SyncSpeedSegment();
            }
        }
    }
}
