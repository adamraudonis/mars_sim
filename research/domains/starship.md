# SpaceX Starship V3 — Transport Parameter Database

**Domain key:** `starship` · **Compiled:** 2026-07-05 · **Status of vehicle:** Starship/Super Heavy V3 flew once (Flight 12, 2026-05-22, partial success: full ascent, payload deploy, controlled ship splashdown on 2 of 3 landing engines; booster boostback anomaly). All performance numbers to Mars remain **projections**, not flight-demonstrated.

> **Reading guide.** Figures labeled *(SpaceX claim, date)* are company statements (Musk presentations, SpaceX web/X posts) and should be treated as targets. Independent/peer-reviewed estimates are labeled with their source. Where sources disagree, both are given and the disagreement is discussed in §9.

---

## 1. Vehicle geometry and masses

| Quantity | Value | Source / notes |
|---|---|---|
| Stack height (V3) | **124.4 m** | SpaceX claim (Musk, "The Road to Making Life Multiplanetary," 2025-05-29); confirmed by Flight 12 vehicle (V2 was 123.1 m) |
| Super Heavy booster height (V3) | **72.3 m** | Wikipedia/NSF compilation of SpaceX statements |
| Ship height (V3) | **~52.1 m** | Derived: 124.4 − 72.3. One Wikipedia table lists "61 m" for a Block-3 ship — that figure matches the stretched **V4** ship announced for ~2027 and is treated here as erroneous/conflated for V3 |
| Diameter | **9 m** | SpaceX, all orbital Starships (high confidence) |
| Ship propellant load (V3) | **1,600 t** | SpaceX claim (May 2025 presentation); V2 = 1,500 t, V1 = 1,200 t |
| Booster propellant load (V3) | **4,050 t** | SpaceX claim (May 2025); V2 = 3,650 t. One secondary source (New Space Economy, 2026-04-16) says "~3,400 t" — outlier, not used |
| Stack liftoff mass | **~5,250 t** | rocketlaunch.org V3 page; New Space Economy gives "~5,300 t" — consistent |
| Ship dry mass | **~125 t (est.)** | No official V3 figure. Musk 2021: ~100 t for Block 1 ships; Wikipedia lists 85 t target for Block 2 (aspirational); Maiwald et al. (Sci. Rep. 2024) bottom-up estimate for a crewed Mars Starship: **210.5 t incl. 20 % margin**. 125 t is our engineering estimate for a V3 *cargo/tanker* ship; use 100–160 t band, and note crewed variants trend heavier |
| Payload bay (cargo) volume | **614 m³** | Wikipedia (from SpaceX Users Guide-era data); Users Guide standard fairing ~8 m dia × ~17.24 m usable |
| Pressurized volume (crew config) | **~1,000–1,100 m³** | "on order of 1,100 m³ forward space, most of which will be pressurized" — Heldmann et al., New Space 10(3), 2022 (based on SpaceX Users Guide); SpaceX has historically quoted 1,000 m³ |
| Payload deployment doors | **3 m × 3 m (nominal)** | Heldmann et al. 2022 (customizable) |

## 2. Propulsion — Raptor 3

| Quantity | Value | Source |
|---|---|---|
| Sea-level thrust | **280 tf = 2.746 MN** | SpaceX claim (X post, 2024-08-04); Wikipedia notes 250 tf "nominal" operating rating on early V3 flights |
| SL Isp | **350 s** | SpaceX claim (2024-08-04) |
| Vacuum (RVac) thrust | **~306 tf target / 275 tf nominal** | SpaceX statements compiled by Wikipedia |
| Vacuum Isp (RVac) | **380 s** | SpaceX claim; Maiwald et al. use 378 s |
| Chamber pressure | **330–350 bar** | SpaceX statements 2024–2026 |
| Mixture ratio (O/F) | **3.6** | Wikipedia (SpaceX Raptor); Heldmann et al. use ~3.5 → CH₄ is 21.7–22.2 wt% of propellant |
| Engine mass | **1,525 kg** (+195 kg vehicle-side) | SpaceX claim (2024-08-04) |
| Ship engines (V3) | **9 = 3 SL + 6 RVac** | SpaceX May 2025 presentation; Flight 12 ship flew with 6 engines per SpaceX Flight-12 coverage (media reported "six Raptor engines" at hot-staging) — configuration still evolving between 6 and 9 |
| Booster engines | **33** | Flight-demonstrated (Flight 12: all 33 Raptor 3s lit) |
| Booster liftoff thrust | **~81–91 MN** | 33 × 250 tf = 81 MN (rocketlaunch.org lists 80.8 MN); 33 × 280 tf = 90.6 MN at full rating; SpaceX aspiration "10,000 tf" (~98 MN) with Raptor 3.x |

**Propellant split at O/F = 3.6:** for ship load $m_p$: $m_{CH_4} = m_p/4.6 = 0.217\,m_p$, $m_{LOX}=0.783\,m_p$. For 1,600 t: **348 t CH₄ + 1,252 t LOX**. (Heldmann et al., using O/F 3.5 on the 1,200 t V1 ship: 267 t CH₄ + 933 t O₂.)

## 3. Payload performance

### 3.1 To LEO
- **Reusable: "100+ t"** *(SpaceX claim, May 2025 presentation; also quoted as "up to 150 t" on SpaceX web materials)*. Demonstrated to date: V2 lofted ~35 t of simulators on suborbital-energy trajectories (2025); Flight 12 (V3) deployed 20 Starlink simulators + 2 modified Starlinks.
- **Expendable: ~200 t** *(SpaceX claim; range 180–250 t across 2024–2025 Musk statements)*.

### 3.2 To Mars surface
- **SpaceX claim: 100 t (statements 2017–2026; "100–150 t" in some materials).** Commercial pricing floated at "$100 M/t" for Mars cargo services NET 2028 (Aerospace America, 2026).
- **Independent estimates diverge:**
  - Heldmann et al. 2022 (New Space): adopt ~100 t as workable for architecture planning.
  - Maiwald et al. 2024 (Scientific Reports): with SpaceX's *claimed* 100–120 t dry mass, up to **114.4 t** landed is kinematically possible (2029 opportunity, Δv LEO→surface 5,588 m/s, Isp 378 s); but with their bottom-up dry mass (210.5 t) they "were not able to find a feasible Mars mission scenario," and **Earth-return was infeasible in all their scenarios**.
  - Rapp 2024–25 (IgMin): TMI burn consumes ~950 t of the 1,200 t (V1-class) load; ~30 t midcourse; ~220–250 t remains for EDL — sufficient only because ~99 % of entry energy is shed aerodynamically; net payload highly uncertain.
  - This database's **independent working value: 50 t (range 10–114 t)** for early flights, rising toward the claim as dry mass and EDL mature.

### 3.3 Landed-mass model (for the sim)
Treat landed payload $m_{pl}$ as a free parameter constrained by an EDL propellant budget:
$$m_{prop,EDL} = (m_{dry}+m_{pl})\left(e^{\Delta v_{land}/(g_0 I_{sp,SL})}-1\right)$$
with $\Delta v_{land} \approx 500{-}800$ m/s (terminal burn after aerodynamic deceleration from ~7.5 km/s entry to ~ Mach 2–3; Aerospace America 2026 quotes entry at 7.5 km/s, aero-braking to ~1 km/s). At $\Delta v=650$ m/s, Isp 350 s: propellant fraction ≈ 0.208 of landed mass → 175 t landed (125 dry + 50 pl) needs **~36 t** landing propellant plus reserves.

## 4. Orbital refilling (Earth side)

Number of tanker flights per Mars-bound ship:
$$N_{tanker} = \frac{m_{prop,TMI} - m_{resid}}{m_{deliver}}$$
- $m_{prop,TMI}$: full V3 load 1,600 t (fast transits use essentially full tanks).
- $m_{deliver}$: propellant delivered to depot per tanker flight — **100 t (Rapp), 150 t (mid), 200 t (New Space Economy 2026 claim for dedicated V3 tankers)**.
- Reference points: Musk 2017 IAC showed 5 tankers (1,100 t ship); SpaceX has said **"8–10"** for lunar HLS; NASA/GAO estimated **~16, up to 19** for HLS incl. boiloff losses; NASA OIG (IG-26-004, Mar 2026) reviews cite mid-teens; Rapp computes **~12** for a 1,200 t V1-class Mars ship at 100 t/tanker.
- **Recommended baseline: 12 (range 8–20).** Five-ship 2026/27 uncrewed Mars fleet → ~60 tanker launches (Rapp, in Aerospace America 2026).

## 5. Mars ascent / Earth return

**Δv, Mars surface → trans-Earth injection (TEI):**
- Surface → 500 km circular LMO: **4.3 km/s**; surface → 1-sol elliptical orbit: **5.6 km/s** (incl. losses; Rapp/IgMin, after Wooster et al. 2007).
- 1-sol orbit → TEI: ~1.3–2.0 km/s depending on opportunity and return C3.
- Direct ascent-to-TEI (SpaceX plan, no parking orbit assembly): **≈ 6.9 km/s (range 6.4–7.5)** — consistent with Mars Direct-heritage budgets. Maiwald et al. conclude this exceeds Starship capability *at their 210 t dry mass*; at ≤130 t dry it closes (see check below).

**Propellant to relaunch a ship from Mars:**
$$m_{prop} = (m_{dry}+m_{pl})\left(e^{\Delta v/(g_0 I_{sp})}-1\right)$$
With $m_{dry}=125$ t, $m_{pl}=20$ t (return cargo/crew), Isp 375 s (mixed SL/vac), Δv 6.9 km/s → $m_{prop} = 145\,(e^{1.876}-1) = 145 \times 5.53 \approx$ **800 t**. Full V3 tanks (1,600 t) give $\Delta v = 9.81\times380\times\ln(1745/145) \approx 9.3$ km/s — comfortable margin; a V1-class 1,200 t load gives ~8.3 km/s.
- Heldmann et al. 2022 (V1-class ship): full refill = **1,200 t = 933 t LOX + 267 t CH₄**, requiring ~600 t of water and ~1 GWh-scale ISRU energy per ship (M. Hecht: ~600 kW continuous to make ~600 t LOX per 26-month cycle, quoted in Aerospace America 2026).
- Rapp: **~920 t CH₄+O₂** for a single stripped-down returning Starship.
- **Recommended baseline: 1,200 t (range 780–1,600 t)** per returning ship; CH₄ fraction 21.7 %.

## 6. Cryogenic boiloff on the Mars surface

Governing conduction/insulation model (flat-plate through insulation):
$$\dot Q = \frac{k_e A_s}{L_{ins}}\,\Delta T,\qquad \dot m_{boiloff} = \frac{\dot Q}{h_{fg}}$$
- $h_{fg}$(LOX) = 213 kJ/kg; $h_{fg}$(LCH₄) = 511 kJ/kg.
- NASA KSC analysis (Liu, NTRS 20170011674, 2017), Mars worst case (290 K daytime ambient, 7 torr CO₂): 22 mm Layered Composite Insulation, $k_e$ = 2 mW/m·K → **q″ = 15.4–18.1 W/m²**; a 162 m² insulated tank set absorbs **3.5–3.7 kW**, and **zero-boiloff refrigeration needs 8.3–11.5 kW electrical** (reverse turbo-Brayton, 70 % of Carnot; Carnot η 0.46–0.60). Radiator sizing ~15 m² for 11.5 kW rejection; convection in 7 mbar CO₂ is nearly useless.
- Casey Handmer (2025 blog, independent): bare/uninsulated tanks in full sun ~300 kW class heat leak (lunar case) → tens of t/day; **20-layer MLI cuts heat ~99 % → <1 t/day**, "years to boil off."
- Scaled to a Starship on Mars (~900 m² tank area, LCI-class insulation, diurnal-average ΔT): $\dot Q \approx$ 9–16 kW → **3.6–6.5 t/day ≈ 0.23–0.42 %/sol of a 1,600 t load** if all heat goes to LOX. Uninsulated stainless tanks: several %/sol. NASA CFM reference for well-insulated LOX in space: 0.016 %/day.
- **Recommended baseline: 0.25 %/sol passive (range 0.05–2 %/sol); zero-boiloff option costs ~40 kW_e (≈ 990 kWh/sol)** scaled from Liu (8.3 kW_e per 3.5 kW lift at 109 K).

## 7. Landing accuracy

- Earth demonstrations: Super Heavy tower catches (first 2024-10-13, repeated 2025) require ~dm-class lateral precision; SpaceX VP Gerstenmaier claimed Flight 4 booster splashdown "with half a centimeter accuracy" (2024, widely judged a misstatement — RTK-class ~2.5 cm is the physical floor; treat as "cm–dm class on Earth with GNSS").
- Mars has no GNSS: precision depends on terrain-relative navigation. State of the art: Perseverance TRN achieved ~5 m map-relative position knowledge inside a 7.7 × 6.6 km ellipse (2021). Starship's powered terminal descent + TRN should deliver **~10–100 m CEP**; first landings without beacons plausibly 100 m–2 km.
- **Recommended baseline: 100 m CEP (range 10–2,000 m), speculative.** Pad-to-pad operations with local beacons later: ~10 m.

## 8. Cargo unloading constraints

- Payload section base sits high: NASA JSC study of offloading a 90-t habitat from Starship on Mars gives lowering height from the payload section as **~33.5 m** (Howard et al., ASCEND 2023, NTRS 20220010430).
- Same study: crane system budget **≤10 t** (hackathon design: 5.27 t of beams alone, excl. motors/fixtures); Starship nosecone-as-counterweight schemes proposed to prevent tipping; habitat must land in a **1.58 m trench** to mate surface elements — i.e., unloading is a *system*, not an afterthought.
- Doors 3 × 3 m nominal (Heldmann et al. 2022). Starship was **the only lander concept able to deliver a monolithic 90-t payload**, but "SpaceX has stopped short of advertising" any unloading capability (Howard et al.).
- Sim implication: budget ~5–10 t and a deployment timeline (sols, crew/robot hours) per cargo ship for offloading; cap single-item mass by crane capacity, not ship capacity.

## 9. Disagreements between sources (summary)

| Parameter | SpaceX claim | Independent | Resolution used |
|---|---|---|---|
| Mars payload | 100 (–150) t | 114 t max optimistic (Maiwald); infeasible at realistic dry mass; ~10–50 t early (various) | claim + independent param, 50 t working value |
| Ship dry mass | ~100 t (2021); 85 t table value | 210.5 t (Maiwald, crewed w/ margin) | 125 t cargo ship, range 100–211 |
| Tanker flights | 5 (2017), 8–10 (HLS-era) | 12 (Rapp), 16–19 (GAO/NASA) | 12, range 8–20 |
| Booster thrust | 10,000 tf aspiration | 80.8 MN (250 tf/engine nominal) | 81 MN, range to 90.6 |
| Ship height V3 | 52.1 m (derived) | 61 m (one wiki table — likely V4) | 52.1 m |
| Return propellant | full tanks (1,200–1,600 t) | 920 t (Rapp), 780 t (rocket-eq. min) | 1,200 t |

## 10. Fidelity tiers for the simulation

**L0 — single scalars.** Ship delivers `payload_mars_surface` (50 t indep. / 100 t claim) per window; each Mars ship costs `tanker_refills_per_mars_ship` (12) tanker launches; return consumes `mars_return_propellant_t` (1,200 t) of ISRU propellant; boiloff a flat 0.25 %/sol on stored cryogens; unloading fixed 5 t equipment tax + N sols.

**L1 — analytic per-window model.**
- Landed mass from rocket equation with Δv_land ~ N(650, 100) m/s and dry-mass uncertainty; payload = min(claim, EDL-closed value).
- Tanker count $N = \lceil (m_{TMI} - m_{resid})/m_{deliver}\rceil$ with depot boiloff loss term $\lambda t_{loiter}$ (λ ≈ 0.1 %/day in LEO) — reproduces the GAO-vs-SpaceX spread by varying $m_{deliver}$ (100–200 t) and λ.
- Return: Δv(opportunity) table 6.4–7.5 km/s over the 668.6-sol Mars year; propellant from rocket equation; ISRU production must equal `mars_return_propellant_t` per ship per synodic period (26 months ≈ 780 sols).
- Boiloff: $\dot m = k_e A_s \Delta T(t_{sol}, L_s)/(L_{ins} h_{fg})$ with diurnal/seasonal ambient from your Mars climate domain; cryocooler offsets $\dot Q$ at COP ≈ 0.42·Carnot.

**L2 — physics-based sub-sol model.**
- EDL: 3-DOF entry from 7.5 km/s with L/D ≈ 0.3, ballistic coefficient ~1,500 kg/m² (9 m cylinder, ~200 t), supersonic retropropulsion ignition ~Mach 2.5; Monte-Carlo winds/densities (couple to MarsGRAM-like atmosphere) → landed-mass and accuracy distributions.
- Tank thermal: lumped 2-node (LOX, CH₄) with radiative + 7 mbar CO₂ free-convection film (h ≈ 0.5–2 W/m²K), solar flux ~590 W/m² max, dust optical depth; vent-vs-cryocooler control; autogenous pressurization state.
- Ascent: full rocket equation with thrust-to-weight (Mars g = 3.71 m/s²), 9-engine thrust, gravity losses computed, staging of header vs main tanks; verifies the 6.9 km/s budget.
- Launch/catch ops on Earth side only enter via tanker cadence constraints (pad turnaround, weather).

### Key modeling cautions
1. **Nothing beyond LEO is demonstrated** (as of 2026-07): one V3 flight, no ship-to-ship propellant transfer yet (planned next V3 flights), no Starship EDL beyond Earth.
2. Treat dry mass as the master uncertainty — it drives payload, refill count, and return feasibility simultaneously; correlate these in Monte-Carlo.
3. Company claims have historically been ~2 windows optimistic and ~30–50 % payload-optimistic (V1 "100 t" → actual V1/V2 demonstrated ≪ that); apply a schedule/performance haircut model.

---

## Sources

1. SpaceX (X post), Raptor 3 specs, 2024-08-04 — https://x.com/SpaceX/status/1819772716339339664
2. E. Musk / SpaceX, "The Road to Making Life Multiplanetary," Starbase presentation, 2025-05-29 (5 ships in 2026/27 window; V3 specs; 20/100/500 mission ramp; transit 80–150 d)
3. Wikipedia, "SpaceX Starship" and "SpaceX Starship (spacecraft)" (accessed 2026-07-05) — V1/V2/V3 propellant, heights, dry masses
4. Wikipedia, "SpaceX Raptor" (accessed 2026-07-05) — O/F 3.6, Isp, chamber pressure
5. rocketlaunch.org, "Starship V3 Vehicle Overview" — 124.4 m, 5,250 t, 80.8 MN, 100 t LEO
6. NASASpaceFlight.com, "Starship Flight 12: Block 3 Impresses on First Flight," 2026-05 — https://www.nasaspaceflight.com/2026/05/starship-flight-12-block-3-pad-2/
7. Payload, "What to Expect from Starship V3," 2026-03-09 — https://payloadspace.com/what-to-expect-from-starship-v3/
8. New Space Economy, "Detailed Review of Starship V3," 2026-04-16 — https://newspaceeconomy.ca/2026/04/16/detailed-review-of-starship-v3/
9. Heldmann, J. et al., "Mission Architecture Using the SpaceX Starship Vehicle to Enable a Sustained Human Presence on Mars," New Space 10(3), 2022 — https://pmc.ncbi.nlm.nih.gov/articles/PMC9527650/
10. Maiwald, V. et al., "About feasibility of SpaceX's human exploration Mars mission scenario with Starship," Scientific Reports 14, 2024 — https://www.nature.com/articles/s41598-024-54012-0
11. Rapp, D., "Human Missions to Mars Using the Starship," IgMin Research, 2024–25 — https://www.igminresearch.com/articles/html/igmin308 (also igmin274, igmin292)
12. Aerospace America (AIAA), "A Closer Look at SpaceX's Mars Plan," 2026 — https://aerospaceamerica.aiaa.org/aiaa-spacex/ (Rapp 12-tanker calc; Hecht 600 kW ISRU; 933/267 t split)
13. Howard, R.L. et al., "Options for Offloading a 90-Ton Common Habitat from its Lander on the Surface of Mars," AIAA ASCEND 2023, NTRS 20220010430 — https://ntrs.nasa.gov/citations/20220010430
14. Liu, G.F., "Long-Term Cryogenic Propellant Storage on Mars with Hercules Propellant Storage Facility," NASA KSC, 2017, NTRS 20170011674 — https://ntrs.nasa.gov/citations/20170011674
15. Plachta, D. et al., "NASA cryocooler technology developments and goals to achieve zero boil-off...," Cryogenics 94, 2018, NTRS 20180004710 (LOX passive boiloff 0.016 %/day reference)
16. Handmer, C., "Long duration propellant stability in Starship," blog, 2025-03-14 — https://caseyhandmer.wordpress.com/2025/03/14/long-duration-propellant-stability-in-starship/
17. NASA OIG, IG-26-004, "NASA's Management of the Human Landing System Contracts," 2026-03 (tanker-count context); GAO HLS reviews 2023–24 (16–19 flights)
18. Ataee, N. & Elvander, J. (Sci. Rep. 15, 2025), "3 months transit time to Mars for human missions using SpaceX Starship" (via phys.org summary, 2025-06) — 15 tankers/crewed ship, 90–104 d transits
19. Gerstenmaier, W. (SpaceX), Flight 4 booster splashdown accuracy statement, 2024 (disputed; see theshamblog.com analysis)
20. Wooster, P. et al., "Mission design options for human Mars missions," MARS Journal, 2007 (ascent Δv heritage values, as quoted in IgMin/Rapp)
