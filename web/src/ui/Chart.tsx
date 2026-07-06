import { useEffect, useRef, useState } from 'react';
import type { AppRunner } from '../runner';
import { compact } from './format';

export interface SeriesSpec {
  id: string;
  color: string;
  label?: string;
}

/**
 * Multi-series telemetry chart (canvas, x = mission sol). Draws the FULL mission arc always
 * — so you see the whole multi-year story at a glance — with a moving playhead line and a
 * dot per series at the current sol. Reads from the cached Recording, never the engine.
 */
export function Chart({ runner, title, series }: { runner: AppRunner; title: string; series: SeriesSpec[] }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [legend, setLegend] = useState<Array<{ label: string; value: string; color: string }>>([]);
  const [unit, setUnit] = useState('');
  const [empty, setEmpty] = useState(true);

  useEffect(() => {
    let raf = 0;
    let last = 0;
    const draw = (t: number) => {
      raf = requestAnimationFrame(draw);
      if (t - last < 120) return; // ~8 Hz (cheap; playhead should feel live)
      last = t;
      const canvas = canvasRef.current;
      const reader = runner.reader;
      if (!canvas || !reader) return;

      const dpr = Math.min(window.devicePixelRatio, 2);
      const w = canvas.clientWidth;
      const h = canvas.clientHeight;
      if (w === 0) return;
      if (canvas.width !== w * dpr || canvas.height !== h * dpr) {
        canvas.width = w * dpr;
        canvas.height = h * dpr;
      }
      const ctx = canvas.getContext('2d')!;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, w, h);

      const frames = reader.frameCount;
      const duration = runner.durationSols;
      const playIdx = reader.solToIndex(runner.playheadSol);

      let mn = Infinity;
      let mx = -Infinity;
      let anyData = false;
      let unitFound = '';
      const arrays: Array<{ values: number[]; color: string; label: string }> = [];
      const legendNext: Array<{ label: string; value: string; color: string }> = [];
      for (const s of series) {
        const ser = reader.series(s.id);
        if (ser && ser.values.length > 1) {
          anyData = true;
          for (const v of ser.values) {
            if (v < mn) mn = v;
            if (v > mx) mx = v;
          }
          if (!unitFound && ser.unit) unitFound = ser.unit;
          arrays.push({ values: ser.values, color: s.color, label: s.label ?? ser.name });
          legendNext.push({ label: s.label ?? ser.name, value: compact(ser.values[playIdx] ?? 0), color: s.color });
        } else {
          legendNext.push({ label: s.label ?? s.id, value: '', color: s.color });
        }
      }
      setLegend(legendNext);
      setUnit(unitFound);
      setEmpty(!anyData);
      if (!anyData) {
        ctx.fillStyle = 'rgba(92,89,82,0.8)';
        ctx.font = '10px Menlo, monospace';
        ctx.textAlign = 'center';
        ctx.fillText('AWAITING TELEMETRY', w / 2, h / 2 + 3);
        return;
      }
      if (mn > mx) {
        mn = 0;
        mx = 1;
      }
      if (mx - mn < 1e-9) {
        const p0 = Math.abs(mx) * 0.1 + 0.5;
        mn -= p0;
        mx += p0;
      }
      const pad = (mx - mn) * 0.07;
      mn -= pad;
      mx += pad;

      const padL = 8;
      const padR = 8;
      const padT = 8;
      const padB = 8;
      const pw = w - padL - padR;
      const ph = h - padT - padB;
      const xAt = (i: number) => padL + (frames > 1 ? i / (frames - 1) : 0) * pw;
      const yAt = (v: number) => padT + ph - ((v - mn) / (mx - mn)) * ph;

      // Grid.
      ctx.strokeStyle = 'rgba(255,255,255,0.045)';
      ctx.lineWidth = 1;
      for (let g = 1; g < 4; g++) {
        const y = padT + (ph * g) / 4;
        ctx.beginPath();
        ctx.moveTo(padL, y);
        ctx.lineTo(padL + pw, y);
        ctx.stroke();
      }
      // Year gridlines (Earth years, matching the timeline + clock).
      const yearSols = 365.25 / 1.02749;
      for (let yr = 1; yr * yearSols < duration; yr++) {
        const x = xAt(reader.solToIndex(yr * yearSols));
        ctx.beginPath();
        ctx.moveTo(x, padT);
        ctx.lineTo(x, padT + ph);
        ctx.stroke();
      }

      const stride = Math.max(1, Math.floor(frames / Math.max(64, pw)));
      for (let si = arrays.length - 1; si >= 0; si--) {
        const a = arrays[si];
        // Area fill.
        ctx.beginPath();
        ctx.moveTo(xAt(0), padT + ph);
        for (let i = 0; i < frames; i += stride) ctx.lineTo(xAt(i), yAt(a.values[i]));
        ctx.lineTo(xAt(frames - 1), yAt(a.values[frames - 1]));
        ctx.lineTo(xAt(frames - 1), padT + ph);
        ctx.closePath();
        ctx.fillStyle = a.color + '14';
        ctx.fill();
        // Line.
        ctx.beginPath();
        ctx.moveTo(xAt(0), yAt(a.values[0]));
        for (let i = stride; i < frames; i += stride) ctx.lineTo(xAt(i), yAt(a.values[i]));
        ctx.lineTo(xAt(frames - 1), yAt(a.values[frames - 1]));
        ctx.strokeStyle = a.color;
        ctx.lineWidth = 1.6;
        ctx.lineJoin = 'round';
        ctx.stroke();
        // Playhead dot.
        ctx.beginPath();
        ctx.arc(xAt(playIdx), yAt(a.values[playIdx] ?? a.values[0]), 2.6, 0, Math.PI * 2);
        ctx.fillStyle = a.color;
        ctx.fill();
      }

      // Playhead line.
      const px = xAt(playIdx);
      ctx.strokeStyle = 'rgba(255,255,255,0.35)';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(px, padT);
      ctx.lineTo(px, padT + ph);
      ctx.stroke();

      // Inset labels.
      ctx.font = '9px Menlo, monospace';
      ctx.fillStyle = 'rgba(92,89,82,1)';
      ctx.textAlign = 'left';
      ctx.fillText(compact(mx), padL, padT + 6);
      ctx.fillText(compact(mn), padL, padT + ph - 1);
      ctx.textAlign = 'right';
      ctx.fillText(`SOL ${runner.playheadSol.toFixed(0)}`, padL + pw, padT + ph - 1);
    };
    raf = requestAnimationFrame(draw);
    return () => cancelAnimationFrame(raf);
  }, [runner, series]);

  return (
    <div className="chart">
      <div className="titlerow">
        <div className="title">{title}</div>
        <div className="unit">{unit}</div>
      </div>
      <div className="legend">
        {legend.map((l, i) => (
          <div className="item" key={i}>
            <span className="dot" style={{ background: l.color, width: 6, height: 6 }} />
            <span>{l.label}</span>
            <span className="val" style={{ color: l.color }}>
              {l.value}
            </span>
          </div>
        ))}
      </div>
      <canvas ref={canvasRef} style={{ opacity: empty ? 0.8 : 1 }} />
    </div>
  );
}
