import { ParameterRegistry } from './sim/params';
import { parseScenario, type Scenario } from './sim/scenario';
import { recordMission, RecordingReader, type Recording, type RecFrame } from './sim/recorder';
import type { Fidelity } from './sim/module';

export const SPEED_PRESETS = [0.02, 0.1, 0.5, 2, 10, 50];

/**
 * The app is a PLAYER over a precomputed mission Recording, not a live stepper. Presets ship
 * a cached recording (public/data/cache/*.json) so you can scrub straight to year 5 with
 * zero compute. Editing a parameter regenerates the recording client-side (~0.7 s). This
 * makes playback buttery-smooth, background-tab safe, and instantly seekable.
 */
export class AppRunner {
  readonly params = new ParameterRegistry();
  scenarios: Array<{ file: string; name: string; json: unknown }> = [];
  scenario!: Scenario;

  recording: Recording | null = null;
  reader: RecordingReader | null = null;

  playheadSol = 0;
  solsPerSecond = 2;
  paused = false;

  /** Per-module fidelity chosen in the UI (applied on re-run). */
  fidelityOverrides: Record<string, Fidelity> = {};

  /** Params/fidelity differ from the cached recording → offer a re-run. */
  dirty = false;
  recomputing = false;
  recomputeProgress = 0;

  private rebuildListeners: Array<() => void> = [];
  private dirtyListeners: Array<() => void> = [];

  static async create(): Promise<AppRunner> {
    const r = new AppRunner();
    const base = import.meta.env.BASE_URL ?? './';
    const db = await (await fetch(`${base}data/parameters_master.json`)).json();
    r.params.loadDatabase(db);

    const files = ['baseline.json', 'nuclear.json', 'h2_import.json'];
    for (const file of files) {
      try {
        const json = await (await fetch(`${base}data/scenarios/${file}`)).json();
        r.scenarios.push({ file, name: (json as { name?: string }).name ?? file, json });
      } catch {
        /* missing scenario */
      }
    }

    // Track parameter overrides so we can offer a re-run.
    r.params.onChanged(() => r.refreshDirty());

    await r.loadScenario(r.scenarios[0]?.file ?? '');
    return r;
  }

  /** Scenario with the UI's fidelity choices merged in. */
  private effectiveScenario(): Scenario {
    return { ...this.scenario, fidelity: { ...this.scenario.fidelity, ...this.fidelityOverrides } };
  }

  setFidelity(moduleId: string, f: Fidelity): void {
    this.fidelityOverrides[moduleId] = f;
    this.refreshDirty();
  }

  currentFidelity(moduleId: string, fallback: Fidelity): Fidelity {
    return this.fidelityOverrides[moduleId] ?? this.scenario.fidelity[moduleId] ?? fallback;
  }

  get durationSols(): number {
    return this.recording?.durationSols ?? this.scenario?.durationSols ?? 2000;
  }

  onRebuild(fn: () => void): () => void {
    this.rebuildListeners.push(fn);
    return () => {
      this.rebuildListeners = this.rebuildListeners.filter((l) => l !== fn);
    };
  }

  onDirtyChange(fn: () => void): () => void {
    this.dirtyListeners.push(fn);
    return () => {
      this.dirtyListeners = this.dirtyListeners.filter((l) => l !== fn);
    };
  }

  private emitRebuild(): void {
    for (const l of this.rebuildListeners) l();
  }

  /** Recompute dirty accurately: any active override (param or fidelity) vs the cached preset. */
  private refreshDirty(): void {
    if (this.recomputing) return;
    const hasOverride =
      Object.keys(this.fidelityOverrides).length > 0 ||
      this.params.all.some((p) => p.userOverride !== null || p.distilledOverride !== null);
    if (hasOverride !== this.dirty) {
      this.dirty = hasOverride;
      for (const l of this.dirtyListeners) l();
    }
  }

  async loadScenario(file: string): Promise<void> {
    // Loading a preset clears any user/fidelity overrides so the cached recording is valid.
    this.fidelityOverrides = {};
    for (const p of this.params.all) {
      if (p.userOverride !== null) this.params.setUserOverride(p.id, null);
      if (p.distilledOverride !== null) this.params.setDistilledOverride(p.id, null);
    }

    const entry = this.scenarios.find((s) => s.file === file) ?? this.scenarios[0];
    this.scenario = entry ? parseScenario(entry.json) : parseScenario({ name: 'Empty', flights: [] });

    let rec: Recording | null = null;
    if (entry) {
      try {
        const base = import.meta.env.BASE_URL ?? './';
        const res = await fetch(`${base}data/cache/${entry.file}`);
        if (res.ok) rec = (await res.json()) as Recording;
      } catch {
        /* fall through to client-side record */
      }
    }
    if (!rec) rec = recordMission(this.effectiveScenario(), this.params, { scenarioFile: entry?.file ?? null });

    this.setRecording(rec);
    this.dirty = false;
    for (const l of this.dirtyListeners) l();
  }

  /** Re-run the current scenario with current (edited) parameters + fidelity choices. */
  recomputeLive(): void {
    this.recomputing = true;
    this.recomputeProgress = 0;
    // Synchronous but fast (~0.7 s); a brief overlay covers it.
    const rec = recordMission(this.effectiveScenario(), this.params, {
      onProgress: (f) => {
        this.recomputeProgress = f;
      },
    });
    const keepSol = this.playheadSol;
    this.setRecording(rec);
    this.playheadSol = Math.min(keepSol, this.durationSols);
    this.recomputing = false;
    this.dirty = false;
    for (const l of this.dirtyListeners) l();
  }

  private setRecording(rec: Recording): void {
    this.recording = rec;
    this.reader = new RecordingReader(rec);
    this.playheadSol = 0;
    this.emitRebuild();
  }

  /** Advance the playhead for a real-time frame (no sim compute). */
  tick(dtRealSeconds: number): void {
    if (this.paused || this.solsPerSecond <= 0 || !this.recording) return;
    this.playheadSol = Math.min(this.durationSols, this.playheadSol + dtRealSeconds * this.solsPerSecond);
    if (this.playheadSol >= this.durationSols) this.paused = true;
  }

  seek(sol: number): void {
    this.playheadSol = Math.max(0, Math.min(this.durationSols, sol));
  }

  get frame(): RecFrame | null {
    if (!this.reader || !this.recording) return null;
    return this.recording.frames[this.reader.solToIndex(this.playheadSol)];
  }

  /** Events up to the current playhead sol. */
  eventsUpTo(): Recording['events'] {
    if (!this.recording) return [];
    const sol = this.playheadSol;
    // events are chronological
    const evs = this.recording.events;
    let hi = evs.length;
    // binary search for last event with sol <= playhead
    let lo = 0;
    while (lo < hi) {
      const mid = (lo + hi) >> 1;
      if (evs[mid].sol <= sol) lo = mid + 1;
      else hi = mid;
    }
    return evs.slice(0, lo);
  }
}
