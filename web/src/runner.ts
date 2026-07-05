import { SimulationEngine } from './sim/engine';
import { ParameterRegistry } from './sim/params';
import { parseScenario, type Scenario } from './sim/scenario';
import { buildSimulation } from './sim/builder';

export const SPEED_PRESETS = [0.02, 0.1, 0.5, 2, 10, 50];

/**
 * Owns the simulation for the app: loads the sourced parameter DB + scenarios, builds the
 * engine, advances it in timelapse decoupled from render rate. Everything visual reads
 * from `engine`; nothing visual writes except via the registry and scenario reloads.
 */
export class AppRunner {
  readonly params = new ParameterRegistry();
  scenarios: Array<{ file: string; name: string; json: unknown }> = [];
  scenario!: Scenario;
  engine!: SimulationEngine;

  solsPerSecond = 0.5;
  paused = false;

  private stepDebt = 0;
  private rebuildListeners: Array<() => void> = [];

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
        // missing scenario file — skip
      }
    }
    r.loadScenario(r.scenarios[0]?.file ?? '');
    return r;
  }

  onRebuild(listener: () => void): () => void {
    this.rebuildListeners.push(listener);
    return () => {
      this.rebuildListeners = this.rebuildListeners.filter((l) => l !== listener);
    };
  }

  loadScenario(file: string): void {
    const entry = this.scenarios.find((s) => s.file === file) ?? this.scenarios[0];
    this.scenario = entry
      ? parseScenario(entry.json)
      : parseScenario({ name: 'Empty', flights: [] });
    this.rebuild();
  }

  rebuild(): void {
    this.engine = buildSimulation(this.scenario, this.params);
    this.stepDebt = 0;
    for (const l of this.rebuildListeners) l();
  }

  /** Advance sim steps for a real-time frame; caps work at ~25 ms to never hitch the UI. */
  tick(dtRealSeconds: number): void {
    if (!this.engine || this.paused || this.solsPerSecond <= 0) return;
    this.stepDebt += (dtRealSeconds * this.solsPerSecond) / this.engine.clock.dtSols;
    let steps = Math.floor(this.stepDebt);
    if (steps <= 0) return;
    steps = Math.min(steps, 4000);
    this.stepDebt -= steps;

    const t0 = performance.now();
    for (let i = 0; i < steps; i++) {
      this.engine.step();
      if (performance.now() - t0 > 25) {
        this.stepDebt = 0;
        break;
      }
    }
  }
}
