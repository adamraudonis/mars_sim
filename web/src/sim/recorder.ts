import { SimulationEngine } from './engine';
import type { ParameterRegistry } from './params';
import type { Scenario } from './scenario';
import { buildSimulation } from './builder';
import type { SimEvent } from './events';
import type { ModuleHealth } from './module';
import { CrewModule, Habitat } from './modules/life-support';
import { SolarFarm, NuclearPlant } from './modules/power-modules';
import { Greenhouse, IceMine, IsruPropellantPlant } from './modules/production';
import { StarshipFleet, RobotFleet, LaunchCampaign } from './modules/logistics';

/**
 * A precomputed mission "recording": everything the UI needs to render any moment of the
 * mission instantly, without re-simulating. Presets ship one of these so you can scrub
 * straight to year 5. Custom parameter edits regenerate one client-side (~0.5 s).
 */

export interface RecEnv {
  sunEl: number;
  sunAz: number;
  tau: number;
  storm: boolean;
  airT: number;
  ghi: number;
}

/** Derived structure counts the 3D view reconciles to (see scene reconciler). */
export interface RecScene {
  habModules: number;
  greenhouses: number;
  growingAreaM2: number;
  lightsKw: number;
  solarTables: number;
  solarDust: number;
  reactors: number;
  isruPlant: boolean;
  depotTanks: number;
  rigs: number;
  minePit: boolean;
  crates: number;
  crew: number;
  robots: number;
}

export interface RecModuleState {
  status: string;
  health: ModuleHealth;
  values: number[]; // parallel to moduleMeta[i].figures
}

export interface RecFrame {
  sol: number;
  env: RecEnv;
  scene: RecScene;
  modules: RecModuleState[]; // parallel to moduleMeta
}

export interface RecShip {
  name: string;
  role: string;
  contributesHabitatVolume: boolean;
  landedSol: number;
  departedSol: number | null;
}

export interface Recording {
  version: 1;
  scenarioName: string;
  scenarioFile: string | null;
  durationSols: number;
  sampleSols: number;
  latitudeDeg: number;
  seriesMeta: { id: string; name: string; unit: string }[];
  series: number[][]; // parallel to seriesMeta; each length = frames.length
  moduleMeta: { id: string; name: string; maxFidelity: number; figures: [string, string][] }[]; // [label, unit]
  frames: RecFrame[];
  ships: RecShip[];
  events: SimEvent[];
}

/** How often to sample display frames + series, in sols. Coarser = smaller cache. */
export const DEFAULT_SAMPLE_SOLS = 2;

function round4(x: number): number {
  if (!Number.isFinite(x)) return 0;
  if (x === 0) return 0;
  const m = Math.pow(10, 3 - Math.floor(Math.log10(Math.abs(x))));
  return Math.round(x * m) / m;
}

/**
 * Run a full mission and capture a Recording. Deterministic for (scenario, seed, params).
 * onProgress(fraction) is called occasionally so a client-side regenerate can show a bar.
 */
export function recordMission(
  scenario: Scenario,
  params: ParameterRegistry,
  opts: { sampleSols?: number; scenarioFile?: string | null; onProgress?: (f: number) => void } = {},
): Recording {
  const sampleSols = opts.sampleSols ?? DEFAULT_SAMPLE_SOLS;
  const engine = buildSimulation(scenario, params);
  const duration = scenario.durationSols;

  const fleet = engine.find(StarshipFleet);
  const hab = engine.find(Habitat);
  const solar = engine.find(SolarFarm);
  const nuclear = engine.find(NuclearPlant);
  const greenhouse = engine.find(Greenhouse);
  const isru = engine.find(IsruPropellantPlant);
  const mine = engine.find(IceMine);
  const crew = engine.find(CrewModule);
  const robots = engine.find(RobotFleet);
  const campaign = engine.find(LaunchCampaign);

  const moduleMeta = engine.modules.map((m) => ({
    id: m.id,
    name: m.displayName,
    maxFidelity: m.maxFidelity,
    figures: m.keyFigures.map(([label, , unit]) => [label, unit] as [string, string]),
  }));

  const frames: RecFrame[] = [];
  const seriesSamples = new Map<string, number[]>();

  const shipVolPerShip =
    params.v('starship.pressurized_volume_m3') * params.v('starship.habitat_usable_fraction');

  const sample = () => {
    const env = engine.context.env;
    const shipHabVol =
      (fleet?.ships.filter((s) => !s.departed && s.contributesHabitatVolume).length ?? 0) * shipVolPerShip;
    const depotT =
      ((engine.stores.get('depot_ch4')?.amountKg ?? 0) + (engine.stores.get('depot_lox')?.amountKg ?? 0)) / 1000;

    const scene: RecScene = {
      habModules: Math.ceil(Math.max(0, (hab?.pressurizedVolumeM3 ?? 0) - shipHabVol) / 500),
      greenhouses: Math.ceil((greenhouse?.growingAreaM2 ?? 0) / 50),
      growingAreaM2: greenhouse?.growingAreaM2 ?? 0,
      lightsKw: engine.history.get('greenhouse.power')?.latest ?? 0,
      solarTables: Math.min(1200, Math.ceil((solar?.arrayAreaM2 ?? 0) / 186)),
      solarDust: solar?.dustFraction ?? 0,
      reactors: nuclear?.units ?? 0,
      isruPlant: (isru?.capacityKgPerSol ?? 0) > 0,
      depotTanks: Math.ceil(depotT / 250),
      rigs: Math.ceil((mine?.capacityKgPerSol ?? 0) / 1500),
      minePit: (mine?.capacityKgPerSol ?? 0) > 0,
      crates: Math.min(24, (campaign?.deploymentsPending ?? 0) * 2),
      crew: crew?.count ?? 0,
      robots: robots?.count ?? 0,
    };

    frames.push({
      sol: round4(engine.clock.sol),
      env: {
        sunEl: round4(env.sunElevationDeg),
        sunAz: round4(env.sunAzimuthDeg),
        tau: round4(env.opticalDepthTau),
        storm: env.globalDustStorm,
        airT: round4(env.airTemperatureC),
        ghi: round4(env.globalHorizontalWm2),
      },
      scene,
      modules: engine.modules.map((m) => ({
        status: m.statusLine,
        health: m.health,
        values: m.keyFigures.map(([, v]) => round4(v)),
      })),
    });

    for (const s of engine.history.all) {
      let arr = seriesSamples.get(s.id);
      if (!arr) {
        arr = [];
        seriesSamples.set(s.id, arr);
      }
      // pad late-created series with leading zeros to align with frame count
      while (arr.length < frames.length - 1) arr.push(0);
      arr.push(round4(s.latest));
    }
  };

  sample(); // sol 0
  let nextSampleSol = sampleSols;
  const guard = 60_000_000;
  let steps = 0;
  while (engine.clock.sol < duration && steps++ < guard) {
    engine.step();
    if (engine.clock.sol >= nextSampleSol) {
      sample();
      nextSampleSol += sampleSols;
      if (opts.onProgress && frames.length % 40 === 0) opts.onProgress(engine.clock.sol / duration);
    }
  }

  // Ships timeline: landing sols from the fleet, departure sols from the event log.
  const departedSolByName = new Map<string, number>();
  for (const e of engine.events.events) {
    const m = /^(.*?) departed for Earth/.exec(e.message);
    if (m) departedSolByName.set(m[1], round4(e.sol));
  }
  const ships: RecShip[] = (fleet?.ships ?? []).map((s) => ({
    name: s.name,
    role: s.role,
    contributesHabitatVolume: s.contributesHabitatVolume,
    landedSol: round4(s.landedSol),
    departedSol: departedSolByName.get(s.name) ?? null,
  }));

  // Normalize series to full length.
  const seriesMeta: { id: string; name: string; unit: string }[] = [];
  const series: number[][] = [];
  for (const s of engine.history.all) {
    const arr = seriesSamples.get(s.id) ?? [];
    while (arr.length < frames.length) arr.push(arr.length ? arr[arr.length - 1] : 0);
    seriesMeta.push({ id: s.id, name: s.displayName, unit: s.unit });
    series.push(arr);
  }

  return {
    version: 1,
    scenarioName: scenario.name,
    scenarioFile: opts.scenarioFile ?? null,
    durationSols: duration,
    sampleSols,
    latitudeDeg: scenario.latitudeDeg,
    seriesMeta,
    series,
    moduleMeta,
    frames,
    ships,
    events: engine.events.events.map((e) => ({ ...e, sol: round4(e.sol) })),
  };
}

/** Convenience index for reading a recording's series by id. */
export class RecordingReader {
  private seriesIndex = new Map<string, number>();

  constructor(readonly rec: Recording) {
    rec.seriesMeta.forEach((m, i) => this.seriesIndex.set(m.id, i));
  }

  get frameCount(): number {
    return this.rec.frames.length;
  }

  solToIndex(sol: number): number {
    return Math.max(0, Math.min(this.rec.frames.length - 1, Math.round(sol / this.rec.sampleSols)));
  }

  series(id: string): { values: number[]; name: string; unit: string } | null {
    const i = this.seriesIndex.get(id);
    if (i === undefined) return null;
    return { values: this.rec.series[i], name: this.rec.seriesMeta[i].name, unit: this.rec.seriesMeta[i].unit };
  }
}
