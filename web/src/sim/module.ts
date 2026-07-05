import type { SimClock } from './clock';
import type { EventLog, EventSeverity } from './events';
import type { History } from './history';
import type { LaborPool } from './labor';
import type { ParameterRegistry } from './params';
import type { PowerBus } from './power';
import type { SimRandom } from './rng';
import type { StoreSet } from './store';
import type { SimulationEngine } from './engine';

/**
 * Fidelity ladder. L0 = distilled averages, L1 = analytic daily/seasonal (default),
 * L2 = physics-based sub-sol models. Engine clamps to the module's max.
 */
export enum Fidelity {
  L0 = 0,
  L1 = 1,
  L2 = 2,
}

export type ModuleHealth = 'nominal' | 'degraded' | 'failed' | 'offline';

/** Per-step environmental conditions every module can read. */
export class EnvironmentState {
  sunElevationDeg = 0;
  sunAzimuthDeg = 0;
  directNormalWm2 = 0;
  globalHorizontalWm2 = 0;
  topOfAtmosphereWm2 = 0;
  opticalDepthTau = 0.5;
  globalDustStorm = false;
  airTemperatureC = -55;
  groundTemperatureC = -55;
  pressurePa = 720;
  windSpeedMs = 5;
  surfaceDoseMsvPerSol = 0.66;
}

/** Everything a module needs during a step. */
export interface SimContext {
  clock: SimClock;
  stores: StoreSet;
  power: PowerBus;
  labor: LaborPool;
  history: History;
  events: EventLog;
  params: ParameterRegistry;
  env: EnvironmentState;
  readonly dtSeconds: number;
  readonly dtHours: number;
  readonly dtSols: number;
}

/**
 * Base class for every simulated subsystem. Per step: preTick declares power offers/demands
 * and labor demands; tick exchanges mass at the granted fractions. Modules self-describe
 * (statusLine, keyFigures) so the UI is fully generic.
 */
export abstract class SimModule {
  id = '';
  engine!: SimulationEngine;
  protected rng!: SimRandom;

  fidelity: Fidelity = Fidelity.L1;
  get maxFidelity(): Fidelity {
    return Fidelity.L1;
  }
  get effectiveFidelity(): Fidelity {
    return Math.min(this.fidelity, this.maxFidelity) as Fidelity;
  }

  health: ModuleHealth = 'nominal';
  /** Capacity multiplier from failures/degradation, applied by subclasses. */
  capacityFactor = 1;
  active = true;

  /** Whether this module fills ctx.env; ticked before all others. */
  get isEnvironmentProvider(): boolean {
    return false;
  }

  /**
   * Fraction of time this module's hardware operates (failures accrue per operating hour —
   * dormant equipment does not consume its MTBF).
   */
  get failureDutyCycle(): number {
    return this.active ? 1 : 0;
  }

  get displayName(): string {
    return this.id;
  }

  bind(engine: SimulationEngine, rng: SimRandom): void {
    this.engine = engine;
    this.rng = rng;
  }

  protected get p(): ParameterRegistry {
    return this.engine.params;
  }

  init(_ctx: SimContext): void {}
  preTick(_ctx: SimContext): void {}
  abstract tick(ctx: SimContext): void;

  get statusLine(): string {
    return this.health;
  }

  get keyFigures(): Array<[label: string, value: number, unit: string]> {
    return [];
  }

  protected record(ctx: SimContext, id: string, name: string, unit: string, v: number): void {
    ctx.history.record(id, name, unit, v);
  }

  protected log(ctx: SimContext, severity: EventSeverity, message: string): void {
    ctx.events.log(ctx.clock.sol, severity, this.displayName, message);
  }
}
