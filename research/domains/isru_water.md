# Water ISRU on Mars — Ice Mining and Regolith Water

**Domain key:** `isru_water` · **Compiled:** July 2026 · **For:** Mars habitat/settlement trade-study simulation

**Conventions:** SI units; energy in kWh (1 kWh = 3.6 MJ); 1 sol = 88,775 s = 24.6597 h; 1 Mars year = 668.6 sols. "Thermal" energy is heat delivered to the process (can come from reactor waste heat); "electric" is bus power.

---

## 1. The resource base

### 1.1 Subsurface ice (the preferred feedstock)

The Subsurface Water Ice Mapping (SWIM) project (Putzig, Morgan et al., NASA-funded, Planetary Science Institute) integrated neutron spectroscopy, thermal imaging, radar (SHARAD), and geomorphology into ~3 km/pixel "ice consistency" maps over depth ranges 0–1 m, 1–5 m, and >5 m. Key results:

- **Where:** The strongest shallow-ice signatures (consistency > 0.5 in the 0–1 m band) occur across **Arcadia Planitia and northern Utopia Planitia** (roughly 35–50° N), at low elevations compatible with EDL. The refined SWIM maps (Morgan et al. 2025, *Planet. Sci. J.* 6:37, doi:10.3847/PSJ/ad9b24) find ice excursions **equatorward of 30° N/S** in places, and emphasize that the shallowest ground ice is highly spatially variable at sub-km scale.
- **How deep:** In favorable Arcadia sites the ice table is **decimeters below the surface** (expanded secondary craters and thermokarst indicate tens of cm); Dundas et al. (2018, *Science* 359:199, doi:10.1126/science.aao1619) observed eight erosional scarps where massive ice begins **1–2 m below the surface** and extends **>100 m** down.
- **How much / how pure:** Bramson et al. (2015, *GRL* 42:6566, doi:10.1002/2015GL064844) combined SHARAD returns with crater morphology in Arcadia: a widespread excess-ice layer of **~10^4 km³**, depth-to-base mode **42 m** (mean 51 ± 18 m), bulk dielectric constant **ε′ ≈ 2.5** — consistent with a layer that is **mostly clean water ice** (pure ice ε′ ≈ 3.1; the low value implies porous, low-lithic ice). Dundas et al. scarps show massive, layered, relatively dust-poor ice (bluish in HiRISE color, sublimating actively). The M-WIP study's ice reference case assumes **90% ice / 10% sand** as a working purity. Treat purity 0.5–0.99, nominal 0.9.

**Simulation implication:** at a good Arcadia-class site, the model can assume an effectively unlimited (multi-decade) ice reservoir; the binding constraints are *overburden removal or drilling depth (0.2–2 m to top of ice, more conservatively up to 5–10 m)*, *energy per kg*, and *well/plant duty cycle* — not resource exhaustion.

### 1.2 Regolith bound water (the fallback available "anywhere")

From M-WIP (Abbud-Madrid et al. 2016, *Mars Water In-Situ Resource Utilization (ISRU) Planning (M-WIP) Study*, 90 pp.):

| Feedstock (M-WIP case) | Recoverable water (bulk wt%) | Process temp | Ore per 16 t H2O |
|---|---|---|---|
| A: Massive ice | ~90 wt% of excavated ice | 273 K (melt) | ~19 t |
| B: Gypsum-enriched regolith (40% gypsum) | **8.6 wt%** | ~425 K | 186 t |
| C: Smectite-enriched regolith (40% smectite) | **2.7 wt%** | ~575 K | 584 t |
| D1: Typical regolith @ 425 K | ~0.8 wt% | 425 K | 2,050 t |
| D2: Typical regolith @ 575 K | ~1.3 wt% | 575 K | 1,270 t |

Mineral-level numbers: gypsum (CaSO4·2H2O) holds **20.9 wt% water** (stoichiometric) and releases it at a benign 100–150 °C; smectites hold ~2–7 wt% (Na-smectite ~2, Ca-smectite ~7) but need ~300 °C; typical Gale-crater regolith holds 1.5–3 wt% total (MSL DAN/SAM), only ~1.3 wt% recoverable below 300 °C (Kleinhenz & Paz 2017). Phoenix and Curiosity results suggest hydrated phases are globally common, so the regolith route works at almost any latitude — it just costs 3–20× more energy and ~10–100× more excavated mass per kg of water than ice (Section 3).

### 1.3 Contaminants

Phoenix WCL measured **0.4–0.6 wt% perchlorate** (mostly Mg/Ca/Na perchlorate) in soil (Hecht et al. 2009, *Science* 325:64). Chlorates likely comparable. Massive glacial ice should be much cleaner than soil, but melt water from any route must be assumed to carry dust, perchlorates/chlorates and dissolved salts, and must be cleaned before electrolysis (membranes/deionizers are perchlorate- and particulate-sensitive) or human use (perchlorate is a thyroid toxin at ppb–ppm levels).

---

## 2. Getting at it: excavation and drilling hardware anchors

### 2.1 RASSOR-class excavator (granular regolith / disaggregated icy soil)

RASSOR 2.0 (NASA KSC; Mueller et al., ASCE Earth & Space 2016, NTRS 20210011366) is the standard reference excavator in NASA Mars ISRU sizing studies (M-WIP, Kleinhenz & Paz):

- Mass **66 kg**; counter-rotating bucket drums (net-zero excavation reaction force, works in low g)
- Payload **80 kg** regolith per trip (2 × 40 kg drums)
- Excavation rate **≥ 2.7 t/day** (≈ 2,770 kg/sol), 0.38 kg vehicle mass per (kg/h) of excavation rate
- Excavation power **4 W per (kg/h)** → specific excavation energy **≈ 0.004 kWh per kg regolith**
- Traverse speed **0.25 m/s**; battery-powered, ~60% on-duty / 40% recharge assumed in M-WIP
- Demonstrated cut depth ~5 cm per pass (M-WIP areal-mining assumption)

Excavation energy is thus *negligible* per kg of ore (4 Wh/kg); what matters is the **water yield of the ore**: at 8.6 wt% yield, excavation costs ~0.05 kWh per kg water; at 1.3 wt%, ~0.31 kWh/kg water — and fleet size/traffic scales the same way (M-WIP: 1 excavator suffices for gypsum ore at ≤3 km haul; typical regolith needs 2–3 excavators even at 100 m).

### 2.2 TRIDENT drill (Honeybee Robotics) — access through overburden, prospecting

- Mass **< 20 kg**, 1-m-class rotary-percussive drill; rotary and percussive actuators **200 W each**; max weight-on-bit 200 N
- "Bite" sampling in 10 cm increments (2 cm bites in hard/low-permeability material)
- Field performance (Glass et al. 2024, LPSC, NTRS 20240000585): 7.8 m of borehole in 6 days at Haughton Crater; in massive ice-cemented material as slow as **7 cm in 27 min** (~0.16 m/h). Loose regolith drills ~10× faster.
- Implied drilling energy: at 100–400 W average and 0.15–2 m/h → **~0.05–2 kWh per meter** of hole (soft soil → ice-cemented ground). Small compared to extraction energy, but drives *time* and *reliability* budgets.

RedWater (Honeybee; Mellerowicz et al. 2022, *New Space*, doi:10.1089/space.2021.0057) packages a coiled-tubing drill with a Rodwell: drill through **up to ~20–25 m** of overburden/ice, then use the coiled tubing as the water conduit. TRL ~6 (full-scale demos in ice-cemented simulant chambers). This is the reference architecture for "ice sheet at depth" scenarios.

### 2.3 Rodriguez well (Rodwell) — the mature bulk-water technique

Terrestrial heritage (NASA/TP-20205011353, *Mars Rodwell Experiment Final Report*, 2020): Camp Century Greenland (1960s, ~300 kW nuclear waste heat), old South Pole well 1972–73 (**416 t** delivered before equipment failure), the South Pole Water Well since 1995 (**~2,005 t/yr** average station supply, wells last ~a decade each, abandoned when pump lift exceeds ~150 m), and IceCube construction Rodwells (**~1,140 t/season each; ~8,000 t total**). NASA judges the technique TRL 6 for Mars.

Mars-specific simulations (CRREL model adapted by Hoffman et al.; "Mining Water Ice on Mars" assessment, NASA JSC 2015/2016; AIAA 2020, NTRS 20205007716; quoted in TP-20205011353):

- Production **120–400 L/day at 2–10 kW thermal** input, for ice at Viking-2-like −80 °C; final well cavities 5–10 m diameter, 2.5–5 m tall
- At a 100 gal/day (379 kg/day) withdrawal rate, **~10 kW** thermal balances melt and withdrawal; below that the well shrinks and eventually "collapses" (refreezes); above it the pool grows (wasted heat)
- Implied steady-state specific energy: **0.4–0.7 kWh(th)/kg** (10 kW × 24 h / 379 kg ≈ 0.63 kWh/kg)
- Wells can be deliberately frozen down and restarted (demonstrated at South Pole and Camp Century); restart cost ≈ initial startup cost
- Mars complications: pool surface is near the triple point — the borehole must be sealed/pressurized (e.g., with CO2) or evaporative losses and boiling instability occur. JSC bell-jar experiments (TP-20205011353) found pool-surface heat/mass transfer follows Nu, Sh ~ Ra^(1/3), i.e., evaporation is a first-order loss term at low pressure, not a correction.

---

## 3. Energy to extract water: theory and practice

### 3.1 Governing thermodynamics

Per kg of **water recovered**, with ore water mass-fraction *y* (0–1), thermal-capture efficiency η, soil specific heat c_reg ≈ 0.8 kJ/(kg·K), ice c_ice ≈ 1.6–2.1 kJ/(kg·K):

```
E_thermal = [ ((1−y)/y)·c_reg·ΔT_matrix  +  c_ice·ΔT_ice  +  ΔH_release ] / (3600·η)   [kWh/kg]

ΔH_release = 334 kJ/kg   (melting — Rodwell, enclosed melt reactors)
           = 2,838 kJ/kg (sublimation/evaporation — open or vapor-transport systems)
           ≈ 2,700–3,300 kJ/kg (mineral dehydration + vaporization — gypsum/smectite routes)
```

Reference evaluations (η = 1):

| Route | y | ΔT | ΔH path | Theory E (kWh/kg H2O) |
|---|---|---|---|---|
| Melt −80 °C massive ice (Rodwell) | 0.90 | 80 K | melt | **0.135** (NASA JSC chart: ~140 kWh/t) |
| Melt −20 °C ice | 0.90 | 20 K | melt | 0.10 |
| Sublime pore ice, 30 wt% icy regolith heated ~150 K | 0.30 | 150 K | sublimation | **0.89** |
| Sublime pore ice, 20 wt% | 0.20 | 150 K | sublimation | 1.06 |
| Gypsum ore, 8.6 wt%, to 425 K | 0.086 | 130 K | dehydration | ~1.15 |
| Smectite ore, 2.7 wt%, to 575 K | 0.027 | 280 K | dehydration | ~3.1 |
| Typical regolith, 1.3 wt%, to 575 K | 0.013 | 280 K | dehydration | ~5.7 |

The often-quoted **0.6–1.1 kWh/kg "icy regolith" band** is the sublimation/vapor-transport route through 20–40 wt% ice-cemented ground, including modest matrix heating — the practical NASA study numbers below bracket it.

### 3.2 Practical numbers from NASA studies

- **Rodwell (massive ice):** 0.4–0.7 kWh(th)/kg steady state (2–10 kW → 120–400 L/day, CRREL/Hoffman). Add pumping (~150 m lift on Mars: mgh ≈ 0.0002 kWh/kg — negligible electrically) and borehole/startup amortization. Heat can be reactor waste heat, so the *electric* burden can be far below the thermal number.
- **M-WIP soil-processing power for 33 kg H2O/sol (16 t over 480 sols):** Case B gypsum ~2 kW → **1.5 kWh/kg**; Case C smectite ~5 kW → **3.7 kWh/kg**; Case D2 typical regolith ~8 kW → **5.9 kWh/kg** (processing only, excluding excavation and propellant plant).
- **Kleinhenz & Paz (AIAA 2017-0423, NTRS 20170001421), end-to-end MAV ISRU sizing:** water demand 15,701 kg in 480 days (1.36 kg/h) from 1.3 wt% regolith (68.2 kg/h soil at 2% design point; 785 t total). Regolith-heating thermal load **17 kW → 12.5 kWh(th)/kg water**; whole system (excavation + soil processing + atmosphere + electrolysis + liquefaction) 1.7 t and 52 kW. With 8.6 wt% gypsum ore instead: −7% mass, **−27% power**. They note the 17 kW is heat and could be recuperated from a fission power system.
- **Kleinhenz (NTRS 20180005542):** NASA ISRU project baseline 1.5 kg/h water → 0.67 kg/h CH4 + 2.68 kg/h O2; 434-day, 24 h/sol campaign; hardware options auger dryer, microwave, open-air dryer (hydrated minerals) and Rodwell (ice). Early open-air breadboard (Linne & Kleinhenz, NTRS 20160010258) captured only **~17% of heater energy into the soil** — immature capture efficiency is the big gap between theory and lab practice (the 2024–25 LUWEX lunar demo was even worse, ~44 kWh/kg, in cryo-vacuum conditions; Mars's 600 Pa CO2 atmosphere makes vapor capture much easier than lunar vacuum).

**Recommended trade-study scalars (thermal, at-plant):** ice/Rodwell **0.6 kWh/kg** (0.4–1.1); gypsum ore **1.5 kWh/kg** (1.2–2.5); smectite **3.7** (3–5); typical regolith **6–12.5**. Multiply by 1/η_capture ≈ 1.2–2.0 at low TRL.

### 3.3 Water cleanup

Melted Rodwell water ≈ dilute glacial melt (dust, ppm–% level salts incl. perchlorates); regolith-dryer condensate is dirtier (volatilized HCl/H2S species above 400 °C aggravate corrosion — a reason to prefer low-temperature gypsum ore). Treatment train: filtration → ion exchange (perchlorate-selective resins are terrestrial COTS) or biocatalytic perchlorate reduction → polish deionization; or vapor-compression distillation (robust to everything, ~0.1–0.2 kWh/kg with compression recovery). Budget **0.02–0.3 kWh/kg, nominal 0.1** (electric), confidence low — no integrated Mars-water treatment demo has published end-to-end energy. Perchlorate is also a *resource* (O2 release via catalysis/electrolysis of brines), but no baseline design credits it.

### 3.4 Hauling

Transport energy for ore/water over distance d: E ≈ C_rr·g_Mars·d/η_drive per kg hauled. With C_rr ≈ 0.15–0.25 (loose regolith), g = 3.71 m/s², η ≈ 0.5: **0.15–0.6 kWh per tonne-km, nominal 0.3** (estimate; consistent with M-WIP RASSOR traverse assumptions, 0.25 m/s, 80 kg loads). M-WIP found ore hauling practical to **~3 km** with 2 RASSOR-class excavators (gypsum case, 480-sol campaign); typical-regolith cases are already excavator-limited at 100 m. Rule: put the plant at the mine; haul water (10×+ less mass than low-grade ore), or pump it.

---

## 4. Demand anchor: one Starship reload

Sabatier + electrolysis net stoichiometry (all H from water, all C from atmospheric CO2):

```
electrolysis: 2 H2O → 2 H2 + O2       Sabatier: CO2 + 4 H2 → CH4 + 2 H2O (recycled)
net:          2 H2O + CO2 → CH4 + 2 O2
mass:         2.246 kg H2O per kg CH4, co-producing 3.99 kg O2 per kg CH4
```

SpaceX figures (company claims, 2016–2025 presentations/website): Starship (ship) propellant load **~1,200 t ≈ 267 t CH4 + 933 t O2** (O/F ≈ 3.5–3.8). Water to make the methane: 267 × 2.246 ≈ **600 t H2O**; the co-produced O2 (267 × 3.99 ≈ 1,065 t) already exceeds the 933 t needed, with ~130 t surplus for life support/losses. **So ≈ 600 t of water (500–660 t) fully fuels one Starship** — no separate water for oxygen is required in an all-Sabatier plant.

Scale check: 600 t over one synodic period (~640 sols of production) = **~940 kg/sol ≈ 38 kg/h** — 28× the Kleinhenz & Paz MAV study rate, ~2.4× the *entire* South Pole station's annual draw, but only ~30% of one IceCube seasonal Rodwell. At Rodwell 0.6 kWh(th)/kg this is **~360 MWh(th) per synodic period ≈ 24 kW(th) continuous** — small next to the ~2.5–5 GWh(e) the electrolysis/liquefaction plant needs for the same reload. **Water extraction is not the energy driver of propellant ISRU; it is the logistics/reliability driver.** Cross-checks: Kleinhenz & Paz need 15.7 t water for a 7 t-CH4 MAV (ratio 2.25 ✓); M-WIP 16 t water per MAV ✓. Crew use is trivial by comparison (6–13.3 kg/person/day, Hoffman assessment; ~67 t per 4-crew/500-sol mission incl. margins).

---

## 5. Disagreements and open issues between sources

1. **Typical-regolith processing energy:** M-WIP says ~5.9 kWh/kg (D2); Kleinhenz & Paz say 12.5 kWh/kg thermal for nearly the same ore. Differences: yield (1.26 vs 1.3 wt%), heat recuperation assumptions, batch vs continuous dryers. Carry the range; don't average.
2. **Ice purity:** Dundas scarps and Bramson radar support "mostly clean ice" but neither yields a tight purity number; SWIM consistency values are *not* concentrations. M-WIP's 90% is an assumption. Excess-ice fraction 50–99% spans the credible range; purity mainly affects Rodwell sump sediment management, not energy.
3. **Depth to ice:** SWIM 0–1 m band positives vs Dundas 1–2 m scarp burial vs "several m" conservative engineering assumptions (RedWater designs to 20+ m). Site-specific; treat as a site parameter, not a constant.
4. **Lab capture efficiency vs theory:** Linne & Kleinhenz breadboard 17% soil-heating capture; LUWEX ~1–2% overall — versus 60–90% assumed in system studies. TRL risk sits here, not in the thermodynamics.
5. **Rodwell numbers assume −80 °C ice** (Viking 2 analog). Mid-latitude ice at 1–5 m depth averages ~210–230 K; warmer ice cuts sensible-heat cost ~20–40% but raises stability/collapse questions in dirty ice horizons. No Mars-environment full-scale Rodwell demo exists (JSC tests were sub-scale bell-jar).
6. **SpaceX figures are company claims** (payload user's guide-level, not peer-reviewed); O/F ratio is inferred (3.5–3.8). Propellant load has drifted 1,100–1,500 t across Starship versions (V1→V3); parameterize per-ship reload rather than hard-coding.

---

## 6. Fidelity tiers for the simulation

### L0 — single scalar (campaign-level trades)

```
water_rate [kg/sol] = P_thermal_alloc [kW] × 24.66 / e_specific [kWh/kg]
e_specific = 0.6 (Rodwell/ice) | 1.5 (gypsum) | 3.7 (smectite) | 6–12.5 (typical regolith)
+ 0.1 kWh(e)/kg cleanup; + 0.004/y kWh(e)/kg excavation (regolith routes); + 0.3 kWh/t·km hauling
```

Apply availability factor 0.6–0.8 (duty cycle, dust storms, maintenance). Starship anchor: 600 t/reload.

### L1 — analytic daily/seasonal model

- Ice temperature T_ice(L_s, latitude, depth) from an annual-wave model: T(z) = T_mean + ΔT·exp(−z/δ)·sin(2πt/T_yr − z/δ), skin depth δ ≈ 1–2 m in icy soil → sensible-heat term and dryer feed temperature vary with season/site.
- Rodwell as a two-state system: startup phase (drill E_drill·depth + establish pool: ~10–30 sols at full power before first water) then steady state with the withdrawal-balance constraint ṁ_max = η·P/(c_ice·ΔT + L_fus) and collapse if ṁ demanded > ṁ_max for > a few sols; well migrates downward ~5–10 m per 100–400 t withdrawn (pump lift and cable/hose length grow; abandon/re-drill at ~150 m equivalent).
- Regolith route: fleet model — N_excavators = ceil(ore_rate / (2,770 kg/sol × duty)); haul time per trip = 2d/v + load/dump overhead; battery-recharge duty 60%.
- Solar vs nuclear branch: thermal routes may draw reactor waste heat (credit e_specific × f_waste_heat against the electric bus).

### L2 — physics-based sub-sol model (governing equations)

**Rodwell (CRREL-type lumped pool + Mars vapor loss):**
```
m_pool·c_w·dT_pool/dt = P_in − Q_melt − Q_cond − Q_evap − Q_withdraw
Q_melt = h_i·A_ice·(T_pool − 273.15);  h_i from Nu = C·Ra^(1/3) (JSC: turbulent regime holds even at small scale)
Ra = gβΔT L³/(να);  melt mass flux ṁ_ice = Q_melt/(c_ice(273−T_ice) + L_fus) → dV_cavity/dt
Q_cond ≈ k_ice·A·∇T into an ice annulus ~4.5·r_pool (Lunardini & Rand 1995 thermal-disturbance radius)
Q_evap = h_m·A_surf·(ρ_v,sat(T_pool) − ρ_v,hole);  Sh = C′·Ra^(1/3); suppressed by borehole pressurization P_hole > P_triple (611 Pa)
```
Track pool geometry (cylinder/hemisphere), downward migration when overpumped, refreeze when P_in < Q_cond + Q_evap.

**Regolith dryer/auger (per batch or plug flow):**
```
ṁ_ore·[c_reg·(T_out − T_in) + y·(c_w·ΔT + ΔH_release(T))] = P_heater·η_capture − Q_loss
condenser: ṁ_v·L_cond recovered at T_cond; recuperator effectiveness ε on spent-ore heat (ε ≈ 0.3–0.7 is the main lever between the 6 and 12.5 kWh/kg study results)
```
Excavator: kinematics + battery (80 kg loads, 0.25 m/s, 4 W/(kg/h) dig power, recharge at plant), Monte-Carlo ore grade y ~ N(site mean, ±30% — M-WIP heterogeneity assumption).

**Sublimation-front extraction (thermal probes / open-pit icy regolith):** Stefan-problem with vapor diffusion through desiccated lag: front recession ẋ = q_net/(ρ_ice·f_ice·(L_sub + c_iceΔT)), vapor flux limited by Darcy/Knudsen diffusion through overburden — needed only if simulating in-situ (non-excavated) volatilization concepts.

---

## 7. Parameter table (machine-readable copy in `isru_water.json`)

| id (isru_water.) | value | unit | range | conf. |
|---|---|---|---|---|
| ice_burial_depth_arcadia_m | 0.6 | m | 0.2–2.0 | medium |
| ice_deposit_thickness_arcadia_m | 40 | m | 20–70 | medium |
| massive_ice_purity_frac | 0.9 | fraction | 0.5–0.99 | medium |
| gypsum_mineral_water_frac | 0.209 | fraction | — | high |
| gypsum_deposit_recoverable_water_frac | 0.086 | fraction | 0.03–0.09 | medium |
| smectite_deposit_recoverable_water_frac | 0.027 | fraction | 0.02–0.07 | medium |
| typical_regolith_water_frac | 0.015 | fraction | 0.013–0.03 | medium |
| regolith_bulk_density_kg_m3 | 1800 | kg/m³ | 1350–2200 | medium |
| perchlorate_soil_frac | 0.005 | fraction | 0.004–0.006 | high |
| rassor_mass_kg | 66 | kg | — | high |
| rassor_payload_kg | 80 | kg | — | high |
| rassor_excavation_rate_kg_per_sol | 2770 | kg/sol | 2770–5000 | high |
| excavation_specific_energy_kwh_per_kg | 0.004 | kWh/kg regolith | 0.003–0.008 | medium |
| rassor_traverse_speed_m_s | 0.25 | m/s | 0.2–0.5 | high |
| haul_distance_practical_max_m | 3000 | m | 100–3000 | medium |
| transport_specific_energy_kwh_per_t_km | 0.3 | kWh/(t·km) | 0.15–0.6 | low |
| melt_energy_theory_kwh_per_kg | 0.135 | kWh/kg H2O | 0.10–0.14 | high |
| sublimation_energy_theory_kwh_per_kg | 0.82 | kWh/kg H2O | 0.79–0.95 | high |
| icy_regolith_extraction_energy_kwh_per_kg | 0.85 | kWh(th)/kg H2O | 0.6–1.1 | medium |
| rodwell_production_rate_kg_per_sol | 390 | kg/sol | 120–410 | medium |
| rodwell_specific_thermal_energy_kwh_per_kg | 0.6 | kWh(th)/kg H2O | 0.4–0.7 | medium |
| rodwell_borehole_depth_m | 15 | m | 8–25 | medium |
| gypsum_route_specific_thermal_energy_kwh_per_kg | 1.5 | kWh(th)/kg H2O | 1.2–2.5 | medium |
| smectite_route_specific_thermal_energy_kwh_per_kg | 3.7 | kWh(th)/kg H2O | 3.0–5.0 | medium |
| typical_regolith_route_specific_thermal_energy_kwh_per_kg | 9.0 | kWh(th)/kg H2O | 5.9–12.5 | medium |
| trident_drill_mass_kg | 20 | kg | 15–25 | high |
| drilling_energy_kwh_per_m | 0.4 | kWh/m | 0.05–2.0 | low |
| water_cleanup_energy_kwh_per_kg | 0.1 | kWh(e)/kg H2O | 0.02–0.3 | low |
| water_per_kg_ch4_kg | 2.246 | kg H2O/kg CH4 | — | high |
| starship_reload_water_t | 600 | t H2O/reload | 500–660 | medium |

---

## 8. Sources

1. Morgan, G.A., Putzig, N.E., et al. (2021). "Availability of subsurface water-ice resources in the northern mid-latitudes of Mars." *Nature Astronomy* 5, 230–236. doi:10.1038/s41550-020-01290-z
2. Morgan, G.A., et al. (2025). "Refined Mapping of Subsurface Water Ice on Mars to Support Future Missions." *Planetary Science Journal* 6:37. doi:10.3847/PSJ/ad9b24 (SWIM 3/4; swim.psi.edu)
3. Bramson, A.M., et al. (2015). "Widespread excess ice in Arcadia Planitia, Mars." *GRL* 42, 6566–6574. doi:10.1002/2015GL064844
4. Dundas, C.M., et al. (2018). "Exposed subsurface ice sheets in the Martian mid-latitudes." *Science* 359, 199–201. doi:10.1126/science.aao1619
5. Abbud-Madrid, A., et al. (2016). *Mars Water In-Situ Resource Utilization (ISRU) Planning (M-WIP) Study*, 90 pp. https://mepag.jpl.nasa.gov/reports/Mars_Water_ISRU_Study.pdf
6. Hoffman, S.J., et al. (2016). *"Mining" Water Ice on Mars — An Assessment of ISRU Options in Support of Future Human Missions*. NASA JSC. https://www.nasa.gov/wp-content/uploads/2015/06/mars_ice_drilling_assessment_v6_for_public_release.pdf
7. Hoffman, S.J., et al. (2020). *Mars Rodwell Experiment Final Report*. NASA/TP-20205011353. https://ntrs.nasa.gov/citations/20205011353
8. Hoffman, S.J., Andrews, A., Watts, K.D., Benson, M. (2020). "Progress in Simulated Water Well Performance on Mars." AIAA ASCEND. NTRS 20205007716
9. Kleinhenz, J.E., Paz, A. (2017). "An ISRU Propellant Production System to Fully Fuel a Mars Ascent Vehicle." AIAA 2017-0423. NTRS 20170001421
10. Kleinhenz, J.E. (2018). "ISRU Technology Development for Extraction of Water from the Mars Surface." NTRS 20180005542
11. Linne, D.L., Kleinhenz, J.E., et al. (2016). "Extraction and Capture of Water from Martian Regolith — Experimental Proof-of-Concept." NTRS 20160010258
12. Mueller, R.P., et al. (2016). "Design of an Excavation Robot: RASSOR 2.0." ASCE Earth & Space 2016. NTRS 20210011366
13. Zacny, K., et al. (2023–2025). TRIDENT drill papers (VIPER/PRIME-1), incl. *PSJ* doi:10.3847/PSJ/ae0b51; Glass, B., et al. (2024) "TRIDENT Drill Validation at Mars and Lunar Analog Field Sites," LPSC 55, NTRS 20240000585
14. Mellerowicz, B., Zacny, K., et al. (2022). "RedWater: Water Mining System for Mars." *New Space* 10(2). doi:10.1089/space.2021.0057
15. Hecht, M.H., et al. (2009). "Detection of Perchlorate and the Soluble Chemistry of Martian Soil at the Phoenix Lander Site." *Science* 325, 64–67. doi:10.1126/science.1172466
16. Lunardini, V.J., Rand, J. (1995). *Thermal Design of an Antarctic Water Well*. CRREL Special Report 95-10
17. SpaceX (2016–2025). Starship vehicle page and IAC presentations (Musk, "Making Life Multiplanetary," 2017, *New Space* 6(1)) — company claims for propellant load 1,100–1,500 t; 267 t CH4 / 933 t O2 split widely cited
