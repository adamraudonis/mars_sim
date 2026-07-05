import { useEffect, useRef, useState } from 'react';
import { AppRunner, SPEED_PRESETS } from '../runner';
import { SceneView } from '../three/sceneView';
import { LogPanel, ParamsPanel, SystemsPanel, TelemetryPanel, useTick } from './panels';
import { severityColor } from './format';

function TopBar({
  runner, orbit, setOrbit, hidden, setHidden, dayNight, setDayNight, onScenario,
}: {
  runner: AppRunner;
  orbit: boolean;
  setOrbit: (v: boolean) => void;
  hidden: boolean;
  setHidden: (v: boolean) => void;
  dayNight: boolean;
  setDayNight: (v: boolean) => void;
  onScenario: () => void;
}) {
  useTick(120);
  const [, force] = useState(0);
  const clock = runner.engine?.clock;
  const earth = clock ? new Date(clock.earthUtcMs) : null;
  const lst = clock ? clock.localSolarHours : 0;

  const speedIndex = runner.paused
    ? 0
    : SPEED_PRESETS.findIndex((s) => Math.abs(s - runner.solsPerSecond) < 1e-9) + 1;

  return (
    <div className="panel topbar">
      <div className="brand">
        <div className="mark" />
        <div className="title">MARS HABITAT SIM</div>
      </div>

      <button className="ghost scenario-btn" onClick={onScenario}>
        {runner.scenario?.name ?? 'scenario'} ▾
      </button>

      <div className="clock">
        <div className="block">
          <span className="cap">EARTH</span>
          <span className="val">{earth ? earth.toISOString().slice(0, 10) : '—'}</span>
        </div>
        <div className="block">
          <span className="cap">SOL</span>
          <span className="val">{clock ? clock.solNumber.toLocaleString('en-US').replace(/,/g, ' ') : '—'}</span>
        </div>
        <div className="block">
          <span className="cap">LTST</span>
          <span className="val">
            {String(Math.floor(lst)).padStart(2, '0')}:{String(Math.floor((lst % 1) * 60)).padStart(2, '0')}
          </span>
        </div>
        <div className="block">
          <span className="cap">SEASON Ls</span>
          <span className="val">{clock ? `${clock.ls.toFixed(0)}°` : '—'}</span>
        </div>
      </div>

      <div className="segmented">
        {['II', ...SPEED_PRESETS.map(String)].map((label, i) => (
          <button
            key={label}
            className={speedIndex === i ? 'active' : ''}
            onClick={() => {
              if (i === 0) runner.paused = !runner.paused;
              else {
                runner.solsPerSecond = SPEED_PRESETS[i - 1];
                runner.paused = false;
              }
              force((v) => v + 1);
            }}
          >
            {label}
          </button>
        ))}
      </div>
      <span className="caption faint" style={{ fontSize: 8, letterSpacing: 1 }}>
        SOLS/S
      </span>

      <button
        className={`ghost${dayNight ? ' active' : ''}`}
        onClick={() => setDayNight(!dayNight)}
        title="Render the real day/night cycle (off = steady daylight; simulation physics keep the true cycle either way)"
      >
        DAY/NIGHT
      </button>
      <button className={`ghost${orbit ? ' active' : ''}`} onClick={() => setOrbit(!orbit)} title="Slow auto-orbit">
        ORBIT
      </button>
      <button
        className={`ghost${hidden ? ' active' : ''}`}
        onClick={() => setHidden(!hidden)}
        title="Hide panels for a clean timelapse (Tab)"
      >
        HIDE
      </button>
      <button className="ghost" onClick={() => runner.rebuild()}>
        RESET
      </button>
    </div>
  );
}

function Ticker({ runner }: { runner: AppRunner }) {
  useTick(250);
  const events = runner.engine?.events.events;
  const last = events && events.length > 0 ? events[events.length - 1] : null;
  return (
    <div className="panel ticker">
      <span
        className="bar"
        style={{ width: 3, height: 16, borderRadius: 2, background: last ? severityColor(last.severity) : '#444' }}
      />
      <span className="mono caption">{last ? `SOL ${last.sol.toFixed(1)}` : ''}</span>
      <span style={{ fontSize: 11, overflow: 'hidden', textOverflow: 'ellipsis' }}>
        {last ? `${last.source} — ${last.message}` : '—'}
      </span>
    </div>
  );
}

function ScenarioPopup({ runner, onClose }: { runner: AppRunner; onClose: () => void }) {
  return (
    <div className="panel scenario-popup">
      <div className="section-header">Scenarios</div>
      {runner.scenarios.map((s) => (
        <button
          key={s.file}
          className={runner.scenario.name === s.name ? 'current' : ''}
          onClick={() => {
            runner.loadScenario(s.file);
            onClose();
          }}
        >
          {s.name}
          <div className="desc">{(s.json as { description?: string }).description?.slice(0, 220)}</div>
        </button>
      ))}
    </div>
  );
}

export function App({ runner }: { runner: AppRunner }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const viewRef = useRef<SceneView | null>(null);
  const [tab, setTab] = useState(0);
  const [orbit, setOrbit] = useState(false);
  const [dayNight, setDayNight] = useState(false); // off by default: no strobing at high timelapse
  const [hidden, setHidden] = useState(false);
  const [scenarioOpen, setScenarioOpen] = useState(false);
  const [, force] = useState(0);

  // Three.js world + main loop.
  useEffect(() => {
    const canvas = canvasRef.current!;
    const view = new SceneView(canvas);
    viewRef.current = view;
    view.setEngine(runner.engine);
    // Debug/console handle (harmless in production; handy for tooling).
    (window as unknown as Record<string, unknown>).__mars = { runner, view };
    const offRebuild = runner.onRebuild(() => {
      view.setEngine(runner.engine);
      force((v) => v + 1);
    });

    const resize = () => view.resize(window.innerWidth, window.innerHeight);
    resize();
    window.addEventListener('resize', resize);

    // Sim advances on a steady interval (keeps ticking in background/unfocused tabs,
    // where rAF is throttled); rendering stays on rAF.
    let lastSim = performance.now();
    const simTimer = setInterval(() => {
      const now = performance.now();
      runner.tick(Math.min(0.5, (now - lastSim) / 1000));
      lastSim = now;
    }, 50);

    let raf = 0;
    let lastT = performance.now();
    const loop = (t: number) => {
      raf = requestAnimationFrame(loop);
      const dt = Math.min(0.1, (t - lastT) / 1000);
      lastT = t;
      view.render(dt);
    };
    raf = requestAnimationFrame(loop);

    return () => {
      clearInterval(simTimer);
      cancelAnimationFrame(raf);
      window.removeEventListener('resize', resize);
      offRebuild();
      view.renderer.dispose();
    };
  }, [runner]);

  useEffect(() => {
    viewRef.current?.setAutoOrbit(orbit);
  }, [orbit]);

  useEffect(() => {
    if (viewRef.current) viewRef.current.dayNightCycle = dayNight;
  }, [dayNight]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Tab') {
        e.preventDefault();
        setHidden((v) => !v);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <>
      <canvas ref={canvasRef} className="world" />
      <TopBar
        runner={runner}
        orbit={orbit}
        setOrbit={setOrbit}
        hidden={hidden}
        setHidden={setHidden}
        dayNight={dayNight}
        setDayNight={setDayNight}
        onScenario={() => setScenarioOpen((v) => !v)}
      />
      {scenarioOpen && <ScenarioPopup runner={runner} onClose={() => setScenarioOpen(false)} />}
      {!hidden && (
        <div className="panel dock-left">
          <SystemsPanel runner={runner} />
        </div>
      )}
      {!hidden && (
        <div className="panel dock-right">
          <div className="segmented tabs">
            {['TELEMETRY', 'PARAMETERS', 'LOG'].map((label, i) => (
              <button key={label} className={tab === i ? 'active' : ''} onClick={() => setTab(i)}>
                {label}
              </button>
            ))}
          </div>
          {tab === 0 && <TelemetryPanel runner={runner} />}
          {tab === 1 && <ParamsPanel runner={runner} />}
          {tab === 2 && <LogPanel runner={runner} />}
        </div>
      )}
      <Ticker runner={runner} />
    </>
  );
}
