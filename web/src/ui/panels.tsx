import { useEffect, useMemo, useRef, useState } from 'react';
import type { AppRunner } from '../runner';
import { Fidelity } from '../sim/module';
import type { Param } from '../sim/params';
import type { SimEvent } from '../sim/events';
import { Distiller } from '../sim/distiller';
import { Chart } from './Chart';
import { CHART_PALETTE as P, COLOR, compact, severityColor } from './format';

/** Re-render at a slow tick so panels track the sim without chart-rate churn. */
export function useTick(ms: number): number {
  const [n, setN] = useState(0);
  useEffect(() => {
    const id = setInterval(() => setN((v) => v + 1), ms);
    return () => clearInterval(id);
  }, [ms]);
  return n;
}

const healthColor: Record<string, string> = {
  nominal: COLOR.good,
  degraded: COLOR.warn,
  failed: COLOR.bad,
  offline: COLOR.textFaint,
};

export function SystemsPanel({ runner }: { runner: AppRunner }) {
  useTick(350);
  const engine = runner.engine;
  if (!engine) return null;
  return (
    <>
      <div className="section-header">Systems</div>
      <div className="scroll">
        {engine.modules.map((m) => (
          <div className="card" key={m.id}>
            <div className="head">
              <span className="dot" style={{ background: healthColor[m.health] }} />
              <span className="name">{m.displayName}</span>
              {m.maxFidelity > Fidelity.L0 && (
                <span className="segmented">
                  {Array.from({ length: m.maxFidelity + 1 }, (_, f) => (
                    <button
                      key={f}
                      className={m.effectiveFidelity === f ? 'active' : ''}
                      onClick={() => {
                        m.fidelity = f as Fidelity;
                      }}
                    >
                      L{f}
                    </button>
                  ))}
                </span>
              )}
            </div>
            <div className="status">{m.statusLine}</div>
            {m.keyFigures.map(([label, value, unit], i) => (
              <div className="kv" key={i}>
                <span className="k">{label}</span>
                <span className="v">{compact(value)}</span>
                <span className="u">{unit}</span>
              </div>
            ))}
          </div>
        ))}
      </div>
    </>
  );
}

const confChip: Record<string, [string, string]> = {
  high: ['HIGH', COLOR.good],
  medium: ['MED', COLOR.accent2],
  low: ['LOW', COLOR.warn],
  speculative: ['SPEC', COLOR.bad],
};

function ParamRow({ runner, p }: { runner: AppRunner; p: Param }) {
  const [text, setText] = useState(p.value.toPrecision(6).replace(/\.?0+$/, ''));
  const overridden = p.userOverride !== null || p.distilledOverride !== null;

  const commit = () => {
    const v = Number(text);
    if (Number.isFinite(v)) {
      if (Math.abs(v - p.value) > Number.EPSILON) runner.params.setUserOverride(p.id, v);
    } else {
      setText(p.value.toPrecision(6).replace(/\.?0+$/, ''));
    }
  };

  const [confLabel, confColor] = confChip[p.confidence] ?? confChip.medium;
  return (
    <div className="param-row">
      <div className="head">
        <span className="name" title={p.id}>
          {p.name}
        </span>
        {overridden && (
          <button
            className="reset"
            title={`Reset to sourced value ${p.baseValue}`}
            onClick={() => {
              runner.params.setUserOverride(p.id, null);
              runner.params.setDistilledOverride(p.id, null);
              setText(p.value.toPrecision(6).replace(/\.?0+$/, ''));
            }}
          >
            ↺
          </button>
        )}
        <input
          className={overridden ? 'overridden' : ''}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onBlur={commit}
          onKeyDown={(e) => e.key === 'Enter' && commit()}
        />
        <span className="unit">{p.unit}</span>
      </div>
      <div className="meta">
        <span className="chip" style={{ color: confColor, background: confColor + '22' }}>
          {confLabel}
        </span>
        {overridden && (
          <span className="chip" style={{ color: COLOR.accent, background: COLOR.accent + '22' }}>
            OVERRIDE
          </span>
        )}
        <span className="src">{p.source}</span>
      </div>
    </div>
  );
}

export function ParamsPanel({ runner }: { runner: AppRunner }) {
  const [filter, setFilter] = useState('');
  const tick = useTick(2000);
  const matches = useMemo(() => {
    const f = filter.toLowerCase();
    return runner.params.all.filter(
      (p) =>
        !f ||
        p.id.toLowerCase().includes(f) ||
        p.name.toLowerCase().includes(f) ||
        p.domain.toLowerCase().includes(f),
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter, runner, tick]);

  return (
    <>
      <div className="section-header">Parameters</div>
      <div className="caption" style={{ marginBottom: 6 }}>
        Every value is sourced. Edit to override — the simulation applies it live.
      </div>
      <input
        className="search"
        placeholder="search  ·  solar, o2, mtbf, kwh …"
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
      />
      <div className="caption faint" style={{ margin: '4px 0 4px' }}>
        {matches.length} of {runner.params.all.length} parameters
        {matches.length > 80 ? ' · showing first 80' : ''}
      </div>
      <div className="scroll">
        {matches.slice(0, 80).map((p) => (
          <ParamRow key={p.id} runner={runner} p={p} />
        ))}
      </div>
    </>
  );
}

export function LogPanel({ runner }: { runner: AppRunner }) {
  useTick(500);
  const scrollRef = useRef<HTMLDivElement>(null);
  const events = runner.engine?.events.events ?? [];
  const shown = events.slice(-250);

  useEffect(() => {
    const el = scrollRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  });

  return (
    <>
      <div className="section-header">Mission log</div>
      <div className="scroll" ref={scrollRef}>
        {shown.map((e: SimEvent, i) => (
          <div className="log-row" key={events.length - shown.length + i}>
            <span className="bar" style={{ background: severityColor(e.severity) }} />
            <span className="sol">{e.sol.toFixed(1)}</span>
            <span className={`msg${e.severity === 'info' ? ' info' : ''}`}>
              {e.source} — {e.message}
            </span>
          </div>
        ))}
      </div>
    </>
  );
}

export function TelemetryPanel({ runner }: { runner: AppRunner }) {
  const [distillNote, setDistillNote] = useState('');
  return (
    <>
      <div className="scroll">
        <Chart runner={runner} title="Cabin atmosphere" series={[
          { id: 'hab.ppo2', color: P[0], label: 'ppO₂ kPa' },
          { id: 'hab.ppco2', color: P[1], label: 'ppCO₂ kPa' },
        ]} />
        <Chart runner={runner} title="Consumable reserves" series={[
          { id: 'crew.food_days', color: P[2], label: 'food sols' },
          { id: 'crew.water_days', color: P[0], label: 'water sols' },
        ]} />
        <Chart runner={runner} title="Power" series={[
          { id: 'power.offered', color: P[4], label: 'available' },
          { id: 'power.demand', color: P[1], label: 'demand' },
          { id: 'power.unmet', color: COLOR.bad, label: 'unmet' },
        ]} />
        <Chart runner={runner} title="Battery state of charge" series={[
          { id: 'power.battery_soc', color: P[5], label: 'SoC %' },
        ]} />
        <Chart runner={runner} title="Return propellant" series={[
          { id: 'fleet.return_prop', color: P[3], label: 'readiness %' },
          { id: 'depot.ch4', color: P[6], label: 'CH₄ t' },
          { id: 'depot.lox', color: P[0], label: 'LOX t' },
        ]} />
        <Chart runner={runner} title="Water & ISRU" series={[
          { id: 'store.water_potable', color: P[0], label: 'water kg' },
          { id: 'icemine.production', color: P[6], label: 'mined kg/sol' },
          { id: 'isru.water_demand', color: P[1], label: 'plant kg/sol' },
        ]} />
        <Chart runner={runner} title="Dust" series={[
          { id: 'env.tau', color: P[1], label: 'optical depth τ' },
          { id: 'solar.dust', color: P[4], label: 'panel dust %' },
        ]} />
        <Chart runner={runner} title="Crew" series={[
          { id: 'crew.count', color: P[2], label: 'crew' },
          { id: 'crew.health', color: P[0], label: 'health' },
          { id: 'crew.dose', color: P[1], label: 'dose mSv' },
        ]} />
        <Chart runner={runner} title="Maintenance" series={[
          { id: 'maint.queue', color: P[1], label: 'open repairs' },
          { id: 'maint.awaiting_spares', color: COLOR.bad, label: 'no spares' },
          { id: 'labor.unmet', color: P[4], label: 'unmet labor h' },
        ]} />

        <div className="distill-card">
          <div className="section-header" style={{ color: COLOR.accent }}>
            Distill L2 → L0
          </div>
          <div className="caption">
            Run a subsystem at max fidelity in isolation, fit the averaged coefficients, and install them as
            overrides for fast campaign-scale runs.
          </div>
          <div className="row">
            <button
              className="ghost"
              onClick={() => setDistillNote(Distiller.distillSolar(runner.params, runner.scenario.latitudeDeg).summary)}
            >
              Solar → kWh/sol/kW
            </button>
            <button
              className="ghost"
              onClick={() => setDistillNote(Distiller.distillGreenhouse(runner.params).summary)}
            >
              Greenhouse → utilization
            </button>
          </div>
          {distillNote && (
            <div className="caption" style={{ marginTop: 6 }}>
              {distillNote}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
