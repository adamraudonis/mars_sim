/**
 * Headless mission validation: runs a scenario to completion and prints vitals + outcome.
 * Usage: bun run scripts/run-baseline.ts [scenario] [sols]
 */
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ParameterRegistry } from '../src/sim/params';
import { parseScenario } from '../src/sim/scenario';
import { buildSimulation } from '../src/sim/builder';
import { CrewModule } from '../src/sim/modules/life-support';
import { StarshipFleet } from '../src/sim/modules/logistics';

const scenarioName = process.argv[2] ?? 'baseline.json';
const sols = Number(process.argv[3] ?? 2200);

const dataDir = join(import.meta.dir, '..', 'public', 'data');
const params = new ParameterRegistry();
params.loadDatabase(JSON.parse(readFileSync(join(dataDir, 'parameters_master.json'), 'utf8')));
const scenario = parseScenario(JSON.parse(readFileSync(join(dataDir, 'scenarios', scenarioName), 'utf8')));

const t0 = performance.now();
const engine = buildSimulation(scenario, params);

console.log(`=== ${scenario.name} → sol ${sols} ===`);
console.log('sol  ppO2  ppCO2  water_t  food_t  o2res_t  crew health  ch4_t  lox_t  tau');
let nextSample = 0;
while (engine.clock.sol < sols) {
  engine.step();
  if (engine.clock.sol >= nextSample) {
    nextSample += 200;
    const h = engine.history;
    const g = (id: string) => h.get(id)?.latest ?? 0;
    console.log(
      [
        engine.clock.sol.toFixed(0).padStart(4),
        g('hab.ppo2').toFixed(1).padStart(5),
        g('hab.ppco2').toFixed(2).padStart(6),
        (g('store.water_potable') / 1000).toFixed(1).padStart(8),
        (g('store.food') / 1000).toFixed(1).padStart(7),
        (g('store.o2_reserve') / 1000).toFixed(1).padStart(8),
        g('crew.count').toFixed(0).padStart(5),
        g('crew.health').toFixed(2).padStart(6),
        g('depot.ch4').toFixed(0).padStart(6),
        g('depot.lox').toFixed(0).padStart(6),
        g('env.tau').toFixed(1).padStart(5),
      ].join(' '),
    );
  }
}
const elapsed = performance.now() - t0;

const crew = engine.find(CrewModule)!;
const fleet = engine.find(StarshipFleet)!;
console.log('\n--- OUTCOME ---');
console.log(`wall time: ${(elapsed / 1000).toFixed(2)} s for ${engine.clock.stepCount} steps (${(engine.clock.stepCount / (elapsed / 1000) / 1000).toFixed(0)}k steps/s)`);
console.log(`crew: ${crew.count} alive, ${crew.fatalities} fatalities, health ${(crew.healthIndex * 100).toFixed(0)}%`);
console.log(`ships departed: ${fleet.ships.filter((s) => s.departed).map((s) => s.name).join(', ') || 'none'}`);
console.log(`conservation error: ${engine.conservationError().toExponential(2)} kg`);
console.log(`events: ${engine.events.count}`);
const criticals = engine.events.events.filter((e) => e.severity === 'critical');
console.log(`criticals (${criticals.length}):`);
for (const e of criticals.slice(0, 12)) console.log(`  sol ${e.sol.toFixed(1)} — ${e.source}: ${e.message}`);
