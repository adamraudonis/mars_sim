# Solar Power on the Mars Surface — Parameter Research

**Domain key:** `power_solar`
**Date:** 2026-07-05
**Purpose:** Provide everything needed to simulate Mars-surface photovoltaic power at high fidelity (sun angle + optical depth + dust deposition) and to distill to a single "kWh/sol per kW installed" scalar for system-level trade studies.

Conventions: SI units, energy in kWh, time in sols (1 sol = 88,775 s = 24.6597 h; 1 Mars year = 668.6 sols). Areocentric longitude Ls in degrees (Ls = 0° northern spring equinox, 90° N summer solstice, 180° N autumn equinox, 270° N winter solstice; perihelion at Ls ≈ 251°).

---

## 1. Solar irradiance at the top of the Mars atmosphere

Primary source: Appelbaum & Flood, *Solar Radiation on Mars*, NASA TM-102299 (1989), and the revision *Solar Radiation on Mars — Update 1990*, NASA TM-103623. Equations below were extracted directly from the TM-102299 text (equation numbers preserved).

The beam irradiance at the top of the Mars atmosphere is

```
Gob = S / r²                                  (1)
r   = a (1 − e²) / (1 + e cos θ)              (2)
θ   = Ls − 248°   (true anomaly)              (3)
```

with S = 1371 W/m² (solar constant at 1 AU as used by A&F), a = 1.5236915 AU, e = 0.093377. A&F evaluate the mean beam irradiance at Mars as 1371/1.5236915² = **590 W/m²**, giving

```
Gob(Ls) = 590 · [1 + e·cos(Ls − 248°)]² / (1 − e²)²      [W/m²]       (4)
```

- Perihelion (Ls = 248–251°): **Gob = 718 W/m²**
- Aphelion (Ls = 68–71°): **Gob = 493 W/m²**
- Swing: ±19% about the mean, a factor 1.45 peri/aphelion — this is the dominant *deterministic* seasonal driver.

**Source disagreement.** A&F (1989) used S = 1371 W/m²; the modern total solar irradiance is 1361 W/m² (SORCE/TIM scale), which gives a Mars mean of **586 W/m²** (peri 713, aph 490). The difference (0.7%) is negligible for trade studies; we adopt 586 W/m² as the "truth" value and note that any implementation of the A&F lookup tables self-consistently uses 590. Likewise A&F used Ls,peri = 248° and obliquity 24.936°; modern values are **Ls,peri = 251°** and **obliquity 25.19°** (NSSDC Mars Fact Sheet). Use the modern values in new code; the error from the 1989 values is < 1% in daily insolation.

### 1.1 Solar geometry

```
cos z = sin φ sin δ + cos φ cos δ cos ω        (6)   z = zenith angle, φ = latitude
sin δ = sin δ0 · sin Ls,   δ0 = 24.936° (A&F; modern 25.19°)   (7)
ω = 15°·T − 180°           (T = Mars solar hour, sol divided into 24 "Mars hours")   (8)
ω_ss = cos⁻¹(−tan φ tan δ)  (sunset hour angle)  (9)
T_d = (2/15°)·cos⁻¹(−tan φ tan δ)  [Mars hours of daylight; ×(24.6597/24) for real hours]  (10)
```

Daily beam insolation on a horizontal surface at the top of the atmosphere:

```
H_obh = (24/π)·Gob·[ (π ω_ss/180°)·sin φ sin δ + cos φ cos δ sin ω_ss ]   [Wh/m²-sol, Mars-hour basis]  (13)
```

(multiply by 24.6597/24 to get terrestrial watt-hours per sol).

---

## 2. Surface irradiance: the Appelbaum & Flood model

Three components on a horizontal surface: `Gh = Gbh + Gdh` (16).

**Direct beam** (Beer's law through optical depth τ):

```
Gb  = Gob · exp(−τ · m(z)),  m(z) ≈ 1/cos z        (14, 15)
Gbh = Gob · cos z · exp(−τ / cos z)                 (18)
```

**Global horizontal** via the *normalized net flux function* f(z, τ, al), derived from Pollack's multiple-wavelength, multiple-scattering radiative transfer calculations:

```
Gh = Gob · cos z · f(z, τ, al) / (1 − al)           (17)
```

where `al` is surface albedo. TM-102299 tabulates f for al = 0.1 (hence the 0.9 divisor in the 1989 paper); TM-103623 (Update 1990) re-derives the table and adds al = 0.4 (Tables II(a),(b)), with linear interpolation valid between. **Diffuse is obtained by difference:** `Gdh = Gh − Gbh`.

### 2.1 Normalized net flux function f(z, τ), albedo = 0.1 (TM-103623, Table I — condensed)

| τ \ z | 0° | 20° | 40° | 50° | 60° | 70° | 80° | 85° |
|-------|------|------|------|------|------|------|------|------|
| 0.10 | .883 | .881 | .875 | .868 | .855 | .830 | .757 | .640 |
| 0.30 | .848 | .842 | .826 | .806 | .773 | .712 | .571 | .433 |
| 0.40 | .830 | .823 | .801 | .776 | .736 | .663 | .510 | .385 |
| 0.50 | .813 | .804 | .778 | .748 | .701 | .619 | .462 | .351 |
| 0.75 | .770 | .757 | .721 | .682 | .623 | .530 | .379 | .297 |
| 1.00 | .728 | .712 | .669 | .623 | .557 | .459 | .324 | .261 |
| 1.50 | .649 | .630 | .576 | .524 | .455 | .364 | .260 | .215 |
| 2.00 | .578 | .557 | .498 | .445 | .377 | .298 | .217 | .182 |
| 3.00 | .457 | .436 | .376 | .329 | .273 | .216 | .162 | .137 |
| 4.00 | .363 | .344 | .290 | .251 | .208 | .166 | .127 | .107 |
| 5.00 | .289 | .273 | .227 | .196 | .162 | .131 | .101 | .086 |
| 6.00 | .229 | .215 | .178 | .154 | .128 | .104 | .081 | .069 |

(Full 62 × 18 table available in TM-103623, pp. 10–11; the paper also provides a 6×6×2 polynomial fit `f(z,τ,al) = Σ p(i,j,k)·τ^i·(z/100)^j·al^k·(1−al)`, Table III, mean error ≈ 0.7%, worst 7% at z ≥ 80°, τ > 5. For a simulator, bilinear interpolation in the lookup table is simpler and more robust. For τ > 6 — global storms — extrapolate log-linearly in τ: f roughly ∝ exp(−0.19·τ) at z = 0 over τ = 4–6.)

Key physics: **the dusty Mars sky is strongly forward-scattering, so global irradiance degrades far more slowly with τ than the direct beam.** At noon, τ = 0.4: beam 395 W/m², global 544 W/m² (27% diffuse — matches Landis's "30% indirect at τ = 0.4"). At τ = 1.0: beam 217, global 477 (55% diffuse). Even during a τ > 4 storm, surface insolation falls only by a factor of 2–3, not by e^−τ (Lemmon et al. 2015, MER record).

### 2.2 Validation values (primary tables)

Daily global insolation on a horizontal surface, from TM-103623 Table V (Viking-year τ history, albedo 0.1):

| Site | Best sol (clear season) | Worst sol (winter and/or 1977 storms) |
|------|------------------------|----------------------------------------|
| VL1 (22.3°N) | 4181 Wh/m²-sol (Ls = 140°, τ ≈ 0.35) | 935 Wh/m²-sol (Ls = 290°, τ ≈ 3–4) |
| VL2 (47.7°N) | 4210 Wh/m²-sol (Ls = 120°) | 315 Wh/m²-sol (Ls = 280°, winter + storm) |

Our independent integration of the model (equator, equinox, Gob = 590 W/m²) reproduces the table and gives the τ-scaling used throughout this domain:

| τ | H (kWh/m²/sol) | noon Gh (W/m²) | H / 590 (equiv. hours) |
|------|------|-----|------|
| 0.4 | 4.05 | 544 | 6.9 |
| 1.0 | 3.37 | 477 | 5.7 |
| 3.0 | 1.96 | 300 | 3.3 |
| 5.0 | 1.21 | 189 | 2.0 |
| 6.0 | 0.95 | 150 | 1.6 |

---

## 3. Optical depth (τ) climatology

Flight data (visible-wavelength column dust optical depth; 880 nm values from solar imaging are the standard reference):

- **Clear season** (aphelion half of the year, Ls ≈ 0–135°): τ ≈ **0.3–0.6** at low latitudes. Spirit fell from 0.9 (landing, post-storm) to < 0.3 by Ls = 45°; Opportunity to < 0.5; MSL fall/winter is "relatively lower and more stable" (Lemmon et al. 2015; Lemmon et al. 2024 MSL record). Landis notes MER-site τ as low as 0.2. Planetary background average ≈ 0.48 (ICES-2019 GDS review).
- **Dusty season** (perihelion half, Ls ≈ 180–360°): background τ ≈ **0.7–1.2** with regional storms to τ ≈ 1.2–3+ lasting days–weeks (MER B/329 regional storm τ = 1.24; Viking VL1 saw 2.7–3.6 during the 1977 a/b storms).
- **Global (planet-encircling) dust events (GDS/PEDE):** peak τ = **8.5** measured by MSL Mastcam and **> 10.7** (lower limit, reported 10.8) by Opportunity during the June 2018 (MY34) event — the highest ever measured on the surface. Onset: local storm at Ls = 185° grew to encircle the planet by Ls = 197° (≈ 2–3 weeks from first sighting to global). Decay: exponential with **e-folding time 43 sols**; τ and temperatures back to seasonal norms by Ls = 250° (≈ 100 sols total enhanced-τ duration at Gale). Surface UV flux at MSL dropped 97% at peak.
- **Frequency:** 3 GDS in Mars years 25, 28, 34 (1 per ~3.3 MY average, i.e. probability ≈ 0.3 per Mars year), but aperiodic — the MY28→MY34 gap was 6 Mars years. All historical GDS initiate in the Ls ≈ 185–310° window. Local storms: ~100 per Mars year planet-wide, opacity ~1, a few days each (TM-102299 summary of Viking observations).
- **Design case used by NASA HEOMD studies:** dust storm of **τ = 5 lasting 120 sols** (Rucker et al., Solar vs. Fission Surface Power for Mars, 2016).

**Disagreement/nuance:** τ is wavelength- and technique-dependent (± ~0.1 between imaging bands); MSL's 8.5 vs Opportunity's 10.8 in the same storm partly reflects geography (Gale was at the storm edge), partly instrument saturation — treat "GDS peak τ" as 8–11 site-dependent.

---

## 4. Dust deposition on arrays and cleaning events

Define the **dust factor** DF = (array short-circuit current with dust)/(clean array), so P = DF · P_clean.

Flight data:

| Mission | Deposition rate | Notes |
|---------|----------------|-------|
| Pathfinder (1997) | **0.28 %/sol** | first 30 sols (Landis & Jenkins; quoted in NASA/TM-2004-213367) |
| MER Spirit/Opportunity | **~0.14 %/sol** long-term | "about half" the Pathfinder rate between cleaning events (TM-2004-213367); punctuated by wind-driven cleaning events |
| InSight (2018–2022) | **0.2–0.28 %/sol** | 0.2%/sol observed over first 800 sols; steady-state accumulation model 0.28%/sol reproduces the power history (Lorenz et al. 2020/2021) |
| All landers (review) | **0.05–2 %/sol** envelope | rate varies with season, site, storm activity (Lorenz et al. 2021, P&SS) |

Deposition surges by an order of magnitude during/immediately after dust storms (fallout), and vertical or steeply tilted panels accumulate ~10× less (Landis polar-rover study used 0.028 %/sol for a near-vertical array).

**Cleaning events** (wind gusts / dust-devil vortices removing dust) are the wild card:
- MER: frequent enough at both sites to keep the rovers alive 6+ and 14+ years; a 2014 series boosted Opportunity's output by ~70%, restoring the arrays to near-clean (DF ≈ 0.9+). Seasonal — clustered in the local "windy season."
- InSight (Elysium Planitia): essentially **no** significant natural cleaning in 4 years (one 0.7% vortex-related uptick, sol 65); DF fell monotonically to ≈ 0.1 (energy 5.0 → 0.5 kWh/sol) and ended the mission.
- Conclusion for modeling: cleaning-event frequency is **site-specific and must be treated as a (possibly zero-rate) Poisson process**, not a reliable maintenance mechanism. For crewed/settlement systems, assume **active cleaning** (brushing/electrodynamic dust shield) restores DF to 0.95–1.0 at some labor/energy cost, and use the deposition rate as the between-cleanings decay.

Model form: `DF(n+1) = DF(n) · (1 − r_dep(τ, season))` with r_dep ≈ 0.0028/sol nominal; on a cleaning event (Poisson rate λ_clean, site parameter 0–6 per Mars year), DF ← max(DF, DF_restore ≈ 0.85–0.95).

---

## 5. PV cell and array performance

- **Cell efficiency (BOL, AM0, 28°C):** flight-standard III-V triple-junction: Spectrolab **XTJ Prime 30.7%** (production average; EOL 26.7% at 1E15 1-MeV e⁻/cm², radiation degradation mostly irrelevant under Mars's ~20 g/cm² atmospheric shielding), SolAero ZTJ 29.5%; MER-era GaInP/GaAs/Ge ≈ 27.5%. These are manufacturer datasheet values (company claims, 2016-era, but flight-proven). Landis (NTRS 20070010752) notes 3J bandgaps can be adapted to the red-shifted Mars surface spectrum for ≈ 32%.
- **Temperature coefficients (XTJ Prime datasheet, 15–75°C):** ΔVmp/ΔT = −6.5 mV/°C (on Vmp = 2.390 V), ΔJmp/ΔT = +8.9 µA/cm²/°C (on 17.4 mA/cm²) → **relative power coefficient ≈ −0.22 %/°C** (−0.0022/K). Mars arrays run cold: daytime cell temperatures roughly −50 to +10°C (Landis polar study: −15°C peak day, −127°C night; equatorial noon warmer). Operating at −20°C instead of the 28°C rating point gives ≈ **+11% relative efficiency**. LILT effects at Mars intensity (0.43 AM0) are minor for modern 3J cells.
- **Spectral effect of airborne dust:** dust preferentially absorbs blue; the top (GaInP) junction of a 3J stack is current-limiting under dusty spectra. Net effect ranges from ≈ −10% (high τ) to slightly positive for red-rich-optimized cells; treat as a multiplicative factor 0.88–1.02, nominal 0.95 at τ ≈ 1 (Landis, low confidence — proper treatment needs the spectral model in NTRS 20070010752).
- **Solar absorptance / emittance** (for cell temperature modeling): α = 0.88, ε = 0.85 (XTJ Prime datasheet).

### Array mass (Mars surface, including structure & deployment)

| Source | Configuration | Number |
|--------|--------------|--------|
| Compact Telescoping Surface Array, NASA LaRC (NTRS 20190000437, 2019) | 1000 m² deployable, lander-mounted, 6 wings | **≈ 1500 kg total (1.5 kg/m² incl. all mechanical + electrical); blanket 0.5–1.0 kg/m²; 50–80 kW daytime near equator clear skies; "200 W/kg at 1 AU"** → **33–53 W/kg at Mars noon**, ≈ 19–30 kg per daytime-peak kW |
| Rucker AIAA Space 2016 (NTRS 20160011275) | 4 landers, 12-m UltraFlex-class arrays each, Jezero | Power generation 1321 kg/lander; whole solar power system (gen + storage + structure + thermal) **11,713 kg for ≈ 35 kW day / 26.8 kW night crewed loads** (12,679 kg at Columbus Crater); energy storage 3168 kg |
| Rucker, Solar vs. Fission (NTRS 20160010550/20160002628) | storm-survivable demo system | ≈ 9,800 kg for 22 kW keep-alive through a 120-sol τ = 5 storm, 35 kW peak clear-sky; comparison point: 4 × 10 kWe Kilopower fission = 9,154 kg |
| Orbital ATK flight data | UltraFlex (Phoenix, InSight heritage), MegaFlex | 100–150 W/kg wing-level at 1 AU BOL (in-space); Mars-surface gravity + wind loads roughly halve this before structure |

Consistent distillation: **space-qualified deployable array hardware lands at ≈ 1.5–3 kg/m² system-level on Mars; ≈ 25 (range 19–30) kg per kW of noon-clear output; ≈ 300–450 kg per kW of *continuous* (day+night, storm-tolerant) power once storage is included.** Fission crossover: solar+storage beats Kilopower-class fission on mass only when storm keep-alive requirements are mild and latitude < ~30°.

---

## 6. Energy storage

- **Li-ion, flight state-of-practice (pack level): < 100 Wh/kg** (JPL D-101146, *Energy Storage Technologies for Future Planetary Science Missions*, 2017 — Yardney MER/Phoenix/MSL heritage packs). Development targets in the same report: 150–250 Wh/kg rechargeable, with "150 Wh/kg at < −40°C" as the planetary-surface goal.
- Modern (2020s) space-rated cells reach 240–270 Wh/kg at cell level; realistic near-term Mars surface **pack** values **130–180 Wh/kg** after packaging, thermal, and de-rating (medium confidence).
- **Round-trip efficiency:** Li-ion cell-level 94–97%; system-level (converter + line losses) **≈ 0.90–0.93** (Tesla Powerwall claims 92.5% DC round-trip — company claim; terrestrial literature 84–96%).
- **Depth of discharge:** limit to ≈ 70% for the ~2000+ cycle (3+ Mars year) life needed by a settlement (estimate from cycle-life curves; low confidence).
- **Regenerative fuel cells (H2/O2)** are the NASA reference for >100-hour storage (storm ride-through): effective specific energy 300–600 Wh/kg at multi-sol durations, round-trip efficiency only ~40–55%. JPL solar/storage reports target > 500 Wh/kg for human landers (speculative — no flight heritage).

**Sizing logic:** night at equator ≈ 12.3 h → night energy = P_night × 12.3 h; battery mass = night energy /(DOD × η_rt × e_pack). Storm ride-through: either (a) array oversizing so that τ = 5 output (≈ 30% of clear-sky daily energy) still covers keep-alive, plus 1-sol batteries, or (b) multi-sol RFC/battery bank — Rucker's 120-sol τ = 5 case drove the 9.8 t solar system. A useful design identity from §2.2: **daily energy ratio storm/clear ≈ 0.30 at τ = 5, 0.24 at τ = 6** (not e^−τ!).

---

## 7. Latitude effects

From TM-103623 Table IV/V and the geometry equations: equatorial sites see 12.32-h days year-round; poleward of ±49° there are winter sols with zero daylight (arctic night) and summer sols with midnight sun. Practical results (horizontal arrays, Viking-year τ):

- 22.3°N (VL1): annual range 0.94–4.18 kWh/m²/sol (worst sol = 22% of best).
- 47.7°N (VL2): annual range 0.32–4.21 kWh/m²/sol (worst = 7.5% of best) — winter + dust storm compound.
- The southern hemisphere has summer at perihelion (+45% flux) but that is also dust-storm season; the north trades milder dust for aphelion summers.
- NASA crewed-site studies restrict solar architectures to ≈ ±30° latitude; every degree poleward increases required array+storage mass ~2–4%/deg beyond 30° (Rucker Jezero → Columbus Crater: +8% system mass).

Tilted/tracking arrays: at low latitudes, horizontal is within ~10% of optimal annual energy because diffuse is a large fraction; equator-facing tilt ≈ latitude helps most in winter at mid-latitudes. (Appelbaum's follow-on TMs 106321/106700 cover inclined surfaces; use beam geometry + isotropic diffuse as a first approximation.)

---

## 8. Distilled headline numbers (the "kWh/sol per kW installed" bridge)

Define **installed kW** = η_cell · A · G_ref with G_ref = 590 W/m² (TOA-normal reference). Then per installed kW, a horizontal equatorial array delivers (multiply by DF × spectral factor × PMAD η ≈ 0.85–0.95):

| Condition | kWh/sol per installed kW |
|-----------|--------------------------|
| Clear season (τ = 0.4) | **6.9** |
| Dusty-season background (τ = 1.0) | 5.7 |
| Regional storm (τ = 3) | 3.3 |
| Global storm design case (τ = 5) | 2.0 |
| GDS peak (τ = 8.5, extrapolated f) | ≈ 1.0 |

Rule of thumb for L0: **a well-maintained equatorial solar farm averages ≈ 5.5–6 kWh/sol per installed kW over a storm-free Mars year, ≈ 5 with climatological dust, and must be designed for a 1-in-3-Mars-year event that cuts output to ≈ 15–30% of nominal for ~100 sols.**

Mass distillation: 1 kW *continuous* (24.66-h) load ⇒ ≈ 4.4 kW installed array (equator, τ-climatology, DF 0.9) ≈ 110–170 kg array + ≈ 190–320 kg storage/PMAD ⇒ **≈ 300–450 kg/kW continuous**, vs. Kilopower fission ≈ 230–260 kg/kW — consistent with Rucker's finding that the two architectures are within ~30% of each other at Expedition scale.

---

## 9. Fidelity tiers (recommended model forms)

### L0 — single scalar
`E_sol [kWh/sol] = P_installed [kW] × EPH × DF × η_sys`
with EPH (equivalent peak-hours per sol at the reference rating) = **5.5** (equator, climatological dust, no GDS), DF = 0.85 (maintained arrays), η_sys = 0.90 (PMAD). Apply a mission-risk case: multiply EPH by 0.3 for a 100-sol GDS with probability 0.3 per Mars year. Storage: battery kWh = 12.5 h × P_night / (0.7 × 0.92 × pack Wh/kg for mass).

### L1 — analytic daily/seasonal model
Per sol s (→ Ls via Kepler or lookup):
1. `Gob(Ls)` from eq. (4) (use e = 0.0934, Ls,peri = 251°).
2. Daylight and daily TOA insolation from eqs. (9), (10), (13) with δ(Ls) from eq. (7) (obliquity 25.19°).
3. Atmospheric transmission: `H_h = H_obh × T(τ)` where T(τ) is the equator-equinox ratio table from §2.2 (T = 0.86, 0.71, 0.41, 0.26, 0.20 at τ = 0.4, 1, 3, 5, 6 relative to TOA) — adequate within ±10% for |φ| ≤ 40°.
4. τ(Ls): piecewise climatology — 0.45 for Ls ∈ [0°,180°], 0.9 for Ls ∈ [180°,360°]; superpose stochastic regional storms (Poisson, dusty season, Δτ ~ +1–2, 20–40 sols) and GDS (Bernoulli p ≈ 0.3/MY, onset U(185°,310°), rise ~10 sols to τ_peak ~ N(9, 1), exponential decay 43 sols).
5. Dust factor ODE of §4 with site cleaning rate; PV output = η(T̄_day) × DF × H_h × A.

### L2 — physics-based sub-sol timestep (e.g., 1/24 sol)
Governing equations at each timestep t:
- Geometry: cos z(t) from eq. (6)–(8); refuse z > 90°.
- Direct beam: `Gb = Gob exp(−τ/cos z)` (14–15); global: `Gh = Gob cos z f(z,τ,al)/(1−al)` with **bilinear interpolation in the TM-103623 Table I lookup** (this file's §2.1, full table in source); diffuse `Gdh = Gh − Gbh` (16–18).
- Tilted array plane: `G_arr = Gb·cos(AOI) + Gdh·(1+cos β)/2 + al·Gh·(1−cos β)/2` (isotropic sky; β = tilt).
- Cell temperature: `C dT/dt = α G_arr − ε σ (T⁴ − T_sky⁴) − h_conv (T − T_air)` with α = 0.88, ε = 0.85 (both faces), h_conv ≈ 2 W/m²K in ~600 Pa CO2 with u ~ 5 m/s; or the simpler NOCT-style fit T_cell ≈ T_air + 0.02·G_arr.
- Power: `P = η_ref [1 − 0.0022 (T_cell − 301 K)] × S_spec(τ) × DF(t) × G_arr × A × η_PMAD`.
- τ(t): L1 storm process, optionally with the MSL diurnal τ modulation (±10%, morning peak in dusty season).
- DF(t): deposition each sol (0.28%/sol nominal, ×3–10 during storm decay 30 sols, ×0.1 for near-vertical surfaces), Poisson cleaning events, and scheduled crew/robotic cleaning.
- Storage dispatch: battery SOC integration with η_chg·η_dis = 0.92, DOD limit 0.7; RFC for multi-sol deficits.
- Validation targets: reproduce VL1/VL2 Table V (±5%), InSight 4.6 kWh/sol at landing (5.14 m², τ ≈ 0.7) decaying to ~0.5 kWh/sol by sol 1400, and MSL 97% UV drop at GDS peak.

---

## 10. Key disagreements & gaps

1. **Solar constant scale:** 590 (A&F, S=1371) vs 586 W/m² (modern TSI) — adopt 586, keep 590 inside A&F table implementations.
2. **f-table validity above τ = 6:** all A&F tables stop at 6; GDS peaks at 8.5–11 require extrapolation (log-linear in τ) — flag results in that regime as extrapolated.
3. **Dust deposition rates spread ×40** (0.05–2 %/sol) across sites/seasons; and cleaning-event frequency ranges from "every few hundred sols" (MER) to "never" (InSight). Site wind climate is the hidden variable; treat both as site parameters with the InSight case as conservative bound.
4. **Array specific mass:** CTSA's 1.5 kg/m² is a design goal (TRL ~4-5); Rucker's UltraFlex-derived 2.9 kg/m² has flight-adjacent pedigree. We carry 1.5 (range 1.2–2.9).
5. **Battery numbers:** flight-qualified pack heritage (<100 Wh/kg) badly lags commercial cells (270 Wh/kg); settlement-era assumption 130–180 Wh/kg is a projection, not flight data.
6. **GDS statistics:** only ~10 well-observed events in the spacecraft era; "1 per 3 Mars years" has wide confidence bounds (gaps of 1–6 MY observed).

---

## 11. Sources

1. Appelbaum, J. & Flood, D.J., *Solar Radiation on Mars*, NASA TM-102299, Aug 1989. https://ntrs.nasa.gov/citations/19890018252 (equations 1–18, Viking τ record, local-storm statistics; Tables I–VI)
2. Appelbaum, J. & Flood, D.J., *Solar Radiation on Mars — Update 1990*, NASA TM-103623, 1990. https://ntrs.nasa.gov/citations/19910005804 (revised f(z,τ,al) tables at al = 0.1/0.4, polynomial fit, daylight-hours table, VL1/VL2 daily insolation over full Mars year)
3. Appelbaum, J., et al., updates TM-105216 (1991), TM-106321 (1993, inclined surfaces), TM-106700 (1994) — inclined-surface & τ-refinement follow-ons.
4. Landis, G.A., et al., *Mars Solar Power*, NASA/TM-2004-213367 (also AIAA-2004-5555). https://ntrs.nasa.gov/citations/20040191326 (Pathfinder 0.28 %/sol, MER ≈ half; τ at MER landing 0.9→0.2; diffuse fraction; polar-rover array sizing table; cell temp figures)
5. Landis, G.A. & Hyatt, D., *The Solar Spectrum on the Martian Surface and its Effect on Photovoltaic Performance*, NTRS 20070010752 (spectral/red-shift effect on 3J cells).
6. Lemmon, M.T., et al., "Dust aerosol, clouds, and the atmospheric optical depth record over 5 Mars years of the MER mission," Icarus 251 (2015). NTRS 20150008268 (seasonal τ climatology, storm τ, factor 2–3 insolation reduction at τ > 4)
7. Lemmon, M.T., et al., "The MSL record of optical depth measurements via solar imaging," arXiv:2309.07378 / Icarus (2024) (Gale multi-MY τ record, diurnal τ variation).
8. Guzewich, S.D., et al., "MSL Observations of the 2018/Mars Year 34 Global Dust Storm," GRL 46 (2019), doi:10.1029/2018GL080839 (peak τ = 8.5 at Gale).
9. *The Mars Global Dust Storm of 2018*, ICES-2019 paper, NTRS 20190027303 (storm timeline Ls 185→250°, Opportunity τ > 10.7, decay constant 43 sols, GDS frequency MY 25/28/34, background τ ≈ 0.48, 97% UV reduction).
10. Lorenz, R.D., et al., "Scientific Observations with the InSight Solar Arrays: Dust, Clouds, and Eclipses on Mars," Earth & Space Science 7 (2020), doi:10.1029/2019EA000992 (4.6 kWh/sol initial, 5.14 m², 0.28 %/sol model, τ 0.7 baseline/1.9 storm peak, 1/(1+0.3τ) flux approximation).
11. Lorenz, R.D., et al., "Lander and rover histories of dust accumulation on and removal from solar arrays on Mars," Planet. Space Sci. 207 (2021) 105337 (0.05–2 %/sol envelope, cleaning-event statistics).
12. Rucker, M.A., *Surface Power for Mars* / *Solar vs. Fission Surface Power for Mars*, NTRS 20160014032, 20160002628, 20160010550; AIAA Space 2016 presentation NTRS 20160011275 (11,713 kg Jezero solar system, 9,154 kg 4×Kilopower, 9.8 t storm-survivable solar, τ = 5 × 120-sol design storm).
13. Warren, T., Mikulas, M., et al., *Compact Telescoping Surface Array for Mars Solar Power*, NTRS 20190000437 (1000 m², ~1500 kg, 50–80 kW day equatorial clear, blanket 0.5–1 kg/m²).
14. Spectrolab, *XTJ Prime 30.7% Triple Junction Space Grade Solar Cell* datasheet (7/2016) — company datasheet (efficiency, temp coefficients, α/ε).
15. JPL D-101146, *Energy Storage Technologies for Future Planetary Science Missions* (2017) (SOP Li-ion pack < 100 Wh/kg; targets 150–250 Wh/kg; RFC > 500 Wh/kg goal).
16. NASA NSSDC Mars Fact Sheet (orbital elements, obliquity 25.19°, Ls,peri ≈ 251°).
17. NASA JPL/news & mission reports: InSight end-of-mission power 5.0 → 0.5 kWh/sol (2022); MER cleaning-event press data (Spirit 2009, Opportunity 2014 +70%).
