# Nuclear Surface Power for Mars — Parameter Research

**Domain key:** `power_nuclear`
**Compiled:** July 2026, from primary sources (NASA NTRS documents fetched and read in full where noted).
**Conventions:** SI units, energy in kWh, 1 sol = 88,775 s = 24.6597 h, 1 Mars year = 668.6 sols.

---

## 1. Flight-demonstrated anchors

### 1.1 KRUSTY nuclear ground test (the only modern datum with fission "flight-like" hardware)

Source: Gibson, Poston, McClure, Godfroy, Briggs, Sanzi, *"The Kilopower Reactor Using Stirling TechnologY (KRUSTY) Nuclear Ground Test Results and Lessons Learned,"* AIAA Propulsion & Energy 2018, NTRS 20180005435 (full text read).

| Quantity | Value | Notes |
|---|---|---|
| Test date / duration | 20–21 Mar 2018, 28 h continuous | Duration capped by fission/activation limit, not hardware |
| Steady-state thermal power | > 4 kWt @ 800 °C core | Objective 1 met; same core would give 8 kWt at 800 °C with bigger converters |
| Stirling converters | 2 × Sunpower 80 We-class (95 We + 88 We = 183 We sustained) | 6 positions filled by thermal simulators (up to 600 Wt each) |
| Stirling thermal efficiency | **30–34 %** (~50 % of Carnot) | Measured by gas calorimetry at steady state |
| Passive load following | Core temp deviation < 4 % of 800 °C setpoint under > 175 % thermal-load swings | No control-rod motion needed; negative temperature feedback |
| Total loss-of-cooling transient | +14 °C overshoot, damped in ~1 h | Bounding off-nominal case |
| Startup (cold to 800 °C) | ~1.5 h (test, "worst-case fast"); flight plan ~10 °C/min ramp | Very little stored energy needed (rod motor + controllers only) |
| Reactivity margin / droop | ~10 °C/yr core-temperature droop if rod untouched (1 kWe design); ≈ $0.02/yr excess reactivity to hold setpoint | Governs "degradation" of a Kilopower-class reactor |
| Power turn-down | up to 16:1 (engine amplitude + selective shutdown), more with rod | |
| TRL after test | 5 (reactor nuclear + mechanical systems) | |

### 1.2 MMRTG (small radioisotope reference point, flying on Mars now)

Sources: NASA/JPL MMRTG fact sheet; Wikipedia MMRTG page (checked Jul 2026); Curiosity (2012–) and Perseverance (2021–) flight experience.

| Quantity | Value |
|---|---|
| Electrical power | ~110 We at beginning of Mars mission (design 125 We BOL, ≥100 We at 14 yr) |
| Thermal power | ~2,000 Wt (4.8 kg PuO₂, ⁸⁷·⁷-yr half-life ⁲³⁸Pu) |
| Mass | 45 kg → ~2.4 We/kg |
| Conversion efficiency | ~6 % (thermoelectric) |
| Degradation | ~1.8 %/yr from the design curve (125→100 We in 14 yr). ⁲³⁸Pu decay alone is 0.79 %/yr; thermocouple degradation adds the rest. Flight-data analyses give effective compounded rates from ~1.3 %/yr to ~4.8 %/yr early-mission depending on fit window — see §11 Disagreements. |
| Design life | ≥14 yr (Perseverance unit rated up to 17 yr) |

The MMRTG is the correct "small" end-member for the sim: rover-scale keep-alive and instrument power, **not** habitat/ISRU power (that gap is exactly what Kilopower/FSP fills — 10–1000× more power).

---

## 2. Kilopower flight designs (1 kWe and 10 kWe)

Sources: Smith/Mason/Palac/Gibson, *"Kilopower: Small & Affordable Fission Power Systems for Space,"* NTRS 20180000691 (read); Gibson et al. IEEE Aerospace 2017; Rucker AIAA 2016-5452 (read, see §9); Kilopower Wikipedia for the core masses.

| Quantity | 1 kWe | 10 kWe |
|---|---|---|
| Complete system mass (NASA-quoted) | **~400 kg** | **~1,500 kg** |
| Reactor thermal power | 4.3 kWt | 43.3 kWt |
| Core (U-235 metal, cast UMo) | 28 kg core | 226 kg core (43.7 kg U-235) |
| Specific power | ~2.5 We/kg | ~6.7 We/kg |
| Design life | 12–15 yr (NASA materials say "10 years or more"; several NASA charts say 12–15) | same |
| Conversion | 8 × 125 We free-piston Stirling | Stirling array, ~25 % system-level |
| Heat rejection (10 kWe Stirling option) | — | ~9.6 m² cylindrical/parasol radiator (Mason/Gibson/Poston, NTRS 20140010823) — small because rejection is at ~500 K |

**Mars, crew-rated variant (the number a Mars sim should use):** the COMPASS point design for the Rucker 2016 study put one 10 kWe Kilopower for Mars at **1,754 kg including a 15 % mass-growth allowance and a radiation shield sized to keep crew exposure < 3 mR/h at 500 m**, with a fixed conical upper radiator needing no deployment, 6 m landed footprint × 5.14 m height, 106 W keep-alive power after landing, in a 2,751 kg total demonstrator payload. Press statements by the Kilopower team (space.com, 2019) quote "~2,000 kg with shielding, less if buried." So: 1,500 kg (lightly shielded science config) → 1,750–2,000 kg (crew-rated shadow shield) per 10 kWe unit.

---

## 3. NASA Fission Surface Power (FSP) program — 40 kWe class

### 3.1 2010 concept (the buried-reactor reference)

*Fission Surface Power System Initial Concept Definition*, NASA/TM-2010-216772: 40 kWe net, pumped-NaK reactor + Stirling, **5,800 kg (145 kg/kWe)**, 8-yr full-power life, reactor emplaced in a **~2 m excavation so that regolith limits dose to < 5 rem/yr at 100 m** — the key data point for buried/berm siting.

### 3.2 2022 industry contracts (Phase 1)

NASA/DOE awarded three 12-month, **$5M** contracts (June 2022): Lockheed Martin (+BWXT, Creare), Westinghouse (+Aerojet Rocketdyne), IX = Intuitive Machines + X-energy (+Maxar, Boeing). Requirements: **40 kWe for 10 years**, HALEU fuel, no crew intervention for a decade; goals: **≤ 6,000 kg** total incl. growth, stow in 4 m dia × 6 m, ≥5 kWe after any single fault, transportable/relocatable, <5 rem/yr at 1 km. Contracts extended into Phase 1a (Jan 2024).

### 3.3 Government reference design (best openly-published numbers)

Mason, Kaldon, Corbisiero, Rao, *"Key Design Trades for a Near-term Lunar Fission Surface Power System,"* NETS 2025, NTRS 20250000841 (full text read). UN fuel pins (19.75 % HALEU), YH moderator, 4 × 25 % independent power strings (≥75 % power after any single failure):

| | HP-Stirling | GC-Brayton |
|---|---|---|
| Net power | 40 kWe | 40 kWe |
| Reactor thermal power | 175 kWt | 230 kWt |
| **Net system efficiency** | **23 %** | **17 %** |
| Converter / PMAD efficiency | ~ (4×12.6 kWe gross) / 83 % | (4×11.7 kWe gross) / 90 % |
| Cycle hot / cold end | 1050 K / 420 K | 1100 K / 390 K |
| Main radiator | 140 m², 403–433 K, rejects 119 kWt | 180 m², 379–504 K, 180 kWt |
| Reactor assembly | 1,132 kg | 1,413 kg |
| Shield (LiH/B₄C/W shadow) | 1,370 kg | 1,500 kg |
| **Total with 20 % MGA** | **6,820 kg (170 kg/kWe)** | **7,502 kg (188 kg/kWe)** |

The 2022 COMPASS deployable version (Oleson et al., NETS 2022, NTRS 20220004670, read in full) came in at **10,046 kg** (incl. MGA + 15 % margin) plus a 2,080 kg deployment rover — i.e., the 6 t goal has *not* been met by any full-fidelity design; 6.8–10 t is the honest range for 40 kWe.

### 3.4 2025 redirection (current program of record, mark as agency claims)

August 2025 NASA directive (Duffy): FSP moved to ESDMD, target raised to **100 kWe-class**, closed-Brayton, mass allocation up to **15 t** on a heavy lander, **launch-ready by Q1 FY2030 (late CY2029)**; FY2026 budget request $350M "Mars Technology" line growing to ~$500M/yr, explicitly Mars-forward (ANS Nuclear Newswire, 2 Sep 2025). Schedule confidence: low (prior FSP milestones have slipped repeatedly; SpaceNews reported fiscal challenges through 2024–2025). Note 100 kWe @ ≤15 t = ≤150 kg/kWe, consistent with the scaling law below.

---

## 4. Mass/power scaling, 10–1000 kWe

Fitting the Mason NETS-2025 DAC3 table (masses include 20 % MGA):

| kWe | HP-Stirling mass | kWt | radiator | GC-Brayton mass |
|---|---|---|---|---|
| 10 | 2.7 t (270 kg/kWe) | 49 | 40 m² | 3.4 t |
| 20 | 4.1 t (205) | 90 | 80 m² | 4.6 t |
| 40 | 6.8 t (170) | 175 | 140 m² | 7.5 t |
| 80 | 10.7 t (134) | 343 | 279 m² | 11.0 t |
| 100 | 12.6 t (126) | 429 | 351 m² | 12.5 t |

**L1 scaling law (this work, least-squares on the table above):**

    M_FSP[kg] ≈ 562 · P^0.673   (P in kWe, HP-Stirling, valid 10–100 kWe, R² > 0.999)
    M_FSP[kg] ≈ 863 · P^0.580   (GC-Brayton; crossover ≈ 90 kWe, Brayton lighter above)

Extrapolation: 200 kWe → ~20 t (100 kg/kWe); 1,000 kWe → ~59 t (~59 kg/kWe). Treat >100 kWe as low confidence: the exponent flattens because reactors scale superbly (kWt/kg) while radiators scale linearly, but new failure modes (large deployables, PMAD voltage) appear. Anchor at the MW end: the MTAS NEP study (Mason et al., NASA/TM-20210016968, read in full) got **13 kg/kWe (HALEU) / 11 kg/kWe (HEU) at 1.9 MWe** — but that is an *in-space* system with a shadow-shield dose limit of 50 rem/yr, 2-yr life, 1,200 K SCO₂ Brayton, and 2,500 m² radiator; a Mars *surface* MW-class plant with 5 rem/yr crew dose and 10-yr life will land between the two curves (engineering estimate: 30–60 kg/kWe at 1 MWe). Same TM: HALEU costs ~3,500 kg extra vs HEU at 10 MWt (7,600 vs 4,100 kg reactor+shield) — the HALEU-policy mass penalty.

Multiple-small vs one-large (NETS 2025): 4 × 10 kWe = 10 t vs 1 × 40 kWe = 6.8 t (+ grid); 2 × 20 kWe = 8 t (+17 %). Redundancy costs ~20–50 % mass.

---

## 5. Shielding, keep-out distance, buried/berm siting

Design dose limits used across NASA FSP work (NETS 2025, Table + FSP SOW):
- **Crew: < 5 rem/yr (50 mSv/yr) at the habitat boundary**, assumed 1 km away, 100 % occupancy.
- Power-conversion hardware at ~1 m: ≤10 MRad gamma, 5×10¹⁴ n/cm² (>100 keV).
- Controller electronics at 10–50 m: ≤25–300 kRad.

**Shadow shield (surface-sited reactor):** LiH/B₄C neutron layers + tungsten-alloy gamma layer, shaped to a ~36° included angle covering a 1-km-diameter outpost at 1 km. Masses (40 kWe HP-Stirling): **1,370 kg at 1 km separation; ~2,700 kg at 0.5 km (52° angle); ~700 kg at 2 km (22°)**. System-mass optimum separation ≈ 1.8 km but nearly flat 1–2.5 km (cable mass ~ linear: the 1-km, ±2800 VDC cable is only ~45 kg at 95 % efficiency for 43.5 kWe; a 3-km cable is 240 kg at 2800 V vs 3,300 kg at 500 V — COMPASS NETS 2022). **Outside the shielded cone the dose at 1 km is still ~50 rem/yr** → the sim should model an all-azimuth keep-out zone except the shielded sector, or a full-perimeter berm.

**Buried / berm option:** 2010 FSP concept: reactor in a ~2 m hole, regolith shielding → **< 5 rem/yr at only 100 m**, and the 5.8 t system carries almost no shield mass. Trade: requires excavation equipment and placement ops (regolith moving ≈ several tonnes of machinery + crew/robot time) vs ~1.4–2.7 t of launched shield + 1 km of cable. Kilopower team statements: 10 kWe unit "~2,000 kg with shielding, could be pared down if buried." Mars regolith (ρ ≈ 1,500 kg/m³) needs roughly 2–3 m thickness for full 4π protection near the unit.

**Post-shutdown access:** after 10 yr operation, a reactor needs an extended cool-down (**~months**) before short-term crew approach for service/relocation (NETS 2025). No maintenance is planned within the 10-yr design life.

---

## 6. Heat rejection on Mars

Governing equation (per unit radiator area, both sides if deployed panel):

    q = ε σ (T_r⁴ − T_sink⁴) · F + h_conv (T_r − T_atm)

with ε ≈ 0.85 end-of-life, α_s ≈ 0.2 (FSP panel spec); T_r = 403–433 K (Stirling cold end 420 K); Mars effective sink: sky ≈ 150–170 K, ground 170–270 K, T_atm ≈ 180–250 K; h_conv ≈ 1–5 W/m²K in 600–800 Pa CO₂ (wind-dependent). At T_r = 418 K radiation dominates (εσT⁴ ≈ 1.47 kW/m² one-sided ideal); flight-type panel packing, view factors, fin effectiveness and margin bring the realized flux to ~0.85 kW/m² (140 m² rejecting 119 kWt, NETS 2025).

**Sizing rule of thumb: 3.5–4.0 m² of deployed two-sided radiator per kWe net** for FSP-class systems at ~420 K rejection (lunar 270 K design sink; Mars sink is colder on average, so lunar-sized radiators are conservative on Mars — but add margin for dust settling on panels and ~62 % more area if rejection were sized for a lunar-equator-like hot case, per NETS 2022). Kilopower shows the other corner of the trade: reject at ~500 K and 10 kWe needs only ~9.6 m² (≈1 m²/kWe) at lower cycle efficiency. Vertical panel orientation minimizes dust loading and ground IR view factor.

---

## 7. Deployment, startup, crew time

- **Requirement (FSP SOW 2021/2022): self-contained; no crew or robotic support for startup, operation, or maintenance for 10 yr.** Nominal crew time ≈ 0.
- Autonomous startup sequence (NETS 2025): initiate criticality at low power → warm heat-pipes/coolant → start converters one at a time → deploy radiators using their own waste heat (avoids water freeze) → ramp to full power. **Entire sequence < 8 h**, powered by a 4 kWh battery + 1 kWe auxiliary array. KRUSTY demonstrated 1.5 h cold-to-hot as a bounding fast start.
- Deployment (COMPASS NETS 2022, lunar analog): a ~2 t robotic rover moves the reactor ≥1 km from the outpost in 1–2 trips, then places electronics 50 m away and unreels the 1-km cable to the user pallet (~870 kg). Timeline of order days, not months. On/off cycling and relocation are design goals (DG-3, DG-6).
- Crew involvement realistically enters only for: cable hookups at the user interface (safe, 1 km from reactor), site survey, and contingency — engineering estimate 0–40 crew-hours per unit, speculative.

---

## 8. Degradation and reliability

- **Fission output is essentially flat over life.** The reactor is a constant-temperature device; burnup reactivity loss appears as ~10 °C/yr temperature droop (1 kWe Kilopower) if unmanaged, or is nulled with ~$0.02/yr control-rod motion. Fuel burnup < 1 % over 10 yr (FSP TRL table). Electrical degradation is driven by converter/radiator aging: no wear-out mechanism demonstrated for free-piston Stirlings (multi-year NASA endurance tests); dust settling on Mars radiators may claw a few % over a decade. **Sim recommendation: 0–1 %/yr (0.5 %/yr nominal, low confidence), plus discrete string failures.**
- **Fault tolerance is discrete, not gradual:** 4 × 25 % strings → any single failure leaves ≥ 75 % power (design requirement ≥ 5 kWe residual). COMPASS 40 kWe: converter failures forced in pairs, −25 % each pair.
- MMRTG: continuous compounding decline ~1.8 %/yr design (see §1.2 and §11).

---

## 9. The NASA solar-vs-fission Mars trade (Rucker et al.) — numbers the sim must reproduce

Source: M. Rucker, *"Solar vs. Fission Surface Power for Mars,"* AIAA SPACE 2016 (AIAA 2016-5452), NTRS 20160002628 / 20160011275 (presentation read in full; COMPASS team study).

**Crewed mission (4 crew, ~500 sols, ISRU making 23 t LOX in 420 days, Jezero 18.9° N vs Columbus 29.5° S):**
- Power need: **~26.4 kWe peak cargo phase (ISRU 19.7 kWe + MAV 6.7 kWe), ~31.1 kWe peak / ~14.8 kWe keep-alive crew phase.**
- Fission: **4 × 10 kWe Kilopower (+1 spare) = 35 kWe continuous**, generation mass **9,154 kg incl. spare + MGA** (paper's headline apples-to-apples number for 4 units without spare: **~7,000 kg**), same mass at any latitude/season/weather.
- Solar: distributed UltraFlex (4 × 12 m dia per lander) + Li-ion (165 Wh/kg, 60 % DoD), sized to survive a **120-sol dust storm at optical depth τ = 5** providing **22 kWe keep-alive / 35 kWe clear-sky peak**: **~9,800 kg** (paper headline; presentation MEL: 11,713 kg at Jezero, 12,679 kg at Columbus, both excluding lander-to-lander PMAD which "could add a metric ton per lander").
- Headline: **50 kWe of fission ≈ 20 % less landed mass than 35 kWe solar at Jezero for Expedition 1**; by Expedition 3 cumulative solar mass exceeds 2× fission, but the accumulated array area then rides through storms with little disruption. Diffuse light during storm ≈ 30–40 % of clear direct (Opportunity data). Solar hardware ~$100M cheaper for the demo mission; fission has lower TRL and higher development cost but full dust-storm immunity.
- Equatorial ISRU demonstrator (10 kWe unit, 6.45 kWe delivered): fission payload 2,751 kg vs solar options 1,128–2,425 kg — fission *loses* the small-demo trade at the equator.

**2024 decision:** NASA's Moon-to-Mars 2024 Architecture Concept Review formally **baselined nuclear fission as the primary surface power technology for crewed Mars missions** (ACR24 white paper, read in full), citing loss-of-mission risk, dust storms, mass/volume advantages; minimum ~10 kWe for a minimal 2-crew/30-sol mission, hundreds of kWe to MW-class for ISRU-heavy architectures. Solar flux at Mars ≤ 45 % of Earth's.

---

## 10. Cost & availability caveats (mostly agency claims — treat as low-confidence programmatics)

| Item | Value | Basis |
|---|---|---|
| DUFF proof-of-concept (2012) | < $1M, < 6 months, 24 We | NTRS 20180000691 |
| Kilopower/KRUSTY project | $15–20M, 3.5 yr, TRL 5 | NASA quotes "3 years and $15M"; ~$20M widely cited incl. DOE |
| FSP Phase 1 (2022) | 3 × $5M, 12 mo, concepts only | NASA award announcements |
| FSP flight demo (current) | $350M FY26 → ~$500M/yr FY27 request (whole Mars Technology line); launch-ready target Q1 FY2030 | Aug 2025 directive; schedule confidence low |
| MMRTG availability | ⁲³⁸Pu production ~1.5 kg/yr (DOE target); each MMRTG needs 4.8 kg PuO₂ → ~1 unit every 2–3 yr without stockpile draw | DOE/ORNL supply program, low confidence on exact rate |
| Fission for a *settlement* sim | No flight-qualified unit exists as of mid-2026; first flight article NET ~2030 (Moon), Mars-qualified later. Development cost of the class is $0.5–2B (est.) | Speculative |

---

## 11. Disagreements between sources

1. **Kilopower 10 kWe mass:** ~1,500 kg (NASA overview, minimal shield) vs 1,754 kg (COMPASS Mars, crew shield + 15 % MGA) vs ~2,000 kg (team press statements with shielding). Resolution: shielding assumption; use 1,750–2,000 kg for crewed proximity, 1,500 kg for remote/science.
2. **Rucker fission mass:** 7,000 kg (paper abstract, 4 units) vs 9,154 kg (presentation MEL incl. 5th spare unit + Stirling PMAD). Both correct; different scope. Sim should carry the spare explicitly.
3. **40 kWe FSP mass:** 6 t (goal) vs 5.8 t (2010 buried concept, 8-yr life, older tech + regolith shielding) vs 6.8–7.5 t (2023–25 government reference, 20 % MGA) vs 10 t (2022 fully-deployable COMPASS with 15 % margin stacked on MGA). Resolution: siting and margin policy dominate; use 6.8 t (surface-sited, shadow shield) and 5.8–6 t (buried) as the two branches.
4. **Kilopower design life:** "10 years or more" vs "12–15 years" across NASA materials. Use 10 yr contractual / 12 yr nominal / 15 yr stretch.
5. **MMRTG degradation:** design curve ~1.8 %/yr; a 2026 quantitative analysis argues ~1.3–1.35 %/yr effective compounded; early Curiosity flight data often quoted as high as ~4.8 %/yr power-loss-equivalent. Use 1.8 %/yr nominal with 1.3–4.8 sensitivity bounds.
6. **Specific mass at MW scale:** extrapolated surface-FSP fit (~59 kg/kWe at 1 MWe) vs in-space NEP design (13 kg/kWe at 1.9 MWe). Different shield/life/thermal assumptions; surface value should be used for the settlement sim (30–60 kg/kWe band, low confidence).

---

## 12. Fidelity tiers (recommended model forms)

### L0 — single scalar average
Fission power is *the* constant in a Mars power model. Represent each unit as constant net power with a step to zero (or −25 % per string) on failure:
- `P_net = P_rated` (no diurnal/seasonal/dust dependence — this is fission's whole value proposition).
- Mass from the scaling law: `M[kg] = 562·P^0.673` (10–100 kWe, +20 % MGA included) or fixed points (10 kWe → 1.75–2.0 t Kilopower; 40 kWe → 6.8 t).
- Energy bookkeeping: `E_sol = P_net × 24.6597 h` (40 kWe → 986 kWh/sol; ≈ 0.145 kWh/sol per kg landed).
- MMRTG: `P(t) = 110 We × (1−0.018)^t[yr]`.

### L1 — analytic daily/seasonal model
Adds what actually varies:
- **Availability & discrete faults:** k-of-n string model, ≥75 % power after any single fault; Weibull or constant hazard per string calibrated so P(all strings survive 10 yr) is a study input. On/off cycles allowed (DG-3); restart cost = startup sequence ≤8 h at zero net output.
- **Slow degradation:** `P_net(t) = P_rated × (1 − r)^t`, r = 0–1 %/yr (dust on radiators, converter aging); reactor reactivity droop nulled by control (no power effect) until end-of-life at 10–15 yr.
- **Thermal environment coupling (small):** net power varies ±2–4 % with sink temperature (lunar-pole analog: 40.7–41.7 kWe over the synodic period, NETS 2025); on Mars, scale with sol-averaged sink `T_sink(L_s, latitude)` — effect is minor because T_r⁴ >> T_sink⁴.
- **Siting/mass trade:** shield mass vs separation: {0.5 km: 2,700 kg; 1 km: 1,370 kg; 2 km: 700 kg} + cable mass ≈ 45 kg/km at ±2.8 kV (95 % efficiency, ~43 kWe) with mass ∝ P·L/η at fixed voltage; buried option: −(shield mass) + excavation equipment/time, keep-out shrinks 1 km → 100 m.
- **Solar-vs-fission trade reproduction:** implement Rucker's dust-storm case (τ = 5, 120 sols, diffuse fraction 30–40 %) and check the crossover: solar+storage sized to 22 kWe keep-alive ≈ 9.8–12.7 t vs fission 7–9.2 t at 35–50 kWe continuous.

### L2 — physics-based sub-sol timestep
- **Reactor:** point-kinetics with strong negative fuel-temperature reactivity feedback (KRUSTY behavior): `dT_core/dt = (Q_fission(ρ(T)) − Q_hp(T_core − T_hot_end))/C_core`; load-following is passive, ΔT_core < 4 % for ΔQ_load up to 175 %; startup ramp ≤ 10 °C/min; loss-of-cooling transient bounded (+14 °C, 1-h damping).
- **Conversion:** Stirling with η ≈ 0.5 × η_Carnot(T_h = 1050 K, T_c = 420 K) → ~26 % converter, × 0.87–0.90 local PMAD × 0.95 cable × 0.96–0.98 down-conversion ≈ 0.78 end-to-end from converter terminals to 120 VDC user bus (COMPASS NETS 2022).
- **Radiator (Mars):** `Q_rej = A [ ε σ (T_r⁴ − T_sky⁴)F_sky + ε σ (T_r⁴ − T_gnd⁴)F_gnd + h (T_r − T_atm) ] − α_s A G_solar(t)`, with ε = 0.85 EOL, α_s = 0.2, F_sky ≈ F_gnd ≈ 0.5 for vertical two-sided panels, h = 1–5 W/m²K, T_sky ≈ 150–170 K, `G_solar` from the sim's insolation model (τ-dependent); dust deposition as slowly declining ε_eff and growing conductive resistance.
- **Dose field:** `D(r,θ) = D_1km × (1000/r)²` with D_1km = 5 rem/yr inside the 36° shielded cone, ~50 rem/yr outside it (surface-sited); buried: D_100m = 5 rem/yr, 1/r² beyond, negligible with 4π regolith. Add months-long cool-down clock after shutdown before approach < 100 m.
- **Grid:** per-string electrical model, parasitic-load radiator absorbing excess (reactor never sees load steps), bidirectional startup feed, cable I²R at chosen voltage.

---

## Sources (primary unless noted)

1. Gibson et al., *KRUSTY Nuclear Ground Test Results and Lessons Learned*, AIAA P&E 2018, NTRS 20180005435. (read)
2. Smith/Mason/Palac/Gibson, *Kilopower: Small & Affordable Fission Power Systems for Space*, 2018, NTRS 20180000691. (read)
3. Mason, Kaldon, Corbisiero, Rao, *Key Design Trades for a Near-term Lunar FSP System*, NETS 2025, NTRS 20250000841. (read)
4. Oleson et al., *A Deployable 40 kWe Lunar Fission Surface Power Concept*, NETS 2022, NTRS 20220004670, COMPASS CD-2021-187. (read)
5. Rucker, *Solar vs. Fission Surface Power for Mars*, AIAA 2016-5452, NTRS 20160011275 / 20160002628. (read)
6. NASA ESDMD, *Mars Surface Power Technology Decision*, 2024 Moon-to-Mars Architecture Concept Review white paper (Dec 2024). (read)
7. FSP Team, *Fission Surface Power System Initial Concept Definition*, NASA/TM-2010-216772.
8. Mason et al., *Nuclear Power Concepts and Development Strategies for High-Power Electric Propulsion Missions to Mars*, NASA/TM-20210016968, 2022. (read)
9. NASA press: FSP 2022 contract awards; NucNet Jan 2024 extension coverage.
10. ANS Nuclear Newswire, *Nuclear power on the moon: what we're watching*, 2 Sep 2025 (Aug 2025 NASA directive, 100 kWe/15 t/2030).
11. Mason/Gibson/Poston, *Kilopower: Small Fission Power Systems for Mars and Beyond*, NTRS 20140010823 (10 kWe radiator area).
12. NASA/JPL MMRTG fact sheet; Wikipedia (MMRTG, Kilopower) for consolidated spec tables — secondary, cross-checked.
