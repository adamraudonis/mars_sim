export function compact(v: number): string {
  const a = Math.abs(v);
  if (!Number.isFinite(v)) return '—';
  if (a >= 1e9) return (v / 1e9).toFixed(1) + 'B';
  if (a >= 1e6) return (v / 1e6).toFixed(1) + 'M';
  if (a >= 1e4) return (v / 1e3).toFixed(1) + 'k';
  if (a >= 100) return v.toFixed(0);
  if (a >= 10) return v.toFixed(1);
  if (a >= 1) return v.toFixed(2);
  if (a === 0) return '0';
  return v.toFixed(2);
}

export const CHART_PALETTE = [
  '#61c7fa', '#ff944d', '#73e07a', '#ed7acc', '#fade61', '#a69efa', '#5ce0d1', '#eba98c',
];

export const COLOR = {
  accent: '#ff7a3d',
  accent2: '#54c7e8',
  good: '#4ade80',
  warn: '#fbbf24',
  bad: '#f87171',
  text: '#c9c5bd',
  textHi: '#f2efe9',
  textDim: '#8b877f',
  textFaint: '#5c5952',
};

export function severityColor(sev: string): string {
  return sev === 'critical' ? COLOR.bad : sev === 'warning' ? COLOR.warn : sev === 'milestone' ? COLOR.good : COLOR.textFaint;
}
