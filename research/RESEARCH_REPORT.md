# Mars Habitat Simulation — Parameter Research Report

**Generated:** 2026-07-05
**Master database:** `parameters_master.json` (354 parameters across 12 domains, every value cited)
**Verification:** A primary-source verification pass reviewed ~200 load-bearing values; 12 domains were audited, ~30 corrections filed, of which the critical/moderate ones and unambiguous minor ones are applied in this database.

---

## Executive Summary

This database consolidates twelve research domains into a single sourced parameter set for a Mars-habitat simulation. Every parameter carries a value, a range where meaningful, a citation, a source URL, and a confidence label. The dominant finding across domains is that a Mars settlement is **power- and logistics-constrained, not resource-constrained**: water ice and atmospheric CO₂ are effectively unlimited at good sites, but converting them to propellant, closing life-support loops, growing food, and keeping hardware alive all bottleneck on continuous electrical power, spare-parts upmass, and crew/robot labor.

Key cross-cutting conclusions:

- **Propellant ISRU is a ~2.5 MW-continuous problem per Starship.** Refueling one V2/V3-class ship (~1,500 t methalox) in a synodic period requires ~30 GWh, ~740 t of water, and ~720 t of CO₂ — an ~10⁶× scale-up over MOXIE.
- **Nuclear fission is the baselined primary surface power** (NASA ACR 2024) because its output is invariant to latitude, season, sol cycle, and dust; solar wins only at low-latitude sites with modest storm keep-alive requirements.
- **Reliability and spares dominate long-term logistics.** ISS flight data shows failure-rate predictions were optimistic (14 of 20 ECLSS ORUs ran worse than predicted; some UPA units 7.5–22× worse); Mars-transit spares mass is ~6 t deterministic but 12–17 t once epistemic uncertainty is included, and spares grow to ~62% of all Earth-delivered mass at settlement scale.
- **All SpaceX vehicle figures are company claims** and were the largest source of errors in the raw research — a Wikipedia table conflating the flown V3 with a future stretch vehicle propagated wrong propellant loads, GLOM, and engine counts, now corrected to flown-V3 values (124.4 m stack, 1,550 t ship / 3,650 t booster, 6-engine ship, ~80.8 MN liftoff).

---

## Per-Domain Key Findings

### ECLSS (Environmental Control & Life Support)
- Reference crew (82 kg, BVAD Rev 2): O₂ ~0.90 kg/CM-d, CO₂ ~1.04–1.09 kg/CM-d at RQ 0.86, metabolic heat 12.43 MJ/CM-d (143.8 W, ~51% sensible/49% latent). Design spread ±25%.
- Water: total human consumption 2.5 kg/CM-d; hygiene 0.7 (ISS-minimal) to 6.62 (mature base); condensate 2.27 kg/CM-d recovered to WPA.
- **Correction:** CDRA nominal-flow CO₂ removal is ~3.5 kg/day (3.17–3.49 measured at 20.4 SCFM/2 torr), not the 4.1–10.5 CM-eq figures, which are a high-flow (25 SCFM, +200 W heater) configuration — the prior curve overstated nominal capacity ~20–25%.
- Sabatier recovers only ~47–48% of metabolic O₂ (H₂-limited, CH₄ vented); WRS reaches 98% water closure with the 2023 Brine Processor Assembly (93–94% before). UPA de-rates 85%→70% from calcium-sulfate precipitation.
- For a no-resupply Mars mission the binding trade is reliability/spares/crew-time, not steady-state efficiency.

### Starship (Transport Vehicle)
- Flown V3 baseline (Flight 12, 2026-05-22, partial success): 124.4 m stack, 9 m diameter, **3,650 t booster + 1,550 t ship propellant** (~5,600–5,750 t GLOM), 33 Raptor-3 booster engines + **6 ship engines**, ~80.8 MN liftoff thrust.
- **Corrections applied:** booster propellant 4,050→3,650 t (critical; 4,050 t was a future-stretch figure), GLOM 5,250→5,650 t, ship propellant 1,600→1,550 t, ship engines 9→6. These stemmed from a Wikipedia table mixing flown-V3 and future-stretch specs.
- Payload to Mars surface: 100 t **company claim**; peer-reviewed analysis (Maiwald et al. 2024) finds ≤114 t optimistic and Earth-return infeasible at their 210 t dry-mass estimate. Model 10–100 t credible band, ~50 t working value.
- Refilling one Mars-bound ship needs ~12 tanker flights (8–20 across sources); a 5-ship wave ≈ 60 tanker launches inside a ~30–45 d window — **Earth pad cadence, not astrodynamics, is the bottleneck**.
- Cargo offloading (~33.5 m lowering height, ≤10 t crane budget) is an unsolved, binding constraint.

### Solar Power
- Mars TOA flux 586 W/m² mean (43% of Earth), swinging 490–713 W/m² over the eccentric orbit; perihelion boost coincides with dust-storm season, so the two largest seasonal effects partially cancel.
- Because Mars dust is forward-scattering, global insolation falls only ×2–3 in a τ=5 storm (4.0→1.2 kWh/m²/sol), **not** exp(−τ) — solar survives storms at ~25–30% output.
- Flight 3-junction cells ~30% efficiency; system array mass ~1.5 kg/m². Complete solar+storage for continuous crewed load ~300–450 kg/kW vs ~230 kg/kW for Kilopower fission.
- Dust deposition ~0.2–0.28%/sol; natural cleaning is unschedulable (InSight saw none in 4 years) — settlement designs must assume active dust removal.

### Nuclear Power
- KRUSTY (2018) is the only modern flight-like fission ground test: >4 kWt, 30–34% Stirling efficiency, passive load-following, 1.5 h startup, ~$15–20M.
- Mass scales as M[kg]=562·P^0.673 (HP-Stirling, 10–100 kWe); 40 kWe reference design is 6.8 t (the 6 t goal is unmet). A crew-shielded 10 kWe Kilopower is 1.75–2.0 t.
- Shielding is a siting trade: 1,370 kg shadow shield → 5 rem/yr at 1 km within a 36° cone; burying under ~2 m regolith shrinks keep-out from 1 km to 100 m.
- **Corrections:** MMRTG degradation 1.8→2.5 %/yr (official design curve + flight data); cable mass scales as L² not linearly (240 kg for 3 km, not 135 kg); Kilopower-life source URL repointed.
- No space fission unit is flight-qualified as of mid-2026; NASA program-of-record targets a 100 kWe-class lunar reactor, ≤15 t, launch-ready late CY2029 (low schedule confidence).

### ISRU — Atmosphere (Propellant)
- MOXIE (flight-proven 2021–2023): 122 g O₂ total, 6–12 g/hr, ~300 W = 25–50 kWh/kg O₂; scale fixes efficiency (MAV-class SOXE plant ~9–17 kWh/kg system-level vs 4.91 kWh/kg thermodynamic floor).
- End-to-end methalox plant (Kleinhenz & Paz): 20.2 kWh/kg propellant, 23 kg hardware per (kg/day). **Water electrolysis dominates energy** (~6 kWh per kg propellant); Sabatier is exothermic (free 350–400 °C heat).
- **Correction:** Starship propellant load 1,200→1,500 t (V1→V2/Block 2); derived refuel energy recomputed to ~30.3 GWh and average power to ~2.46 MW continuous.
- Bring-your-own-H₂ leverage is 18.4:1 with water recycle, but LH₂ transport at 20 K (0.1–0.5%/day boiloff, 33–100 kWh/kg liquefaction) is why NASA architectures chose Mars water.

### ISRU — Water
- Massive ~90%-pure excess ice confirmed at decimeters-to-2 m depth across Arcadia/Utopia (SWIM, Dundas 2018, Bramson 2015); ~10⁴ km³ in Arcadia alone — effectively unlimited.
- The Rodwell is the standout technique: 60+ yr Antarctic heritage, TRL 6, 0.4–0.7 kWh(th)/kg (within 3–5× of the 0.135 kWh/kg melt floor, and the heat can be reactor waste heat).
- Regolith bound water costs 2.5–20× more energy per kg (gypsum ~1.5, smectite ~3.7, typical regolith 5.9–12.5 kWh(th)/kg) but works anywhere.
- **Correction:** water per Starship reload 600→740 t (Block 2/V3 1,500–1,600 t load); ~1,160 kg/sol over a synodic period. Water supply is a logistics driver; electrolysis/liquefaction dominate energy.

### Food & Crops
- Crew demand 3,037 kcal/CM-d; prepackaged food 1.83–2.39 kg/CM-d incl. ~0.27 kg packaging; ≥5 yr shelf life is an unsolved gap driving in-situ production. **No corrections — unusually clean dataset.**
- Potato is the calorie champion: ~0.066 kWh(elec)/kcal, ~2.6× cheaper than wheat, ~3× cheaper than soybean under LEDs.
- Lighting energy per calorie = 1/(RUE_edible × LED_efficacy × delivery × caloric_density), **independent of DLI** (DLI only trades area vs. cycle time) — the key structural insight.
- Full balanced-diet closure ~40–60 m²/CM and ~10–20 kW/CM — the single largest life-support power line item if food is grown.

### Reliability
- ISS crews average 6.0 h/CM-week scheduled maintenance; ECLSS alone consumes 13–15 h/week station-wide vs ~1 h/week predicted at design (10× underestimate).
- Failure-rate prediction error dominates sparing risk (14/20 ECLSS ORUs worse than predicted; UPA FCPA 1,000 h flight vs 22,759 h predicted).
- Anchor spares mass for a 4-crew, 1,100-day transit hab: 5,984 kg (deterministic) → 12,000–17,232 kg (with epistemic uncertainty), i.e. ~10%/yr of supported dry mass.
- At settlement scale spares reach ~62% of Earth-delivered mass; levers are commonality (33–50% fewer spares), in-space manufacturing (28–34% reduction), and shared spares pools.

### Human Factors, Crew Time & Radiation
- ISS crew-time budget transfers to Mars (×1.0275 sol scaling): 8.5 h sleep, 6.5 h work, 2.5 h exercise, 3.0 h meals; taxable labor pool (VST) 69.7 h/CM-week. After all liens, NASA's integrated model leaves only ~8.5% of surface crew-hours for science.
- Measured radiation: 0.64 mSv/day surface, 1.84 mSv/day transit → unmitigated conjunction mission ~1.0 Sv vs 600 mSv NASA career limit.
- Regolith shielding is **non-monotonic**: <~0.7 m dry regolith *increases* equivalent dose via secondary neutrons; ~1.3 m halves it.
- **Corrections:** the 250 "mGy-Eq 30-day BFO" limit is retired NCRP-132; current NASA-STD-3001 Rev B uses 250 mSv effective dose per design SPE. A phantom "Martinez-Sierra 2024" citation is actually Ralha et al. 2024; the 23 g/cm² atmospheric column is a Gale-like low-site value (~16 at datum). Evac-class rate 0.017/py relabeled a derived estimate.

### Robots
- No Tesla Optimus performance figure is field-demonstrated as of July 2026; mass 57 kg and 2.3 kWh battery are stable claims, but runtime/payload/speed are unverified upper bounds.
- Physics excludes Earth-based teleoperation of manipulation (6–45 min round-trip vs ~0.7 s tolerance); local teleop from the hab is fine → robot productivity is structurally different before vs after crew arrival.
- Robot:human work-rate ratio spans three orders of magnitude with task structure (0.92 purpose-built stationary; 0.5–0.75 humanoid structured logistics; 0.03–0.3 unstructured; 0.001 sol-scale supervised rover). A single "robot = X% of human" scalar is the wrong model form.
- Specialized machines beat humanoids for the biggest workloads (RASSOR ≥2.7 t/day excavation; Hadrian-X-class 5–20× human bricklaying).
- **Corrections:** spares source misquoted (62% not 64%, wrong NTRS doc); runtime survey supports 2–2.5 h under heavy load.

### Mission Architecture
- Campaign clock is the 779.94-d synodic period, but windows are unequal: min departure C3 swings 7.78 (2033, cheapest) to 14.84 km²/s² (2037) — deliverable tonnage per window varies ~2×.
- Conjunction-class is the only viable settlement architecture: 150–210 d transits (NASA) or 90–150 d (Starship claims; 90–104 d peer-confirmed at high energy), ~500–620 sol stays, ~914 d round trips.
- **Correction:** DRA 5.0 "landed useful mass per lander" 56.8→40.4 t — 56.8 t includes the 16.4 t descent stage; useful payload is 40.4 t (overstated surface delivery ~40%).
- EDL is commit-to-land (no abort-to-orbit); validated anchor 109.7 t entry → 40.4 t useful payload (56.8 t total landed).
- SpaceX slipped its first uncrewed attempt from Nov-Dec 2026 to the 2028/29 window (Feb 2026 announcement); crewed landing target now ~2031–2033 (company claims).

### Mars Environment
- Perihelion (Ls 251) ≈ southern-summer solstice: TOA flux swings 490–713 W/m² (±19%), and the perihelion boost coincides with dust-storm season.
- CO₂ condensation cycle moves ~25% of the atmosphere through polar caps yearly; surface pressure cycles ~700–955 Pa at a low site.
- Global dust storms: probability ~0.33/Mars-year, peak visible τ 5–11, elevated 60–150 sols; diffuse light keeps 15–25% of insolation.
- Flight-measured regolith: k=0.039 W/m/K, ~217 K at depth — excellent insulation, stable heat sink, radiation shield.
- **Corrections:** N₂/CO₂ composition source URLs repointed from Franz 2015 (which gives contradicting numbers) to Franz 2017; wind-gust source relabeled (Bickel orbital study, not MEDA); TOA range and season lengths made self-consistent; InSight dust note corrected (~2.1 Mars years, not 4).

---

## Trade-Study Implications

### Solar vs. Nuclear
Fission is the constant term: output independent of latitude, season, sol cycle, and dust optical depth, which is why NASA's 2024 Architecture Concept Review baselined it for crewed Mars. The Rucker 2016 trade the sim should reproduce: at Jezero, solar sized to keep 22 kWe alive through a 120-sol τ=5 storm masses ~9.8–12.7 t vs ~7–9.2 t for 35–50 kWe of Kilopower fission — fission ~20% lighter and storm-immune, but it *loses* the small equatorial-demo trade (2,751 kg vs 1,128–2,425 kg solar). **Rule:** solar for small, low-latitude, storm-tolerant loads; fission for anything needing guaranteed multi-hundred-kW continuous power (propellant ISRU, food lighting). A realistic settlement uses both — solar+battery for daytime flexible loads, fission for the ISRU base load and storm ride-through.

### ISRU: Ice-Mining vs. Atmosphere-Only with Imported H₂
Water route (CO₂ + 2H₂O → CH₄ + 2O₂) naturally yields O:F 4.0 — slightly O₂-rich vs Raptor's 3.6 — so a water-fed plant needs no separate SOXE and banks ~11% surplus O₂. Bring-your-own-H₂ has 18.4:1 propellant leverage (6 t H₂ → 107 t methalox, Zubrin), which slashes landed ISRU mass **if** you can transport and store LH₂ at 20 K. But passive LH₂ boiloff (0.1–0.5%/day → 30–70% loss over a transit+wait) and liquefaction cost (33–100 kWh/kg) make imported H₂ unattractive at human scale. **Conclusion:** mine Mars water. One Starship reload needs ~740 t water = ~1,160 kg/sol over a synodic period ≈ 2.5 ten-kW Rodwells; water extraction is the *logistics* driver, electrolysis/liquefaction the *energy* driver (~6 kWh/kg propellant). Atmosphere-only (Sabatier + imported H₂) survives only as a small-scale bootstrap (à la Mars Direct ERV) where a ~0.4 t H₂ seed makes ascent propellant.

### Solar Array Mass
System-level Mars array mass ~1.5 kg/m² (CTSA goal) to 2.9 kg/m² (UltraFlex-derived), i.e. ~19–30 kg per noon-peak kW generation-only. For the ~2.46 MW continuous a single Starship refuel implies, at a PV capacity factor of 0.2–0.25 you need ~8–12 MW peak → ~40,000–60,000 m² of array (~60–170 t of blanket+structure) *plus* multi-sol storage for storms — which is why propellant-scale ISRU is nuclear-favored. For a crew-scale hab (~30 kW), solar+storage at ~335 kg/kW ≈ 10 t is competitive.

### Flights Needed for an Initial Base
Cross-check anchors: DRA 5.0 lands ~114 t total (2 landers, ~40 t useful each) + crew lander for 6, at ~850–1,250 t IMLEO. Mars Direct lands ~53 t for 4. Budget **~10–30 t landed per person**. A SpaceX-style base at 50–100 t useful per ship (working estimate, not the 100–150 t claim) needs, per uncrewed wave: cargo ships for power (fission units), ISRU plant, habitat, and consumables. Each Mars ship costs ~12 tanker flights, so a 5-ship wave ≈ 60 Earth launches inside a ~30–45 d window. **The binding constraint is Earth launch cadence × depot boiloff, not landed mass.**

### Spares Mass
For a 4-crew, 1,100-day transit hab: 5,984 kg at R=0.99 with perfect MTBF knowledge, rising to 12,000–17,232 kg once epistemic uncertainty is carried (uncertainty doubles-to-triples spares). That is ~10%/yr of supported dry mass (vs the 5%/yr ISS heuristic that only holds *with* resupply), up to ~28%/yr conservative for new hardware. At settlement scale spares reach ~62% of all Earth-delivered mass by month 130. **Levers:** parts commonality (−33–50% spares), in-space manufacturing of the ~33% fluid-flow item class (−28–34% maintenance mass), shared spares pools (−50%), cannibalization (~30% recovery). Model spares as Poisson demand → probability-of-sufficiency with a lognormal error factor (EF~2 heritage, 5–10 new designs).

### Food: Grow vs. Bring
Bring: 1.83–2.39 kg/CM-d prepackaged, well-characterized, but ≥5 yr shelf life is unsolved and mass scales linearly with crew-days. Grow: ~40–60 m²/CM and 10–20 kW/CM for full closure, plus ~95 kg/CM-yr fertilizer (>50% recyclable). Crossover favors growing for **multi-year, no-resupply** stays, but never model >50–70% steady-state closure without a ≥1-crop-cycle stored-food buffer (crops fail fast: Fusarium, ethylene depressing wheat HI 0.40→0.29). A tuber-biased menu (potato/sweet-potato at ~0.066 kWh/kcal) minimizes the power penalty; grains/legumes are nutrition supplements, not calorie staples. Bonus: a farm is a water processor (~4 L/m²-d clean condensate).

### Robot Labor Impact
A defensible planning point for one Optimus-class robot is ~10 productive h/sol (12 h duty × 0.85 availability) at ~0.6 kWh/work-hour (~9 kWh/sol incl. night heating). At structured-task quality q=0.5 that's ~0.8 crew-equivalents with zero EVA overhead — but only ~0.15 crew-equivalents at unstructured tasks. **Two hard structural features:** (1) pre-crew unstructured work inherits rover-class rates (q~0.001) because Earth teleop is excluded by light-time, while crewed-phase structured work with local teleop reaches warehouse rates (0.3–0.9); (2) specialized machines (RASSOR excavators, block-layers) should own excavation/construction while humanoids cover logistics/inspection/greenhouse. Maintenance is the tail risk: field MTBF for a 50-actuator humanoid in abrasive dust at −80 °C nights is unknown (est. 500 h, 100–2000 h range); each synod should carry 25–30% of fleet mass in spares. A 100-robot fleet is a ~25–40 kW continuous power load.

### Plausible 2028–2040 Launch Schedule
Windows (min-energy departure dates, NASA/TM-2010-216764): **Dec 2028/Jan 2029, Jan–Feb 2031, Apr 2033 (cheapest), Apr–May 2035, Jun 2037 (dearest), Jul 2039**. A credible ramp, treating SpaceX dates as slipping ~1 synod per synod:

- **2028/29** — First uncrewed wave: ~2–5 cargo Starships with Optimus robots; test intact landing, survey ice, deploy first fission unit and a pilot ISRU/Rodwell demo. (SpaceX's post-Feb-2026 target window.)
- **2031** — Second uncrewed wave: scale ISRU toward propellant production, pre-position habitat + power, demonstrate a partial Earth-return-propellant batch. Verify-before-crew gate (DRA 5.0): MAV/return ship must be fuel-verified before any crew departs.
- **2033** — First crewed landing (company target ~2031–2033; cheapest window, favorable radiation via fast transit). Crew of ~4–6, ~500–600 sol stay, returning on ISRU propellant made by pre-deployed plant.
- **2035** — Crew rotation + base expansion (Type-I fast transit optimal this anomalous window).
- **2037** — Dearest window: minimize new mass, resupply spares/consumables only; ~2× payload penalty vs 2033.
- **2039** — Return to cheaper energy; scale toward self-sustaining propellant + food production.

NASA's Moon-to-Mars planning independently holds crewed Mars in the **late 2030s–2040s**; the sim should treat crew date as a scenario variable gated on (a) uncrewed landing success and (b) an ISRU propellant-production demonstration.

---

## Model Fidelity Recommendations (L0 / L1 / L2 per subsystem)

- **ECLSS:** L0 constant per-CM scalars for mass balance. L1 activity-block daily model with atmosphere ODEs and hardware capacity as f(ppCO₂, rate). L2 sub-sol physics: CDRA adsorption isotherms, OGA polarization curves, Sabatier H₂-limited kinetics, condensing-HX latent/sensible split, de-ratable water-recovery network.
- **Starship/Transport:** L0 scalar averages (payload, tanker count, transit time). L1 analytic per-window (rocket equation + opportunity-dependent Δv table + linear boiloff). L2 3-DOF EDL Monte-Carlo, 2-node tank thermal with diurnal forcing, full ascent integration. Correlate ship dry mass (100–210 t) as the master uncertainty driver.
- **Solar:** L0 E = P_installed × EPH × DF × η. L1 Appelbaum-Flood seasonal insolation + τ(Ls) climatology + GDS Bernoulli process + dust-factor ODE. L2 sub-sol cos-z net-flux table, cell-temp energy balance, battery SOC integration.
- **Nuclear:** L0 constant P_rated, M=562·P^0.673, energy = P×24.66 h/sol. L1 k-of-n string reliability, slow drift, siting/shield/cable trade. L2 point-kinetics with negative temperature feedback, Stirling η≈0.5·Carnot, radiator Q balance, dose field D(r).
- **ISRU (both):** L0 fixed kWh/kg and kg/(kg-day). L1 per-sol energy ledger with seasonal atmospheric-density forcing, plant availability, heat-integration switch. L2 sub-sol: compressor flow ∝ ρ_atm(t), SOXE cell V=V_Nernst+i·ASR, electrolyzer overpotentials, cryocooler P=Q·(T_rej−T_c)/(T_c·η₂ₗ), Rodwell pool energy balance with conduction+evaporation.
- **Food:** L0 single scalars (~15 g dw/m²-d, ~0.10 kWh/kcal, ~45 m²/CM). L1 per-crop analytic (edible_prod = RUE × DLI × f_abs, staggered planting, seasonal insolation, water/nutrient loops). L2 light-response photosynthesis, Penman-Monteith transpiration, coupled O₂/CO₂/water/thermal, stochastic crop failure.
- **Reliability:** L0 spares = 10%/yr of dry mass, maintenance 6 h/CM-week, availability 0.99. L1 per-subsystem negative-binomial demand + POS optimization + crew-time check. L2 per-ORU renewal (exponential electronics, Weibull β~1.8 rotating machinery, life clocks for beds/filters), β-factor CCF, cannibalization queue, ISM job shop.
- **Human factors:** L0 fixed crew-time liens + linear dose (1.84·transit + 0.66·S·surface). L1 solar-cycle GCR modulation, atmospheric column correction, non-monotonic regolith-shield S(depth), SPE Poisson events. L2 HZETRN/GEANT4 spectra through time-varying column, ICRP effective dose on phantoms.
- **Robots:** L0 scalar (10.2 robot-h/sol × q_c). L1 sol-indexed with dust/power/aging/spares dynamics and autonomy learning curve. L2 sub-sol SOC-thermal-reliability sim with task queues and teleop-slot scheduling. Use distinct pre-crew vs crewed q-vectors.
- **Mission architecture:** L0 synodic-period clock + per-window C3 table. L1 rocket-equation + opportunity-Δv + abort state machine. L2 full trajectory optimization with launch-period packing and depot boiloff.
- **Mars environment:** L0 seasonal scalars. L1 Allison-McEwen Ls series, 2-harmonic pressure fit, seasonal temperature sinusoids, τ(Ls) + storm processes. L2 1-D regolith conduction PDE with surface energy balance, Weibull winds, diurnal sky-temperature.

---

## Bibliography (deduplicated across domains)
- **'Mining' Water Ice on Mars - An Assessment of ISRU Options** — NASA JSC (Hoffman et al.), 2016  
  <https://www.nasa.gov/wp-content/uploads/2015/06/mars_ice_drilling_assessment_v6_for_public_release.pdf>
- **18 Months of MOXIE operations on the surface of Mars** — Acta Astronautica 210:547-553, 2023  
  <https://www.sciencedirect.com/science/article/abs/pii/S0094576523002187>
- **3 months transit time to Mars for human missions using SpaceX Starship (Sci Rep 15:17764)** — Scientific Reports / UCSB, 2025  
  <https://doi.org/10.1038/s41598-025-00565-7>
- **3 months transit time to Mars for human missions using SpaceX Starship (Scientific Reports 15)** — Ataee & Elvander, 2025  
  <https://www.nature.com/articles/s41598-025-00565-7>
- **A Brief Survey of Telerobotic Time Delay Mitigation (direct teleop degrades >300 ms, collapses ~1 s RTT)** — Frontiers in Robotics and AI, 2020  
  <https://www.frontiersin.org/journals/robotics-and-ai/articles/10.3389/frobt.2020.578805/full>
- **A Closer Look at SpaceX's Mars Plan** — Aerospace America (AIAA), 2026  
  <https://aerospaceamerica.aiaa.org/aiaa-spacex/>
- **A Deployable 40 kWe Lunar Fission Surface Power Concept (NETS 2022, COMPASS CD-2021-187)** — NASA GRC / LANL, 2022  
  <https://ntrs.nasa.gov/api/citations/20220004670/downloads/40%20kW%20Deployable%20FSP%20Paper_FINAL.pdf>
- **A Historical Review of Logistics Mass and Crew Time Demands for ISS Operations (ICES-2024-132)** — NASA Langley / Binera, 2024  
  <https://ntrs.nasa.gov/citations/20240005648>
- **A post-Pathfinder evaluation of areocentric solar coordinates with improved timing recipes for Mars seasonal/diurnal climate studies** — NASA GISS (Allison & McEwen), Planet. Space Sci. 48, 2000  
  <http://amsat-bda.org/files/2000_Allison_McEwen.pdf>
- **About feasibility of SpaceX's human exploration Mars mission scenario with Starship** — Scientific Reports / DLR, 2024  
  <https://doi.org/10.1038/s41598-024-54012-0>
- **About feasibility of SpaceX's human exploration Mars mission scenario with Starship (Scientific Reports 14)** — Maiwald et al. / DLR, 2024  
  <https://www.nature.com/articles/s41598-024-54012-0>
- **Abundance and Isotopic Composition of Gases in the Martian Atmosphere from the Curiosity Rover** — Science 341 (Mahaffy et al., MSL SAM), 2013  
  <https://www.science.org/doi/10.1126/science.1237966>
- **Advancing ECLSS Reliability Modeling: Integrating ISS Data for Sustainable Long-Duration Mission Planning (ICES-2025-127)** — The Aerospace Corporation / NASA JSC, 2025  
  <https://ntrs.nasa.gov/citations/20250003955>
- **An Advanced CO2 Capture and Compression System for Mars ISRU (ICES-2020-151)** — TDA Research, 2020  
  <https://ttu-ir.tdl.org/server/api/core/bitstreams/93f2e845-bcf6-4817-a30b-7129eec234d0/content>
- **An independent assessment of the technical feasibility of the Mars One mission plan - Updated analysis, Acta Astronautica 120:192-228** — MIT, 2016  
  <https://dspace.mit.edu/handle/1721.1/103973>
- **An ISRU Propellant Production System to Fully Fuel a Mars Ascent Vehicle (AIAA 2017-0423)** — NASA GRC/JSC, NTRS 20170001421, 2017  
  <https://ntrs.nasa.gov/citations/20170001421>
- **Assessment of Crew Time for Maintenance and Repair Activities for Lunar Surface Missions (Stromgren et al., IEEE Aerospace)** — NASA NTRS 20210026843, 2021  
  <https://ntrs.nasa.gov/api/citations/20210026843/downloads/IEEE%20Assessment%20of%20Crew%20Time%20for%20Maintenance%20and%20Repairs%20Activities%20for%20Lunar%20Surface%20Missions.pdf>
- **Availability of subsurface water-ice resources in the northern mid-latitudes of Mars (SWIM)** — Nature Astronomy / Planetary Science Institute, 2021  
  <https://doi.org/10.1038/s41550-020-01290-z>
- **Biomass Production of the EDEN ISS Space Greenhouse in Antarctica During the 2018 Experiment Phase** — DLR / Zabel et al., 2020  
  <https://pmc.ncbi.nlm.nih.gov/articles/PMC7264257/>
- **Challenges of Sustaining the International Space Station through 2020 and Beyond: Including Epistemic Uncertainty in Reassessing Confidence Targets (AIAA 2012-5320)** — NASA / Boeing ISS S&MA, 2012  
  <https://ntrs.nasa.gov/citations/20120014060>
- **CO2 Cryofreezer Coldhead design and testing (TFAWS 2018)** — NASA KSC, NTRS 20190000481, 2018  
  <https://ntrs.nasa.gov/api/citations/20190000481/downloads/20190000481.pdf>
- **CO2 Removal Onboard the International Space Station (4-bed molecular sieve / 4BCO2)** — NASA MSFC, 2019  
  <https://ntrs.nasa.gov/api/citations/20190030370/downloads/20190030370.pdf>
- **Comments on the MIT Assessment of the Mars One Plan (ICES-2015-044)** — NASA Ames Research Center, 2015  
  <https://ntrs.nasa.gov/citations/20160001251>
- **Communication Delays, Disruptions, and Blackouts for Crewed Mars Missions (ASCEND 2022; NTRS 20220013418)** — NASA, 2022  
  <https://ntrs.nasa.gov/citations/20220013418>
- **Compact Telescoping Surface Array for Mars Solar Power** — NASA Langley (NTRS 20190000437), 2019  
  <https://ntrs.nasa.gov/api/citations/20190000437/downloads/20190000437.pdf>
- **Comparison of Spares Logistics Analysis Techniques for Long Duration Human Spaceflight (ICES-2015-288)** — MIT / Binera / NASA Langley, 2015  
  <https://ntrs.nasa.gov/citations/20160006509>
- **Crop productivities and radiation use efficiencies for bioregenerative life support, Adv. Space Res. 41:706-713** — NASA KSC / Wheeler et al., 2008  
  <http://bigidea.nianet.org/wp-content/uploads/2018/07/Adv-Space-Res-2008-Crop-Prod-and-Rad-Use-Eff.pdf>
- **Demonstration Proves Nuclear Fission System Can Provide Space Exploration Power (KRUSTY press release)** — NASA, 2018  
  <https://www.nasa.gov/news-release/demonstration-proves-nuclear-fission-system-can-provide-space-exploration-power/>
- **Design of an Excavation Robot: RASSOR 2.0 (ASCE Earth & Space)** — NASA KSC (Mueller et al.), 2016  
  <https://ntrs.nasa.gov/citations/20210011366>
- **Design of ISS Environmental Control and Life Support Systems / ECLSS Technology Evolution for Exploration** — NASA (Carrasquillo et al.), 2005  
  <https://www.nasa.gov/wp-content/uploads/2015/05/design_iss_environment_control_and_life_support_systems.pdf>
- **Detailed Review of Starship V3** — New Space Economy, 2026  
  <https://newspaceeconomy.ca/2026/04/16/detailed-review-of-starship-v3/>
- **Detection of Perchlorate and the Soluble Chemistry of Martian Soil at the Phoenix Lander Site** — Science (Hecht et al.), 2009  
  <https://doi.org/10.1126/science.1172466>
- **Developing a Crew Time Model for Human Exploration Missions to Mars (Stromgren et al., IEEE Aerospace)** — NASA NTRS 20150009469, 2015  
  <https://ntrs.nasa.gov/api/citations/20150009469/downloads/20150009469.pdf>
- **Digit Moves Over 100,000 Totes in Commercial Deployment (GXO) - company claim** — Agility Robotics, 2025  
  <https://www.agilityrobotics.com/content/digit-moves-over-100k-totes>
- **Dispelling the myth of robotic efficiency (human field scientist vs rover ~1 sol : 45 s; Squyres estimate)** — Crawford, Astronomy & Geophysics / arXiv:1203.6250, 2012  
  <https://arxiv.org/pdf/1203.6250>
- **Driving Curiosity: Mars Rover Mobility Trends During the First Seven Years (28.9 m avg per driving sol; max 142.5 m)** — NASA/JPL, IEEE Aerospace, NTRS 20220000780, 2020  
  <https://ntrs.nasa.gov/citations/20220000780>
- **Dust Accumulation and Solar Panel Array Performance on the Mars Exploration Rover Project** — NASA NTRS 20050186821 (Stella & Herman), 2005  
  <https://ntrs.nasa.gov/citations/20050186821>
- **Dust aerosol, clouds, and the atmospheric optical depth record over 5 Mars years of the Mars Exploration Rover mission** — Icarus (Lemmon et al.), 2015  
  <https://ntrs.nasa.gov/citations/20150008268>
- **Dust aerosol, clouds, and the atmospheric optical depth record over 5 Mars years of the MER mission** — Icarus (Lemmon et al.), 2015  
  <https://arxiv.org/abs/1403.4234>
- **Dust Devil Frequency of Occurrence and Radiative Effects at Jezero Crater as Measured by MEDA RDS** — JGR Planets (Toledo et al.), 2023  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2022JE007494>
- **Effective dose equivalent estimation for humans on Mars (Martinez-Sierra et al.)** — arXiv:2409.02001, 2024  
  <https://arxiv.org/abs/2409.02001>
- **Effects of the MY34/2018 Global Dust Storm as Measured by MSL REMS in Gale Crater** — JGR Planets (Viudez-Moreiras et al.), 2019  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2019je005985>
- **Eight-year climatology of dust optical depth on Mars** — Icarus 251 (Montabone et al.), 2015  
  <https://arxiv.org/abs/1409.4841>
- **Energy Storage Technologies for Future Planetary Science Missions (JPL D-101146)** — NASA JPL, 2017  
  <https://solarsystem.nasa.gov/system/downloadable_items/716_Energy_Storage_Tech_Report_FINAL.PDF>
- **ESA ISS ECLSS / Node 3 Factsheet (leakage, hardware sizing)** — ESA, 2014  
  <https://wsn.spaceflight.esa.int/docs/Factsheets/30%20ECLSS%20LR.pdf>
- **Estimating medical risk in human spaceflight (Antonsen et al.)** — npj Microgravity 8:8, 2022  
  <https://www.nature.com/articles/s41526-022-00193-9>
- **Evaluation of low-pressure mechanical compression for Martian atmospheric CO2 capture** — Acta Astronautica, 2024  
  <https://www.sciencedirect.com/science/article/abs/pii/S0094576524007690>
- **Exposed subsurface ice sheets in the Martian mid-latitudes** — Science, 2018  
  <https://doi.org/10.1126/science.aao1619>
- **Extraction and Capture of Water from Martian Regolith - Experimental Proof-of-Concept** — NASA GRC (Linne, Kleinhenz et al.), 2016  
  <https://ntrs.nasa.gov/citations/20160010258>
- **Fission Surface Power System Initial Concept Definition (NASA/TM-2010-216772)** — NASA / DOE FSP Team, 2010  
  <https://ntrs.nasa.gov/api/citations/20100033102/downloads/20100033102.pdf>
- **From physics to fixtures to food: current and potential LED efficacy, Hortic. Res. 7:56** — Utah State University / Kusuma, Pattison & Bugbee, 2020  
  <https://www.nature.com/articles/s41438-020-0283-7>
- **Fusarium oxysporum as an Opportunistic Fungal Pathogen on Zinnia hybrida Plants Grown on board the International Space Station** — NASA / Schuerger et al., 2021  
  <https://www.researchgate.net/publication/351237242>
- **Geomorphological Evidence of Near-Surface Ice at Candidate Landing Sites in Northern Amazonis Planitia, Mars** — JGR Planets, 2025  
  <https://agupubs.onlinelibrary.wiley.com/doi/10.1029/2024JE008724>
- **Hadrian X bricklaying robot speed records (200-300 blocks/h) - company claims** — FBR / New Atlas coverage, 2023  
  <https://newatlas.com/robotics/hadrian-x-bricklaying-robot/>
- **High Reliability Requires More Than Providing Spares (ECLSS reliability)** — NASA, 2019  
  <https://ntrs.nasa.gov/api/citations/20190027319/downloads/20190027319.pdf>
- **High Reliability Requires More Than Providing Spares (ICES-2019-14)** — NASA Ames Research Center, 2019  
  <https://ntrs.nasa.gov/citations/20190027319>
- **Human Exploration of Mars Design Reference Architecture 5.0 (NASA-SP-2009-566)** — NASA, 2009  
  <https://www.nasa.gov/wp-content/uploads/2015/09/373665main_nasa-sp-2009-566.pdf>
- **Human Missions to Mars Using the Starship (IgMin Research)** — D. Rapp, 2025  
  <https://www.igminresearch.com/articles/html/igmin308>
- **Humanoid Robot Battery Runtime: Claimed vs Real-World (30-40% shortfall under load)** — RobotWale (trade press), 2025  
  <https://www.robotwale.com/article/humanoid-robot-battery-runtime-reality-check>
- **In-Space Manufacturing Production Rate and Reliability Targets for On-Demand Fabrication of ECLSS Spares (ICES-2020)** — MIT, 2020  
  <https://ttu-ir.tdl.org/handle/2346/84476>
- **Incidence of clinical symptoms during long-duration orbital spaceflight (Crucian et al.)** — Int. J. Gen. Med. 9:383, 2016  
  <https://pmc.ncbi.nlm.nih.gov/articles/PMC5098747/>
- **Influence of crop cultivation conditions on space greenhouse equivalent system mass, CEAS Space Journal** — Boscheri et al., 2020  
  <https://link.springer.com/article/10.1007/s12567-020-00317-5>
- **Interannual variability of planet-encircling dust storms on Mars** — JGR (Zurek & Martin), NTRS 19930046754, 1993  
  <https://ntrs.nasa.gov/search.jsp?R=19930046754>
- **International Space Station USOS Crew Quarters Development (Broyan et al., 08ICES-0222)** — NASA NTRS 20080013462, 2008  
  <https://ntrs.nasa.gov/api/citations/20080013462/downloads/20080013462.pdf>
- **Interplanetary Mission Design Handbook: Earth-to-Mars Mission Opportunities 2026 to 2045 (NASA/TM-2010-216764)** — NASA Glenn Research Center, 2010  
  <https://ntrs.nasa.gov/citations/20100037210>
- **ISRU Pilot Excavator (IPEx) TRL-5 Design Overview (Schuler et al., ASCEND 2024): 42 kg/h, 30 cm/s, actuator life tests** — NASA KSC, 2024  
  <https://www.nasa.gov/wp-content/uploads/2024/08/ascend24-ipex-trl-5-design-overview.pdf>
- **ISRU Technology Development for Extraction of Water from the Mars Surface** — NASA GRC (Kleinhenz), 2018  
  <https://ntrs.nasa.gov/citations/20180005542>
- **Key Design Trades for a Near-term Lunar Fission Surface Power System (NETS 2025)** — NASA GRC / INL / LANL, 2025  
  <https://ntrs.nasa.gov/api/citations/20250000841/downloads/Mason%20NETS%20paper%202025c.pdf>
- **Kilopower: Small & Affordable Fission Power Systems for Space** — NASA GRC, 2018  
  <https://ntrs.nasa.gov/api/citations/20180000691/downloads/20180000691.pdf>
- **Kilopower: Small Fission Power Systems for Mars and Beyond (10 kWe radiator sizing)** — NASA GRC, 2014  
  <https://ntrs.nasa.gov/api/citations/20140010823/downloads/20140010823.pdf>
- **Lander and rover histories of dust accumulation on and removal from solar arrays on Mars** — Planetary and Space Science 207, 105337 (Lorenz et al.), 2021  
  <https://www.sciencedirect.com/science/article/pii/S0032063321001768>
- **Life Support Baseline Values and Assumptions Document (BVAD), NASA/TP-2015-218570 Rev2** — NASA (Anderson, Ewert, Keener), 2022  
  <https://ntrs.nasa.gov/api/citations/20210024855/downloads/BVAD_2.15.22-final.pdf>
- **Life Support Baseline Values and Assumptions Document (NASA/TP-2015-218570/REV2)** — NASA Johnson Space Center, 2022  
  <https://ntrs.nasa.gov/citations/20210024855>
- **Liquefaction of Cryogenic Fluids for Production and Storage (CryoFILL)** — NASA, NTRS 20210023574, 2021  
  <https://ntrs.nasa.gov/api/citations/20210023574/downloads/SCW_CryoFILL_Presentation_v4.pdf>
- **Logistics Needs for Deep Space Missions / Mars One feasibility (ISS ~10 t/yr spares; spares -> 64% of resupply by year 10)** — Owens & de Weck, MIT / NASA NTRS 20150003005, 2015  
  <https://news.mit.edu/2014/technical-feasibility-mars-one-1014>
- **Long duration propellant stability in Starship (blog)** — C. Handmer, 2025  
  <https://caseyhandmer.wordpress.com/2025/03/14/long-duration-propellant-stability-in-starship/>
- **Long-Term Cryogenic Propellant Storage on Mars with Hercules Propellant Storage Facility (NTRS 20170011674)** — G. Liu, NASA KSC, 2017  
  <https://ntrs.nasa.gov/citations/20170011674>
- **Maintenance, reliability and policies for orbital space station life support systems, Reliability Engineering & System Safety 92(6):808-820** — University of Colorado, 2007  
  <https://www.sciencedirect.com/science/article/abs/pii/S0951832006001050>
- **Making Humans a Multi-Planetary Species / Making Life Multi-Planetary (IAC 2016/2017 talks, New Space)** — SpaceX (company claims), 2017  
  <https://www.liebertpub.com/doi/10.1089/space.2018.29013.emu>
- **Mars Direct: A Simple, Robust, and Cost Effective Architecture (Zubrin & Baker)** — Martin Marietta / Mars Society, 1991  
  <http://www.astronautix.com/m/marsdirect.html>
- **Mars Exploration Rover spacecraft specs (5 cm/s top speed, ~1 cm/s hazard-avoidance average); Opportunity records 45.16 km / 5111 sols** — NASA NSSDCA / JPL, 2018  
  <https://nssdc.gsfc.nasa.gov/nmc/spacecraft/display.action?id=2003-027A>
- **Mars Fact Sheet** — NASA NSSDC, 2024  
  <https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html>
- **Mars Oxygen ISRU Experiment (MOXIE)** — Hecht et al., Space Science Reviews 217:9, 2021  
  <https://link.springer.com/article/10.1007/s11214-020-00782-8>
- **Mars Oxygen ISRU Experiment (MOXIE) - Preparing for human Mars exploration** — MIT/NASA JPL, Science Advances 8(35) eabp8636, 2022  
  <https://pmc.ncbi.nlm.nih.gov/articles/PMC9432831/>
- **Mars Propellant Liquefaction and Storage Performance Modeling (Creare 90K/500W turbo-Brayton)** — NASA, NTRS 20170009150, 2017  
  <https://ntrs.nasa.gov/api/citations/20170009150/downloads/20170009150.pdf>
- **Mars Radiation Environment Under Different Atmospheric and Regolith Depths (Zhang et al.)** — JGR Planets e2021JE007157, 2022  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2021JE007157>
- **Mars Rodwell Experiment Final Report, NASA/TP-20205011353** — NASA JSC, 2020  
  <https://ntrs.nasa.gov/citations/20205011353>
- **Mars Science Laboratory Observations of the 2018/Mars Year 34 Global Dust Storm** — Geophysical Research Letters (Guzewich et al.), 2019  
  <https://doi.org/10.1029/2018GL080839>
- **Mars Science Laboratory Observations of the 2018/Mars Year 34 Global Dust Storm** — GRL 46 (Guzewich et al.), 2019  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2018GL080839>
- **Mars Soil Temperature and Thermal Properties From InSight HP3 Data** — GRL 51 (Spohn et al.), 2024  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2024GL108600>
- **Mars Solar Power** — NASA NTRS 20040191326 (Landis et al.), 2004  
  <https://ntrs.nasa.gov/api/citations/20040191326/downloads/20040191326.pdf>
- **Mars Solar Power (NASA/TM-2004-213367, AIAA-2004-5555)** — NASA Glenn Research Center (Landis, Kerslake, Jenkins, Scheiman), 2004  
  <https://ntrs.nasa.gov/citations/20040191326>
- **Mars Surface Power Technology Decision (2024 Moon-to-Mars Architecture Concept Review white paper)** — NASA ESDMD, 2024  
  <https://www.nasa.gov/wp-content/uploads/2024/12/acr24-mars-surface-power-decision.pdf>
- **Mars Water In-Situ Resource Utilization (ISRU) Planning (M-WIP) Study** — NASA/MEPAG (Abbud-Madrid et al.), 2016  
  <https://mepag.jpl.nasa.gov/reports/Mars_Water_ISRU_Study.pdf>
- **Mars' Surface Radiation Environment Measured with the Mars Science Laboratory's Curiosity Rover (Hassler et al.)** — Science 343:1244797, 2014  
  <https://www.science.org/doi/10.1126/science.1244797>
- **Mars24 Sunclock - Algorithm and Worked Examples** — NASA GISS, 2024  
  <https://www.giss.nasa.gov/tools/mars24/help/algorithm.html>
- **Mars24 Sunclock - Time on Mars (sol length; Allison & McEwen 2000)** — NASA GISS, 2000  
  <https://www.giss.nasa.gov/tools/mars24/help/notes.html>
- **MARSTHERM Thermal Model Documentation** — Planetary Science Institute, 2023  
  <https://marstherm.psi.edu/model_documentation.php>
- **Measurements of Energetic Particle Radiation in Transit to Mars on the Mars Science Laboratory (Zeitlin et al.)** — Science 340:1080, 2013  
  <https://www.science.org/doi/10.1126/science.1235989>
- **Methane and oxygen from energy-efficient, low temperature ISRU (MOXIE power-normalized benchmarks)** — AIChE Journal 69(5) e18010, 2023  
  <https://arxiv.org/pdf/2404.00800>
- **Minimum Acceptable Net Habitable Volume for Long-Duration Exploration Missions (Whitmire et al., NASA/TM-2015-218564)** — NASA HRP, 2015  
  <https://ntrs.nasa.gov/api/citations/20140016951/downloads/20140016951.pdf>
- **Mission Architecture Using the SpaceX Starship Vehicle to Enable a Sustained Human Presence on Mars (New Space 10(3))** — Heldmann et al., 2022  
  <https://pmc.ncbi.nlm.nih.gov/articles/PMC9527650/>
- **Mission design options for human Mars missions (MARS Journal; ascent delta-v heritage values as quoted by Rapp/IgMin)** — Wooster et al., 2007  
  <https://www.igminresearch.com/articles/html/igmin232>
- **Mission design options for human Mars missions (Wooster, Braun, Ahn, Putnam)** — Int. J. Mars Science and Exploration, 2007  
  <https://ui.adsabs.harvard.edu/abs/2007IJMSE...3...12W/abstract>
- **MMRTG fact sheet / flight performance (Curiosity, Perseverance)** — NASA/JPL, DOE, 2020  
  <https://mars.nasa.gov/internal_resources/788/>
- **Modeling a 15-min EVA prebreathe protocol using NASA's exploration atmosphere 56.5 kPa/34% O2 (Abercromby et al.)** — Acta Astronautica 109:76, 2015  
  <https://www.sciencedirect.com/science/article/abs/pii/S0094576514004937>
- **Modeling Logistics and Supportability for Crewed Missions Beyond Low Earth Orbit (ICES-2024-95)** — Binera / NASA Langley, 2024  
  <https://ntrs.nasa.gov/citations/20240005642>
- **Modeling Martian Surface Thermal Environments in Thermal Desktop (TFAWS 2024 Passive Thermal Paper Session)** — NASA MSFC (Popok), 2024  
  <https://tfaws.nasa.gov/wp-content/uploads/TFAWS2024-PT-2.pdf>
- **Musk: 50-50 chance of uncrewed Starship (carrying Optimus) to Mars in late-2026 window** — Al Jazeera / space.com, 2025  
  <https://www.aljazeera.com/news/2025/5/30/musk-says-50-50-chance-of-sending-uncrewed-starship-to-mars-by-late-2026>
- **NASA Achieves Water Recovery Milestone on the International Space Station (98%)** — NASA, 2023  
  <https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/>
- **NASA Announces Artemis Concept Awards for Nuclear Power on Moon (FSP Phase 1 contracts)** — NASA / DOE, 2022  
  <https://www.nasa.gov/exploration-systems-development-mission-directorate/fission-surface-power/>
- **NASA cryocooler technology developments and goals (zero boil-off, propellant liquefaction)** — Plachta et al., Cryogenics / NTRS 20180004709, 2018  
  <https://ntrs.nasa.gov/citations/20180004709>
- **NASA cryocooler technology developments and goals to achieve zero boil-off (Cryogenics 94, NTRS 20180004710)** — Plachta et al., NASA GRC, 2018  
  <https://ntrs.nasa.gov/citations/20180004710>
- **NASA HLS tanker-count statements ('high teens', Nov 2023) and GAO Artemis reporting** — NASA / GAO / SpaceNews, 2023  
  <https://spacenews.com/starship-lunar-lander-missions-to-require-nearly-20-launches-nasa-says/>
- **NASA Retires InSight Mars Lander Mission (end-of-mission power 500 Wh/sol)** — NASA, 2022  
  <https://www.nasa.gov/missions/insight/nasa-retires-insight-mars-lander-mission-after-years-of-science/>
- **NASA's Contributions to Vertical Farming, NASA/TM-2020-5008832** — NASA, 2020  
  <https://ntrs.nasa.gov/api/citations/20205008832/downloads/NASA%20TM-2020-5008832%20NASA's%20Contributions%20to%20Vertical%20Farming.pdf>
- **NASA's Management of the Human Landing System Contracts (IG-26-004); GAO HLS reviews (tanker counts 16-19)** — NASA OIG / GAO, 2026  
  <https://oig.nasa.gov/wp-content/uploads/2026/03/final-report-ig-26-004-nasas-management-of-the-human-landing-system-contracts.pdf>
- **NASA's Oxygen-Generating Experiment MOXIE Completes Mars Mission** — NASA/JPL, 2023  
  <https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/>
- **NASA-STD-3001 Vol 1 Rev B and OCHMO Radiation Protection Technical Brief (600 mSv career limit)** — NASA OCHMO, 2022  
  <https://www.nasa.gov/wp-content/uploads/2023/03/radiation-protection-technical-brief-ochmo.pdf>
- **Nuclear Power Concepts and Development Strategies for High-Power Electric Propulsion Missions to Mars (NASA/TM-20210016968)** — NASA / ORNL, 2022  
  <https://ntrs.nasa.gov/api/citations/20210016968/downloads/TM-20210016968_final.pdf>
- **Nuclear power on the moon: what we're watching (Aug 2025 NASA FSP directive)** — American Nuclear Society, Nuclear Newswire, 2025  
  <https://www.ans.org/news/2025-09-02/article-7336/nuclear-power-on-the-moon-what-were-watching/>
- **OCHMO-TB-004 Carbon Dioxide (technical brief) / NASA-STD-3001 Vol 2 Rev D** — NASA OCHMO, 2024  
  <https://www.nasa.gov/ochmo-tb-004-carbon-dioxide-2/>
- **On-Orbit Maintenance Operations Strategy for the International Space Station - Concept and Implementation** — NASA Johnson Space Center, 2000  
  <https://ntrs.nasa.gov/citations/20100042525>
- **Optimization of the Carbon Dioxide Removal Assembly (CDRA-4EU)** — NASA, 2015  
  <https://ntrs.nasa.gov/api/citations/20150016500/downloads/20150016500.pdf>
- **Optimus (robot) - aggregated specs and production/Mars claims with dates** — Wikipedia, 2026  
  <https://en.wikipedia.org/wiki/Optimus_(robot)>
- **Options for Offloading a 90-Ton Common Habitat from its Lander on the Surface of Mars (ASCEND 2023, NTRS 20220010430)** — Howard et al., NASA JSC/JPL, 2023  
  <https://ntrs.nasa.gov/citations/20220010430>
- **Preliminary interpretation of the REMS pressure data from the first 100 sols of the MSL mission** — JGR Planets (Haberle et al.), 2014  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1002/2013JE004488>
- **Prevalence of sleep deficiency and use of hypnotic drugs in astronauts (Barger et al.)** — Lancet Neurology 13:904, 2014  
  <https://pubmed.ncbi.nlm.nih.gov/25127232/>
- **Progress in Simulated Water Well Performance on Mars (AIAA ASCEND)** — NASA JSC (Hoffman, Andrews, Watts, Benson), 2020  
  <https://ntrs.nasa.gov/citations/20205007716>
- **Radiation environment for future human exploration on the surface of Mars (Guo et al. review)** — Astron. Astrophys. Rev. 29:8, 2021  
  <https://link.springer.com/article/10.1007/s00159-021-00136-5>
- **Raptor 3 specifications (X post)** — SpaceX, 2024  
  <https://x.com/SpaceX/status/1819772716339339664>
- **RedWater: Water Mining System for Mars** — New Space / Honeybee Robotics (Mellerowicz, Zacny et al.), 2022  
  <https://doi.org/10.1089/space.2021.0057>
- **Reevaluated martian atmospheric mixing ratios from the mass spectrometer on the Curiosity rover** — Planet. Space Sci. (Franz et al.), 2017  
  <https://www.sciencedirect.com/science/article/abs/pii/S0032063315000495>
- **Refined Mapping of Subsurface Water Ice on Mars to Support Future Missions (SWIM 3/4)** — Planetary Science Journal, 2025  
  <https://doi.org/10.3847/PSJ/ad9b24>
- **Sabatier Carbon Dioxide Reduction Assembly Development for Closed Loop Water Recovery** — NASA, 2010  
  <https://ntrs.nasa.gov/api/citations/20100033195/downloads/20100033195.pdf>
- **Sabatier System Design Study for a Mars ISRU Propellant Production Plant (ICES-2018)** — NASA, NTRS 20180004697, 2018  
  <https://ntrs.nasa.gov/api/citations/20180004697/downloads/20180004697.pdf>
- **Scientific Observations With the InSight Solar Arrays: Dust, Clouds, and Eclipses on Mars** — Earth and Space Science (Lorenz et al.), 2020  
  <https://doi.org/10.1029/2019EA000992>
- **Seasonal Variations in Atmospheric Composition as Measured in Gale Crater, Mars** — JGR Planets (Trainer et al.), 2019  
  <https://agupubs.onlinelibrary.wiley.com/doi/10.1029/2019JE006175>
- **Solar particle event storm shelter requirements for missions beyond low Earth orbit (Townsend et al.)** — Life Sci. Space Res. 17:32, 2018  
  <https://pubmed.ncbi.nlm.nih.gov/29753411/>
- **Solar Radiation on Mars (NASA TM-102299)** — NASA Lewis Research Center (Appelbaum & Flood), 1989  
  <https://ntrs.nasa.gov/citations/19890018252>
- **Solar Radiation on Mars - Update 1990 (NASA TM-103623)** — NASA Lewis Research Center (Appelbaum & Flood), 1990  
  <https://ntrs.nasa.gov/citations/19910005804>
- **Solar vs. Fission Surface Power for Mars (AIAA Space 2016)** — NASA JSC (Rucker et al.), 2016  
  <https://ntrs.nasa.gov/citations/20160002628>
- **Solar vs. Fission Surface Power for Mars - presentation (NTRS 20160011275)** — NASA JSC (Rucker), 2016  
  <https://ntrs.nasa.gov/api/citations/20160011275/downloads/20160011275.pdf>
- **SpaceX Mars colonization program - dated statement compilation (Sep 2024, May 2025, Feb 2026)** — Wikipedia (secondary, cross-checked), 2026  
  <https://en.wikipedia.org/wiki/SpaceX_Mars_colonization_program>
- **SpaceX Starship / SpaceX Starship (spacecraft) / SpaceX Raptor / SpaceX Mars colonization program (accessed 2026-07)** — Wikipedia, 2026  
  <https://en.wikipedia.org/wiki/SpaceX_Starship>
- **SpaceX Starship Landing Sites on Mars (52nd LPSC, abstract #2420)** — JPL/Caltech with SpaceX, 2021  
  <https://www.hou.usra.edu/meetings/lpsc2021/pdf/2420.pdf>
- **Spare Parts Requirements for Space Missions with Reconfigurability and Commonality, J. Spacecraft & Rockets 44(1)** — MIT, 2007
- **Starship Flight 12: Block 3 Impresses on First Flight** — NASASpaceFlight.com, 2026  
  <https://www.nasaspaceflight.com/2026/05/starship-flight-12-block-3-pad-2/>
- **Starship V3 Vehicle Overview** — rocketlaunch.org, 2026  
  <https://rocketlaunch.org/launch-providers/spacex/starship-v3>
- **Starship vehicle page (propellant load, Raptor mixture ratio) - company claims** — SpaceX, 2026  
  <https://www.spacex.com/vehicles/starship>
- **Starship vehicle specifications and Mars propellant plans (company claims, 2016-2025)** — SpaceX, 2025  
  <https://www.spacex.com/vehicles/starship/>
- **Status of the Advanced Oxygen Generation Assembly Design (ICES-2023-311)** — NASA MSFC, 2023  
  <https://ntrs.nasa.gov/api/citations/20230008013/downloads/ICES_2023_311%20final%205%2015%2023.pdf>
- **Stoichiometric model of a fully closed bioregenerative life support system for autonomous long-duration space missions (MELiSSA)** — Frontiers in Astronomy and Space Sciences, 2023  
  <https://www.frontiersin.org/articles/10.3389/fspas.2023.1198689/full>
- **Stow: Robotic Packing of Items into Fabric Pods (robot 224 vs human 243 units/h, measured at scale)** — Amazon Robotics, arXiv:2505.04572, 2025  
  <https://arxiv.org/pdf/2505.04572>
- **Subsurface Radiation Environment of Mars and Its Implication for Shielding Protection of Future Habitats (Röstel et al.)** — JGR Planets 125:e2019JE006246, 2020  
  <https://agupubs.onlinelibrary.wiley.com/doi/10.1029/2019JE006246>
- **Supportability Challenges, Metrics, and Key Decisions for Future Human Spaceflight (AIAA 2017-5124)** — MIT / Binera / NASA Langley, 2017  
  <https://ntrs.nasa.gov/citations/20170009115>
- **Systems Analysis of In-Space Manufacturing Applications for the International Space Station and the Evolvable Mars Campaign (AIAA 2016-5394)** — MIT, 2016  
  <https://ntrs.nasa.gov/citations/20160011570>
- **Tesla AI Day 2022 presentation (Optimus specs: 2.3 kWh, 100/500 W, payload, <$20k) - company claims** — Tesla / Forbes coverage, 2022  
  <https://www.forbes.com/sites/jamesmorris/2022/10/01/tesla-ai-day-2022-musk-promises-optimus-humanoid-robot-for-under-20000/>
- **Tesla Optimus Gen 2 unveil (57 kg, 11-DoF hands, faster walk) - company claims** — Tesla / Electrek coverage, 2023  
  <https://electrek.co/2023/12/12/tesla-unveils-optimus-gen-2-next-generation-humanoid-robot/>
- **The annual cycle of pressure on Mars measured by Viking Landers 1 and 2** — GRL 7 (Hess et al.), 1980  
  <https://agupubs.onlinelibrary.wiley.com/doi/abs/10.1029/gl007i003p00197>
- **The ATHLETE Rover (850 kg / 300-450 kg payload, ~10 km/h)** — NASA JPL Robotics, 2010  
  <https://robotics.jpl.nasa.gov/how-we-do-it/systems/the-athlete-rover/>
- **The atmosphere of Mars as observed by InSight** — Nature Geoscience 13 (Banfield et al.), 2020  
  <https://www.nature.com/articles/s41561-020-0534-0>
- **The Case for Mars / Mars Direct (H2 import leverage 18:1)** — R. Zubrin, 1996  
  <https://en.wikipedia.org/wiki/Mars_Direct>
- **The fake news about robots and their reliability (vendor 80,000 h MTBF claims vs ~88% real cell availability)** — The Robot Report, 2017  
  <https://www.therobotreport.com/the-fake-news-about-robots-and-their-reliability/>
- **The Kilopower Reactor Using Stirling TechnologY (KRUSTY) Nuclear Ground Test Results and Lessons Learned (AIAA P&E 2018)** — NASA GRC / LANL, 2018  
  <https://ntrs.nasa.gov/citations/20180005435>
- **The Mars Global Dust Storm of 2018 (ICES-2019)** — NASA (NTRS 20190027303), 2019  
  <https://ntrs.nasa.gov/citations/20190027303>
- **The Mars Science Laboratory record of optical depth measurements via solar imaging** — Icarus (Lemmon et al.), 2024  
  <https://arxiv.org/abs/2309.07378>
- **The Modern Near-Surface Martian Climate: A Review of In-situ Meteorological Data from Viking to Curiosity** — Space Science Reviews 212 (Martinez et al.), 2017  
  <https://link.springer.com/article/10.1007/s11214-017-0360-x>
- **The Road to Making Life Multiplanetary (Starbase presentation)** — SpaceX / E. Musk, 2025  
  <https://www.friendsofnasa.org/2025/06/the-road-to-making-life-multiplanetary.html>
- **The Solar Spectrum on the Martian Surface and its Effect on Photovoltaic Performance** — NASA Glenn Research Center (Landis & Hyatt), 2007  
  <https://ntrs.nasa.gov/citations/20070010752>
- **The Threat of Uncertainty - Why Using Traditional Approaches for Evaluating Spacecraft Reliability Are Insufficient for Future Human Mars Missions (AIAA 2016-5307)** — Binera / NASA Langley / MIT, 2016  
  <https://ntrs.nasa.gov/citations/20160011578>
- **The Wait-less EVA Solution: Single-Person Spacecraft (Griffin, AIAA 2020-4170; ISS EVA overhead per NASA EVA-EXP-0031)** — AIAA, 2020  
  <https://spacearchitect.org/pubs/AIAA-2020-4170.pdf>
- **Thermal Conductivity of the Martian Soil at the InSight Landing Site From HP3 Active Heating Experiments** — JGR Planets (Grott et al.), 2021  
  <https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2021JE006861>
- **Thermal Design of an Antarctic Water Well, CRREL Special Report 95-10** — US Army CRREL (Lunardini & Rand), 1995
- **Thermodynamic modeling of in-situ rocket propellant fabrication on Mars** — iScience 25:104323, 2022  
  <https://www.sciencedirect.com/science/article/pii/S2589004222005946>
- **Time delay between Mars and Earth (3.03-22.3 min one-way)** — ESA Mars Express blog, 2012  
  <https://blogs.esa.int/mex/2012/08/05/time-delay-between-mars-and-earth/>
- **Toward sustainable living in space: A review of environmental control and life support system technologies** — 2025  
  <https://www.sciencedirect.com/science/article/pii/S2950616625000452>
- **TRIDENT Drill Validation at Mars and Lunar Analog Field Sites (LPSC 55)** — NASA Ames / Honeybee Robotics (Glass et al.), 2024  
  <https://ntrs.nasa.gov/citations/20240000585>
- **Ultraviolet and biological effective dose observations at Gale Crater, Mars (REMS UV record)** — PNAS, 2025  
  <https://www.pnas.org/doi/10.1073/pnas.2426611122>
- **Valkyrie R5 fact sheet (125 kg, 44 DoF, 1.8 kWh, ~1 h runtime)** — NASA JSC, 2015  
  <https://www.nasa.gov/wp-content/uploads/2023/06/r5-fact-sheet.pdf>
- **What to Expect from Starship V3** — Payload, 2026  
  <https://payloadspace.com/what-to-expect-from-starship-v3/>
- **Widespread excess ice in Arcadia Planitia, Mars** — Geophysical Research Letters, 2015  
  <https://doi.org/10.1002/2015GL064844>
- **XTJ Prime 30.7% Triple Junction Space Grade Solar Cell datasheet (company claim)** — Spectrolab (Boeing), 2016  
  <https://www.spectrolab.com/DataSheets/cells/XTJ_Prime_Data_Sheet_7-28-2016.pdf>
- **Zero Boil-Off Methods for Large Scale Liquid Hydrogen** — NASA, NTRS 20170006481, 2017  
  <https://ntrs.nasa.gov/api/citations/20170006481/downloads/20170006481.pdf>
