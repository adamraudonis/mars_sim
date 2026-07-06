import type { AppRunner } from '../runner';
import { useTick } from '../ui/panels';
import { CONTROL_SYSTEMS, type ControlLaw } from './controlLaws';

/** Resolve a law's threshold value (from a param or a constant). */
function threshold(runner: AppRunner, law: ControlLaw): number | null {
  if (law.thresholdParam && runner.params.has(law.thresholdParam)) return runner.params.v(law.thresholdParam);
  if (law.thresholdConst !== undefined) return law.thresholdConst;
  return null;
}

function fmt(x: number, unit?: string): string {
  const v = Math.abs(x) >= 100 ? x.toLocaleString('en-US', { maximumFractionDigits: 0 })
    : x.toLocaleString('en-US', { maximumFractionDigits: 2 });
  return unit ? `${v} ${unit}` : v;
}

/**
 * The "Simulink" view — every threshold-driven control rule in the sim, made observable.
 * Each law shows its WHEN → THEN, the governing parameter, and (where a live signal
 * exists) whether the rule is firing right now at the current playhead.
 */
export function LogicPage({ runner }: { runner: AppRunner }) {
  useTick(250);
  const reader = runner.reader;
  const idx = reader ? reader.solToIndex(runner.playheadSol) : 0;

  const signal = (law: ControlLaw): number | null => {
    if (!law.signalSeriesId || !reader) return null;
    const s = reader.series(law.signalSeriesId);
    return s ? s.values[idx] ?? null : null;
  };

  const isActive = (law: ControlLaw, sig: number | null, thr: number | null): boolean | null => {
    if (sig === null || thr === null || !law.compare) return null;
    return law.compare === 'gt' ? sig > thr : sig < thr;
  };

  return (
    <div className="doc-page">
      <h1>Control logic</h1>
      <p className="lede">
        This is the whole rulebook — every threshold, setpoint and reflex the simulation runs, in plain terms.
        It’s the Simulink block diagram written out as words: <b>when</b> a monitored signal crosses a governing
        parameter, <b>then</b> the controller acts. Every threshold below is a live parameter you can retune on the
        Simulation page. Rules with a live signal show a badge — <span className="rule-badge on">FIRING</span> means
        the condition is met at sol {Math.floor(runner.playheadSol)} right now.
      </p>

      {CONTROL_SYSTEMS.map((sys) => (
        <section key={sys.system} className="logic-sys">
          <h2>{sys.system}</h2>
          <p className="logic-summary">{sys.summary}</p>
          <div className="logic-laws">
            {sys.laws.map((law, i) => {
              const thr = threshold(runner, law);
              const sig = signal(law);
              const active = isActive(law, sig, thr);
              return (
                <div key={i} className={`logic-law${active ? ' firing' : ''}`}>
                  <div className="logic-rule">
                    <span className="kw when">WHEN</span> {law.when}
                    <span className="kw then">THEN</span> {law.then}
                  </div>
                  <div className="logic-meta">
                    {law.priority && <span className="chip prio">{law.priority} priority</span>}
                    {thr !== null && (
                      <span className="chip thr" title={law.thresholdParam ?? 'constant'}>
                        threshold {fmt(thr, law.thresholdUnit)}
                        {law.thresholdParam && <span className="chip-pid">{law.thresholdParam}</span>}
                      </span>
                    )}
                    {law.signalLabel && sig !== null && (
                      <span className="chip sig">
                        {law.signalLabel} now: <b>{fmt(sig, law.signalUnit)}</b>
                      </span>
                    )}
                    {active !== null && (
                      <span className={`rule-badge ${active ? 'on' : 'off'}`}>{active ? 'FIRING' : 'idle'}</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      ))}

      <p className="logic-foot">
        Every rule here is enforced in the simulation source (power triage in <code>power.ts</code>, atmosphere reflexes
        in <code>habitat.ts</code>, regeneration in <code>eclss.ts</code>, failure accounting in <code>mission.ts</code>).
        Change a threshold on the Simulation page and the recording recomputes in about a second.
      </p>
    </div>
  );
}
