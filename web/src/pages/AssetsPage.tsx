import type { AppRunner } from '../runner';
import { useTick } from '../ui/panels';
import { COLOR } from '../ui/format';
import { buildInventory, totalLandedMassT } from './inventory';

const healthColor: Record<string, string> = {
  nominal: COLOR.good, degraded: COLOR.warn, failed: COLOR.bad, offline: COLOR.textFaint,
};

/** Live table of every asset on the surface at the current sol. */
export function AssetsPage({ runner }: { runner: AppRunner }) {
  useTick(300);
  const groups = buildInventory(runner);
  const totalT = totalLandedMassT(groups);
  const starships = groups.find((g) => g.group === 'Transport')?.rows[0]?.qty.replace(/\D.*/, '') ?? '0';

  return (
    <div className="doc-page">
      <h1>Asset inventory</h1>
      <p className="lede">
        Everything landed on the surface as of <b>sol {Math.floor(runner.playheadSol)}</b>, derived live from the
        mission. Scrub the timeline above to see the base grow flight by flight. Approx. landed mass excludes
        propellant produced in situ.
      </p>

      <div className="stat-cards">
        <div className="stat-card">
          <div className="stat-val">{totalT.toLocaleString('en-US', { maximumFractionDigits: 0 })} t</div>
          <div className="stat-cap">Landed hardware mass</div>
        </div>
        <div className="stat-card">
          <div className="stat-val">{starships}</div>
          <div className="stat-cap">Starships on surface</div>
        </div>
        <div className="stat-card">
          <div className="stat-val">{groups.reduce((s, g) => s + g.rows.length, 0)}</div>
          <div className="stat-cap">Distinct asset types</div>
        </div>
      </div>

      {groups.map((g) => (
        <div key={g.group} className="asset-group">
          <h2>{g.group}</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Asset</th>
                <th>Quantity</th>
                <th className="num">Mass</th>
                <th>Specification</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {g.rows.map((r) => (
                <tr key={r.item}>
                  <td className="strong">{r.item}</td>
                  <td className="mono">{r.qty}</td>
                  <td className="num mono">{r.massT !== null ? `${r.massT.toLocaleString('en-US', { maximumFractionDigits: 1 })} t` : '—'}</td>
                  <td className="dim">{r.spec}</td>
                  <td>
                    <span className="asset-dot" style={{ background: healthColor[r.health] }} />
                    {r.status}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </div>
  );
}
