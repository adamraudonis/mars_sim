# Mars Mission Architecture & Launch Campaign — Parameter Research

**Domain key:** `mission_architecture` · **Compiled:** July 2026 · **For:** Mars habitat/settlement trade-study simulation

This document establishes the orbital-mechanics scaffolding of any Mars launch campaign (windows, energies, transit times, stay times, blackouts), the NASA reference-architecture anchor numbers (DRA 5.0), and the SpaceX Starship campaign claims as publicly stated through mid-2026. All SpaceX numbers are **company claims** and are flagged as such with the date of the statement.

---

## 1. Campaign cadence: the synodic clock

Everything in a Mars campaign beats to the Earth–Mars synodic period:

```
S = 1 / (1/T_Earth − 1/T_Mars) = 1 / (1/365.256 d − 1/686.980 d) = 779.94 d ≈ 25.6 months
```

- **Synodic period: 779.94 days** (≈ 2.135 yr ≈ 759.1 sols). Windows recur every ~26 months but are *not* identical — Mars' orbital eccentricity (e = 0.0934) makes both the minimum departure energy and the transit geometry vary over a ~15-year (7-synodic-cycle) super-cycle.
- **1 sol = 88,775.244 s = 24.6597 h** (Allison & McEwen 2000; NASA GISS Mars24).
- **1 Mars year = 686.98 d = 668.6 sols.**
- Conversion used throughout: `days × 0.97324 = sols` (86400/88775.244).

---

## 2. Earth→Mars launch windows, 2026–2045

Primary source: **Burke, Falck & McGuire, "Interplanetary Mission Design Handbook: Earth-to-Mars Mission Opportunities 2026 to 2045," NASA/TM-2010-216764 (2010)** — MIDAS patched-conic optimization, departure from 407 km LEO. Type I = <180° heliocentric transfer (faster); Type II = 180–360° (slower, usually cheaper).

### 2.1 Ballistic energy minima per opportunity (TM-2010-216764, Tables 1–11)

| Window | Optimal type | Depart | Arrive | C3 min (km²/s²) | TOF (d) | Other type: C3 / dep→arr / TOF |
|---|---|---|---|---|---|---|
| 2026 | II | 2026-10-31 | 2027-08-19 | **9.144** | 292 | I: 11.11 / 11-14-26→08-09-27 / 268 d |
| 2028/29 | II | 2028-12-02 | 2029-10-16 | **8.928** | 318 | I: 9.05 / 12-10-28→07-20-29 / 222 d |
| 2031 | II | 2031-02-23 | 2032-01-09 | **8.237** | 320 | I: 9.00 / 01-28-31→08-06-31 / 190 d |
| 2033 | II | 2033-04-28 | 2034-01-27 | **7.781** | 274 | I: 8.41 / 04-06-33→10-01-33 / 178 d |
| 2035 | I | 2035-04-21 | 2035-11-03 | **10.19** | 196 | II: 17.52 / 06-12-35→07-28-36 / 412 d |
| 2037 | II | 2037-06-18 | 2038-07-19 | **14.84** | 396 | I: 17.07 / 06-02-37→12-17-37 / 198 d |
| 2039 | II | 2039-07-15 | 2040-07-09 | **12.17** | 360 | I: 18.65 / 07-19-39→02-16-40 / 212 d |
| 2041 | II | — | — | 9.818 | — | — |
| 2043 | II | — | — | 8.969 | — | — |
| 2045 | II | — | — | 8.587 | — | — |

Key facts for the simulator:

- Minimum C3 varies **7.8 → 14.8 km²/s²** (nearly ×2 in energy) across the cycle. **2033 is the cheapest window of the 2026–2045 span; 2037–2039 are the most expensive.** A fixed vehicle therefore delivers meaningfully different payload per window.
- Departure dates walk later through the calendar each cycle: Oct/Nov 2026 → Dec 2028 → Jan/Feb 2031 → Apr 2033 → Apr–Jun 2035 → Jun 2037 → Jul 2039.
- 2035 is anomalous: Type II arrival would coincide with Mars aphelion, so Type I is optimal (same effect recurs ~17 yr apart; cf. 2018).
- Arrival V∞ at the energy-optimal solutions is ~2.5–3.3 km/s (2026: 2.73 km/s Type II).
- **Launch period duration** (how long a window stays "open") is a vehicle-capability question: accepting a C3 penalty of a few km²/s² buys roughly ±2–3 weeks around the optimum. Robotic missions typically hold ~3-week launch periods; SpaceX's stated 2026 plan was "November–December 2026," i.e., ~30–45 days. We adopt **30 d (range 14–60 d)**, confidence low.

### 2.2 Useful conversions

```
C3 = v∞²                        [km²/s²]
v_departure(perigee) = sqrt(C3 + 2μ_E/r)        μ_E = 398,600 km³/s²
Δv_TMI (from 407 km circular LEO, v_c = 7.67 km/s):
  C3 = 9   → Δv_TMI ≈ 3.63 km/s
  C3 = 15  → Δv_TMI ≈ 3.89 km/s
  C3 = 30  → Δv_TMI ≈ 4.51 km/s
Mars entry velocity: v_entry = sqrt(v∞,arr² + v_esc,Mars²),  v_esc,Mars ≈ 5.03 km/s
  v∞ = 2.7 → v_entry ≈ 5.7 km/s (min-energy, ≈ MSL's 5.8 km/s)
  v∞ = 5.6 → v_entry ≈ 7.5 km/s (SpaceX's stated figure)
  v∞ = 8.3 → v_entry ≈ 9.7 km/s (Kingdon 90-d transit, before deceleration burn)
```

---

## 3. Transit times: cargo vs. crew

| Mode | Transit | C3 | Source | Confidence |
|---|---|---|---|---|
| Cargo, min-energy ballistic | 180–420 d (typ. ~290 d Type II) | 7.8–15 | TM-2010-216764 | high |
| NASA crew (DRA 5.0, 2037 NTR case) | 174 d out / 201 d back | ~15–25 (fast conjunction) | SP-2009-566 | high |
| SpaceX claim (2016/2017 IAC) | 80–150 d (avg ~115 d) | ~25–45 | company claim | claim documented; achievability medium |
| Fast Starship transit (peer-reviewed) | 90–104 d each way | ~23–32 | Kingdon 2025, *Sci Rep* 15:17764 | medium |

Kingdon (2025) details, the best current peer-reviewed treatment of Starship fast transits:
- Two round-trip trajectories: depart **2033-04-30**, 90 d out; return leg arrives Earth **2035-07-02** after 90 d; second mission departs 2035-07-15, 104 d transits. Implied surface stay ≈ **610 d ≈ 595 sols**.
- Starship assumptions: ~4.6 km/s Δv available after LEO refill (150 km orbit), Isp 370–380 s, ~0.5 km/s reserved for the landing burn.
- **A deceleration burn ~400 s before periapsis cuts Mars arrival speed from 9.73 → 6.87 km/s** to stay inside the (uncertain) TPS entry corridor; peak entry deceleration ≈ 3.5 g.
- Refueling: **15 tanker flights per crew Starship; 4 per slow cargo Starship** (larger V3-class tanker assumed).
- Radiation motivation: 90-d transits keep total mission dose inside NASA career limits where 180-d transits do not (per the paper's analysis).

Counterpoint — **Maiwald et al. (DLR), *Sci Rep* 2024 (doi 10.1038/s41598-024-54012-0)**: attempting to reproduce SpaceX's mass model, they "could not find a feasible mission scenario"; with a 100 t dry Starship the max Δv ≈ 8.71 km/s but the minimum Mars-surface→Earth return demand ≈ 7.12 km/s — implying the *return* leg fails unless ISRU delivers a near-full propellant reload (~1,100–1,500 t) on the surface. Treat SpaceX payload/transit claims as optimistic bounds.

---

## 4. Conjunction-class vs. opposition-class

Verbatim characterization from DRA 5.0 (SP-2009-566, §6.2):

- **Opposition class ("short stay")**: surface stay **30–90 d**, total mission **500–650 d**, one transit leg is long with perihelion pass ≤0.7 AU (often a Venus swing-by), and **much higher total propulsive energy**. Example profile in DRA 5.0: 217 d out / 30 d stay / 403 d return = 650 d.
- **Conjunction class ("long stay")**: surface stay **~500 d or more (DRA reference: 539 d = 525 sols)**, total mission ≈ **900–950 d** (DRA: 914 d), transits **<180–210 d** each way, global minimum-energy solution, "relatively little energy change between opportunities."

All credible Starship-era architectures (and DRA 5.0 itself) select conjunction class. Opposition class survives only in some NASA short-stay hybrid studies (e.g., nuclear-electric + chemical) and costs roughly 2× the in-space propellant for 1/10 the surface time.

### 4.1 Earth-return window timing (drives surface stay)

- The Mars→Earth minimum-energy departure window also recurs once per synodic period. For a Type-II arrival ~8–10 months after Earth departure, the next return window opens **~450–560 d after arrival** — this, not habitat logistics, sets the ~500-sol stay.
- Missing the return window costs a full synodic cycle (**+779.9 d ≈ +759 sols**) of extra surface stay — a first-class simulation event (consumables, spares, power margins must cover it as a contingency branch).
- Fast (90–104 d) transits arrive earlier and return later within the same cycle, *lengthening* the stay to ~600 sols (Kingdon trajectory pair).
- Sim rule of thumb: `t_stay ≈ S − TOF_out − TOF_back + slack`, with slack ±30–60 d from window structure.

---

## 5. EDL: entry, descent & landing

### 5.1 NASA DRA 5.0 reference EDL (SP-2009-566, Table 4-3) — high confidence anchor

| Quantity | Value |
|---|---|
| Aeroshell | 10 m × 30 m triconic, dual-use (aerocapture + entry), PICA/LI-2200 TPS |
| Entry mass (from 1-sol orbit) | **109.7 t** (orbit mass 110.2 t, deorbit Δv 15 m/s) |
| Ballistic coefficient | **471 kg/m²** |
| Descent Δv (supersonic retropropulsion, LOX/LCH4) | **595 m/s** |
| Engine ignition | 1,350 m altitude, Mach 2.29 |
| Peak heat rate / load | 131 W/cm² / 172 MJ/m² |
| **Landed useful mass** | **56.8 t** (landed/entry ≈ 0.52) |

DRA 5.0 delivers each ~103 t payload element (aeroshell + EDL + lander + surface payload) to Mars on a 246.2 t IMLEO cargo vehicle; aerocapture into a 1-sol orbit (250 × 33,793 km), then entry.

### 5.2 Starship EDL (company claims + academic analysis)

- **Entry velocity ~7.5 km/s** for the stated 80–150 d transits: "For Mars entry, you are entering very quickly, going seven and a half kilometers a second" (Musk, IAC 2017, published in *New Space*). Heat shield reused but ablates: "coming in hot enough that you really will see some wear."
- Starship enters directly from the interplanetary trajectory (no aerocapture-to-orbit first — unlike DRA 5.0), body-flap-controlled lifting entry (L/D ≈ 0.3 class), removing "almost all the energy aerodynamically," then flips to a terminal propulsive landing burn ~0.5 km/s (Kingdon 2025 budget).
- Faster transits push entry velocity toward 8.5–9.7 km/s; Kingdon's solution is a pre-entry deceleration burn to 6.87 km/s. TPS entry-corridor limit for Starship at Mars is **not publicly established** — a genuine open uncertainty for the sim.
- **Landing accuracy:** no SpaceX figure published. State of the art at Mars: Perseverance (TRN + range trigger) landed well inside a 7.7 × 6.6 km ellipse, ~1–2 km from center. Starship's terminal guidance on Earth (booster/ship tower catches, 2024–2025) demonstrates meter-class precision propulsive landing in Earth conditions. Settlement build-out (ships landing near pre-placed depots/hab) plausibly requires and achieves **~50–200 m**; we adopt **100 m (range 20–2,000 m), speculative**. DRA 5.0 likewise concluded EDL must emphasize "abort to surface and landing accuracy" — close enough for crew rover reach.

---

## 6. SpaceX Mars campaign — public claims as of mid-2026 (all company claims)

| Date of statement | Claim |
|---|---|
| Sep 27, 2016 (IAC Guadalajara) | ITS/Starship architecture; ~80–150 d transits; 100+ passengers/ship; 1,000-ship fleet vision, ~1M people to Mars |
| Sep 28, 2017 (IAC Adelaide; *New Space* 2018) | BFR: 150 t to LEO reusable; Mars entry 7.5 km/s; aspirational 2022 cargo / 2024 crew (missed) |
| Sep 7, 2024 (Musk/X) | **5 uncrewed Starships** to Mars at the **Nov–Dec 2026 window**; crew ~4 yr later if landings succeed |
| Mar 15, 2025 (Musk/X) | Starship departs end of 2026 carrying **Tesla Optimus** robots; "human landings may start as soon as 2029, although 2031 is more likely" |
| May 29, 2025 (Starbase talk) | 2026/27 attempt "50/50"; ramp ambition: **~20 ships 2028/29 → ~100 in 2030/31 → ~500 by 2033**; ultimately 1,000–2,000 ships per window |
| **Feb 9, 2026 (Musk announcement)** | **Mars ambitions delayed "about five to seven years" to focus Starship on lunar/Artemis work; first uncrewed Mars attempt slips to the 2028/29 window (Dec 2028–Jan 2029)**; Mars-city effort now ~2031–2033 |

Mid-2026 net assessment for the simulator baseline:
- **First uncrewed Starship landing attempts: 2028/29 window (company target; speculative).**
- **First crewed landing: no earlier than 2031, more credibly 2031–2033+ (speculative).** Every crewed date SpaceX has stated since 2016 has slipped by 1–2 synodic cycles per cycle elapsed.
- Fleet growth numbers (20/100/500 per window) are ambitions, not manifests; treat as upper-bound scenario inputs.

### 6.1 Tanker/refill logistics

- Mars-bound Starship propellant load: **~1,200 t (933 t LOX + 267 t LCH4)**; tanker delivery ~100–150 t per flight → **10–16 tanker flights per Mars ship** (we adopt 12; range 10–16).
  - NASA HLS (lunar) official statement: "high teens" of launches per mission (L. Hawkins, NASA, Nov 2023; GAO-24-106256 discussion).
  - Aerospace America (2025) SpaceX-plan analysis: ~12 tankers/ship, 60 tanker launches for the 5-ship 2026 wave.
  - Kingdon 2025: 15 refills (crew, fast transit) vs 4 (cargo, slow, V3 tanker).
- Refills must be rapid (boil-off) and complete inside the ~1–2 months before the window closes → Earth launch cadence, pad count, and depot boil-off are the campaign bottleneck, not Mars-side physics.
- Return propellant must be made on Mars: **~1,100–1,500 t LOX+LCH4 per returning ship** (Sabatier + electrolysis from ice water + atmospheric CO2). Scale check: MOXIE produced ~122 g O2 total (2021–2023) vs ~600+ t O2 needed — a ~10⁶ scale-up plus cryo liquefaction; power estimated O(600 kWe) continuous per Aerospace America's analysis (~1 MWe-class in most academic estimates).

---

## 7. Landing-site selection (SpaceX-commissioned, JPL-led)

Source: **Golombek et al., "SpaceX Starship Landing Sites on Mars," 52nd LPSC (2021), abstract #2420**; HiRISE candidate-site imaging campaign (Arcadia region); Luzzi et al. 2025 (JGR Planets) on Amazonis near-surface ice.

Criteria (engineering-driven):
- **Latitude < 40°** (prefer lower) — solar power and thermal management.
- **Elevation ≤ −2 km MOLA, prefer ≤ −3 km** — more atmosphere above the vehicle = more aero-deceleration timeline for a heavy lander.
- **Shallow, abundant water ice** (SWIM-score screened; ice consistency ~0.6–0.7 at ~40°N in Arcadia) — ISRU propellant feedstock; but ice generally gets shallower/more abundant *poleward*, in tension with the latitude criterion. Arcadia Planitia is one of the few places with excess shallow ice at relatively low latitude.
- **Slopes < 5° over 10 m; rock-abundance probability (>0.5 m rock) < 5%**; radar-reflective, load-bearing surface; no dense expanded secondary craters; nearby lobate debris aprons as bulk-ice backup.
- Seven down-selected candidate sites: **Arcadia Planitia (AP-1, AP-8, AP-9), Erebus Montes (EM-15, EM-16), Phlegra Montes (PM-1, PM-7)** — all in the Arcadia/Amazonis lowlands, ~35–44°N, ~165–190°E. AP-8 scores best on combined ice+safety metrics in follow-up work.

---

## 8. Communications constraints

Source: NASA/NTRS 20220013418 (ASCEND 2022, "Communication Delays, Disruptions, and Blackouts for Crewed Mars Missions").

- **One-way light time: 3 min (opposition) to 22 min (superior conjunction)**; two-way 6–44 min. No real-time Earth support: on-board autonomy required for EDL, medical, ISRU faults.
- **Solar conjunction blackout every synodic period (~26 months):** with the standard Sun–Earth–Mars angle < 2° command-moratorium criterion, **~11–14 days (mean ≈ 12 d)** of no/degraded comm; a conservative <3° criterion gives up to ~3 weeks; aggressive <1° under a week. Flight practice (MRO/MSL/M20 conjunction moratoria, e.g., Oct 2–16, 2021) matches ~2 weeks.
- Sim treatment: schedule a hard 12-d comm blackout (no teleoperation, no new commands) once per synodic cycle; add degraded-rate shoulders ~1 week each side.

---

## 9. Abort logic

From DRA 5.0 (SP-2009-566 §6.5, §6.9) — adopted as the reference abort model:

1. **Pre-TMI (LEO):** full abort available; crew returns in capsule. Refill/assembly failures → stand down to next window (+26 months).
2. **Post-TMI, early:** free-return abort trajectories exist (≈2-yr Earth return loop) but Wooster et al. (2007) showed the 2-yr free return imposes high Mars-flyby/entry speeds and is "less desirable than previously considered" for many opportunities; DRA carries contingency consumables on the MTV instead.
3. **Mars orbit / pre-EDL:** the orbiting transit vehicle acts as **"orbital safe haven"** — if the surface mission aborts, the crew loiters in Mars orbit on contingency consumables **until the TEI window opens** (cannot leave early cheaply).
4. **EDL:** **abort-to-orbit is physically not feasible** during entry ("due to the physics involved during the atmospheric entry phase, ATO was probably not possible") → **abort-to-surface** philosophy: EDL reliability + landing accuracy near the pre-deployed assets. Starship EDL is likewise commit-to-land.
5. **Surface:** ISRU-fueled ascent vehicle means no ascent until propellant is made (DRA mitigates by pre-deploying and *verifying the MAV fully fueled before crew ever leaves Earth* — a key gate the sim should model for Starship ISRU too). Early departure before the return window is essentially unavailable; contingency = shelter in place to the nominal (or next) return window.
6. **Return-window slip:** if ascent/TEI is missed, wait +779.9 d with surface consumable/power margins — the sizing case for contingency logistics.

---

## 10. Cargo-manifest cross-checks (what a campaign must land)

| Architecture | Crew | Landed mass per crewed mission | Power | ISRU | Launches per mission |
|---|---|---|---|---|---|
| **NASA DRA 5.0** (2009) | 6 | 2 pre-deployed landers × **56.8 t landed** (103 t payload elements: MAV+ISRU+30 kWe fission reactor; SHAB) + crew lander; cargo sent **one window ahead**, MAV fueled before crew departs | **30 kWe fission** (ISRU load 25 kWe cont.; hab 12 kWe) | O2 only (ascent oxidizer + ECLSS; CH4 from Earth; 400 kg H2 seed) | 9 Ares V (NTR, IMLEO 848.7 t) or 12 (chem/aerocapture, IMLEO 1,251.8 t) |
| **Mars Direct** (Zubrin & Baker 1991) | 4 | ERV ~28.5 t landed + Hab ~25 t landed (2 × 47 t TMI throws) | ~80–100 kWe-th/kWe-class reactor (design-dependent) | Full CH4+O2 via Sabatier (6 t H2 seed) | 2 heavy-lift per mission, offset one window |
| **SpaceX claim** | 10–100+ | 100 t/ship claim (75–150 range); 2+ cargo ships per crew ship in early waves | solar (deployed arrays; ~MW-class for ISRU at scale) | Full propellant reload ~1,100–1,500 t/ship | 1 SH launch + 10–16 tanker flights per ship |

Per-crew-member landed-mass anchors: DRA 5.0 ≈ 19–28 t/person (117–170 t incl. crew lander & margins); Mars Direct ≈ 13 t/person; SpaceX ambitions imply ≥ 10–20 t/person early, falling with scale. A settlement sim's cargo manifests should sit in the 10–30 t/person band for early phases (high consequence parameter — cross-check against ECLSS/ISRU domain outputs).

---

## 11. Disagreements & open issues between sources

1. **Starship payload to Mars surface:** SpaceX claims 100 t (up to 150 t); Maiwald et al. (2024, DLR) could not close the mass model and find the *return* infeasible without ~full ISRU reload; their max-Δv analysis (8.7 km/s vs 7.1 km/s needed) leaves thin margins. We carry 100 t as **low-confidence company claim**, with 75–150 t range and a sim toggle down to ~50 t.
2. **Fast-transit C3:** Kingdon paper text gives C3 ≈ 23–24 km²/s² in one passage while press summaries state ~31.5–32 km²/s²; both are far above the 7.8–14.8 ballistic minima. We carry 23–32 km²/s² as the fast-transit band (value 28, low confidence within band).
3. **Tanker count:** Musk has claimed as few as 4–8; NASA HLS planning said "high teens"; independent analyses 10–16. Strong function of tanker variant (100 vs 150–200 t delivered). Range kept wide.
4. **Blackout duration:** threshold-dependent (1°/2°/3° SEP): ~5–20 d. The 2°/12 d convention is flight-standard.
5. **First-mission dates:** every SpaceX target since 2016 has slipped ~1 synodic cycle per cycle elapsed; the Feb 2026 announcement (delay 5–7 yr, pivot to Moon) is the current authoritative company statement. NASA's own crewed-Mars planning (M2M objectives) sits in the late 2030s–2040s.
6. **Landing accuracy:** zero published Starship-at-Mars data; our 100 m is an engineering extrapolation (speculative).

---

## 12. Fidelity tiers — recommended model forms

### L0 — single scalars (spreadsheet trade studies)
- Window every **779.94 d**; every window identical: C3 = 10 km²/s², cargo TOF = 290 d, crew TOF = 150 d, launch period 30 d.
- Surface stay = **500 sols**; round trip = 914 d; comm blackout 12 d/cycle; OWLT fixed 12 min.
- Per-ship: 100 t landed (SpaceX scenario) or 56.8 t (DRA-class), 12 tanker flights/ship, ISRU return demand 1,200 t propellant/returning ship.

### L1 — analytic per-window model (campaign scheduler)
- Use the **table in §2.1** (TM-2010-216764) as a lookup: {window, departure date, C3_min(TypeI/II), TOF, arrival V∞}; interpolate a C3(t_dep) parabola per window: `C3(t) ≈ C3_min + k·(t − t_opt)²`, k ≈ 0.005–0.02 km²/s²/day² fitted per window, to trade launch-period length vs. propellant.
- Payload per ship from the rocket equation: `m_pay = m_prop/(e^{Δv/(g0·Isp)} − 1) − m_dry`, Isp 370–380 s, Δv = Δv_TMI(C3) + Δv_landing (0.5 km/s) [+ deceleration burn for fast transits].
- Stay time from window phasing: `t_stay = t_return_open(cycle) − t_arrival`; return windows from the same handbook geometry (mirror-image). Blackout inserted when SEP < 2° from low-order ephemerides.
- Fleet/campaign: integer ships/window × (1 + N_tankers) Earth launches; cap by pad cadence.

### L2 — physics-based sub-sol model
- **Trajectories:** Lambert two-point BVP over (t_dep, t_arr) grids (porkchop generation), patched conics with mid-course Δv; governing relations: `v∞² = C3`, vis-viva `v² = μ(2/r − 1/a)`, Kepler/Lambert time-of-flight equation.
- **EDL:** 3-DOF entry integration `m·dv/dt = −½ρv²C_D A + m·g·sinγ`, `L/D ≈ 0.3`, β = m/(C_D·A) (DRA anchor 471 kg/m²; Starship ≈ 1,300–1,600 kg/m² broadside), Mars-GRAM-class ρ(h, season, dust); heating `q̇ ≈ k·sqrt(ρ/r_n)·v³` (Sutton-Graves, k_Mars = 1.9e-4 SI) against TPS limits; terminal propulsive phase 0.5–0.6 km/s; Monte-Carlo dispersions → landing ellipse.
- **Campaign:** discrete-event simulation of pad turnaround, tanker boil-off (dm/dt), depot fill state, window open/close, per-ship manifest optimization; abort-state machine per §9; comm outage windows from real SEP-angle ephemeris; ISRU propellant-production rate vs. return-window deadline coupling.

---

## 13. Parameter summary table

| id (mission_architecture.*) | value | unit | conf. |
|---|---|---|---|
| synodic_period_days | 779.94 | d | high |
| sol_length_s | 88775.244 | s | high |
| mars_year_sols | 668.6 | sol | high |
| c3_min_2026 / 2028 / 2031 / 2033 / 2035 / 2037 | 9.144 / 8.928 / 8.237 / 7.781 / 10.19 / 14.84 | km²/s² | high |
| launch_period_duration_days | 30 (14–60) | d | low |
| cargo_transit_days | 290 (180–420) | d | high |
| crew_transit_days_nasa | 180 (150–210) | d | high |
| crew_transit_days_spacex_claim | 115 (80–150) | d | medium (claim) |
| fast_transit_days | 97 (90–104) | d | medium |
| surface_stay_conjunction_sols | 525 (450–620) | sol | high |
| total_mission_duration_days | 914 (850–1100) | d | high |
| opposition_stay_days | 60 (30–90) | d | high |
| tanker_flights_per_mars_ship | 12 (10–16) | flights | medium |
| starship_payload_to_mars_surface_t | 100 (75–150) | t | low (claim) |
| starship_return_propellant_t | 1200 (600–1500) | t | low |
| mars_entry_velocity_kms | 7.5 (5.7–9.7) | km/s | medium |
| landing_accuracy_m | 100 (20–2000) | m | speculative |
| landing_site_max_latitude_deg | 40 | °N | high |
| landing_site_max_elevation_km | −2 (prefer ≤ −3) | km MOLA | high |
| comm_blackout_days_per_synodic | 12 (5–20) | d | high |
| dra5_landed_mass_per_lander_t | 56.8 | t | high |
| dra5_imleo_t | 848.7 (NTR) – 1251.8 (chem) | t | high |
| dra5_surface_power_kwe | 30 | kWe | high |
| mars_direct_tmi_throw_t | 47 | t | medium |
| spacex_ships_per_window_ambition_2033 | 500 (5–1000) | ships | speculative |
| crewed_landing_earliest_year | 2031 (2029–2035) | yr | speculative |
| uncrewed_starship_first_window_year | 2028 (2028–2031) | yr | speculative |

---

## 14. References

1. Burke, L.M., Falck, R.D., McGuire, M.L., *Interplanetary Mission Design Handbook: Earth-to-Mars Mission Opportunities 2026 to 2045*, NASA/TM-2010-216764, Oct 2010. https://ntrs.nasa.gov/citations/20100037210
2. Drake, B.G. (ed.), *Human Exploration of Mars Design Reference Architecture 5.0*, NASA-SP-2009-566, 2009 (+ Addendum). https://www.nasa.gov/wp-content/uploads/2015/09/373665main_nasa-sp-2009-566.pdf — Table 4-3 (EDL), §4 (IMLEO/manifests), §5 (ISRU/power), §6.2 (mission classes), §6.5/6.9 (aborts).
3. *Communication Delays, Disruptions, and Blackouts for Crewed Mars Missions*, ASCEND 2022, NASA NTRS 20220013418. https://ntrs.nasa.gov/citations/20220013418
4. Golombek, M., et al., "SpaceX Starship Landing Sites on Mars," 52nd LPSC, abstract #2420 (2021). https://www.hou.usra.edu/meetings/lpsc2021/pdf/2420.pdf
5. Luzzi, E., et al., "Geomorphological Evidence of Near-Surface Ice at Candidate Landing Sites in Northern Amazonis Planitia, Mars," *JGR Planets* (2025), doi:10.1029/2024JE008724.
6. Kingdon, J., "3 months transit time to Mars for human missions using SpaceX Starship," *Scientific Reports* 15:17764 (2025), doi:10.1038/s41598-025-00565-7.
7. Maiwald, V., et al., "About feasibility of SpaceX's human exploration Mars mission scenario with Starship," *Scientific Reports* 14 (2024), doi:10.1038/s41598-024-54012-0.
8. Musk, E., "Making Humans a Multi-Planetary Species," *New Space* 5(2), 2017, doi:10.1089/space.2017.29009.emu; and "Making Life Multi-Planetary," *New Space* 6(1), 2018 (IAC 2017 Adelaide), doi:10.1089/space.2018.29013.emu. (Company claims.)
9. SpaceX Mars colonization program — dated statement compilation (Sep 2024, May 2025, Feb 2026 entries), Wikipedia, accessed Jul 2026. https://en.wikipedia.org/wiki/SpaceX_Mars_colonization_program (secondary; statements cross-checked to Musk/X posts and press).
10. "A Closer Look at SpaceX's Mars Plan," *Aerospace America* (AIAA, 2025). https://aerospaceamerica.aiaa.org/aiaa-spacex/
11. NASA HLS tanker-count statements: L. Hawkins (NASA), Nov 2023 ("high teens"); SpacePolicyOnline/SpaceNews coverage; GAO-24-106256 context.
12. Wooster, P., Braun, R., Ahn, J., Putnam, Z., "Mission design options for human Mars missions," *IJMSE (Mars Journal)* 3:12 (2007) — entry-velocity-constrained trajectory options; free-return abort assessment.
13. Zubrin, R., Baker, D., "Mars Direct: A Simple, Robust, and Cost Effective Architecture..." AAS 90-168 / IAF-91-672 (1991); mass breakdowns via Encyclopedia Astronautica. http://www.astronautix.com/m/marsdirect.html
14. Williams, D.R., *Mars Fact Sheet*, NASA NSSDCA. https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html
15. Allison, M., McEwen, M., "A post-Pathfinder evaluation of areocentric solar coordinates...," *Planet. Space Sci.* 48 (2000) — sol length; NASA GISS Mars24.
