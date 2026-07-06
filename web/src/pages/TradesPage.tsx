import type { ReactNode } from 'react';

interface Trade {
  n: number;
  title: string;
  question: string;
  physics: ReactNode;
  numbers: Array<[string, string]>;
  recommendation: ReactNode;
  verdict: string;
}

const TRADES: Trade[] = [
  {
    n: 1,
    title: 'Power: nuclear vs solar',
    question: 'Should the base run on solar + batteries, fission reactors, or a hybrid of both?',
    physics: (
      <>
        Solar + storage lands at ~300–450 kg per continuous kW at mid-latitude; FSP-class fission is ~230 kg/kW.
        But Mars dust is forward-scattering, so even a τ=10 global storm leaves ~5–10% of clear-sky output. The
        decisive event is the seasonal global dust storm: in the default weather seed a solar base loses ~130 sols
        of propellant production around sol 1347, while a fission base doesn’t blink.
      </>
    ),
    numbers: [
      ['Solar specific mass', '~350 kg/kW·cont'],
      ['Fission specific mass', '~230 kg/kW·cont'],
      ['Clear output kept in τ=10 storm', '5–10%'],
      ['Propellant lost, solar, worst storm', '~130 sols'],
    ],
    recommendation: (
      <>
        Run a <b>fission backbone sized to critical + keep-alive loads</b> (life support, cryocooler, habitat), with a
        <b> solar farm on top for the ISRU/greenhouse swing load</b>. Fission guarantees the crew survives any storm;
        solar cheaply supplies the large, interruptible propellant-plant demand that can throttle. Pure solar puts the
        return-propellant schedule — and, without a deep battery, life support — at the mercy of the weather.
      </>
    ),
    verdict: 'Hybrid: fission for critical, solar for the swing load.',
  },
  {
    n: 2,
    title: 'Propellant: mine water vs import H₂',
    question: 'Make return propellant from mined Martian ice, or ship hydrogen from Earth and pull O₂ from the air?',
    physics: (
      <>
        The water route electrolyses mined ice to supply both H₂ and all the O₂ (~0.489 kg water per kg of O:F-3.6
        methalox). The H₂-import route runs Sabatier on Earth hydrogen and makes O₂ with a MOXIE-style SOXE — zero
        mining, but ~15% more plant energy and cryogenic LH₂ that must survive years at 20 K (~0.3%/day passive boiloff).
      </>
    ),
    numbers: [
      ['Water to refuel one ship (1,200 t)', '~590 t mined'],
      ['LH₂ to refuel one ship instead', '~65 t from Earth'],
      ['Extra plant energy, SOXE route', '~+15%'],
      ['LH₂ passive boiloff', '~0.3%/day'],
    ],
    recommendation: (
      <>
        <b>Mine the ice.</b> The imported-H₂ route trades away ~590 t of “free” local water for 65 t of Earth hydrogen
        that then needs years of near-perfect cryo storage — and the mined water also supplies the settlement for free
        (drinking, ECLSS makeup, agriculture). Import H₂ only makes sense as a <i>bootstrapping fallback</i> for the
        very first window before the mine is commissioned.
      </>
    ),
    verdict: 'Mine ice; keep H₂-import as a first-window fallback.',
  },
  {
    n: 3,
    title: 'Solar array mass',
    question: 'How much does the power farm actually weigh?',
    physics: (
      <>
        At ROSA-class 1.5 kg/m² and ~5 kWh/sol per rated kW at 40°N, a 60,000 m² array is ~90 t of blankets plus ~46 t
        of batteries to ride through the night — roughly 1.9 MW-continuous-equivalent.
      </>
    ),
    numbers: [
      ['Array specific mass', '1.5 kg/m² (ROSA)'],
      ['Baseline array', '60,000 m² ≈ 90 t'],
      ['Battery bank', '~46 t'],
      ['Continuous-equivalent power', '~1.9 MW'],
    ],
    recommendation: (
      <>
        Budget roughly <b>one dedicated cargo Starship of power hardware per crew ship you intend to refuel per window</b>.
        The batteries, not the blankets, dominate storm-resilience mass — which is exactly why pairing solar with a small
        reactor (Trade 1) is cheaper than sizing batteries for a global storm.
      </>
    ),
    verdict: '~136 t (arrays + batteries) ≈ one cargo ship of power.',
  },
  {
    n: 4,
    title: 'Flights for the initial base',
    question: 'How many Starship landings — and Earth launches — does the first base take?',
    physics: (
      <>
        The shipped baseline campaign is 5 cargo (2031) + 5 crew-wave (2033) + 3 resupply (2035). Behind each Mars-bound
        ship stand ~12 tanker flights to LEO. The resupply wave is not optional: remove it and the spares inventory
        collapses around sol 1800 and the crew does not survive.
      </>
    ),
    numbers: [
      ['Cargo landings (2031)', '5'],
      ['Crew-wave landings (2033)', '5'],
      ['Resupply landings (2035)', '3'],
      ['Total Earth launches (incl. tankers)', '~150'],
    ],
    recommendation: (
      <>
        Plan for <b>~13 Mars landings across three windows and ~150 Earth launches</b> — and treat the resupply wave as
        mission-critical, not contingency. The binding constraint is <b>launch-pad cadence, not astrodynamics</b>:
        150 launches in ~26 months is the real schedule risk.
      </>
    ),
    verdict: '~13 landings / ~150 launches; resupply is mandatory.',
  },
  {
    n: 5,
    title: 'Reliability & spares mass',
    question: 'How much spare-parts mass must ride along to keep the base alive for years?',
    physics: (
      <>
        Failures are sampled per operating hour per component group (ECLSS ORUs, SOXE stacks, excavators, robots);
        repairs consume crew/robot hours plus class-pooled spares. ISS experience — 14 of 20 ECLSS ORUs ran worse than
        predicted, some 22× — is why the failure-rate k-factor defaults to 2.0. A spares shortfall is quiet for ~500
        sols, then everything fails at once.
      </>
    ),
    numbers: [
      ['Baseline spares provisioning', '~25 t / ~4 yr'],
      ['ECLSS ORUs worse than predicted (ISS)', '14 of 20'],
      ['Failure-rate k-factor default', '2.0'],
      ['Owens & de Weck 4-crew transit', '12–17 t'],
    ],
    recommendation: (
      <>
        Provision <b>~25 t of class-pooled spares across the three waves</b> and design for
        <b> cannibalization + commonality</b>, which the sim shows stretches the same mass much further. Do not size
        spares to the nominal failure rate — carry the ×2 uncertainty, because the tail is where crews are lost.
      </>
    ),
    verdict: '~25 t spares, sized to 2× nominal failure rate.',
  },
  {
    n: 6,
    title: 'Food: grow vs bring',
    question: 'Ship all the food, or grow calories under LED light on Mars?',
    physics: (
      <>
        Packaged food is 1.83 kg/CM-day (~8 t per crew per 1,000 sols). LED-grown potatoes yield ~76 kcal/m²/sol for
        ~6.2 kWh/m²/sol of lighting — about 0.08 kWh(e)/kcal. Feeding one person entirely from LEDs is a 10–20 kW line
        item, the single largest life-support power draw.
      </>
    ),
    numbers: [
      ['Packaged food', '1.83 kg/CM-day (~8 t/crew/1000 sol)'],
      ['LED crop yield', '~76 kcal/m²/sol'],
      ['Lighting energy', '~0.08 kWh(e)/kcal'],
      ['Baseline greenhouse', '700 m² → ~25% of calories, 12 crew'],
    ],
    recommendation: (
      <>
        <b>Bring the bulk of the calories; grow a supplement.</b> A ~700 m² greenhouse covering ~25% of calories pays
        for itself as an <b>oxygen plant and psychological/fresh-food benefit</b>, but full food closure only makes sense
        once power is cheap (fission) and the base is old. Grow-vs-bring crosses over with power price, not with area.
      </>
    ),
    verdict: 'Bring staple calories; grow ~25% + O₂ as a supplement.',
  },
  {
    n: 7,
    title: 'Optimus robots & the labor market',
    question: 'What do humanoid robots change about who does the work?',
    physics: (
      <>
        Labor is a real market in the sim. Crew supply ~6.5 productive h/sol each; robots supply ~12 h/sol × availability
        × task effectiveness (0.5–0.6 for structured work, ~0.25 for diagnosis-heavy repair). A single “robot = X% human”
        scalar is wrong — effectiveness is task-specific.
      </>
    ),
    numbers: [
      ['Crew labor', '~6.5 h/sol each'],
      ['Robot labor', '~12 h/sol × avail × effectiveness'],
      ['Structured-work effectiveness', '0.5–0.6'],
      ['Pre-crew fleet in baseline', '~40 robots'],
    ],
    recommendation: (
      <>
        <b>Deploy a robot fleet ahead of the crew.</b> ~40 robots erect the entire 90 t solar farm and commission ISRU
        <i> before any human lands</i> — this is what makes an uncrewed-first architecture possible at all. Set robots to
        zero and the first crew spend their surface stay as construction workers instead of scientists.
      </>
    ),
    verdict: 'Robots first: they build the base the crew arrives to.',
  },
];

/** Guided walkthrough of the headline architecture trades, each ending in a recommendation. */
export function TradesPage() {
  return (
    <div className="doc-page">
      <h1>Trade studies &amp; recommendations</h1>
      <p className="lede">
        The seven decisions that shape a Mars base — power, propellant, mass, flights, reliability, food and labor —
        walked through at a high level with the numbers the simulation enforces, each ending in a concrete
        recommendation. Every figure below is a tunable, sourced parameter; open the matching scenario on the
        Simulation page to watch the trade play out over 20 years.
      </p>

      <div className="trade-toc">
        {TRADES.map((t) => (
          <a key={t.n} href={`#/trades`} onClick={(e) => { e.preventDefault(); document.getElementById(`trade-${t.n}`)?.scrollIntoView({ behavior: 'smooth' }); }}>
            {t.n}. {t.title}
          </a>
        ))}
      </div>

      {TRADES.map((t) => (
        <section key={t.n} id={`trade-${t.n}`} className="trade">
          <div className="trade-head">
            <span className="trade-num">{t.n}</span>
            <div>
              <h2>{t.title}</h2>
              <p className="trade-q">{t.question}</p>
            </div>
          </div>

          <p className="trade-physics">{t.physics}</p>

          <div className="trade-numbers">
            {t.numbers.map(([k, v]) => (
              <div key={k} className="trade-fig">
                <div className="trade-fig-v mono">{v}</div>
                <div className="trade-fig-k">{k}</div>
              </div>
            ))}
          </div>

          <div className="trade-rec">
            <div className="trade-rec-tag">Recommendation</div>
            <p>{t.recommendation}</p>
            <div className="trade-verdict">→ {t.verdict}</div>
          </div>
        </section>
      ))}

      <p className="logic-foot">
        These recommendations are the default architecture the shipped baseline scenario encodes. Every number traces to
        a sourced parameter (full citations in the research report); the point of the simulation is to let you disagree —
        change the assumption and watch the 20-year outcome move.
      </p>
    </div>
  );
}
