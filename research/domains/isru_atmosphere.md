# ISRU from the Mars Atmosphere: CO2-Based Propellant & O2 Production

**Domain key:** `isru_atmosphere`
**Compiled:** July 2026, for a system-level Mars habitat/settlement trade-study simulation.
**Scope:** MOXIE flight results, CO2 acquisition, solid-oxide CO2 electrolysis (SOXE) scaling, Sabatier
methanation, water electrolysis, full methalox plant specific energy & mass, the bring-your-own-H2
architecture, cryogenic liquefaction, and a worked scaling anchor: refueling one Starship in ~500 sols.

Conventions: SI units, energy in kWh, 1 sol = 88,775 s = 24.6597 h, 1 Mars year = 668.6 sols.
"Company claim" flags SpaceX numbers that have no independent verification.

---

## 1. Chemistry: the four governing reactions

| # | Reaction | ΔH (298 K) | Per-kg figure |
|---|----------|-----------|----------------|
| R1 | SOXE: 2 CO2 → 2 CO + O2 | +566 kJ/mol O2 | 4.91 kWh/kg O2 (enthalpy minimum) |
| R2 | Sabatier: CO2 + 4 H2 → CH4 + 2 H2O | −165.4 kJ/mol CH4 (exothermic) | releases 2.87 kWh/kg CH4 |
| R3 | Electrolysis: H2O → H2 + ½ O2 | +285.8 kJ/mol (HHV) | 39.4 kWh/kg H2 theoretical; ~4.38 kWh/kg H2O |
| R4 | Net methalox: CO2 + 2 H2O → CH4 + 2 O2 (R2+R3 with H2 recycle) | +890 kJ/mol CH4 | 3.09 kWh/kg of product mix (O:F = 4.0) |

R4 is the thermodynamic floor for water-based methalox production: **≈3.1 kWh per kg of CH4+O2**.
Real end-to-end plants land at **15–25 kWh/kg** (Section 8) — i.e., first-generation hardware runs at
~13–20% of the thermodynamic ideal once acquisition, thermal losses, liquefaction, margins and
regolith-water extraction are included.

Key stoichiometric ratios (exact, from molar masses):
- 2.75 kg CO2 per kg CH4 (44/16)
- 0.50 kg H2 per kg CH4 fed to Sabatier; 0.25 kg H2 net if product water is electrolyzed and H2 recycled
- 2.25 kg H2O produced per kg CH4 (Sabatier); electrolyzing it returns 2.0 kg O2 + 0.25 kg H2 per kg CH4
- Water electrolysis: 1 kg H2O → 0.111 kg H2 + 0.889 kg O2

---

## 2. MOXIE flight results (Mars 2020 Perseverance, Apr 2021 – Aug 2023)

The only ISRU hardware ever operated on another planet. Sources: Hoffman et al., *Science Advances*
8, eabp8636 (2022); Hoffman et al., "18 Months of MOXIE," *Acta Astronautica* 210:547–553 (2023);
NASA/JPL end-of-mission release (Sept 6, 2023); Hecht et al., *Space Sci. Rev.* 217:9 (2021).

| Quantity | Value | Note |
|----------|-------|------|
| Runs completed | 16 | Apr 20, 2021 – Aug 7, 2023, spanning a full Mars year of seasons |
| Total O2 produced | 122 g | first run 5.37 g; final run 9.8 g |
| Nominal production rate | 6–10 g/hr | design goal ≥6 g/hr |
| Peak production rate | 12 g/hr | 2× original goal; limited by 4 A supply, not by the stack |
| O2 purity | ≥98% | typically >99.6% |
| System power | ~300 W | scroll compressor + SOXE stack + avionics |
| SOXE stack power | ~115 W | ~35 W electrolysis + ~80 W stack heater (800 °C operating temp) |
| CO2 intake | 30–80 g/hr | scroll compressor, 2000–4000 rpm; ~55 g/hr max at low-density season |
| SOXE stack | 10 cells (2×5 stacks), YSZ/ScSZ electrolyte | built by OxEon Energy |
| Mass | 17.1 kg | 24 × 24 × 31 cm |
| Cumulative production time | ~15 h (derived: 122 g ÷ ~8 g/hr) | short-duty-cycle runs, constrained by rover power |
| Degradation | iASR < 0.75 → ~0.9 Ω·cm² during burn-in, then small gradual increase per run | "graceful, not catastrophic"; projected >60 cycles |

**Specific energy achieved (flight):**
- Whole system: 300 W / (6–12 g/hr) = **25–50 kWh/kg O2** (≈30 kWh/kg at nominal 10 g/hr)
- Stack only, best case: 2.5 g·W⁻¹·day⁻¹ → **9.6 kWh/kg O2**; typical stack-basis 10–19 kWh/kg

The gulf between 4.9 kWh/kg (thermo minimum) and 25–50 kWh/kg (MOXIE) is almost entirely a
small-scale penalty: at 100 g/hr-class throughput, fixed thermal losses (800 °C stack in a cold rover
belly) and compressor inefficiency dominate. This is why scaled projections (below) are ~3× better.

**CO2 utilization constraint:** only ~50% or less of intake CO2 can be electrolyzed per pass; pushing
utilization (or cell voltage above ~1.1–1.2 V Nernst-adjusted) risks carbon deposition (coking) that
destroys cells. Scaled plants recycle or dump the CO/CO2 effluent.

---

## 3. CO2 acquisition from a 600–900 Pa atmosphere

The atmosphere is ~95–96% CO2 but at 0.6–0.9 kPa and 145–250 K; every architecture must first gather
and pressurize it. Surface pressure swings ±25–30% seasonally (CO2 condensation at the poles), which
directly modulates compressor throughput — MOXIE saw max intake vary ~2× over the year.

| Method | Specific energy (kWh/kg CO2) | Status | Notes |
|--------|------------------------------|--------|-------|
| Thermal-swing sorption (MOF/zeolite) | **0.60** (0.5–0.8) | TDA lab, 4500 cycles | 0.91 kW heat for 1.51 kg/hr CO2 (ICES-2020-151); delivers high-pressure CO2; heat can be waste heat |
| Cryogenic freezer (desublimation ~150 K) | **~1.0** (0.7–1.5) | KSC MACDOF >85 h; baseline in Kleinhenz & Paz | Derived: ~620 kJ/kg (desublimation 573 + sensible) ÷ COP 0.21 (20% Carnot @150 K, per Berg TFAWS-2018 hardware) ≈ 0.82 kWh/kg + fans; delivers pure liquid/high-P CO2 on re-vaporization; also rejects Ar/N2 inherently |
| Mechanical compression (scroll/piston) | **~0.4** (0.15–1.7) | MOXIE flown (small scale); Acta Astr. 2024 prototype 35% single-stage efficiency | Ideal isothermal 0.7 kPa→100 kPa is only 0.055 kWh/kg; real small machines much worse (MOXIE scroll ≈1.7 kWh/kg at 100 W / ~60 g/hr); does not separate Ar/N2 (~4–5% inerts pass through) |

**Trade:** sorption wins on energy if low-grade heat is free; cryofreezing wins on purity and pairs
naturally with a plant that already has 90 K cryocoolers; mechanical is simplest and continuous but
needs downstream inert-gas management. NASA's EMC baseline (Kleinhenz & Paz 2017) chose the
cryofreezer on flight-heritage grounds.

---

## 4. SOXE scaling (CO2 → O2 only, "oxygen-only architecture")

- MOXIE-derived scaling (Hoffman et al. 2023, Acta Astr. 210): a Mars Ascent Vehicle-class plant needs
  **2–3 kg/hr O2** produced continuously for ~14 months, drawing **25–30 kW** → **≈9–14 kWh/kg O2**
  (≈11 nominal) at the plant level, including acquisition and thermal management.
- OxEon Energy (MOXIE stack vendor) is building a "manned-mission scale" stack; four such stacks
  exceed 2.3 kg/hr O2 — the rate needed to make ~30 t of liquid O2 in a 435-day window.
- Kleinhenz & Paz 2017 O2-only case (cryofreezer + SOXE + liquefaction): 22,728 kg O2 in 480 days
  (1.97 kg/hr) at **34 kW** and **0.9 t** hardware → **14–17 kWh/kg O2** system-level.
- Degradation for long-life SOEC stacks (terrestrial data): ~0.5–2% voltage rise per 1000 h — adequate
  for a 10,000 h (14-month) campaign with margin, consistent with MOXIE's "graceful" trend.

O2-only ISRU is attractive because O2 is 75–80% of methalox propellant mass; but you then must land
all CH4 from Earth (~7 t for an MAV; ~260 t for a Starship — untenable at Starship scale, which is
why water or H2 import enters the picture).

---

## 5. Sabatier methanation (CO2 + 4 H2 → CH4 + 2 H2O)

Sources: NASA Sabatier System Design Study, ICES-2018 (NTRS 20180004697); PNNL microchannel
reactor work; ISS CDRA/Sabatier flight experience.

- **Single-pass CO2 conversion: 90–95%** at 350–400 °C (thermodynamics limit conversion at high T,
  kinetics die below ~375 °C fast-kinetics threshold; designs use adiabatic + cooled stages or
  isothermal microchannels to reach >90% of thermodynamic equilibrium). With H2 slightly lean and
  recycle, effective conversion **>99%** (demonstrated over H2:CO2 = 1.8–5).
- **Catalyst: Ru on Al2O3** (flight standard; Ni cheaper but less active/durable).
- **Exothermic: ΔH = −165.4 kJ/mol → 2.87 kWh of heat per kg CH4.** After startup the reactor needs
  no net power; the design problem is heat *rejection* and temperature control, not heat supply. The
  waste heat is high-grade (~350–400 °C) and can drive sorption-bed swings or regolith/water heating.
- NASA design-study scale: 0.34 kg/hr CH4 (CO2 feed 0.94–0.98 kg/hr, H2 feed 0.17–0.22 kg/hr,
  8–75 psia operating pressure).
- Net electrical draw of a flight Sabatier loop (controls, recycle blower, condenser) is small: order
  0.1–0.3 kWh/kg CH4 (estimate).

---

## 6. Water electrolysis

- Theoretical (HHV): 39.4 kWh/kg H2. Commercial PEM/alkaline systems: **50–60 kWh/kg H2**
  (55 typical; 48 kWh_AC/kg reported for good systems; 60–70% efficiency).
- Per kg of water: **6.1 kWh/kg H2O** at 55 kWh/kg H2 (theoretical 4.38).
- Kleinhenz & Paz baseline used a Giner cathode-feed PEM stack; ISS OGA is the flight-proven
  reference (same class of technology, ~50% system efficiency at rack level).
- For methalox plants, electrolysis is **the dominant electrical load**: at 55 kWh/kg H2, the 0.5 kg H2
  cycled per kg CH4 costs 27.5 kWh/kg CH4 ≈ 6.0 kWh per kg of O:F=3.6 propellant mix — two to three
  times all other process steps combined.

---

## 7. Liquefaction and cryogenic storage

Sources: NASA CryoFILL (NTRS 20210023574; TM-20210010564), Mars liquefaction Thermal-Desktop
modeling (NTRS 20170009150, Creare 90 K/500 W reverse turbo-Brayton), iScience 25:104323 (2022);
NASA cryocooler goals (Plachta et al., NTRS 20180004709).

| Fluid | Storage T on Mars | Specific energy (kWh/kg) | Anchor |
|-------|-------------------|--------------------------|--------|
| O2 | ~90 K | **1.2** (0.55–2.4) | CryoFILL: 2.2 kg/hr needs 1.2–4 kW input; iScience: 7.3 kW for 3 kg/hr incl. heat leaks at 10% cryocooler efficiency |
| CH4 | ~112 K | **1.5** (0.8–2.5) | Derived: latent 511 kJ/kg (2.4× O2's 213) but warmer sink; same 90 K cryocooler class serves both |
| H2 | ~20 K | **50** (33–100) | CryoFILL: 0.3 kg/hr needs 10–30 kW (incl. para-ortho conversion) — 20 K cooling is brutally expensive |

- **Cryocooler specific power at 90 K: ~15 W input per W lifted** (range 5–23). CryoFILL data imply
  5–16 W/W; the iScience "10% of Carnot-class overall efficiency" assumption implies ~23 W/W with a
  300 K reject. Carnot floor at 90 K cold / 300 K reject is 2.33 W/W.
- Cryocooler reject temperature varies over the sol on Mars (radiators see 150–290 K sky/air);
  night operation is materially cheaper — an L2 model should capture this (Section 10).
- Boiloff if storage is passive: LH2 0.1–0.5%/day even with good MLI; LOX/LCH4 on Mars in shaded,
  insulated tanks ~0.05–0.2%/day. Long campaigns effectively require zero-boiloff (ZBO) active
  cooling; the liquefaction cryocoolers double as ZBO once tanks are full (ZBO maintenance load is
  roughly the tank heat leak, ~1–2 W/m² × tank area, ÷ cryocooler COP).

---

## 8. Full methalox plant: specific energy and specific mass

**Primary anchor — Kleinhenz & Paz, AIAA 2017-0423 / NTRS 20170001421** (NASA GRC/JSC, Evolvable
Mars Campaign end-to-end model: RASSOR excavation + regolith dryer + cryofreezer CO2 + PNNL
microchannel Sabatier + Giner PEM electrolysis + cryocooler liquefaction; 15% structure + 20% growth
margins; 3 parallel modules at 40% capacity each):

| Case | Products | Hardware mass | Power | Specific energy | Specific mass |
|------|----------|--------------|-------|-----------------|---------------|
| 1: O2-only (atm. CO2 + SOXE) | 22.7 t O2 in 480 d (1.97 kg/hr) | 0.9 t | 34 kW | 17.3 kWh/kg O2 | 19 kg per (kg/day) |
| 2: Full methalox (regolith water @1.3 wt%) | 7.0 t CH4 + 22.7 t O2 in 480 d (3.04 kg/hr total product) | 1.7 t | 52 kW (17 kW of it thermal, regolith drying) | **20.2 kWh/kg propellant** (13.6 electric + 6.6 thermal) | **23 kg per (kg/day)** |
| 2 w/ heat recuperation | same | 1.7 t | 35 kW electric | ~13.6 kWh/kg | 23 |
| 3: + life-support consumables | +O2/H2O for crew | 2.2 t | 80 kW | — | — |

Production ledger for Case 2: CH4 6,978 kg (0.61 kg/hr, the driving requirement), H2O 15,701 kg
(1.36 kg/hr, from 68.2 kg/hr of 2 wt% soil), CO2 19,190 kg (1.67 kg/hr), O2 27,912 kg produced
(22,728 kg needed as propellant; 5,184 kg free surplus for life support — the Sabatier+electrolysis
chain makes O2 at O:F 4.0, more than the 3.5 engine needs).

**Cross-checks:**
- Component build-up (this document): 6.0 (electrolysis) + 0.6–1.7 (CO2 capture) + 1.3 (liquefaction)
  + ~1 (Sabatier aux, dryers, pumps, controls) ≈ 9–10 kWh/kg *before* water extraction, margins, and
  avionics/thermal overhead → consistent with 13.6 kWh/kg electric at system level.
- Thermodynamic floor (R4): 3.1 kWh/kg → Case-2 electric efficiency ≈ 23%.
- MOXIE-scaled O2-only: 9–14 kWh/kg O2 (Section 4) vs Kleinhenz Case-1 14–17 — consistent, with the
  difference explained by margins and cryofreezer-vs-scroll choices.

**Confidence guidance:** treat 15–25 kWh/kg propellant as the credible band for first-generation
plants (water-based CH4+O2, liquefied, with margins); 10–13 kWh/kg as an optimized target (heat
recuperation everywhere, sorption capture driven by Sabatier waste heat); 3.1 kWh/kg as the
physics floor. Specific mass 19–30 kg per (kg/day), excluding the power system.

---

## 9. Bring-your-own-hydrogen architecture and the H2 problem

**Mass leverage (exact stoichiometry):**
- Sabatier only, no water recycle: 1 kg H2 → **2.0 kg CH4** (+4.5 kg H2O byproduct).
- Sabatier + electrolyze product water + recycle H2: 1 kg H2 → **4.0 kg CH4 + 8.0 kg O2 = 12 kg**
  of O:F 2.0 mix.
- Add SOXE (or extra Sabatier-water) O2 to reach O:F 3.6: 1 kg H2 → 4.0 kg CH4 + 14.4 kg O2 =
  **18.4 kg propellant → leverage ≈ 18:1**. This is Zubrin's Mars Direct number (6 t H2 → ~107 t
  methalox, 18:1) — arithmetic verified.
- If instead water is mined locally (no Earth H2), leverage is infinite but you inherit the regolith/
  ice mining plant and its 17+ kW-class thermal load (see `isru_water` domain).

**Why nobody loves it:** the 18:1 leverage is bought with LH2 transport and storage at 20 K:
- LH2 density 71 kg/m³ → 6 t of H2 is ~85 m³ of tankage (vs ~9 m³ for the water it replaces).
- Passive boiloff 0.1–0.5%/day → 30–70% loss over a 180–300 day transit + surface wait unless ZBO
  cryocoolers fly with it (20 K lift at 30–100 kWh/kg re-liquefaction, cryocooler mass ~100+ kg/kW-input class).
- NASA's EMC and Mars DRA 5.0 both rejected Earth-H2 import in favor of Mars water for human-scale
  plants; H2 import remains credible only for small (MAV-class) plants or as startup reactant.

---

## 10. Worked scaling anchor: refuel one Starship in ~500 sols

Assumptions (company claims flagged): Starship ship propellant load **1,200 t** (SpaceX figure for
Block-1 ships, 2019–2023 presentations; Block-2/3 figures are ~1,500 t: 1,170 t LOX + 330 t LCH4);
Raptor O:F = **3.6** → 261 t CH4 + 939 t O2 per 1,200 t load. Window: 500 sols of continuous
production (one synodic cycle minus margins).

| Quantity | Value | Derivation |
|----------|-------|-----------|
| Production rate | **2,400 kg/sol** (97.3 kg/hr; 21.2 CH4 + 76.2 O2) | 1.2e6 kg ÷ 500 sols |
| vs MOXIE peak | ~8,100× MOXIE's 12 g/hr | scale gap illustration |
| vs MAV-class plant (Kleinhenz Case 2) | ~32× (3.04 kg/hr) | |
| Water feed (net) | ~587 t (1.36 t/sol) + losses → ~600–650 t | 2.25 kg H2O/kg CH4; O2 comes free at O:F 4.0, no SOXE strictly needed |
| CO2 feed | ~718 t (1.44 t/sol) | 2.75 kg CO2/kg CH4 |
| Energy @ 20.2 kWh/kg | **24.2 GWh** (range 18–30 GWh at 15–25 kWh/kg) | 1.2e6 kg × 20.2 kWh/kg |
| Average continuous power | **≈2.0 MW** (1.5–2.4 MW) | 24.2e6 kWh ÷ (500 × 24.66 h) |
| If 1,500 t load | 30 GWh, ≈2.5 MW | scale linearly |
| Solar array (if PV) | ~8–12 MW peak, ~40,000–60,000 m² + ~MWh-class storage or production duty-cycling | capacity factor ~0.2–0.25 (night + dust + season); see `power` domain |
| Plant hardware @ 23 kg/(kg/day) | ~55 t (excl. power) — likely 30–40 t with scale economies | Kleinhenz specific mass × 2,400 kg/day, sub-linear scaling expected |

Takeaway for trade studies: **propellant ISRU at Starship scale is a power problem first** (MW-class
continuous for over a Mars year, i.e., a multi-MW solar farm or several fission units), a water-mining
problem second (~600 t of water per ship), and a chemical-plant problem third (the chemistry itself is
mature). One ship per synodic period ≈ 2 MW continuous is a good L0 sizing rule.

---

## 11. Disagreements & gaps between sources

1. **Full-plant specific energy** spans 10–25 kWh/kg: Kleinhenz & Paz 20.2 (with regolith thermal);
   optimized paper studies (heat-integrated, sorption capture) claim 10–13; MOXIE flight reality was
   25–50 for O2 alone at gram scale. This document baselines 20 with range 15–25 for first-gen plants.
2. **CO2 acquisition energy** is the least-consistent number in the literature (0.15–1.7 kWh/kg
   depending on method, scale, and whether heat is "free" waste heat). Model it as a method-dependent
   parameter, not a constant.
3. **Cryocooler efficiency** assumptions range from 10% of Carnot (iScience 2022) to ~20% (Berg
   Stirling hardware, Creare turbo-Brayton targets) — a factor of 2 on all liquefaction energies.
4. **Starship propellant load** moved from 1,100 t (2017 IAC) → 1,200 t (2019–2023) → 1,500 t
   (Block 2/3, 2024–2026). All company claims; no independent verification. Mixture ratio 3.6 is
   likewise a company figure (3.545 implied by the 1,170/330 split).
5. **MOXIE degradation** is qualitative ("small iASR increase per run"); no published %/1000 h rate
   from flight. Long-duration SOEC degradation on Mars (thermal cycling + dust) is the top
   unquantified risk for the O2-only architecture.
6. **Sorption-bed performance with Mars dust** over 10^4 cycles is unproven (TDA showed 4,500
   thermal-swing cycles in the lab, clean gas).

---

## 12. Fidelity tiers for the simulation

### L0 — single scalar averages
- Propellant plant: `E = 20 kWh per kg of CH4+O2 delivered liquid` (electric+thermal), split
  produced mass 21.7% CH4 / 78.3% O2 (O:F 3.6). Plant mass `= 23 kg per (kg/day)`, excl. power.
- O2-only plant: 15 kWh/kg O2, 19 kg/(kg/day).
- One Starship (1,200 t) per 500 sols ⇒ 2.0 MW continuous.
- MOXIE-class demo: 300 W ⇒ 10 g/hr O2.

### L1 — analytic daily/seasonal model
Energy ledger per sol, with each term a parameter from the table below:

```
E_sol = m_CH4·[e_sab_aux] + m_H2O_elec·[e_elec_h2o] + m_CO2·[e_co2_acq(method)]
      + m_O2·[e_liq_o2] + m_CH4·[e_liq_ch4] + E_thermal_water_extraction + E_overhead
```
- Seasonal forcing: atmospheric density ρ(Ls) varies ±25–30% (CO2 polar condensation cycle) —
  scale CO2 acquisition energy/throughput by ρ̄/ρ(Ls); dust season modulates PV power (couple to
  `power` domain).
- Degradation: SOXE voltage (and hence e_soxe) grows 0.5–2%/1000 h; compressor/sorbent capacity
  fades ~5–10% per Mars year (speculative — carry as sensitivity).
- Heat integration switch: Sabatier exotherm (2.87 kWh/kg CH4) can offset up to ~100% of sorption
  capture heat (0.6 kWh/kg CO2 × 2.75 kg CO2/kg CH4 ≈ 1.65 kWh/kg CH4) — an L1 boolean that moves
  plant energy ~8–15%.
- Availability: apply a duty factor 0.85–0.95 (maintenance, dust storms, faults) to production.

### L2 — physics-based sub-sol timestep model
Governing equations per subsystem (timestep ~100–1000 s):

1. **Compressor / acquisition:** ṁ_CO2 = η_v · ρ_atm(t) · V_disp · N(t), with
   ρ_atm(t) from p(t), T(t) (diurnal ±20% density swing). Power
   `W = ṁ cp T_in [(P2/P1)^((γ−1)/γ) − 1]/η_ad` per stage (γ=1.29 for CO2). For cryofreezer:
   Q_lift = ṁ(h_desub + cp ΔT), P = Q_lift/COP(T_cold=150 K, T_rej(t)).
2. **SOXE stack:** V_cell = V_Nernst(T, p_O2, p_CO2/p_CO) + i·ASR(t̃);
   ASR(t̃) = ASR_0·(1+δ·t̃) with ASR_0 ≈ 0.9 Ω·cm² (post burn-in), δ from degradation parameter.
   ṁ_O2 = M_O2 · I·N_cells/(4F). Enforce utilization u = ṁ_O2,stoich/ṁ_CO2,in ≤ 0.5 and
   V_cell below coking threshold. Stack heater: Q = (UA)_stack·(1073 K − T_env(t)) − I²·ASR heating.
3. **Sabatier:** equilibrium conversion X_eq(T,P) from K_eq(T) (or fit: X≈0.95 at 375–400 °C,
   ~8–75 psia, H2:CO2=4); reactor energy balance with exotherm 165.4 kJ/mol removed to a coolant
   loop that can feed the sorption beds/water plant.
4. **Electrolyzer:** V = 1.229 + η_act + η_ohm(i); P = V·i·A·N; ṁ_H2 = M_H2·i·A·N/(2F).
5. **Liquefaction/ZBO:** P_cryo = [ṁ(cp ΔT + h_fg) + Q_leak(tank)]·SP(T_c, T_rej(t)) where
   SP = (T_rej − T_c)/(T_c · η_2L), η_2L ≈ 0.10–0.20. T_rej(t) follows the radiator/ambient
   diurnal cycle — night liquefaction is ~20–40% cheaper; an L2 scheduler can exploit this
   against a solar power profile (produce gas by day, liquefy by night is NOT free — trade
   buffer-tank mass).
6. **Boiloff:** dm/dt = max(0, Q_leak − Q_cryo)/h_fg per tank.
7. **Plant control:** production dispatch against available power P_avail(t) (couples to power
   domain); SOXE and electrolyzers throttle well (0.3–1.0), Sabatier prefers steady state,
   cryofreezers cycle batch-wise.

---

## 13. Parameter table (machine-readable copy in isru_atmosphere.json)

| id (isru_atmosphere.*) | value | unit | conf. | source |
|---|---|---|---|---|
| moxie_o2_rate_nominal | 8 (6–10) | g/hr | high | Hoffman 2022 Sci. Adv.; Hecht 2021 SSR |
| moxie_o2_rate_peak | 12 | g/hr | high | NASA/JPL 2023-09-06 release |
| moxie_total_o2_produced | 122 | g | high | NASA/JPL 2023-09-06 release |
| moxie_runs_completed | 16 | runs | high | NASA/JPL; Acta Astr. 210 (2023) |
| moxie_system_power | 300 | W | high | Hecht 2021 SSR; NASA fact sheet |
| moxie_specific_energy_system | 30 (25–50) | kWh/kg O2 | medium | derived: 300 W ÷ 6–12 g/hr |
| moxie_soxe_stack_specific_energy | 12 (9.6–19) | kWh/kg O2 | medium | 115 W stack; 2.5 g/W/day best case (Shahid 2023 AIChE J) |
| moxie_soxe_asr_post_burnin | 0.9 (0.75–0.9) | ohm·cm² | medium | Hoffman 2022 Sci. Adv. |
| soxe_scaled_plant_power | 27.5 (25–30) | kW | medium | Hoffman 2023 Acta Astr. (2–3 kg/hr O2 plant) |
| soxe_scaled_specific_energy | 11 (9–14) | kWh/kg O2 | medium | derived from above |
| soxe_thermo_min_specific_energy | 4.91 | kWh/kg O2 | high | ΔH: 2CO2→2CO+O2, 566 kJ/mol O2 |
| co2_acq_sorption_energy | 0.60 (0.5–0.8) | kWh/kg CO2 | medium | TDA ICES-2020-151 (0.91 kW @ 1.51 kg/hr) |
| co2_acq_cryofreezer_energy | 1.0 (0.7–1.5) | kWh/kg CO2 | low | derived: desublimation 620 kJ/kg ÷ COP 0.21 (Berg TFAWS-2018) |
| co2_acq_mech_compression_energy | 0.4 (0.15–1.7) | kWh/kg CO2 | low | derived; Acta Astr. 2024 (35% single stage); MOXIE scroll upper bound |
| sabatier_co2_conversion_single_pass | 0.95 (0.90–0.99) | fraction | high | NTRS 20180004697; PNNL microchannel |
| sabatier_heat_release | 2.87 | kWh/kg CH4 | high | ΔH = −165.4 kJ/mol |
| water_electrolysis_energy_h2 | 55 (48–60) | kWh/kg H2 | high | commercial PEM literature; ISS OGA class |
| water_electrolysis_energy_h2o | 6.1 (5.3–6.7) | kWh/kg H2O | medium | derived (÷9) |
| o2_liquefaction_energy | 1.2 (0.55–2.4) | kWh/kg O2 | medium | CryoFILL NTRS 20210023574; iScience 25:104323 |
| ch4_liquefaction_energy | 1.5 (0.8–2.5) | kWh/kg CH4 | low | derived (112 K, h_fg 511 kJ/kg, 90 K cooler class) |
| h2_liquefaction_energy | 50 (33–100) | kWh/kg H2 | medium | CryoFILL: 10–30 kW @ 0.3 kg/hr |
| cryocooler_specific_power_90k | 15 (5–23) | W/W | medium | CryoFILL data; iScience 10%-of-Carnot; Creare RTB |
| lh2_boiloff_passive | 0.3 (0.1–0.5) | %/day | medium | NASA ZBO literature (NTRS 20170006481 etc.) |
| h2_to_ch4_yield_sabatier | 2.0 | kg CH4/kg H2 | high | stoichiometry (no water recycle) |
| h2_propellant_leverage | 18.4 (12–18.4) | kg propellant/kg H2 | high | stoichiometry; Zubrin Mars Direct 6 t → ~107 t |
| plant_specific_energy | 20.2 (15–25) | kWh/kg propellant | medium | Kleinhenz & Paz AIAA 2017-0423 (52 kW, 29.7 t/480 d) |
| plant_specific_mass | 23 (19–30) | kg per kg/day | medium | Kleinhenz & Paz (1.7 t @ 73 kg/day product) |
| starship_propellant_load | 1.2e6 (1.1–1.5e6) | kg | medium | SpaceX company claim (2019–2026; Block 2/3 ≈1.5e6) |
| starship_of_ratio | 3.6 (3.5–3.8) | kg O2/kg CH4 | medium | SpaceX Raptor company claim |
| starship_refuel_energy_500sols | 2.42e7 (1.8–3.0e7) | kWh | medium | derived: 1.2e6 kg × 20.2 kWh/kg |
| starship_refuel_avg_power | 1965 (1500–2400) | kW | medium | derived: ÷ (500 sols × 24.66 h) |

---

## 14. Sources

1. Hecht, M. et al., "Mars Oxygen ISRU Experiment (MOXIE)," *Space Science Reviews* 217:9 (2021).
2. Hoffman, J.A. et al., "Mars Oxygen ISRU Experiment (MOXIE) — Preparing for human Mars exploration," *Science Advances* 8(35), eabp8636 (2022). https://pmc.ncbi.nlm.nih.gov/articles/PMC9432831/
3. Hoffman, J.A. et al., "18 Months of MOXIE operations on the surface of Mars," *Acta Astronautica* 210:547–553 (2023). https://www.sciencedirect.com/science/article/abs/pii/S0094576523002187
4. NASA/JPL, "NASA's Oxygen-Generating Experiment MOXIE Completes Mars Mission," Sept 6, 2023. https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/
5. Kleinhenz, J.E. & Paz, A., "An ISRU Propellant Production System to Fully Fuel a Mars Ascent Vehicle," AIAA 2017-0423 / NTRS 20170001421. https://ntrs.nasa.gov/citations/20170001421
6. NASA, "Sabatier System Design Study for a Mars ISRU Propellant Production Plant," ICES-2018 / NTRS 20180004697. https://ntrs.nasa.gov/api/citations/20180004697/downloads/20180004697.pdf
7. Alptekin, G. et al. (TDA Research), "An Advanced CO2 Capture and Compression System for Mars ISRU," ICES-2020-151. https://ttu-ir.tdl.org/server/api/core/bitstreams/93f2e845-bcf6-4817-a30b-7129eec234d0/content
8. Berg, J., "CO2 Cryofreezer Coldhead" TFAWS 2018 / NTRS 20190000481. https://ntrs.nasa.gov/api/citations/20190000481/downloads/20190000481.pdf
9. NASA CryoFILL team, "Liquefaction of Cryogenic Fluids for Production and Storage," NTRS 20210023574. https://ntrs.nasa.gov/api/citations/20210023574/downloads/SCW_CryoFILL_Presentation_v4.pdf
10. "Mars Propellant Liquefaction and Storage Performance Modeling using Thermal Desktop (Creare 90 K/500 W turbo-Brayton)," NTRS 20170009150. https://ntrs.nasa.gov/api/citations/20170009150/downloads/20170009150.pdf
11. Plachta, D. et al., "NASA cryocooler technology developments and goals to achieve zero boil-off and liquefy cryogenic propellants," *Cryogenics* / NTRS 20180004709.
12. Shahid, M., Chambers, B., Sankarasubramanian, S., "Methane and oxygen from energy-efficient, low-temperature ISRU," *AIChE Journal* 69(5):e18010 (2023) (MOXIE power-normalized benchmarks). https://arxiv.org/pdf/2404.00800
13. "Thermodynamic modeling of in-situ rocket propellant fabrication on Mars," *iScience* 25:104323 (2022) (liquefaction/cryocooler assumptions). https://www.sciencedirect.com/science/article/pii/S2589004222005946
14. "Evaluation of low-pressure mechanical compression for Martian atmospheric CO2 capture," *Acta Astronautica* (2024). https://www.sciencedirect.com/science/article/abs/pii/S0094576524007690
15. OxEon Energy, Mars SOXE scale-up project page (company statements on manned-mission-scale stacks).
16. SpaceX, Starship vehicle page & presentations (propellant load, Raptor O:F) — **company claims**. https://www.spacex.com/vehicles/starship
17. Zubrin, R., *The Case for Mars* (Mars Direct: 6 t H2 → ~107 t methalox, 18:1 leverage); Zubrin et al. Sabatier/RWGS ISPP demonstrations.
18. NASA, "Zero Boil-Off Methods for Large Scale Liquid Hydrogen," NTRS 20170006481 (LH2 boiloff rates).
