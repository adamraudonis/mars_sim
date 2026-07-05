import { SimClock } from './clock';
import { EventLog } from './events';
import { History } from './history';
import { LaborPool } from './labor';
import { EnvironmentState, type SimContext, SimModule } from './module';
import { ParameterRegistry } from './params';
import { PowerBus } from './power';
import { SimRandom } from './rng';
import { StoreSet } from './store';

/**
 * Owns the clock, stores, buses, modules and history; advances the world one fixed step at
 * a time. Deterministic for a given (scenario, master seed).
 *
 * Step pipeline: environment ticks first (fills ctx.env) → preTick declarations →
 * power+labor resolve → tick mass exchange → builtin bookkeeping → history commit.
 */
export class SimulationEngine {
  readonly clock: SimClock;
  readonly stores = new StoreSet();
  readonly power = new PowerBus();
  readonly labor = new LaborPool();
  readonly history = new History();
  readonly events = new EventLog();
  readonly params: ParameterRegistry;
  readonly masterSeed: number;

  readonly modules: SimModule[] = [];
  private byId = new Map<string, SimModule>();
  private ctx: SimContext;
  private initialized = false;

  constructor(params: ParameterRegistry, epochUtcMs: number, dtSeconds: number, masterSeed: number) {
    this.params = params;
    this.masterSeed = masterSeed;
    this.clock = new SimClock(epochUtcMs, dtSeconds);
    this.history.configure(this.clock.dtSols);
    const engine = this;
    this.ctx = {
      clock: this.clock,
      stores: this.stores,
      power: this.power,
      labor: this.labor,
      history: this.history,
      events: this.events,
      params: this.params,
      env: new EnvironmentState(),
      get dtSeconds() {
        return engine.clock.dtSeconds;
      },
      get dtHours() {
        return engine.clock.dtHours;
      },
      get dtSols() {
        return engine.clock.dtSols;
      },
    };
  }

  get context(): SimContext {
    return this.ctx;
  }

  add<T extends SimModule>(module: T, id: string): T {
    if (this.byId.has(id)) throw new Error(`duplicate module id '${id}'`);
    module.id = id;
    module.bind(this, new SimRandom(this.masterSeed, id));
    this.modules.push(module);
    this.byId.set(id, module);
    if (this.initialized) module.init(this.ctx);
    return module;
  }

  findById(id: string): SimModule | undefined {
    return this.byId.get(id);
  }

  find<T extends SimModule>(ctor: abstract new (...args: never[]) => T): T | undefined {
    for (const m of this.modules) if (m instanceof ctor) return m as T;
    return undefined;
  }

  initialize(): void {
    if (this.initialized) return;
    this.initialized = true;
    for (const m of this.modules) m.init(this.ctx);
    this.events.log(0, 'milestone', 'Engine', 'Simulation initialized');
  }

  step(): void {
    if (!this.initialized) this.initialize();

    this.power.beginStep();
    this.labor.beginStep();

    for (const m of this.modules) if (m.isEnvironmentProvider && m.active) m.preTick(this.ctx);
    for (const m of this.modules) if (m.isEnvironmentProvider && m.active) m.tick(this.ctx);

    for (const m of this.modules) if (!m.isEnvironmentProvider && m.active) m.preTick(this.ctx);

    this.power.resolve(this.clock.dtHours);
    this.labor.resolve();

    for (const m of this.modules) if (!m.isEnvironmentProvider && m.active) m.tick(this.ctx);

    this.recordBuiltins();
    this.history.commitStep();
    this.clock.advance();
  }

  steps(n: number): void {
    for (let i = 0; i < n; i++) this.step();
  }

  runToSol(sol: number): void {
    let guard = 0;
    while (this.clock.sol < sol && guard++ < 50_000_000) this.step();
  }

  private recordBuiltins(): void {
    const h = this.history;
    h.record('power.offered', 'Generation available', 'kW', this.power.generationOfferedKw);
    h.record('power.used', 'Generation used', 'kW', this.power.generationUsedKw);
    h.record('power.demand', 'Power demand', 'kW', this.power.demandKw);
    h.record('power.unmet', 'Unmet power demand', 'kW', this.power.unmetKw);
    h.record('power.curtailed', 'Curtailed generation', 'kW', this.power.curtailedKw);
    h.record('power.battery_soc', 'Battery state of charge', '%', this.power.battery.soc * 100);
    h.record('labor.crew_available', 'Crew hours available', 'h/step', this.labor.crewHoursAvailable);
    h.record('labor.robot_available', 'Robot hours available', 'h/step', this.labor.robotHoursAvailable);
    h.record('labor.unmet', 'Unmet labor', 'crew-eq h/step', this.labor.unmetCrewEqHours);

    for (const s of this.stores.all) h.record(`store.${s.id}`, s.displayName, 'kg', s.amountKg);

    const env = this.ctx.env;
    h.record('env.tau', 'Optical depth τ', '', env.opticalDepthTau);
    h.record('env.ghi', 'Global horizontal irradiance', 'W/m²', env.globalHorizontalWm2);
    h.record('env.air_temp', 'Air temperature', '°C', env.airTemperatureC);
    h.record('env.pressure', 'Surface pressure', 'Pa', env.pressurePa);
  }

  /** Mass ledger audit: per store, deposited − withdrawn must equal the current amount. */
  conservationError(): number {
    let err = 0;
    for (const s of this.stores.all)
      err += Math.abs(s.totalDepositedKg - s.totalWithdrawnKg - s.amountKg);
    return err;
  }
}
