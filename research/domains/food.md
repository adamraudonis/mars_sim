# Food & Crops for Mars — Parameter Database

**Domain key:** `food`
**Prepared:** July 2026 · trade-study / system-model parameter set
**Primary sources:** NASA BVAD (NASA/TP-2015-218570 Rev 2, Feb 2022); Wheeler et al. (2008) *Adv. Space Res.* 41:706-713; Kusuma, Pattison & Bugbee (2020) *Hortic. Res.* 7:56; MELiSSA/Frontiers stoichiometric BLSS (2023). All crop productivity data trace to NASA's Biomass Production Chamber (BPC, Kennedy Space Center, 1987-1998), the largest closed-atmosphere crop dataset in existence.

Units: SI. Energy in **kWh** (electrical unless noted). Time in **sols** where natural (1 sol = 88 775 s = 24.6597 h; 1 Mars year = 668.6 sols). Productivity per Earth-equivalent day (24 h) as reported by NASA; convert to per-sol by ×1.0275 when integrating against sol-based power budgets.

---

## 1. Scope and the two sub-problems

Food on Mars splits into two almost-independent trade problems:

1. **Resupplied / prepackaged food** — mass and packaging you fly from Earth. Well-characterized (ISS flight data). Dominated by shelf-life and packaging-mass penalties. This is the *baseline* every architecture pays until crops close the loop.
2. **In-situ crop production (bioregenerative)** — growing area (m²), electrical power (mostly LED lighting, kW), water, nutrients, and crew time to displace some fraction of resupply. This is where the interesting trades live, and where the numbers are soft.

The central lever is **percent diet closure** — what fraction of calories (and separately, of fresh mass / crew morale value) you grow vs. ship. Salad crops (lettuce, tomato) are cheap in area but contribute almost no calories; staple crops (potato, wheat, soybean) carry the caloric load but cost area, power, and cycle time.

---

## 2. Crew demand (the denominator)

| Quantity | Value | Unit | Source |
|---|---|---|---|
| Metabolic energy requirement | **12.707 MJ = 3037 kcal** | /CM-day | BVAD Eq. 3-2 (EER), Tables 4-57/4-61 |
| Prepackaged food, as-shipped (packaged) | **1.83 – 2.39** | kg/CM-day | BVAD Table 4-47 (Perchonok 2002 → Douglas 2017 max 6-mo rate) |
| Food dry mass (reference) | **0.67** | kg/CM-day | BVAD Table 4-47 (MSIS 1995) |
| Food packaging waste | **0.274** (0.25–0.40) | kg/CM-day | BVAD Table 4-40 |
| Water content of as-shipped ISS food | 42 – 53 | % | BVAD Table 4-47 |
| Hydration water demand | **239** (up to 358) | mL / MJ metabolic = 1 mL/kcal | BVAD Table 4-60 fn.136 |
| Required shelf life (Mars transit + surface) | **≥ 5** | years | BVAD §4.5.1 (Cooper 2011) |

**Notes.** The 3037 kcal value is the modern EER-based average astronaut requirement; older CELSS studies used 2500-3000 kcal (e.g. the Lunn 2017 fertilizer projection assumes 2500). Prepackaged food is ~1.8 kg/CM-day plus ~0.27 kg/CM-day packaging, i.e. **packaging is ~13-17 % of upmass**. Shelf life is the hidden killer: current ISS thermostabilized/dehydrated foods degrade (vitamin loss, texture) well before 5 years, so a Mars-transit food system is a genuine open technology problem, and a strong pull toward in-situ production for multi-year surface stays.

---

## 3. The BVAD / Wheeler crop table (the core deliverable)

Two NASA tables define the candidate crops. **BVAD Table 4-89** gives design environment (light, photoperiod, cycle). **BVAD Table 4-90/4-91** gives productivity, harvest index, and gas/water exchange. **Wheeler et al. (2008)** gives the *measured* closed-chamber productivities and — crucially for power modeling — **radiation-use efficiency (RUE, g dry mass per mol PAR photons)**, which BVAD does not tabulate directly.

### 3.1 Combined crop parameter table

Edible productivity is on a **dry-mass** basis (g dw m⁻² d⁻¹). RUE columns from Wheeler 2008 (best single-study values; "opt" = with seedling-transplant nursery and/or VOC scrubbing). DLI = daily light integral at canopy (mol PAR m⁻² d⁻¹).

| Crop | Harvest index | Edible prod. BVAD [g dw/m²·d] | Edible prod. Wheeler best (opt) | RUE edible [g/mol] (opt) | RUE total [g/mol] | DLI [mol/m²·d] | Photoperiod [h/d] | Cycle [dAP] | Edible water [%] | Energy density [kcal/g dw] |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **White potato** | 0.70 | 21.06 | 18.4 (20.8) | 0.44 (0.50) | 0.64 | 28–42 | 12 | 105–132 | 80 | ~3.7–4.0 |
| **Sweet potato** | 0.60 | 24.7 | — | — | — | 28 | 12 | 85 | 71 | ~3.8 |
| **Wheat** | 0.40 (0.29 obs) | 20.0 | 11.3 (15.8) | 0.17 (0.24) | 0.59 | 37–80 † | 20–24 | 75–90 | 12 | ~3.8–4.0 |
| **Rice** | 0.30 | 9.07 | — | — | — | 33 | 12 | 85 | 12 | ~3.8 |
| **Soybean** | 0.40 | 4.54 | 6.0 (6.9) | 0.16 (0.19) | 0.43 | 21–35 | 12 | 90–97 | 10 | ~4.9 |
| **Dry bean** | 0.40 | 10.0 | — | — | — | 24 | 18 | 85 | 10 | ~4.0 |
| **Peanut** | 0.25 | 5.63 | — | — | — | 27 | 12 | 104 | 5.6 | ~5.5 |
| **Tomato** | 0.45 | 10.43 | 9.8 (11.4) | 0.25 (0.29) | 0.51 | 24–39 | 12 | 84–91 | 94 | ~3.7 |
| **Lettuce** | 0.90 | 6.57 | 7.1 (11.8) | 0.42 (0.70) | 0.46 | 16–19 | 16 | 28–30 | 95 | ~2.5–3.0 |

† **Disagreement flagged.** BVAD Table 4-89 lists wheat PPF as **115 mol m⁻² d⁻¹** (≈1330-1600 µmol m⁻² s⁻¹ continuous, a Bugbee/Salisbury high-light value). Wheeler's *measured* BPC wheat ran at **37-80 mol m⁻² d⁻¹** (509-930 µmol m⁻² s⁻¹, 20-24 h). The BVAD nominal wheat productivity (20 g dw/m²·d edible) is internally inconsistent with a 115 mol/m²·d light input — at that light and RUE_total 0.59 you would expect ~68 g total / ~27 g edible. **Use Wheeler's RUE, not the BVAD PPF, for power modeling.**

### 3.2 Gas exchange & transpiration (BVAD Table 4-91; Wheeler 2008)

Per unit growing area, at nominal productivity:

| Crop | O₂ production [g/m²·d] | CO₂ uptake [g/m²·d] | Transpiration [kg(=L)/m²·d] |
|---|---|---|---|
| Wheat | 56.0 | 77.0 | **11.79** |
| White potato | 32.2 | 45.2 | 4.00 |
| Soybean | 13.9 | 19.1 | 4.70 |
| Sweet potato | 41.1 | 56.5 | 2.88 |
| Rice | 36.6 | 50.3 | 3.43 |
| Tomato | 26.4 | 36.2 | 2.77 |
| Lettuce | 7.8 | 10.7 | 2.10 |

**Mixed-staple average (Wheeler 2008, optimized):** 14 g edible dw m⁻² d⁻¹, 41 g CO₂ removed, 30 g O₂ produced, **4.5 L transpired** m⁻² d⁻¹. Water usage per dry biomass (BVAD Table 4-93, Wheeler 1999): wheat 0.13, potato 0.15, soybean 0.32, lettuce 0.34 **L/g dw**.

**Transpiration is a feature, not just a cost.** Crop transpiration (~4 L m⁻² d⁻¹ average) is condensed on the chamber's cooling coils as **near-distilled clean water** and is essentially 100 % recoverable — it feeds directly back to the nutrient solution and can offset habitat potable-water recycling. A large farm is also a large humidifier/water-processor. Wheeler's BPC condensed ~3.96 L m⁻² d⁻¹ averaged over all crops.

---

## 4. Caloric density of crops

Reference value for edible higher-plant dry matter: **4000 kcal/kg dw = 4.0 kcal/g dw**, derived by MELiSSA by averaging bread wheat, durum wheat, potato, and soybean (Frontiers/MELiSSA 2023). Idealized edible plant composition: **70 % carbohydrate, 20 % protein, 10 % lipid.** Per-crop refinement: starch/cereal staples (wheat, potato, rice) ≈ 3.7-4.0 kcal/g dw; oil/protein crops higher (soybean ≈ 4.9, peanut ≈ 5.5 kcal/g dw); leafy greens lower (lettuce ≈ 2.5-3.0). For L0/L1 modeling, **4.0 kcal/g dw** is an adequate scalar for staples.

---

## 5. Lighting — the dominant power term

Photosynthesis is photon-limited, so crop electrical power is set almost entirely by **how many mol of PAR photons you must deliver per day** and **how efficiently the LED converts electricity to those photons**.

### 5.1 LED photon efficacy (2026 state of the art)

| Quantity | Value | Unit | Source |
|---|---|---|---|
| LED fixture efficacy, mid-range deployable | **2.5 – 3.0** | µmol/J | Kusuma/Bugbee 2020; industry 2023-2024 |
| Best commercial fixtures (2024) | ~3.5 | µmol/J | industry |
| Practical ceiling, white+red / blue+red | **3.4 / 4.1** | µmol/J | Kusuma, Pattison & Bugbee 2020 |
| Diode electrical→photon efficiency: blue / white / red | 0.93 / 0.76 / 0.81 | — | Kusuma & Bugbee 2020 |
| Light delivery efficiency (PPF at canopy / PPF emitted) | **0.8** (0.3–0.8) | — | BVAD Table 4-86 |
| PAR conversion efficiency (W_PAR/W_elec), nominal | 0.84 (0.18 low) | — | BVAD Table 4-86 (Hardy 2020 / Sager 1999) |

Add-on optics for water/humidity protection cut efficacy ~10 %. A defensible 2026 planning value is **3.0 µmol/J at the diode with 0.8 delivery → 2.4 µmol/J of usable PAR per electrical joule at the canopy.**

### 5.2 Electrical energy per mol of photons (governing conversion)

E_elec per mol delivered = 1 / (efficacy × delivery). At the diode (delivery = 1) and at canopy (delivery = 0.8):

| Efficacy | kWh/mol at LED output | kWh/mol delivered to canopy |
|---|---|---|
| 2.5 µmol/J | 0.111 | 0.139 |
| **3.0 µmol/J** | **0.0926** | **0.116** |
| 3.5 µmol/J | 0.0794 | 0.099 |

So roughly **0.10-0.14 kWh(elec) per mol of PAR photons at the canopy** across the plausible 2026 range.

### 5.3 Derived kWh(electric) per kcal of food

Chain: photons per g edible dw = 1/RUE_edible → electrical energy = photons/(efficacy×delivery) → per kcal divide by caloric density (4.0 kcal/g). At efficacy 3.0 µmol/J, delivery 0.8:

| Crop | RUE_edible [g/mol] | **kWh(elec)/kcal (lighting only)** |
|---|---|---|
| **Potato** | 0.44 (0.50 opt) | **0.066 (0.058)** |
| Lettuce | 0.42 (0.70 opt) | 0.069 (0.041) |
| Tomato | 0.25 | 0.116 |
| **Wheat** | 0.17 (0.24 opt) | **0.170 (0.121)** |
| Soybean | 0.16 (0.19 opt) | 0.181 (0.152) |

**Key finding:** per calorie, **potato is ~2.6× more energy-efficient than wheat** and ~3× more efficient than soybean under artificial light — driven by potato's high harvest index (0.70) and high edible RUE. Lettuce, though cheap per gram of PAR, is a calorie desert (edible dry mass is tiny), so its kWh/kcal only looks good on paper. For calorie closure under LEDs, **potato and sweet potato are the champions; wheat and legumes are expensive.**

---

## 6. Growth-system mass, water, nutrients

| Quantity | Value | Unit | Source |
|---|---|---|---|
| Plant growth chamber ESM (with HPS lamps) | **101.5** | kg/m² grow area | BVAD Table 4-88 (Drysdale 1999b) |
| — of which crops+shoot+root structure | ~66 | kg/m² | " (20 crops + 3.6 shoot + 36.8 root-zone water/nutrients) |
| — lamps + ballasts (HPS; LEDs lighter) | ~31 | kg/m² | " (not updated for LED) |
| Chamber power (HPS lamps) | 2.6 | kW/m² | BVAD Table 4-88 — **replace with LED-derived value (§7)** |
| Non-lighting power (fans, pumps, controls) | ~0.28–0.44 | kW/m² | BVAD Table 4-88 (shoot 0.3 + root 0.14) |
| Fertilizer salt, full-diet | **90–100** | kg/CM-year | BVAD §4.14, Lunn 2017 |
| — fraction recyclable from inedible biomass | >50 | % | Strayer 2002 |
| Growth-chamber biomass yield per energy | 1.6 (JSC VPGC) – 10 (South Pole FGC) | g/kWh | BVAD §4.14.1 |
| Analog yield (EDEN ISS, Antarctica) | 27.4 = 0.075 | kg/m²·yr = kg/m²·d fresh edible | Zabel 2020 |
| Power-equivalent mass, Mars surface | 121–145 | kg/kW | Boscheri 2020 (CEAS Space J.) |

**Hydroponic vs. aeroponic.** NASA BPC data are all **NFT hydroponic** (gravity-return, works in Mars 0.38 g but not µg). Aeroponic delivery (mist) cuts standing water mass and can raise root-zone O₂; several Mars-greenhouse concepts (e.g. 38 m² footprint / 48 m² grow area) baseline aeroponics specifically to reduce water inventory. As a modeling scalar, **root-zone water + nutrient hardware ≈ 30-40 kg/m²** dominates the non-lighting mass; aeroponics can shave this.

---

## 7. Percent-diet-closure trade

Because lighting energy per calorie is fixed by RUE × efficacy (independent of DLI — DLI only trades **area vs. time**), the closure trade is nearly linear in calories for a *fixed crop mix*, but the *mix* matters enormously (potato-heavy is cheap; balanced-nutrition is expensive because protein/oil crops have low RUE).

Rough per-crew-member envelopes (LED at 3.0 µmol/J, delivery 0.8, +25 % non-lighting overhead, 4.0 kcal/g dw):

| Closure | Crop mix | Growing area [m²/CM] | Electrical power [kW/CM] | Notes |
|---|---|---|---|---|
| **25 % cal** | potato-dominant | ~10–14 | ~3–4 | Cheapest calories |
| **50 % cal** | potato + wheat | **~21–27** | **~5.5–9** | Worked example, §8 |
| **100 % cal** | potato-only (idealized) | ~41 | ~8–10 (light only) | Nutritionally inadequate alone |
| **100 % cal + balanced nutrition** | staples + legumes + salad | **~40–60** | **~10–20** | Protein/oil crops drag efficiency; matches CELSS/ICES literature |

The classic CELSS rule of thumb — **~40-50 m² per person** for full caloric closure — falls out of this directly (3037 kcal ÷ 4 kcal/g ÷ ~15-18 g edible dw/m²·d ≈ 42-50 m²). Adding nutritional balance (soybean/peanut for protein and fat) pushes both area and power up because those crops have RUE_edible ~0.16, roughly 3× worse than potato.

---

## 8. Worked example — 50 % of one crew member's calories from potato + wheat

**Given:** target = 0.50 × 3037 = **1518.5 kcal/CM-day**, split 50/50 potato/wheat (759 kcal each). Caloric density 4.0 kcal/g dw. LED 3.0 µmol/J, delivery 0.8. Governing equations:

```
edible_dw [g/d]     = calories / caloric_density
area [m²]           = edible_dw / edible_productivity
photons [mol/d]     = edible_dw / RUE_edible
lighting [kWh/d]    = photons / (efficacy[mol/J] × delivery) / 3.6e6
DLI [mol/m²·d]      = photons / area
avg power [kW]      = lighting_kWh / 24.6597 h  (per sol)
```

**Case A — as-measured BPC (Wheeler nominal; wheat HI 0.29):**

| Crop | Edible dw [g/d] | Area [m²] | Photons [mol/d] | DLI [mol/m²·d] | Lighting [kWh/d] |
|---|---|---|---|---|---|
| Potato (RUE 0.44, 18.4 g/m²·d) | 189.8 | 10.3 | 431 | 41.8 | 49.9 |
| Wheat (RUE 0.17, 11.3 g/m²·d) | 189.8 | 16.8 | 1117 | 66.5 | 129.2 |
| **Total** | 380 | **27.1** | 1548 | — | **179 kWh/d** |

→ lighting **7.3 kW** average; **+25 % overhead → ~224 kWh/sol ≈ 9.1 kW** total electrical. Lighting energy intensity **0.118 kWh/kcal** (0.147 with overhead).

**Case B — optimized (seedling transplant + VOC scrubbing; wheat HI 0.40):**

| Crop | Area [m²] | Lighting [kWh/d] |
|---|---|---|
| Potato (RUE 0.50, 20.8 g/m²·d) | 9.1 | 43.9 |
| Wheat (RUE 0.24, 15.8 g/m²·d) | 12.0 | 91.5 |
| **Total** | **21.1** | **135 kWh/d → 5.5 kW light; ~6.9 kW with overhead** |

**Efficacy sensitivity (Case B):** at 3.5 µmol/J → 116 kWh/d (4.7 kW light); at 2.5 µmol/J → 163 kWh/d (6.6 kW light).

**Bottom line:** feeding one crew member **50 % of calories on potato + wheat costs roughly 21-27 m² of growing area and 5.5-9 kW of electrical power** (~135-224 kWh per sol), with **wheat responsible for ~70 % of the power despite ~half the calories.** A potato-heavier mix collapses this: 50 % calories from potato alone would need ~18 m² and ~100 kWh/sol (~4 kW light). This single result is the strongest argument in the whole domain for biasing a Mars menu toward high-HI tubers and treating grain as a morale/nutrition supplement, not a staple.

---

## 9. Crop-failure and closure risks (qualitative, for reliability modeling)

Bioregenerative food is a *tightly coupled, low-margin ecosystem*; unlike a food locker, it can fail catastrophically and on the timescale of one crop cycle (weeks-months). Model as an availability/redundancy penalty:

- **Pathogen outbreak.** *Fusarium oxysporum* infected zinnia in ISS Veggie (2015-16) after a ventilation failure raised humidity; plant pathogens can be *more* virulent in reduced gravity. Seedborne fungal contamination has been documented in space-grown wheat. Monoculture + closed atmosphere = fast spread.
- **VOC / ethylene accumulation.** In BPC, ethylene >100 ppb cut wheat harvest index from 40 % to 29 % — a ~30 % yield loss from an invisible atmospheric problem. Requires active scrubbing (KMnO₄ or catalytic oxidation).
- **Single-point equipment failures.** Loss of lighting, nutrient pump, or thermal control for even hours-days during a light cycle stresses or kills a canopy. Power interruption is existential for LED-lit crops.
- **Long cycle times.** Staples take 75-132 days seed-to-harvest — a failed potato crop is ~3-4 months of lost calories with no quick recovery. This is why closure is never designed to 100 % without deep resupply reserves.
- **Nutrient loop instability.** Sodium/boron buildup, microbial N loss, pH excursions in recirculated hydroponics force periodic solution replacement.

**Modeling recommendation:** never let a trade study assume >~50-70 % steady-state closure without a resupplied/stored-food buffer sized to ≥1 full crop cycle per staple.

---

## 10. Fidelity tiers (recommended model forms)

### L0 — single scalar averages (spreadsheet / architecture sizing)
Per crew member, pick a closure fraction *f* and a crop mix; use fixed intensities:
- Food demand: 3037 kcal/CM-day; resupply mass = (1−f) × 1.83 kg/CM-day + packaging 0.27 kg/CM-day.
- Crop area = f × 3037 kcal ÷ 4 kcal/g ÷ Ȳ, with Ȳ ≈ 15 g edible dw/m²·d (mixed staples) → ~40-50 m² at f=1.
- Crop power = f × 3037 kcal × ē, with ē ≈ 0.10 kWh/kcal (mix) or 0.066 (potato) / 0.17 (wheat), ×1.25 overhead.
- System mass = area × 100 kg/m² (or ~70 with LEDs). Water: transpiration ~4 L/m²·d, ~100 % recovered.

Single governing scalars: **Ȳ ≈ 15 g edible dw m⁻² d⁻¹**, **ē ≈ 0.10 kWh(elec)/kcal**, **~45 m²/CM and ~10-15 kW/CM for full closure.**

### L1 — analytic per-crop, daily/seasonal
Model each crop *i* explicitly with its own {RUE_edible, HI, cycle length, DLI, photoperiod, caloric density}. Compute:
```
edible_prod_i   = RUE_edible_i × DLI_i × f_abs        [g dw/m²·d]   (f_abs ≈ 0.9 closed canopy)
area_i          = (f × cal_i_target) / (caloric_density_i × edible_prod_i)
lighting_i      = DLI_i × area_i / (efficacy × delivery)              [MJ/m²·d → kWh]
O2/CO2/H2O_i    = per-crop Table 4-91 coefficients × area_i
```
Add staggered planting (batch vs. continuous) so harvest and O₂/CO₂/transpiration outputs are smoothed rather than pulsed; overlay a **seasonal Mars-insolation model** if any natural light is used (Mars top-of-atmosphere ~590 W/m²; usable DLI up to ~30 mol/m²·d at favorable latitudes, heavily dust-storm-dependent — BVAD/Clawson 2006). Track nutrient consumption (90-100 kg salt/CM-yr full diet, >50 % recyclable) and a closure buffer. This tier captures the potato-vs-wheat power asymmetry and area/time trades correctly.

### L2 — physics-based sub-sol timestep
Timestep ≤ 1 h over the sol. Replace fixed RUE with a light-response + canopy model:
```
Gross canopy photosynthesis:   Pg = Pg,max · (1 − exp(−k·PPFD/Pg,max))     (non-rectangular hyperbola, per unit LAI)
Net C gain:                     Pn = Pg − Rd(T)                            (Rd = maintenance + growth respiration, Q10≈2)
Biomass:                        dB/dt = Pn · (30/44) · CUE                 (g CH2O; CUE carbon-use eff ≈ 0.6–0.65)
Transpiration (Penman-Monteith): λE = [Δ·Rn + ρ·cp·VPD/ra] / [Δ + γ(1+rs/ra)]
Light input:                    P_elec = PPFD·A / (efficacy·delivery); LED waste heat = P_elec·(1−PAR_conv_eff)
```
Couple to the habitat: O₂ produced and CO₂ consumed (assimilation quotient ≈ 1.0 for starch crops, <1 for oil crops like soybean) feed the atmosphere model; transpired water (Penman-Monteith, canopy VPD-driven) feeds the condensate/water-recovery model; LED and canopy sensible heat (≈ the ~2.4 µmol/J not converted + delivery losses, i.e. most of the electrical input ends as heat) feed the thermal-rejection model — a farm is a multi-kW heat source that must be radiated. State variables per crop batch: leaf-area index, developmental stage (thermal time, °C·d), tuber/grain fill. Governing equations above; parameterize Pg,max, k, Rd, and stage transitions per crop from BPC gas-exchange data. Add stochastic failure events (pathogen, VOC, equipment) as availability draws with cycle-length recovery to represent §9 risk.

---

## 11. Source disagreements & cautions (read before trusting a number)

1. **Wheat light input.** BVAD Table 4-89 says 115 mol/m²·d; Wheeler's measured BPC wheat was 37-80 mol/m²·d. The BVAD value is a high-light (Bugbee) design point that does *not* match BVAD's own nominal wheat yield. Use Wheeler RUE (0.17-0.24 g edible/mol) for energy, not the 115 figure. (medium→low confidence on wheat specifically.)
2. **Potato RUE.** BVAD's implied potato RUE (productivity 21 g/DLI 28 ≈ 0.75 g edible/mol at the low end of its DLI range) is more optimistic than Wheeler's measured 0.44. University of Wisconsin potato reached >30 g edible/m²·d and RUE >0.80. Real values span **0.44-0.80**; using 0.44 is conservative (higher power), 0.5-0.64 is optimistic.
3. **Chamber power in BVAD (2.6 kW/m²) is HPS-lamp-based and obsolete.** Do not use it; derive lighting power from LED efficacy (§5, §7). This is the single biggest error source in older Mars-farm estimates.
4. **Caloric density 4.0 kcal/g dw** is a staple-crop average; oil/protein crops are higher, greens lower. Fine for L0/L1 staples, refine per-crop at L2.
5. **All BPC data are ≤0.38-1 g (Earth) with elevated CO₂ (1000-1200 ppm).** Microgravity is worse (fluid handling, disease); Mars partial gravity is largely untested at crop scale. Treat productivities as **optimistic upper bounds** for early Mars surface systems.
6. **Productivities are per 24-h Earth day** as NASA reports them; a Mars sol is 2.75 % longer, a small but non-zero correction when reconciling per-sol power budgets with per-day yields.

---

## References (primary)
- NASA/TP-2015-218570 Rev 2 (Feb 2022), *Life Support Baseline Values and Assumptions Document* — Tables 4-40, 4-47, 4-57, 4-60/61, 4-86/87/88, 4-89/90/91, 4-93. https://ntrs.nasa.gov/api/citations/20210024855/downloads/BVAD_2.15.22-final.pdf
- Wheeler, R.M. et al. (2008) "Crop productivities and radiation use efficiencies for bioregenerative life support," *Adv. Space Res.* 41(5):706-713. http://bigidea.nianet.org/wp-content/uploads/2018/07/Adv-Space-Res-2008-Crop-Prod-and-Rad-Use-Eff.pdf
- Kusuma, P., Pattison, P.M., Bugbee, B. (2020) "From physics to fixtures to food: current and potential LED efficacy," *Hortic. Res.* 7:56. https://www.nature.com/articles/s41438-020-0283-7
- MELiSSA / Frontiers (2023) "Stoichiometric model of a fully closed bioregenerative life support system." https://www.frontiersin.org/articles/10.3389/fspas.2023.1198689/full
- Boscheri, G. et al. (2020) "Influence of crop cultivation conditions on space greenhouse equivalent system mass," *CEAS Space J.* https://link.springer.com/article/10.1007/s12567-020-00317-5
- Zabel, P. et al. (2020) "Biomass Production of the EDEN ISS Space Greenhouse in Antarctica." (analog yield 27.4 kg/m²·yr)
- Wheeler, R.M. (2017/2022) NASA NTRS talks on bioregenerative life support; NASA/TM-2020-5008832 "NASA's Contributions to Vertical Farming."
