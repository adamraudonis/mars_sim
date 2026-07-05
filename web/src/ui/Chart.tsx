import { useEffect, useRef, useState } from 'react';
import type { AppRunner } from '../runner';
import { compact } from './format';

export interface SeriesSpec {
  id: string;
  color: string;
  label?: string;
}

/**
 * Multi-series telemetry chart (canvas, x = mission sol): translucent area fills, sol
 * gridlines at round intervals, inset min/max labels, live-value legend chips. Draws
 * imperatively on a timer — React never re-renders at chart rate.
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
      if (t - last < 300) return; // ~3 Hz refresh
      last = t;
      const canvas = canvasRef.current;
      const engine = runner.engine;
      if (!canvas || !engine) return;

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

      // Range across series.
      let mn = Infinity;
      let mx = -Infinity;
      let maxCount = 0;
      let anyData = false;
      let unitFound = '';
      const legendNext: Array<{ label: string; value: string; color: string }> = [];
      for (const s of series) {
        const ts = engine.history.get(s.id);
        if (ts && ts.count > 1) {
          anyData = true;
          maxCount = Math.max(maxCount, ts.count);
          const [a, b] = ts.range(0, ts.count);
          mn = Math.min(mn, a);
          mx = Math.max(mx, b);
          if (!unitFound && ts.unit) unitFound = ts.unit;
          legendNext.push({ label: s.label ?? ts.displayName, value: compact(ts.latest), color: s.color });
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
        const pad0 = Math.abs(mx) * 0.1 + 0.5;
        mn -= pad0;
        mx += pad0;
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
      const lastSol = engine.clock.sol;
      if (lastSol > 1) {
        const raw = lastSol / 4;
        const mag = Math.pow(10, Math.floor(Math.log10(Math.max(1e-6, raw))));
        const norm = raw / mag;
        const step = (norm < 1.5 ? 1 : norm < 3.5 ? 2 : norm < 7.5 ? 5 : 10) * mag;
        for (let sol = step; sol < lastSol; sol += step) {
          const x = padL + (sol / lastSol) * pw;
          ctx.beginPath();
          ctx.moveTo(x, padT);
          ctx.lineTo(x, padT + ph);
          ctx.stroke();
        }
      }

      // Series (first drawn last, on top).
      for (let si = series.length - 1; si >= 0; si--) {
        const s = series[si];
        const ts = engine.history.get(s.id);
        if (!ts || ts.count < 2) continue;
        const stride = Math.max(1, Math.floor(ts.count / Math.max(64, pw)));
        const pts: Array<[number, number]> = [];
        for (let i = 0; i < ts.count; i += stride) {
          let v = ts.at(i);
          const end = Math.min(ts.count, i + stride);
          for (let k = i + 1; k < end; k++) if (Math.abs(ts.at(k) - mn) > Math.abs(v - mn)) v = ts.at(k);
          const x = padL + (i / (maxCount - 1)) * pw;
          const y = padT + ph - ((v - mn) / (mx - mn)) * ph;
          pts.push([x, Math.max(padT, Math.min(padT + ph, y))]);
        }
        if (pts.length < 2) continue;

        // Area fill.
        ctx.beginPath();
        ctx.moveTo(pts[0][0], padT + ph);
        for (const [x, y] of pts) ctx.lineTo(x, y);
        ctx.lineTo(pts[pts.length - 1][0], padT + ph);
        ctx.closePath();
        ctx.fillStyle = s.color + '1a';
        ctx.fill();

        // Line.
        ctx.beginPath();
        ctx.moveTo(pts[0][0], pts[0][1]);
        for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
        ctx.strokeStyle = s.color;
        ctx.lineWidth = 1.7;
        ctx.lineJoin = 'round';
        ctx.stroke();

        if (si === 0) {
          ctx.beginPath();
          ctx.arc(pts[pts.length - 1][0], pts[pts.length - 1][1], 2.4, 0, Math.PI * 2);
          ctx.fillStyle = s.color;
          ctx.fill();
        }
      }

      // Inset labels.
      ctx.font = '9px Menlo, monospace';
      ctx.fillStyle = 'rgba(92,89,82,1)';
      ctx.textAlign = 'left';
      ctx.fillText(compact(mx), padL, padT + 6);
      ctx.fillText(compact(mn), padL, padT + ph - 1);
      ctx.textAlign = 'right';
      ctx.fillText(`SOL ${lastSol.toFixed(0)}`, padL + pw, padT + ph - 1);
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
