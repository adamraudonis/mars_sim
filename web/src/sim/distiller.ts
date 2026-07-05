import { SimulationEngine } from './engine';
import { Fidelity, SimModule, type SimContext } from './module';
import type { ParameterRegistry } from './params';
import { Units } from './units';
import { MarsEnvironment } from './modules/environment';
import { Habitat } from './modules/life-support';
import { SolarFarm, NuclearPlant } from './modules/power-modules';
import { Greenhouse } from './modules/production';

export interface DistillationResult {
  moduleId: string;
  summary: string;
  fittedParams: Record<string, number>;
  fitErrorPercent: number;
}

/** Generic labor source for isolated sub-sims (a real base has workers). */
class LaborSupplier extends SimModule {
  hoursPerSol = 100;
  override get displayName() {
    return 'Harness labor supply';
  }
  override preTick(ctx: SimContext): void {
    ctx.labor.supplyCrewHours(this.hoursPerSol * ctx.dtSols);
  }
  override tick(): void {}
}

/**
 * Runs a subsystem at max fidelity in an isolated sub-simulation, fits the distilled (L0)
 * coefficients, and installs them as parameter overrides — the "go deep, then average"
 * workflow.
 */
export const Distiller = {
  /** L2 solar over a Mars year (real dust/storm process) → mean kWh/sol per installed kW. */
  distillSolar(params: ParameterRegistry, latitudeDeg: number, seed = 1234, sols: number = Units.solsPerMarsYear): DistillationResult {
    const engine = new SimulationEngine(params, Date.UTC(2032, 0, 1), Units.solSeconds / 24, seed);
    const env = engine.add(new MarsEnvironment(), 'environment');
    env.latitudeDeg = latitudeDeg;
    env.fidelity = Fidelity.L2;
    const farm = engine.add(new SolarFarm(), 'solar');
    farm.arrayAreaM2 = 1000;
    farm.fidelity = Fidelity.L2;
    engine.add(new LaborSupplier(), 'harness_labor');
    engine.initialize();
    engine.runToSol(sols);

    const series = engine.history.get('solar.output')!;
    let sum = 0;
    for (let i = 0; i < series.count; i++) sum += series.at(i);
    const meanKw = sum / Math.max(1, series.count);
    const perKw = (meanKw * Units.solHours) / Math.max(1e-9, farm.installedKwRating);

    let variance = 0;
    for (let i = 0; i < series.count; i++) variance += (series.at(i) - meanKw) ** 2;
    const cv = meanKw > 0 ? Math.sqrt(variance / Math.max(1, series.count)) / meanKw : 0;

    params.setDistilledOverride('power_solar.l0_kwh_per_sol_per_kw', perKw);
    return {
      moduleId: 'solar',
      summary:
        `L2 run over ${sols.toFixed(0)} sols at ${latitudeDeg.toFixed(0)}°N: ${perKw.toFixed(2)} kWh/sol per installed kW ` +
        `(discarded variability CV=${(cv * 100).toFixed(0)}% — night/storms move into the battery question)`,
      fittedParams: { 'power_solar.l0_kwh_per_sol_per_kw': perKw },
      fitErrorPercent: cv * 100,
    };
  },

  /** L1 greenhouse batch model → distilled L0 utilization (achieved / nominal). */
  distillGreenhouse(params: ParameterRegistry, seed = 1234, sols = 500): DistillationResult {
    const engine = new SimulationEngine(params, Date.UTC(2032, 0, 1), Units.solSeconds / 24, seed);
    engine.add(new MarsEnvironment(), 'environment');
    const hab = engine.add(new Habitat(), 'habitat');
    const gh = engine.add(new Greenhouse(), 'greenhouse');
    gh.growingAreaM2 = 200;
    gh.fidelity = Fidelity.L1;
    const nuc = engine.add(new NuclearPlant(), 'nuclear');
    nuc.units = 100; // unlimited support: measure the crop model, not power/labor
    engine.add(new LaborSupplier(), 'harness_labor');
    engine.initialize();
    hab.addVolume(engine.context, 1000, true);
    const water = engine.stores.getOrCreate('water_potable', 'waterPotable', 0);
    water.addCapacity(200000);
    water.deposit(200000);
    engine.runToSol(sols);

    const series = engine.history.get('greenhouse.kcal_per_sol')!;
    const skip = Math.floor(series.count * (100 / sols));
    let sum = 0;
    let n = 0;
    for (let i = skip; i < series.count; i++) {
      sum += series.at(i);
      n++;
    }
    const kcalPerM2Sol = sum / Math.max(1, n) / 200;

    // Fit the UTILIZATION, not the nominal rate — the nominal is the batch model's input,
    // and overriding it would make repeated distillations self-referential.
    const nominal = params.v('food.kcal_per_m2_sol');
    const utilization = nominal > 0 ? Math.max(0, Math.min(1.2, kcalPerM2Sol / nominal)) : 1;
    params.setDistilledOverride('food.l0_utilization', utilization);
    return {
      moduleId: 'greenhouse',
      summary:
        `L1 batch run over ${sols.toFixed(0)} sols: sustained ${kcalPerM2Sol.toFixed(1)} of nominal ` +
        `${nominal.toFixed(1)} kcal/m²/sol → L0 utilization ${(utilization * 100).toFixed(0)}% (staggering gaps + batch losses)`,
      fittedParams: { 'food.l0_utilization': utilization },
      fitErrorPercent: 0,
    };
  },
};
