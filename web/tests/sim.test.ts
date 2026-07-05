import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { SimClock } from '../src/sim/clock';
import { SimRandom } from '../src/sim/rng';
import { Store } from '../src/sim/store';
import { PowerBus, LoadPriority } from '../src/sim/power';
import { LaborPool } from '../src/sim/labor';
import { SimulationEngine } from '../src/sim/engine';
import { ParameterRegistry } from '../src/sim/params';
import { Fidelity, SimModule, type SimContext } from '../src/sim/module';
import { MarsEnvironment } from '../src/sim/modules/environment';
import { IsruPropellantPlant } from '../src/sim/modules/production';
import { Distiller } from '../src/sim/distiller';
import { parseScenario } from '../src/sim/scenario';
import { buildSimulation } from '../src/sim/builder';
import { CrewModule } from '../src/sim/modules/life-support';
import { StarshipFleet } from '../src/sim/modules/logistics';
import { Units } from '../src/sim/units';

const dataDir = join(import.meta.dir, '..', 'public', 'data');

function loadParams(): ParameterRegistry {
  const reg = new ParameterRegistry();
  reg.loadDatabase(JSON.parse(readFileSync(join(dataDir, 'parameters_master.json'), 'utf8')));
  return reg;
}

function loadScenario(file: string) {
  return parseScenario(JSON.parse(readFileSync(join(dataDir, 'scenarios', file), 'utf8')));
}

class DummyModule extends SimModule {
  tick(): void {}
}

describe('clock', () => {
  test('Ls matches Allison & McEwen anchor at J2000', () => {
    const ls = SimClock.computeLs(0);
    expect(ls).toBeGreaterThan(270);
    expect(ls).toBeLessThan(282);
  });

  test('Mars year is ~668.6 sols', () => {
    const clock = new SimClock(Date.UTC(2031, 8, 15), Units.solSeconds / 24);
    const ls0 = clock.ls;
    let steps = 0;
    let prev = ls0;
    let wrapped = false;
    while (steps < 24 * 800) {
      clock.advance();
      steps++;
      if (prev > 300 && clock.ls < 60) wrapped = true;
      if (wrapped && clock.ls >= ls0 && clock.ls < ls0 + 1) break;
      prev = clock.ls;
    }
    expect(steps / 24).toBeGreaterThan(660);
    expect(steps / 24).toBeLessThan(677);
  });

  test('sun geometry sane', () => {
    expect(SimClock.sunElevationDeg(0, 0, 0.5)).toBeGreaterThan(85);
    expect(SimClock.sunElevationDeg(0, 0, 0)).toBeLessThan(-50);
    const winter = SimClock.sunElevationDeg(270, 40, 0.5);
    expect(winter).toBeGreaterThan(15);
    expect(winter).toBeLessThan(35);
  });

  test('Mars-Sun distance spans perihelion/aphelion', () => {
    expect(SimClock.marsSunDistanceAu(251)).toBeCloseTo(1.381, 2);
    expect(SimClock.marsSunDistanceAu(71)).toBeCloseTo(1.666, 2);
  });
});

describe('rng', () => {
  test('streams are independent and deterministic', () => {
    const a1 = new SimRandom(42, 'solar');
    const a2 = new SimRandom(42, 'solar');
    const b = new SimRandom(42, 'environment');
    for (let i = 0; i < 100; i++) expect(a1.next()).toBe(a2.next());
    const a3 = new SimRandom(42, 'solar');
    let differs = false;
    for (let i = 0; i < 20; i++)
      if (Math.abs(a3.next() - b.next()) > 1e-12) {
        differs = true;
        break;
      }
    expect(differs).toBe(true);
  });
});

describe('store', () => {
  test('ledger always balances', () => {
    const rng = new SimRandom(7);
    const store = new Store('test', 'waterPotable', 1000, 100);
    for (let i = 0; i < 10000; i++) {
      if (rng.chance(0.5)) store.deposit(rng.range(0, 60));
      else store.withdraw(rng.range(0, 60));
    }
    expect(store.totalDepositedKg - store.totalWithdrawnKg).toBeCloseTo(store.amountKg, 6);
    expect(store.amountKg).toBeGreaterThanOrEqual(0);
    expect(store.amountKg).toBeLessThanOrEqual(1000);
  });
});

describe('power bus', () => {
  test('critical loads served first', () => {
    const bus = new PowerBus();
    const critical = new DummyModule();
    critical.id = 'c';
    const low = new DummyModule();
    low.id = 'l';

    bus.beginStep();
    bus.offer(100);
    bus.request(critical, 80, LoadPriority.Critical);
    bus.request(low, 80, LoadPriority.Low);
    bus.resolve(1);

    expect(bus.grantedFraction(critical)).toBeCloseTo(1, 9);
    expect(bus.grantedFraction(low)).toBeCloseTo(0.25, 9);
    expect(bus.unmetKw).toBeCloseTo(60, 9);
  });

  test('battery reserve protects critical loads from low-priority drain', () => {
    const bus = new PowerBus();
    bus.battery.capacityKwh = 100;
    bus.battery.energyKwh = 60;
    bus.battery.minSocFraction = 0;
    bus.battery.criticalReserveSoc = 0.5;
    const isru = new DummyModule();
    isru.id = 'isru';

    // Night: no generation. ISRU (Normal) may only drain to 50% SoC → 10 kWh usable.
    bus.beginStep();
    bus.request(isru, 40, LoadPriority.Normal);
    bus.resolve(1);
    expect(bus.grantedFraction(isru)).toBeCloseTo(0.25, 6);
    expect(bus.battery.energyKwh).toBeCloseTo(50, 6);
  });
});

describe('labor pool', () => {
  test('robots substitute at effectiveness ratio, pro-rata within band', () => {
    const pool = new LaborPool();
    const a = new DummyModule();
    a.id = 'a';
    const b = new DummyModule();
    b.id = 'b';
    pool.beginStep();
    pool.setRobotEffectiveness('logistics', 0.5);
    pool.supplyRobotHours(10); // worth 5 crew-eq
    pool.supplyCrewHours(2);
    pool.request(a, 'logistics', 6, 2);
    pool.request(b, 'logistics', 8, 2);
    pool.resolve();
    // Supply = 7 crew-eq vs demand 14 → both get ~0.5.
    expect(pool.grantedFraction(a, 'logistics')).toBeCloseTo(0.5, 2);
    expect(pool.grantedFraction(b, 'logistics')).toBeCloseTo(0.5, 2);
  });
});

describe('isru chemistry', () => {
  function buildPlant() {
    const reg = new ParameterRegistry();
    const engine = new SimulationEngine(reg, Date.UTC(2032, 0, 1), Units.solSeconds / 24, 1);
    const plant = engine.add(new IsruPropellantPlant(), 'isru_plant');
    engine.initialize();
    return plant;
  }

  test('water-based chain matches stoichiometry', () => {
    const plant = buildPlant();
    const [kwh, water, h2] = plant.chainCoefficients(false);
    expect(water).toBeCloseTo(0.489, 2); // 2.25 kg fresh water / kg CH4 at O:F 3.6
    expect(h2).toBe(0);
    expect(kwh).toBeGreaterThan(5.5);
    expect(kwh).toBeLessThan(10.5);
  });

  test('H2-import chain needs no water, quarter kg H2 per kg CH4', () => {
    const plant = buildPlant();
    const [kwh, water, h2] = plant.chainCoefficients(true);
    expect(water).toBeCloseTo(0, 9);
    expect(h2).toBeCloseTo(0.0543, 3);
    const [kwhWater] = plant.chainCoefficients(false);
    expect(kwh).toBeGreaterThan(kwhWater);
  });

  test('refuel-one-Starship anchor', () => {
    const plant = buildPlant();
    const [kwh, water] = plant.chainCoefficients(false);
    const tonnes = 1200;
    expect(tonnes * water).toBeGreaterThan(500); // ~590 t mined water
    expect(tonnes * water).toBeLessThan(680);
    const gwh = (tonnes * 1000 * kwh) / 1e6;
    expect(gwh).toBeGreaterThan(5);
    expect(gwh).toBeLessThan(13);
  });
});

describe('environment', () => {
  function buildEnv(seed: number, fidelity: Fidelity) {
    const reg = new ParameterRegistry();
    const engine = new SimulationEngine(reg, Date.UTC(2032, 0, 1), Units.solSeconds / 24, seed);
    const env = engine.add(new MarsEnvironment(), 'environment');
    env.latitudeDeg = 40;
    env.fidelity = fidelity;
    engine.initialize();
    return engine;
  }

  test('annual insolation in published band', () => {
    const engine = buildEnv(1, Fidelity.L1);
    const sols = Math.floor(Units.solsPerMarsYear);
    let total = 0;
    let best = 0;
    for (let d = 0; d < sols; d++) {
      let day = 0;
      for (let i = 0; i < 24; i++) {
        engine.step();
        day += (engine.context.env.globalHorizontalWm2 / 1000) * (Units.solHours / 24);
      }
      total += day;
      best = Math.max(best, day);
    }
    const mean = total / sols;
    expect(mean).toBeGreaterThan(1.2);
    expect(mean).toBeLessThan(4);
    expect(best).toBeGreaterThan(2.2);
  });

  test('global storms eventually happen and raise tau', () => {
    const engine = buildEnv(3, Fidelity.L2);
    engine.runToSol(668 * 6);
    const tau = engine.history.get('env.tau')!;
    let maxTau = 0;
    for (let i = 0; i < tau.count; i++) maxTau = Math.max(maxTau, tau.at(i));
    expect(maxTau).toBeGreaterThan(3);
  });

  test('deterministic replay', () => {
    const a = buildEnv(7, Fidelity.L2);
    const b = buildEnv(7, Fidelity.L2);
    a.runToSol(700);
    b.runToSol(700);
    const ta = a.history.get('env.tau')!;
    const tb = b.history.get('env.tau')!;
    expect(ta.count).toBe(tb.count);
    for (let i = 0; i < ta.count; i += 13) expect(ta.at(i)).toBe(tb.at(i));
  });
});

describe('distillation', () => {
  test('distilled solar yield is plausible', () => {
    const reg = new ParameterRegistry();
    const result = Distiller.distillSolar(reg, 40, 5, 700);
    const y = result.fittedParams['power_solar.l0_kwh_per_sol_per_kw'];
    expect(y).toBeGreaterThan(3);
    expect(y).toBeLessThan(7);
  });
});

describe('scenario smoke (research DB + shipped scenarios)', () => {
  test('baseline survives into the crewed era', () => {
    const scenario = loadScenario('baseline.json');
    const engine = buildSimulation(scenario, loadParams());
    engine.runToSol(1000);

    expect(engine.conservationError()).toBeLessThan(1e-3);

    const crew = engine.find(CrewModule)!;
    expect(crew.count).toBe(12);
    expect(crew.fatalities).toBe(0);
    expect(crew.healthIndex).toBeGreaterThan(0.6);

    const ppo2 = engine.history.get('hab.ppo2')!;
    for (let i = 800 * 24; i < ppo2.count; i += 24) {
      expect(ppo2.at(i)).toBeGreaterThan(15);
      expect(ppo2.at(i)).toBeLessThan(30);
    }

    const fleet = engine.find(StarshipFleet)!;
    expect(fleet.returnPropellantFraction).toBeGreaterThan(0.3);

    for (const s of engine.history.all)
      for (let i = 0; i < s.count; i += 7) expect(Number.isFinite(s.at(i))).toBe(true);
  }, 120000);

  test('h2-import scenario produces propellant without an ice mine', () => {
    const scenario = loadScenario('h2_import.json');
    const engine = buildSimulation(scenario, loadParams());
    engine.runToSol(500);

    const propT =
      ((engine.stores.get('depot_ch4')?.amountKg ?? 0) + (engine.stores.get('depot_lox')?.amountKg ?? 0)) / 1000;
    expect(propT).toBeGreaterThan(50);
    expect(engine.stores.get('h2_import')!.amountKg).toBeLessThan(35000);
  }, 60000);

  test('deterministic across rebuilds', () => {
    const run = () => {
      const scenario = loadScenario('baseline.json');
      scenario.seed = 7;
      const engine = buildSimulation(scenario, loadParams());
      engine.runToSol(400);
      return engine.history.get('power.offered')!;
    };
    const sa = run();
    const sb = run();
    expect(sa.count).toBe(sb.count);
    for (let i = 0; i < sa.count; i += 17) expect(sa.at(i)).toBe(sb.at(i));
  }, 60000);
});
