import { useEffect, useRef, useState } from 'react';
import { AppRunner, SPEED_PRESETS } from '../runner';
import { SceneView } from '../three/sceneView';
import { SimClock } from '../sim/clock';
import { Units } from '../sim/units';
import { LogPanel, ParamsPanel, SystemsPanel, TelemetryPanel, useTick } from './panels';
import { severityColor } from './format';

// Earth year in sols (matches the Earth-date clock; "5 years" reads naturally).
const YEAR_SOLS = 365.25 / 1.02749;

function TopBar({
  runner, orbit, setOrbit, hidden, setHidden, dayNight, setDayNight, onScenario, onRerun,
}: {
  runner: AppRunner;
  orbit: boolean;
  setOrbit: (v: boolean) => void;
  hidden: boolean;
  setHidden: (v: boolean) => void;
  dayNight: boolean;
  setDayNight: (v: boolean) => void;
  onScenario: () => void;
  onRerun: () => void;
}) {
  useTick(120);
  const [, force] = useState(0);
  const sol = runner.playheadSol;
  const earth = new Date(runner.scenario.epochUtcMs + sol * Units.solSeconds * 1000);
  const j2000 = (runner.scenario.epochUtcMs - Date.UTC(2000, 0, 1, 12, 0, 0)) / 86400000;
  const ls = SimClock.computeLs(j2000 + sol * (Units.solSeconds / Units.earthDaySeconds));
  const lst = (sol - Math.floor(sol)) * Units.solHours;

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
          <span className="val">{earth.toISOString().slice(0, 10)}</span>
        </div>
        <div className="block">
          <span className="cap">SOL</span>
          <span className="val">{Math.floor(sol).toLocaleString('en-US').replace(/,/g, ' ')}</span>
        </div>
        <div className="block">
          <span className="cap">LTST</span>
          <span className="val">
            {String(Math.floor(lst)).padStart(2, '0')}:{String(Math.floor((lst % 1) * 60)).padStart(2, '0')}
          </span>
        </div>
        <div className="block">
          <span className="cap">SEASON Ls</span>
          <span className="val">{ls.toFixed(0)}°</span>
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
      <span className="caption faint" style={{ fontSize: 8, letterSpacing: 1 }}>SOLS/S</span>

      {runner.dirty && (
        <button className="ghost rerun" onClick={onRerun} title="Parameters changed — re-simulate the mission">
          ⟲ RE-RUN
        </button>
      )}
      <button className={`ghost${dayNight ? ' active' : ''}`} onClick={() => setDayNight(!dayNight)}
        title="Render the real day/night cycle (off = steady daylight; physics keep the true cycle either way)">
        DAY/NIGHT
      </button>
      <button className={`ghost${orbit ? ' active' : ''}`} onClick={() => setOrbit(!orbit)} title="Slow auto-orbit">
        ORBIT
      </button>
      <button className={`ghost${hidden ? ' active' : ''}`} onClick={() => setHidden(!hidden)}
        title="Hide panels for a clean timelapse (Tab)">
        HIDE
      </button>
    </div>
  );
}

/** Bottom timeline: scrubber with year gridlines + event markers, quick-jump chips, ticker. */
function Timeline({ runner }: { runner: AppRunner }) {
  useTick(120);
  const trackRef = useRef<HTMLDivElement>(null);
  const [, force] = useState(0);
  const duration = runner.durationSols;
  const rec = runner.recording;
  const frac = Math.max(0, Math.min(1, runner.playheadSol / duration));

  const seekFromClientX = (clientX: number) => {
    const el = trackRef.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    runner.seek(((clientX - r.left) / r.width) * duration);
    force((v) => v + 1);
  };

  const onDown = (e: React.PointerEvent) => {
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
    runner.paused = true;
    seekFromClientX(e.clientX);
  };
  const onMove = (e: React.PointerEvent) => {
    if (e.buttons) seekFromClientX(e.clientX);
  };

  const years = Math.floor(duration / YEAR_SOLS);
  const jumps: Array<[string, number]> = [['Start', 0]];
  for (let y = 1; y <= years; y++) jumps.push([`${y} yr`, y * YEAR_SOLS]);
  jumps.push(['End', duration]);

  // Event markers: milestones + criticals (cap for perf).
  const markers = (rec?.events ?? [])
    .filter((e) => e.severity === 'milestone' || e.severity === 'critical')
    .slice(0, 400);

  const last = rec ? runner.eventsUpTo().at(-1) : null;

  return (
    <div className="panel dock-bottom">
      <div className="tl-row">
        <div className="tl-track" ref={trackRef} onPointerDown={onDown} onPointerMove={onMove}>
          {Array.from({ length: years }, (_, i) => (
            <div key={i} className="tl-year" style={{ left: `${((i + 1) * YEAR_SOLS) / duration * 100}%` }} />
          ))}
          {markers.map((e, i) => (
            <div
              key={i}
              className="tl-marker"
              style={{ left: `${(e.sol / duration) * 100}%`, background: severityColor(e.severity) }}
              title={`Sol ${e.sol.toFixed(0)} — ${e.message}`}
            />
          ))}
          <div className="tl-fill" style={{ width: `${frac * 100}%` }} />
          <div className="tl-head" style={{ left: `${frac * 100}%` }} />
        </div>
        <div className="tl-jumps">
          {jumps.map(([label, s]) => (
            <button
              key={label}
              className="ghost jump"
              onClick={() => {
                runner.seek(s);
                force((v) => v + 1);
              }}
            >
              {label}
            </button>
          ))}
        </div>
      </div>
      <div className="tl-ticker">
        <span className="bar" style={{ background: last ? severityColor(last.severity) : '#444' }} />
        <span className="mono caption">{last ? `SOL ${last.sol.toFixed(1)}` : ''}</span>
        <span className="tl-msg">{last ? `${last.source} — ${last.message}` : '—'}</span>
      </div>
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
  const [dayNight, setDayNight] = useState(false);
  const [hidden, setHidden] = useState(false);
  const [scenarioOpen, setScenarioOpen] = useState(false);
  const [recomputing, setRecomputing] = useState(false);
  const [, force] = useState(0);

  useEffect(() => {
    const canvas = canvasRef.current!;
    const view = new SceneView(canvas);
    viewRef.current = view;
    (window as unknown as Record<string, unknown>).__mars = { runner, view };

    const applyRecording = () => {
      if (runner.recording) view.setShips(runner.recording.ships);
    };
    applyRecording();
    const offRebuild = runner.onRebuild(() => {
      applyRecording();
      force((v) => v + 1);
    });
    const offDirty = runner.onDirtyChange(() => force((v) => v + 1));

    const resize = () => view.resize(window.innerWidth, window.innerHeight);
    resize();
    window.addEventListener('resize', resize);

    // Playhead advances on a steady interval (background-tab safe); render on rAF.
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
      view.render(dt, runner.frame, runner.playheadSol);
    };
    raf = requestAnimationFrame(loop);

    return () => {
      clearInterval(simTimer);
      cancelAnimationFrame(raf);
      window.removeEventListener('resize', resize);
      offRebuild();
      offDirty();
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

  // Re-run: paint the overlay first, then run the (blocking) recompute on the next frame.
  const doRerun = () => {
    setRecomputing(true);
    requestAnimationFrame(() =>
      requestAnimationFrame(() => {
        runner.recomputeLive();
        setRecomputing(false);
        force((v) => v + 1);
      }),
    );
  };

  return (
    <>
      <canvas ref={canvasRef} className="world" />
      <TopBar
        runner={runner}
        orbit={orbit} setOrbit={setOrbit}
        hidden={hidden} setHidden={setHidden}
        dayNight={dayNight} setDayNight={setDayNight}
        onScenario={() => setScenarioOpen((v) => !v)}
        onRerun={doRerun}
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
      <Timeline runner={runner} />
      {recomputing && (
        <div className="recompute-overlay">
          <div className="mark" />
          <div>SIMULATING MISSION…</div>
        </div>
      )}
    </>
  );
}
