/**
 * Precompute a cached Recording for each preset scenario so the site can scrub instantly
 * to any point in the mission (e.g. year 5) with zero client compute. Output:
 * public/data/cache/<scenario>.json. Run at build time (bun run build) and standalone.
 */
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { ParameterRegistry } from '../src/sim/params';
import { parseScenario } from '../src/sim/scenario';
import { recordMission } from '../src/sim/recorder';

const dataDir = join(import.meta.dir, '..', 'public', 'data');
const cacheDir = join(dataDir, 'cache');
mkdirSync(cacheDir, { recursive: true });

const db = JSON.parse(readFileSync(join(dataDir, 'parameters_master.json'), 'utf8'));
const presets = ['baseline.json', 'nuclear.json', 'h2_import.json'];

let totalBytes = 0;
for (const file of presets) {
  // Fresh registry per preset so scenario overrides don't leak between them.
  const params = new ParameterRegistry();
  params.loadDatabase(db);
  const scenario = parseScenario(JSON.parse(readFileSync(join(dataDir, 'scenarios', file), 'utf8')));

  const t0 = performance.now();
  const rec = recordMission(scenario, params, { scenarioFile: file });
  const ms = performance.now() - t0;

  const outPath = join(cacheDir, file);
  const json = JSON.stringify(rec);
  writeFileSync(outPath, json);
  totalBytes += json.length;
  console.log(
    `${file}: ${rec.frames.length} frames @ ${rec.sampleSols} sols, ` +
      `${rec.seriesMeta.length} series, ${rec.events.length} events, ` +
      `${(json.length / 1024).toFixed(0)} KB — recorded in ${ms.toFixed(0)} ms`,
  );
}
console.log(`total cache: ${(totalBytes / 1024 / 1024).toFixed(2)} MB (uncompressed; gzips ~4-5×)`);
