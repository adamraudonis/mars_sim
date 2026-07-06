import type { AppRunner } from '../runner';

export interface AssetRow {
  item: string;
  qty: string;
  massT: number | null;
  spec: string;
  status: string;
  health: 'nominal' | 'degraded' | 'failed' | 'offline';
}

export interface AssetGroup {
  group: string;
  rows: AssetRow[];
}

/** Read a module's keyFigure value by (module id, figure label) from the current frame. */
function fig(runner: AppRunner, moduleId: string, label: string): number {
  const rec = runner.recording;
  const frame = runner.frame;
  if (!rec || !frame) return 0;
  const mi = rec.moduleMeta.findIndex((m) => m.id === moduleId);
  if (mi < 0) return 0;
  const fj = rec.moduleMeta[mi].figures.findIndex(([l]) => l === label);
  if (fj < 0) return 0;
  return frame.modules[mi]?.values[fj] ?? 0;
}

function health(runner: AppRunner, moduleId: string): AssetRow['health'] {
  const rec = runner.recording;
  const frame = runner.frame;
  if (!rec || !frame) return 'nominal';
  const mi = rec.moduleMeta.findIndex((m) => m.id === moduleId);
  return mi >= 0 ? frame.modules[mi]?.health ?? 'nominal' : 'nominal';
}

const n = (x: number, d = 0) => x.toLocaleString('en-US', { maximumFractionDigits: d });

/** Everything currently on the surface, derived live from the mission recording at the playhead. */
export function buildInventory(runner: AppRunner): AssetGroup[] {
  const rec = runner.recording;
  const frame = runner.frame;
  if (!rec || !frame) return [];
  const sol = runner.playheadSol;
  const p = runner.params;

  // Ships present now.
  const present = rec.ships.filter((s) => s.landedSol <= sol && (s.departedSol === null || sol < s.departedSol));
  const roles = present.reduce<Record<string, number>>((acc, s) => {
    const key = s.role === 'crewTransport' ? 'crew' : s.role === 'tankerDepot' ? 'tanker' : 'cargo';
    acc[key] = (acc[key] ?? 0) + 1;
    return acc;
  }, {});
  const shipDryT = p.has('starship.ship_dry_mass_t') ? p.v('starship.ship_dry_mass_t') : 125;

  const groups: AssetGroup[] = [];
  const g = (group: string, rows: AssetRow[]) => {
    const nonEmpty = rows.filter((r) => r.qty !== '0' && r.qty !== '' && !r.qty.startsWith('0 '));
    if (nonEmpty.length) groups.push({ group, rows: nonEmpty });
  };

  const solarArea = fig(runner, 'solar', 'Array area');
  const solarMass = fig(runner, 'solar', 'Array mass');
  const solarKw = fig(runner, 'solar', 'Installed rating');
  const dust = fig(runner, 'solar', 'Dust obscuration');
  const reactors = fig(runner, 'nuclear', 'Units');
  const reactorMass = fig(runner, 'nuclear', 'Plant mass');
  const reactorKw = fig(runner, 'nuclear', 'Output');
  const battKwh = fig(runner, 'battery', 'Capacity');
  const battMass = fig(runner, 'battery', 'Bank mass');
  const battSoc = fig(runner, 'battery', 'State of charge');

  g('Power', [
    {
      item: 'Solar array', qty: solarArea > 0 ? `${n(solarArea)} m²` : '0',
      massT: solarMass, spec: `${n(solarKw)} kW installed · ${n(dust)}% dust`,
      status: health(runner, 'solar') === 'nominal' ? 'Generating' : 'Degraded', health: health(runner, 'solar'),
    },
    {
      item: 'Fission reactor', qty: reactors > 0 ? `${n(reactors)} units` : '0',
      massT: reactorMass, spec: `${n(reactorKw)} kW output`, status: 'Operating', health: health(runner, 'nuclear'),
    },
    {
      item: 'Battery bank', qty: battKwh > 0 ? `${n(battKwh)} kWh` : '0',
      massT: battMass, spec: `${n(battSoc)}% charged`, status: 'Buffering', health: health(runner, 'battery'),
    },
  ]);

  const habVol = fig(runner, 'habitat', 'Volume');
  const designCrew = fig(runner, 'eclss', 'Design crew');
  g('Habitat & life support', [
    {
      item: 'Pressurized habitat', qty: habVol > 0 ? `${n(habVol)} m³` : '0',
      massT: habVol * 0.2, spec: `${n(fig(runner, 'habitat', 'Pressure'), 0)} kPa cabin`,
      status: 'Pressurized', health: health(runner, 'habitat'),
    },
    {
      item: 'ECLSS racks', qty: designCrew > 0 ? `${n(designCrew)}-crew capacity` : '0',
      massT: designCrew * 1.5, spec: `O₂ reserve ${n(fig(runner, 'eclss', 'O₂ reserve'))} kg`,
      status: designCrew > 0 ? 'Regenerating air & water' : '—', health: health(runner, 'eclss'),
    },
  ]);

  const ghArea = fig(runner, 'greenhouse', 'Growing area');
  g('Agriculture', [
    {
      item: 'Greenhouse', qty: ghArea > 0 ? `${n(ghArea)} m²` : '0',
      massT: fig(runner, 'greenhouse', 'System mass'), spec: `${n(fig(runner, 'greenhouse', 'Lighting draw'))} kW LEDs`,
      status: 'Growing', health: health(runner, 'greenhouse'),
    },
  ]);

  const isruCap = fig(runner, 'isru_plant', 'Capacity');
  const mineCap = fig(runner, 'ice_mine', 'Capacity');
  const ch4 = fig(runner, 'depot', 'CH₄');
  const lox = fig(runner, 'depot', 'LOX');
  g('ISRU & propellant', [
    {
      item: 'Propellant plant', qty: isruCap > 0 ? `${n(isruCap / 1000, 1)} t/sol` : '0',
      massT: fig(runner, 'isru_plant', 'Plant mass'), spec: `${n(fig(runner, 'isru_plant', 'Production') / 1000, 2)} t/sol produced`,
      status: 'Producing methalox', health: health(runner, 'isru_plant'),
    },
    {
      item: 'Ice mine', qty: mineCap > 0 ? `${n(mineCap)} kg/sol` : '0',
      massT: fig(runner, 'ice_mine', 'System mass'), spec: `${n(fig(runner, 'ice_mine', 'Production'))} kg/sol mined`,
      status: 'Mining water', health: health(runner, 'ice_mine'),
    },
    {
      item: 'Cryo propellant', qty: ch4 + lox > 0 ? `${n(ch4 + lox)} t stored` : '0',
      massT: ch4 + lox, spec: `${n(ch4)} t CH₄ + ${n(lox)} t LOX · return ${n(fig(runner, 'fleet', 'Return propellant'))}%`,
      status: 'In depot', health: 'nominal',
    },
  ]);

  const robots = fig(runner, 'robots', 'Robots');
  g('Robotics', [
    {
      item: 'Optimus fleet', qty: robots > 0 ? `${n(robots)} robots` : '0',
      massT: fig(runner, 'robots', 'Fleet mass'), spec: `${n(fig(runner, 'robots', 'Charging draw'))} kW charging`,
      status: 'Working', health: health(runner, 'robots'),
    },
  ]);

  g('Transport', [
    {
      item: 'Starship v3', qty: present.length > 0 ? `${present.length} on surface` : '0',
      massT: present.length * shipDryT,
      spec: [roles.cargo && `${roles.cargo} cargo`, roles.crew && `${roles.crew} crew`, roles.tanker && `${roles.tanker} tanker`]
        .filter(Boolean).join(' · ') || '—',
      status: 'Landed', health: 'nominal',
    },
  ]);

  const sparesT = fig(runner, 'maintenance', 'Spares mass');
  g('Logistics', [
    {
      item: 'Spare parts', qty: sparesT > 0 ? `${n(sparesT, 1)} t` : '0',
      massT: sparesT, spec: `${n(fig(runner, 'maintenance', 'Open repairs'))} open repairs`,
      status: 'In inventory', health: health(runner, 'maintenance'),
    },
  ]);

  const crew = fig(runner, 'crew', 'Crew');
  g('Crew', [
    {
      item: 'Astronauts', qty: crew > 0 ? `${n(crew)} people` : '0',
      massT: null, spec: `health ${n(fig(runner, 'crew', 'Health index'))}% · dose ${n(fig(runner, 'crew', 'Avg dose'))} mSv`,
      status: crew > 0 ? 'On surface' : 'In transit', health: fig(runner, 'crew', 'Fatalities') > 0 ? 'failed' : 'nominal',
    },
  ]);

  return groups;
}

export function totalLandedMassT(groups: AssetGroup[]): number {
  return groups.reduce((s, g) => s + g.rows.reduce((t, r) => t + (r.massT ?? 0), 0), 0);
}
