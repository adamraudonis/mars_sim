/**
 * One tracked series, sampled every engine step (uniform dt): time axis is implicit,
 * sol(i) = i · dtSols. Charts downsample on draw.
 */
export class TimeSeries {
  readonly id: string;
  displayName: string;
  unit: string;
  dtSols = 0;

  private values = new Float32Array(1024);
  count = 0;

  constructor(id: string, displayName: string, unit: string) {
    this.id = id;
    this.displayName = displayName;
    this.unit = unit;
  }

  append(v: number): void {
    if (this.count === this.values.length) {
      const next = new Float32Array(this.values.length * 2);
      next.set(this.values);
      this.values = next;
    }
    this.values[this.count++] = v;
  }

  at(i: number): number {
    return this.values[i];
  }

  solAt(i: number): number {
    return i * this.dtSols;
  }

  get latest(): number {
    return this.count > 0 ? this.values[this.count - 1] : 0;
  }

  range(from: number, to: number): [number, number] {
    let mn = Infinity;
    let mx = -Infinity;
    for (let i = Math.max(0, from); i < Math.min(this.count, to); i++) {
      const v = this.values[i];
      if (v < mn) mn = v;
      if (v > mx) mx = v;
    }
    if (mn > mx) return [0, 1];
    return [mn, mx];
  }
}

/**
 * All recorded series. Modules record named values each step; series auto-create on first
 * use and are zero-padded for steps before creation so every series stays step-aligned.
 */
export class History {
  private series = new Map<string, TimeSeries>();
  readonly all: TimeSeries[] = [];
  private dtSols = 0;
  private step = 0;

  private pending = new Map<string, number>();
  private pendingMeta = new Map<string, { name: string; unit: string }>();

  configure(dtSols: number): void {
    this.dtSols = dtSols;
  }

  get(id: string): TimeSeries | undefined {
    return this.series.get(id);
  }

  getOrCreate(id: string, displayName: string, unit: string): TimeSeries {
    let s = this.series.get(id);
    if (s) return s;
    s = new TimeSeries(id, displayName, unit);
    s.dtSols = this.dtSols;
    for (let i = 0; i < this.step; i++) s.append(0);
    this.series.set(id, s);
    this.all.push(s);
    return s;
  }

  /** Record a value for this step (last write wins within a step). */
  record(id: string, displayName: string, unit: string, value: number): void {
    this.pending.set(id, value);
    if (!this.series.has(id) && !this.pendingMeta.has(id))
      this.pendingMeta.set(id, { name: displayName, unit });
  }

  /** Close the step: flush pending; series not written this step repeat their last value. */
  commitStep(): void {
    for (const [id, meta] of this.pendingMeta) this.getOrCreate(id, meta.name, meta.unit);
    this.pendingMeta.clear();

    for (const s of this.all) {
      const v = this.pending.get(s.id);
      s.append(v !== undefined ? v : s.latest);
    }
    this.pending.clear();
    this.step++;
  }
}
