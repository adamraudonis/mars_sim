# Robotic Labor for Mars — Parameter Database (Tesla Optimus + Planetary Robots)

Domain key: `robots`
Scope: humanoid robot (Tesla Optimus class) specifications and realistic utilization on the
Mars surface, robot:human work-rate ratios by task structure, robot maintenance burden, and
flight/prototype planetary-robot anchors (RASSOR/IPEx, ATHLETE, MER/MSL rovers) — everything
needed for a model in which N robots contribute robot-hours/sol against task categories
(excavation, construction, logistics, inspection, greenhouse tending) with quality
multipliers vs. humans.

Compiled July 2026. **All Tesla Optimus numbers are company claims** (Tesla presentations,
Musk statements, earnings calls) — dated and marked as such. As of the Q4-2025 earnings call
(2026-01-28) Musk described the Optimus fleet at Fremont as "primarily for learning, not
productive tasks" and "still very much in the R&D phase", so *no Optimus performance figure
should be treated as field-demonstrated*. Planetary-robot anchors come from NASA/JPL primary
documents (NTRS, ASCEND/ASCE conference papers, mission fact sheets) and are the only numbers
in this domain with flight or TRL-5 test pedigree.

Unit conventions: SI, energy in kWh, time in sols where natural
(1 sol = 88 775 s = 24.6597 h; 1 Mars year = 668.6 sols).

---

## 1. Tesla Optimus — public claims as of mid-2026

### 1.1 Claim table (all values = company claims, with claim dates)

| Quantity | Value | Claim date / venue | Demonstrated? |
|---|---|---|---|
| Mass | 57 kg (125 lb) | Gen 2 unveil 2023-12-12; carried into Gen 3/V3 statements | Hardware shown publicly |
| Height | 173 cm | Gen 2 unveil 2023-12-12 | Yes (shown) |
| Battery capacity | 2.3 kWh, 52 V pack | AI Day 2022-09-30 | Claim only; V3 pack unstated |
| Runtime | "a full work day" / ~8 h light tasks | AI Day 2022 + subsequent blogs | **No** — no independent 8 h demo |
| Idle power | ~100 W | AI Day 2022-09-30 | Claim only |
| Brisk-walk power | ~500 W | AI Day 2022-09-30 | Claim only |
| Payload (carry) | ~20 kg (45 lb) | AI Day 2022; repeated since | Light-object handling shown |
| Deadlift claim | ~68 kg (150 lb) | AI Day 2022; repeated for Gen 3 | Not publicly demonstrated |
| Walk speed | 0.6 m/s (Gen 2 demo, "30% faster"); V3 claim 5 mph ≈ 2.2 m/s | Gen 2 video 2023-12-12; V3 claims 2025–2026 | 0.6 m/s on video; 2.2 m/s not shown |
| Hand DoF | Gen 2: 11 DoF; Gen 3 hand: 22 DoF, tactile fingertips (~50 actuators total robot) | Gen 2 2023-12-12; Gen 3 hand from We,Robot 2024-10-10 and later Musk statements | Teleoperated demos only |
| Body DoF | 28+ (excl. hands) | 2022–2023 presentations | Shown |
| Unit price target | $20k–$30k at scale; "below $20k" (2026 statements) | Musk, Oct 2024; Abundance Summit 2026-03-12 | Not a market price |
| Fleet status | ">1,000 Optimus at Fremont" (Musk, Q4-2025 call 2026-01-28); independent estimates: low hundreds | 2026-01-28 | Sorting 4680 cells, kitting — data-collection tasks |
| Mars claim | "Starship departs for Mars at end of 2026 carrying Optimus" (Musk on X, 2025-03-15); Musk later put 2026 window at "50/50" (May 2025) | 2025 | Aspirational |

Confidence policy: mass/height/battery/hand-DoF are **medium** (hardware publicly shown,
numbers plausible and stable across 3 years of statements); runtime/payload/speed/power are
**low-to-speculative** (unverified, and the one third-party reality-check literature that
exists on humanoids — e.g. RobotWale's battery-runtime survey, 2025 — finds a 30–40 % runtime
shortfall under load and 15–20 %/8 h standby drain vs. spec-sheet claims).

### 1.2 Consistency check on the energy claims

2.3 kWh / 8 h = 288 W average — sits between Tesla's claimed 100 W idle and 500 W
brisk-walk, so the 8 h claim is *internally* consistent only for a light-duty mix
(~50 % idle). At a sustained working draw of 400–500 W the same pack gives **4.5–5.5 h
gross, ~2.5–4 h net of derating** (cold, DoD limits, aging) — matching the independent
humanoid field observation (Digit at GXO runs a 2:1 work:charge ratio; Agility company
statement 2024–2025). NASA's flight-adjacent humanoid anchor points the same way:
**Valkyrie (R5): 125 kg, 44 DoF, 1.8 kWh battery, ≈1 h runtime → ≈1.8 kW bus draw**
(NASA JSC fact sheet / JFR 2015 paper). A space-hardened humanoid drawing 3–4× the Optimus
claim is the conservative bound.

---

## 2. Planetary robot anchors (NASA/JPL primary data)

| Robot | Key numbers | Source / pedigree |
|---|---|---|
| **RASSOR 2.0** (KSC) | mass 66 kg; ≥2.7 t regolith/day; 0.38 kg vehicle mass per (kg/h) excavation rate (→ ~174 kg/h continuous implied); ~4 W per (kg/h) digging power (→ ≈4 kWh/t digging-only); 1410 Wh battery | Mueller et al., ASCE Earth & Space 2016 / NTRS 20210011366; TRL-4 breadboard, Earth testing |
| **IPEx** (ISRU Pilot Excavator, KSC) | "30 kg-class"; mission spec: 10 000 kg excavated at **42 kg/h**, 70 km total traverse, 11-day lunar demo; drive speed **30 cm/s** nominal; wheel-actuator life test passed 1× mission (6.55 M input revs, 5.7 M required) with one bearing failure mode found; motor bus power during life test ~20–25 W/actuator | Schuler et al., "IPEx TRL-5 Design Overview", ASCEND 2024 (nasa.gov PDF); TRL-5, flight-intended design |
| **ATHLETE** (JPL) | Gen 1: 850 kg, 300 kg payload (Earth g); Gen 2: >4 m tall, **450 kg payload** (Earth g), ~10 km/h max; 6 limbs × 6 DoF + wheels; cargo handling/transport for lunar outposts | JPL Robotics ATHLETE pages; Heverly 2010 Tri-ATHLETE paper; Earth prototype only |
| **MER Spirit/Opportunity** | top speed 5 cm/s (commanded max 3.7 cm/s); **~1 cm/s average under autonomous hazard avoidance**; Opportunity lifetime: 45.16 km over 5111 sols (**8.8 m/sol lifetime average**; record 220 m/sol) | NSSDCA MER pages; NASA/JPL mission records; **flight data** |
| **MSL Curiosity** | **28.9 m per driving sol average** (first 7 years; range 0.026–142.5 m); lifetime ~36.9 km / 4942 sols ≈ 7.5 m/sol over all sols | Rankin et al., "Driving Curiosity", IEEE Aerospace 2020, NTRS 20220000780; **flight data** |
| **Perseverance sampling** | one core sample campaign ≈ 1+ week of sols (abrade → science → charge sol → core → seal); rover reserves a full sol to recharge before coring | NASA/JPL Mars 2020 mission blogs 2021–2023; **flight data** |
| **Valkyrie R5** (JSC) | 125 kg, 190 cm, 44 DoF, 1.8 kWh ≈ 1 h runtime | NASA R5 fact sheet; JFR 2015 |

Key inference for the simulation: **flight-heritage autonomous work rates are 2–4 orders of
magnitude below terrestrial-robot rates** (8.8 m/sol traverse vs. 30 cm/s = 26 km/sol-capable
IPEx hardware). The gap is not actuation — it is autonomy, risk posture, power, and the
sol-scale Earth command cycle. A settlement with **local human supervision removes the
command-cycle bottleneck**, which is why settlement robot productivity should be modeled
closer to warehouse-robot rates than to rover rates, *but only for structured tasks*.

---

## 3. Utilization: latency, autonomy, duty cycle

### 3.1 Teleoperation latency (hard physics + human-factors literature)

- Earth–Mars one-way light time: **3.03–22.3 min** (4–24 min commonly quoted incl. margins;
  NASA/ESA). Round trip 6–44 min, plus DSN scheduling and relay store-and-forward.
- Telerobotics literature: direct manipulation teleop degrades sharply above ~300 ms round
  trip; 400 ms is a workable boundary (lunar case); **above ~0.7–1 s round trip, direct
  ("mimicry") manipulation control effectively collapses** and operators shift to
  supervisory/scripted control (Frontiers Robotics & AI 2020 survey; task-completion studies
  show 100 % → 50 % → 10 % completion as delay grows through the 0.15–1.5 s band).
- Conclusion (high confidence): **Earth-based direct teleoperation of manipulation on Mars is
  physically excluded** (RTT ≥ 360 s ≫ 1 s). Earth can only do sol-scale supervisory tasking
  (the MER/MSL mode, with the productivity shown in §2). **Local teleop from the hab
  (RTT < 50 ms over surface links) is fully viable** and is the enabling assumption for
  humanoid usefulness on Mars before/alongside crew EVA.

### 3.2 Duty cycle model (estimates)

Per sol (24.66 h), a settlement humanoid's time splits into: productive work, charging,
thermal survival/idle, maintenance downtime, and tasking/supervision gaps.

- Battery-limited work bouts: ~2.5–4 h per charge under real load (RobotWale survey;
  Digit 2:1 work:charge company claim). Charge time per 2.3 kWh pack: **~1–3 h**
  (no Tesla statement found; 1C–1.5C charging assumed; battery swap could cut this to
  minutes but is unannounced — model as an option). 
- With night operations limited by cold (−80 °C) and power availability at a solar-powered
  base, a defensible planning number is **10–14 productive robot-h/sol** (nominal 12),
  i.e. duty cycle ≈ 0.5. Optimistic bound (24/7 nuclear base, battery swap): 18–20 h/sol.
  Pessimistic (early ops, dust season): 6–8 h/sol.
- Fleet availability (not down for repair): **A ≈ 0.85** (0.70–0.95), see §5.

### 3.3 Supervision ratios (estimates, terrestrial analogies)

- Early ops (first synod with crew): **~3 robots per human supervisor** (range 1–10) for
  mixed task portfolios — anchored on warehouse AMR/humanoid pilots where one operator
  handles a small fleet with intervention-on-exception, and on the fact that Optimus factory
  work in 2025–2026 still involved heavy teleop/intervention.
- Autonomous fraction for *structured* tasks (no human in loop during execution): **~0.7**
  (0.4–0.95 as autonomy matures). For unstructured repair/dexterous tasks assume execution
  is essentially teleop/co-op (autonomous fraction ≤ 0.2) — speculative.
- Pre-crew phase (Earth supervision only): humanoids fall back to sol-scale command cycles;
  apply the *rover* quality multipliers (§4) to anything unstructured. This asymmetry is the
  single most important scheduling feature of the model.

---

## 4. Robot:human work-rate ratios (quality multipliers q_c)

What the literature actually supports, from best- to worst-case task structure:

| Task structure | Ratio q (robot-h → human-equivalent-h) | Evidence |
|---|---|---|
| Purpose-built stationary manipulation (analog: Amazon stow) | **0.92** (224 vs 243 units/h, robot vs human) | Amazon Robotics stow paper, arXiv:2505.04572 (2025) — measured at scale, *not* humanoid |
| Humanoid, structured logistics (analog: Digit @ GXO) | **0.5–0.75** | Agility 100 000-tote milestone (company release 2025-11); ROI-implied throughput 60–75 % of a human picker (press analyses; low confidence) |
| Specialized construction robot, robot-friendly design (analog: Hadrian X) | **2–20× human** for its one task | FBR claims 200–300 blocks/h vs. human mason 300–500/day (company claims 2023) |
| Humanoid, unstructured field/repair work | **0.03–0.3** (assume 0.1) | No deployed evidence; bounded above by structured ratios, below by rover experience; speculative |
| Sol-scale supervised rover science (flight experience) | **~0.001** (a perfect rover sol ≈ 30–45 s of a human field geologist) | Squyres 2005 statement, repeated 2022 (Slate/The End of Astronauts); Crawford, "Dispelling the myth of robotic efficiency," A&G 2012 (arXiv:1203.6250) |

Recommended default quality multipliers by simulation task category (robot-hours × q =
human-equivalent hours), assuming local supervision available and Optimus-class hardware:

| Task category | q default | range | Rationale |
|---|---|---|---|
| Excavation / regolith moving | *rate-based, not q-based* — use kg/h from §2 | — | Specialized robots (RASSOR-class) beat humans-in-suits; don't model as humanoid work |
| Construction (structured, robot-adapted assembly) | 0.5 | 0.3–0.9 | Warehouse-structured analogy |
| Logistics (carry, stow, kitting, cargo transfer) | 0.6 | 0.4–0.9 | Best humanoid evidence class |
| Inspection (routine visual/sensor rounds) | 0.8 | 0.5–1.2 | Repetitive, low-dexterity, robots don't get bored; can exceed 1 for coverage tasks |
| Greenhouse tending (delicate manipulation, biology judgment) | 0.25 | 0.1–0.5 | Ag-robotics (harvest robots reach 50–80 % human pick rates on *single crops* after years of tuning); mixed-crop judgment much worse |
| Maintenance/repair of complex equipment | 0.15 | 0.05–0.3 | Unstructured; ISS experience: even human repair is hard |
| EVA-substituting *any* task | multiply q by **EVA leverage ≈ 1.5–3** | — | Robot outdoor hours avoid EVA overhead (prep/suit/airlock ≈ 50 % of the lunar-habitat maintenance estimate in Stromgren 2021: 12.5 of 24.7 h was EVA overhead) and retire crew risk |

The human side of the ratio: one crew member ≈ **6.5 productive h/sol** (NASA crew-time
models), of which EVA hours are scarce (~2 × 4 h EVAs/week sustainable). One Optimus-class
robot at 12 h/sol × A 0.85 × q 0.5 ≈ **5.1 human-equivalent h/sol ≈ 0.8 crew-equivalents**
for structured outdoor work — *if* the hardware works as claimed. At q = 0.1 it is ~0.16
crew-equivalents. This ×5 spread is the dominant uncertainty in the whole domain.

---

## 5. Maintenance burden of the robots themselves

- **MTBF, field humanoid on Mars:** no data exists; recommend **500 h** between
  failures-requiring-intervention (range 100–2000 h), speculative. Bounds: FANUC-class
  factory arms claim **80 000 h MTBF** (vendor claim, benign environment, 6-DoF arm — an
  upper bound that a 50-actuator dusty cold humanoid will not approach; note The Robot
  Report's caution that *cell-level* MTTF in practice is minutes-to-hours scale and ~88 %
  availability); IPEx wheel-actuator TRL-5 life test barely exceeded 1× its 11-day mission
  requirement before a bearing failure mode appeared — i.e., ~250 h-class life for a
  flight-intended dusty actuator at current TRL.
- **Maintenance labor:** ~**3 h per 100 robot-operating-hours** (1–8), speculative — implies
  each robot at 12 h/sol consumes ~0.36 h/sol of technician time; a 10-robot fleet ≈ 0.55
  crew-equivalents of maintenance if humans do it all. Robots can absorb part of this
  (q_maint ≈ 0.15) — the model should close this loop and check it converges
  (N·m·(1−r_self·q) < crew time budgeted).
- **Spares:** **10 %/Earth-year of fleet mass** (5–25 %), low confidence — anchored on
  Owens & de Weck ISS-derived supportability analyses (ISS consumes ~10 t/yr of
  spares/maintenance upmass; their Mars-One assessment found spares grow to **64 % of
  resupply mass by year 10**). With 26-month synods, each window must carry ≥2.2 years of
  spares plus reserve; recommend carrying 25–30 % of fleet mass per synod.
- **Fleet availability:** A = MTBF/(MTBF + MTTR_eff) with MTTR_eff including diagnosis,
  spares wait, and queue; **0.85** (0.70–0.95).

---

## 6. Power draw per robot-work-hour

Electrical energy per *productive* robot-hour, humanoid class, including charge inefficiency
and Mars thermal overhead:

E_h = P_work/η_chg + P_therm·(t_idle/t_work)

- P_work: 400–500 W claimed (Optimus); 1.8 kW measured-class (Valkyrie). 
- η_chg ≈ 0.90; Mars cold adds battery/joint heating, est. +50–150 W averaged.
- **Recommended: 0.6 kWh per productive robot-hour (range 0.3–1.8)** → ~7 kWh/sol per robot
  at 12 h/sol, ~9 kWh/sol including night survival heating (est. 1–3 kWh/night, speculative;
  scale with habitat thermal domain). A 100-robot fleet is then ~0.9 MWh/sol — comparable to
  ~25–40 kW continuous, a first-order load on the power domain.
- Specialized excavation: **≈4 kWh/t digging-only** (RASSOR 2.0 paper's 4 W per kg/h);
  multiply ×2–3 for drive/avionics/thermal → **8–12 kWh/t delivered** total-system estimate
  (low confidence). Cross-check: RASSOR's 1410 Wh battery and 2.7 t/day imply several
  charge cycles per day at these rates — consistent.

---

## 7. Parameter table (see robots.json for machine-readable form)

| id | value | unit | conf. |
|---|---|---|---|
| robots.optimus.mass | 57 | kg | medium (claim) |
| robots.optimus.battery_capacity | 2.3 | kWh | medium (claim) |
| robots.optimus.runtime_claimed | 8 (2.5–8) | h | speculative |
| robots.optimus.payload_carry | 20 (20–68 deadlift) | kg | medium (claim) |
| robots.optimus.walk_speed | 0.6 (0.5–2.2) | m/s | medium (demo) |
| robots.optimus.hand_dof | 22 (11–22) | DoF | speculative |
| robots.optimus.power_idle | 100 | W | low (claim) |
| robots.optimus.power_working | 500 (300–1800) | W | low |
| robots.ops.oneway_light_time | 12.5 (3.03–22.3) | min | high |
| robots.ops.direct_teleop_rtt_limit | 0.7 (0.3–1.5) | s | medium |
| robots.ops.productive_hours_per_sol | 12 (8–18) | h/sol | low |
| robots.ops.charge_time | 2 (1–4) | h | speculative |
| robots.ops.robots_per_supervisor | 3 (1–10) | robots | speculative |
| robots.ops.autonomous_fraction_structured | 0.7 (0.4–0.95) | – | speculative |
| robots.work.q_structured | 0.5 (0.3–0.9) | – | low |
| robots.work.q_unstructured | 0.1 (0.03–0.3) | – | speculative |
| robots.work.q_rover_science_legacy | 0.0014 (0.0005–0.002) | – | medium |
| robots.work.excavation_rate_specialized | 42 (42–174) | kg/h | medium |
| robots.work.excavation_energy_digging | 4 (4–12) | kWh/t | medium/low |
| robots.work.construction_speedup_specialized | 5 (2–20) | × human | low |
| robots.maint.mtbf_field | 500 (100–2000) | h | speculative |
| robots.maint.labor_per_100h | 3 (1–8) | h/100 robot-h | speculative |
| robots.maint.spares_mass_fraction | 0.10 (0.05–0.25) | fleet-mass/yr | low |
| robots.maint.fleet_availability | 0.85 (0.70–0.95) | – | low |
| robots.power.energy_per_work_hour | 0.6 (0.3–1.8) | kWh/h | low |
| robots.anchor.rassor2_excavation | 2.7 | t/day | medium |
| robots.anchor.athlete_payload | 450 (300–450) | kg (Earth g) | high |
| robots.anchor.curiosity_drive_per_sol | 28.9 (7.5–142.5) | m/driving-sol | high |
| robots.anchor.valkyrie_bus_power | 1.8 (1.0–2.0) | kW | medium |
| robots.demand.habitat_corrective_maint | 24.7 (9.0–70.5) | crew-h/28 d | medium |

---

## 8. Governing model

**Core bookkeeping (all tiers).** For robot type r, task category c, sol t:

RH(t) = Σ_r N_r · A_r · H_r(t)                         [robot-hours/sol]
HEH_c(t) = f_c(t) · RH(t) · q_c · s(t)                  [human-equivalent h/sol]
subject to: Σ_c f_c = 1; supervision constraint Σ_r N_r·(1−a_r) ≤ S·ρ  (S = supervisor
crew-h/sol available, ρ = robots per supervisor); energy constraint Σ N·H·e_h ≤ E_alloc;
maintenance closure: N·H·m = human tech hours + robot self-maint hours/q_maint.

Excavation is rate-based, not q-based: ṁ_reg = N_exc · A · H_exc · R_exc, energy
E_exc = ṁ_reg · e_dig · k_overhead.

**Disagreements between sources (carry as ranges, not point values):**
1. Optimus runtime: 8 h (Tesla claim) vs 2.5–4 h under load (independent humanoid surveys).
2. Optimus payload: 20 kg carry vs 68 kg deadlift — blogs conflate them; carry is the
   planning number.
3. Walk speed: 0.6 m/s demonstrated vs 2.2 m/s claimed (V3) — 3.7× gap.
4. Robot:human ratio spans 0.001–0.92 (three orders of magnitude) depending entirely on task
   structure and supervision locality. Any single "robots are X% of a human" scalar is wrong;
   the task-category split is load-bearing.
5. MTBF: 80 000 h (vendor, factory arm) vs ~250 h-class TRL-5 dusty-actuator life (IPEx) vs
   88 % cell availability in practice — we anchor field MTBF at 500 h, but this is the
   least-supported number in the domain.
6. RASSOR 2.7 t/day ("minimum", TRL-4 claim) vs implied 4.2 t/day continuous — duty cycle
   ~0.65 hides inside the headline.
7. Curiosity 28.9 m/driving-sol vs 7.5 m/sol lifetime-average — pick denominator carefully;
   we expose both.

## 9. Fidelity tiers

**L0 — single scalar average.**
Robot-hours/sol = N · 0.85 · 12 = 10.2 per robot. Human-equivalent output = 10.2 · q_c
(defaults §4). Energy = 7 kWh/sol/robot. Maintenance = 0.36 h/sol/robot of tech time +
10 %/yr fleet mass as spares. Excavation by RASSOR-class units: 2.7 t/sol/unit at ~11 kWh/t.
Good enough for settlement-level sizing; the q_c defaults carry the uncertainty.

**L1 — analytic daily/seasonal model.**
Sol-indexed: H(t) = H_max · k_power(t) · k_dust(t) · k_autonomy(t) where k_power follows the
solar/nuclear energy allocation (couples to power domain; solar bases lose robot-hours in
dust season exactly when maintenance demand spikes), k_dust ∈ {1, storm derate 0.2–0.5},
k_autonomy ramps 0.6 → 1.0 over the first 2–3 synods (learning curve). Fleet aging:
A(t) from a Weibull hazard (shape 1.5–2, characteristic life = MTBF·Γ-corrected), spares
inventory decremented per failure; when inventory hits zero, failed robots become parts
donors (cannibalization: recovers ~50 % of demand, halves fleet). Supervision constraint
binds pre-crew (Earth-only: force q_unstructured → q_rover for manipulation) and relaxes
on crew arrival — model pre-crew and crewed phases with different q-vectors.

**L2 — physics-based sub-sol timestep (minutes).**
Per-robot state: SOC E(t), temperature T(t), task assignment, health.
- Energy: dE/dt = η_c·P_chg·u(t) − P_base − P_task(task, payload, terrain) − P_htr(T_env)
  with P_htr = max(0, (T_set − T_env)/R_th − Q_waste); T_env(t) from the environment domain
  (diurnal −80…0 °C); charging only when docked; optional battery-swap = instantaneous
  SOC restore at logistics cost.
- Work: dW_c/dt = q_c · s(D(t)) · 1{assigned} with dust-loading degradation s and stochastic
  intervention events (Poisson, rate = (1−a)·λ_int) that block a teleoperator slot for
  MTTR_int ~ lognormal(15 min median).
- Reliability: per-actuator hazard λ(t) = λ_0·(1 + k_dust·D(t))·(1 + k_cold·1{T<T_min});
  robot down → repair queue; repair consumes tech-hours + spares from inventory.
- Excavation: ṁ = min(R_max, (P_avail − P_drive)/e_dig), traverse at 0.3 m/s IPEx-class,
  haul-cycle geometry (dig site ↔ dump site distance) explicit.
- Fleet scheduler: assign robots to task queues per sol respecting charge windows,
  supervisor availability (local teleop slots), and daylight/power windows.

## 10. Sources

1. Tesla AI Day presentation, 2022-09-30 (battery 2.3 kWh, 100 W/500 W, payload/deadlift, <$20k target) — company claims. Coverage: Forbes, Teslarati.
2. Tesla Optimus Gen 2 unveil video/posts, 2023-12-12 (57 kg, 11-DoF hands, faster walk) — company claims. Coverage: Electrek.
3. Musk statements on Optimus V3/Gen 3, Oct 2024 – Mar 2026 (22-DoF hand, 5 mph, mass production summer 2026, ~1 M/yr capacity, <$20k, >1,000 units at Fremont, "primarily learning" Q4-2025 call 2026-01-28) — company claims; Wikipedia "Optimus (robot)".
4. Musk on X / press, Mar–May 2025: Optimus on 2026 Mars Starship, "50/50" odds (Al Jazeera 2025-05-30; digitaltrends; space.com).
5. Schuler, J.M. et al., "ISRU Pilot Excavator (IPEx) TRL-5 Design Overview," ASCEND 2024. https://www.nasa.gov/wp-content/uploads/2024/08/ascend24-ipex-trl-5-design-overview.pdf
6. Mueller, R. et al., RASSOR 2.0 design papers, ASCE Earth & Space 2016; NTRS 20210011366; RASSOR overview NTRS 20130008972; Schuler LSIC 2019 abstract 5061.
7. JPL Robotics, "The ATHLETE Rover" (robotics.jpl.nasa.gov); Heverly et al., Tri-ATHLETE, AMS 2010; Wikipedia ATHLETE.
8. Rankin, A. et al., "Driving Curiosity: Mars Rover Mobility Trends During the First Seven Years," IEEE Aerospace 2020; NTRS 20220000780.
9. NSSDCA Mars Exploration Rover pages (5 cm/s top, ~1 cm/s hazard-avoidance average); NASA/JPL Opportunity mission records (45.16 km, 5111 sols, 220 m record sol 410).
10. NASA/JPL Mars 2020 sampling blogs, 2021-2023 ("sampling takes over a week"; recharge sol before coring).
11. NASA R5/Valkyrie fact sheet; Radford et al., "Valkyrie: NASA's First Bipedal Humanoid Robot," J. Field Robotics 2015 (125 kg, 1.8 kWh, ~1 h).
12. Stromgren, C., Lynch, C., Cho, J., Cirillo, W., Owens, A., "Assessment of Crew Time for Maintenance and Repair Activities for Lunar Surface Missions," IEEE Aerospace 2021; NTRS 20210026843 (24.7 h expected corrective maintenance per 28-d 2-crew mission; 12.52 h EVA share; 99th pct 70.5 h).
13. Owens, A., de Weck, O., MIT space-logistics/supportability work incl. Mars One assessment (2014): spares → 64 % of resupply mass by year 10; ISS ≈ 10 t/yr spares upmass (NTRS 20150003005, 20220006045).
14. Agility Robotics press: "Digit Moves Over 100,000 Totes in Commercial Deployment" (2025-11); GXO multi-year agreement (2024); Digit specs (~65 kg, 16 kg payload) — company claims.
15. "Stow: Robotic Packing of Items into Fabric Pods," Amazon Robotics, arXiv:2505.04572 (2025): robot 224 UPH vs human 243 UPH measured at scale.
16. FANUC reliability marketing (">80,000 h MTBF") and The Robot Report, "The fake news about robots and their reliability" (cell availability ~88 %) — vendor claim + practitioner correction.
17. Telerobotics latency: "A Brief Survey of Telerobotic Time Delay Mitigation," Frontiers Robotics & AI 2020; vision-teleop degradation studies (arXiv 2603.06850); surgical/vehicle teleop latency literature (300–700 ms thresholds).
18. Earth–Mars light time: ESA Mars Express blog (3.03–22.3 min); NASA ASCEND paper NTRS 20220013418 (crewed-Mars comm delays 15–20+ min one-way).
19. FBR Hadrian X company statements (200–300 blocks/h; 2023 speed records) vs human mason 300–500 bricks/day — company claims.
20. RobotWale, "Humanoid Robot Battery Runtime: Claimed vs Real-World" (2025): 30–40 % runtime shortfall under load; 15–20 %/8 h standby drain — trade-press survey, low confidence.
