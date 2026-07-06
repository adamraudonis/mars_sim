import { useEffect, useRef, useState, type ReactNode } from 'react';
import type { AppRunner } from '../runner';
import { SimClock } from '../sim/clock';
import { Units } from '../sim/units';
import { useTick } from '../ui/panels';
import { DocNav, type Route } from './nav';

const YEAR_SOLS = 365.25 / 1.02749;

/** Shared time control for the document pages: sol/date readout + scrubber + play. */
function DocTimeBar({ runner }: { runner: AppRunner }) {
  useTick(150);
  const [, force] = useState(0);
  const trackRef = useRef<HTMLDivElement>(null);
  const duration = runner.durationSols;
  const sol = runner.playheadSol;
  const earth = new Date(runner.scenario.epochUtcMs + sol * Units.solSeconds * 1000);
  const j2000 = (runner.scenario.epochUtcMs - Date.UTC(2000, 0, 1, 12, 0, 0)) / 86400000;
  const ls = SimClock.computeLs(j2000 + sol * (Units.solSeconds / Units.earthDaySeconds));
  const frac = Math.max(0, Math.min(1, sol / duration));

  const seekAt = (clientX: number) => {
    const el = trackRef.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    runner.seek(((clientX - r.left) / r.width) * duration);
    force((v) => v + 1);
  };

  const years = Math.floor(duration / YEAR_SOLS);
  const jumps: Array<[string, number]> = [['Start', 0]];
  for (let y = 1; y <= years; y++) jumps.push([`${y}yr`, y * YEAR_SOLS]);
  jumps.push(['End', duration]);

  return (
    <div className="doc-timebar">
      <button
        className="m-play"
        onClick={() => {
          runner.paused = !runner.paused;
          force((v) => v + 1);
        }}
      >
        {runner.paused ? '▶' : 'II'}
      </button>
      <div className="doc-time-readout mono">
        <span>SOL {Math.floor(sol)}</span>
        <span className="faint">{earth.toISOString().slice(0, 10)} · Ls {ls.toFixed(0)}°</span>
      </div>
      <div
        className="tl-track"
        ref={trackRef}
        onPointerDown={(e) => {
          (e.target as HTMLElement).setPointerCapture(e.pointerId);
          runner.paused = true;
          seekAt(e.clientX);
        }}
        onPointerMove={(e) => e.buttons && seekAt(e.clientX)}
      >
        {Array.from({ length: years }, (_, i) => (
          <div key={i} className="tl-year" style={{ left: `${((i + 1) * YEAR_SOLS) / duration * 100}%` }} />
        ))}
        <div className="tl-fill" style={{ width: `${frac * 100}%` }} />
        <div className="tl-head" style={{ left: `${frac * 100}%` }} />
      </div>
      <div className="doc-jumps">
        {jumps.map(([label, s]) => (
          <button key={label} className="ghost jump" onClick={() => { runner.seek(s); force((v) => v + 1); }}>{label}</button>
        ))}
      </div>
    </div>
  );
}

export function DocShell({ runner, route, children }: { runner: AppRunner; route: Route; children: ReactNode }) {
  // Keep the mission playing while on a document page (the 3D loop is unmounted here).
  useEffect(() => {
    let last = performance.now();
    const id = setInterval(() => {
      const now = performance.now();
      runner.tick(Math.min(0.5, (now - last) / 1000));
      last = now;
    }, 50);
    return () => clearInterval(id);
  }, [runner]);

  return (
    <div className="doc-shell">
      <DocNav route={route} />
      <DocTimeBar runner={runner} />
      <div className="doc-content">{children}</div>
    </div>
  );
}
