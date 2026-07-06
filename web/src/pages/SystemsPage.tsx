import { useMemo } from 'react';
import { ReactFlow, Background, Controls, Handle, Position, MarkerType, type Node, type Edge, type NodeProps } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { AppRunner } from '../runner';
import { useTick } from '../ui/panels';

const RES = {
  power: '#fade61', o2: '#61c7fa', co2: '#ff944d', water: '#5ce0d1',
  propellant: '#a69efa', food: '#73e07a', labor: '#ed7acc', spares: '#8b877f',
  env: '#eba98c', heat: '#f87171',
};

interface SysData extends Record<string, unknown> {
  label: string;
  sub?: string;
  accent?: string;
  live?: string;
}

function SysNode({ data }: NodeProps) {
  const d = data as SysData;
  return (
    <div className="sys-node" style={{ borderColor: d.accent ?? 'rgba(255,255,255,0.16)' }}>
      <Handle type="target" position={Position.Left} />
      <div className="sys-node-title" style={{ color: d.accent }}>{d.label}</div>
      {d.sub && <div className="sys-node-sub">{d.sub}</div>}
      {d.live && <div className="sys-node-live mono">{d.live}</div>}
      <Handle type="source" position={Position.Right} />
    </div>
  );
}
const nodeTypes = { sys: SysNode };

function edge(id: string, source: string, target: string, res: keyof typeof RES, label?: string, animated = true): Edge {
  return {
    id, source, target, animated, label,
    style: { stroke: RES[res], strokeWidth: 2 },
    labelStyle: { fill: RES[res], fontSize: 10, fontFamily: 'Menlo, monospace' },
    labelBgStyle: { fill: '#0b0e14', fillOpacity: 0.8 },
    markerEnd: { type: MarkerType.ArrowClosed, color: RES[res] },
  };
}

export function SystemsPage({ runner }: { runner: AppRunner }) {
  useTick(400);
  const reader = runner.reader;
  const idx = reader ? reader.solToIndex(runner.playheadSol) : 0;
  const live = (id: string, unit: string, digits = 0): string => {
    const s = reader?.series(id);
    if (!s) return '';
    return `${(s.values[idx] ?? 0).toLocaleString('en-US', { maximumFractionDigits: digits })} ${unit}`;
  };

  // ---------- Top-level system map ----------
  const topNodes: Node[] = useMemo(() => ([
    { id: 'env', type: 'sys', position: { x: 0, y: 220 }, data: { label: 'Mars environment', sub: 'sun · dust · temp · ice · radiation', accent: RES.env, live: live('env.tau', 'τ', 2) } },
    { id: 'solar', type: 'sys', position: { x: 260, y: 40 }, data: { label: 'Solar farm', sub: 'PV → power', accent: RES.power, live: live('solar.output', 'kW') } },
    { id: 'nuclear', type: 'sys', position: { x: 260, y: 150 }, data: { label: 'Fission plant', sub: 'reactors → power', accent: RES.power, live: live('nuclear.output', 'kW') } },
    { id: 'mine', type: 'sys', position: { x: 260, y: 300 }, data: { label: 'Ice mine', sub: 'excavate → water', accent: RES.water, live: live('icemine.production', 'kg/sol') } },
    { id: 'power', type: 'sys', position: { x: 540, y: 90 }, data: { label: 'Power bus', sub: 'priority triage + battery', accent: RES.power, live: live('power.used', 'kW') } },
    { id: 'battery', type: 'sys', position: { x: 540, y: 210 }, data: { label: 'Battery', sub: 'day/night buffer', accent: RES.power, live: live('power.battery_soc', '% SoC') } },
    { id: 'water', type: 'sys', position: { x: 540, y: 330 }, data: { label: 'Water store', sub: 'potable + feedstock', accent: RES.water, live: live('store.water_potable', 'kg') } },
    { id: 'eclss', type: 'sys', position: { x: 820, y: 30 }, data: { label: 'ECLSS', sub: 'air + water loops', accent: RES.o2, live: live('eclss.o2_production', 'kg/sol O₂') } },
    { id: 'greenhouse', type: 'sys', position: { x: 820, y: 150 }, data: { label: 'Greenhouse', sub: 'LED crops', accent: RES.food, live: live('greenhouse.kcal_per_sol', 'kcal/sol') } },
    { id: 'isru', type: 'sys', position: { x: 820, y: 270 }, data: { label: 'ISRU plant', sub: 'CO₂ + H₂O → CH₄ + O₂', accent: RES.propellant, live: live('isru.production', 'kg/sol') } },
    { id: 'robots', type: 'sys', position: { x: 820, y: 390 }, data: { label: 'Robot fleet', sub: 'labor', accent: RES.labor, live: live('robots.hours', 'h/sol') } },
    { id: 'habitat', type: 'sys', position: { x: 1100, y: 60 }, data: { label: 'Habitat + Crew', sub: 'breathe · eat · work', accent: RES.o2, live: live('hab.ppo2', 'kPa ppO₂', 1) } },
    { id: 'depot', type: 'sys', position: { x: 1100, y: 250 }, data: { label: 'Propellant depot', sub: 'cryo CH₄ + LOX', accent: RES.propellant, live: live('depot.total', 't') } },
    { id: 'starship', type: 'sys', position: { x: 1100, y: 370 }, data: { label: 'Return Starship', sub: 'fuelled to leave', accent: RES.propellant, live: live('fleet.return_prop', '% ready') } },
    { id: 'maint', type: 'sys', position: { x: 820, y: 500 }, data: { label: 'Maintenance', sub: 'spares + repairs', accent: RES.spares, live: live('maint.queue', 'open') } },
    { id: 'mission', type: 'sys', position: { x: 1100, y: 500 }, data: { label: 'Mission control', sub: 'unmet-power failure budget', accent: RES.heat, live: live('mission.brownout_hours', 'h brownout', 1) } },
    // eslint-disable-next-line react-hooks/exhaustive-deps
  ]), [idx]);

  const topEdges: Edge[] = useMemo(() => ([
    edge('e-env-solar', 'env', 'solar', 'env', 'sunlight'),
    edge('e-env-isru', 'env', 'isru', 'co2', 'CO₂'),
    edge('e-env-mine', 'env', 'mine', 'env', 'ice'),
    edge('e-env-hab', 'env', 'habitat', 'heat', 'radiation'),
    edge('e-solar-power', 'solar', 'power', 'power'),
    edge('e-nuclear-power', 'nuclear', 'power', 'power'),
    edge('e-power-battery', 'power', 'battery', 'power'),
    edge('e-power-eclss', 'power', 'eclss', 'power'),
    edge('e-power-gh', 'power', 'greenhouse', 'power'),
    edge('e-power-isru', 'power', 'isru', 'power'),
    edge('e-power-robots', 'power', 'robots', 'power'),
    edge('e-power-hab', 'power', 'habitat', 'power'),
    edge('e-power-depot', 'power', 'depot', 'power'),
    edge('e-power-mission', 'power', 'mission', 'heat', 'unmet critical'),
    edge('e-mine-water', 'mine', 'water', 'water'),
    edge('e-water-eclss', 'water', 'eclss', 'water'),
    edge('e-water-isru', 'water', 'isru', 'water'),
    edge('e-water-gh', 'water', 'greenhouse', 'water'),
    edge('e-water-hab', 'water', 'habitat', 'water'),
    edge('e-eclss-hab', 'eclss', 'habitat', 'o2', 'O₂'),
    edge('e-hab-eclss', 'habitat', 'eclss', 'co2', 'CO₂'),
    edge('e-gh-hab', 'greenhouse', 'habitat', 'food', 'food'),
    edge('e-isru-depot', 'isru', 'depot', 'propellant'),
    edge('e-depot-starship', 'depot', 'starship', 'propellant'),
    edge('e-robots-maint', 'robots', 'maint', 'labor', 'labor'),
    edge('e-maint-eclss', 'maint', 'eclss', 'spares', 'spares'),
  ]), []);

  // ---------- ECLSS detail ----------
  const eNodes: Node[] = useMemo(() => ([
    { id: 'crew', type: 'sys', position: { x: 0, y: 160 }, data: { label: 'Crew (breathing)', sub: 'consume O₂ · exhale CO₂', accent: RES.o2 } },
    { id: 'cabin', type: 'sys', position: { x: 250, y: 160 }, data: { label: 'Cabin atmosphere', sub: 'ideal-gas volume', accent: RES.o2, live: live('hab.ppco2', 'kPa ppCO₂', 2) } },
    { id: 'cdra', type: 'sys', position: { x: 520, y: 40 }, data: { label: 'CO₂ removal (CDRA)', sub: 'scrub cabin CO₂', accent: RES.co2, live: live('eclss.co2_scrubbed', 'kg/sol') } },
    { id: 'co2buf', type: 'sys', position: { x: 780, y: 40 }, data: { label: 'CO₂ buffer', accent: RES.co2 } },
    { id: 'oga', type: 'sys', position: { x: 520, y: 160 }, data: { label: 'O₂ generation (OGA)', sub: 'electrolyse water', accent: RES.o2, live: live('eclss.o2_production', 'kg/sol') } },
    { id: 'sab', type: 'sys', position: { x: 780, y: 160 }, data: { label: 'Sabatier', sub: 'CO₂ + 4H₂ → CH₄ + 2H₂O', accent: RES.propellant } },
    { id: 'wrs', type: 'sys', position: { x: 520, y: 300 }, data: { label: 'Water recovery (WRS)', sub: '98% closure', accent: RES.water } },
    { id: 'waterp', type: 'sys', position: { x: 250, y: 300 }, data: { label: 'Potable water', accent: RES.water, live: live('store.water_potable', 'kg') } },
    { id: 'waste', type: 'sys', position: { x: 0, y: 300 }, data: { label: 'Wastewater', accent: RES.water } },
    // eslint-disable-next-line react-hooks/exhaustive-deps
  ]), [idx]);

  const eEdges: Edge[] = useMemo(() => ([
    edge('ec1', 'crew', 'cabin', 'co2', 'CO₂ out'),
    edge('ec2', 'cabin', 'crew', 'o2', 'O₂ in'),
    edge('ec3', 'cabin', 'cdra', 'co2'),
    edge('ec4', 'cdra', 'co2buf', 'co2'),
    edge('ec5', 'co2buf', 'sab', 'co2'),
    edge('ec6', 'oga', 'cabin', 'o2'),
    edge('ec7', 'waterp', 'oga', 'water'),
    edge('ec8', 'oga', 'sab', 'o2', 'H₂'),
    edge('ec9', 'sab', 'waterp', 'water', 'recycled H₂O'),
    edge('ec10', 'crew', 'waste', 'water', 'urine/condensate'),
    edge('ec11', 'waste', 'wrs', 'water'),
    edge('ec12', 'wrs', 'waterp', 'water'),
  ]), []);

  return (
    <div className="doc-page wide">
      <h1>Systems map</h1>
      <p className="lede">
        How every subsystem connects — power, air, water, propellant, food, labor and spares all flow between the same
        shared buses, and everything ultimately traces back to the Mars environment. Edge labels carry the live flow at
        the current sol. Drag nodes, scroll to zoom.
      </p>

      <div className="flow-legend">
        {Object.entries({ Power: RES.power, 'O₂': RES.o2, 'CO₂': RES.co2, Water: RES.water, Propellant: RES.propellant, Food: RES.food, Labor: RES.labor, Spares: RES.spares, Environment: RES.env }).map(([k, c]) => (
          <span key={k} className="flow-legend-item"><span className="flow-swatch" style={{ background: c }} />{k}</span>
        ))}
      </div>

      <div className="flow-wrap">
        <ReactFlow nodes={topNodes} edges={topEdges} nodeTypes={nodeTypes} fitView proOptions={{ hideAttribution: true }}
          minZoom={0.3} maxZoom={1.8} defaultEdgeOptions={{ type: 'default' }}>
          <Background color="rgba(255,255,255,0.06)" gap={22} />
          <Controls showInteractive={false} />
        </ReactFlow>
      </div>

      <h2 style={{ marginTop: 28 }}>ECLSS — the regenerative life-support loop</h2>
      <p className="lede">
        The crew breathe against a well-mixed cabin. CO₂ is scrubbed and fed to the Sabatier reactor; water is
        electrolysed to make O₂ (and H₂ for Sabatier); wastewater is recovered at ~98%. This is the loop that keeps
        everyone alive — power to it is <b>Critical</b> priority and never shed first.
      </p>
      <div className="flow-wrap eclss">
        <ReactFlow nodes={eNodes} edges={eEdges} nodeTypes={nodeTypes} fitView proOptions={{ hideAttribution: true }}
          minZoom={0.4} maxZoom={1.8}>
          <Background color="rgba(255,255,255,0.06)" gap={22} />
          <Controls showInteractive={false} />
        </ReactFlow>
      </div>
    </div>
  );
}
