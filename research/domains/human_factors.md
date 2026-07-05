# Human Factors: Crew Time, Habitability, Radiation, Health

**Domain key:** `hf`
**Purpose:** Parameters to drive (a) a labor-supply model (available crew-hours/sol by category) and (b) a radiation dose tracker, plus habitability volume and medical event-rate inputs for a Mars habitat/settlement trade-study simulation.
**Compiled:** July 2026. Units SI; energy in kWh where applicable; 1 sol = 88,775 s = 24.6597 h; 1 Mars year = 668.6 sols.

---

## 1. Crew time budget (ISS-derived baseline)

The canonical open reference is the crew schedule in the NASA Life Support Baseline Values and Assumptions Document (BVAD), NASA/TP-2015-218570 Rev 2 (Feb 2022, NTRS 20210024855), **Table 3-28** ("Time Allocation for a Nominal Crew Schedule in Weightless Environment – Current ISS", after Langston 2005):

| Activity | Weekday (CM-h/CM-d) | Weekend day (CM-h/CM-d) | Class |
|---|---|---|---|
| Daily planning conferences | 0.5 | 0.0 | VST |
| Daily plan review / report prep | 1.0 | 0.0 | VST |
| Work preparation | 0.5 | 0.0 | VST |
| Scheduled assembly, systems & utilization ops (incl. maintenance, science, medical ops) | 6.5 | 0.3 | VST |
| Meals (prep + eat + cleanup, prepackaged food) | 3.0 | 3.0 | VST |
| Housekeeping & laundry | 0.0 | 2.0 | VST |
| Post-sleep | 0.5 | 0.5 | IST |
| Exercise, hygiene, setup/stow | 2.5 | 2.5 | IST |
| Recreation | 0.0 | 6.0 | IST |
| Pre-sleep | 1.0 | 1.0 | IST |
| Sleep | 8.5 | 8.5 | IST |
| **Total** | **24.0** | **24.0** | |

- Week structure: 5 workdays + 2 weekend days; 8 vacation days/yr (BVAD §3.2.5).
- **Variably-Scheduled Time (VST)** — the pool a mission planner can actually tax for work, life-support ops, meals, housekeeping — averages **67.2 CM-h/wk in weightlessness** and **69.7 CM-h/wk on a planetary surface** (BVAD assumes exercise is 0.5 h/d shorter under gravity). Range 50 (minimum expected work) to 76.7 (max, sustainable ≤28 days per Skylab experience) CM-h/wk (BVAD Table 3-29).
- **Invariantly-Scheduled Time (IST)** (sleep, pre/post-sleep, exercise, recreation): 100.8 CM-h/wk weightless, 98.3 CM-h/wk surface.

**Flight-data caveat (sleep):** astronauts *actually* sleep ~**6.1 h/night** on ISS (5.96 h Shuttle), not the scheduled 8.5 h — actigraphy over >4,200 in-flight nights, 85 astronauts (Barger et al. 2014, *Lancet Neurology* 13:904-912). A simulation should schedule 8.5 h but may model chronic sleep restriction as a performance/error-rate risk factor, not as recoverable labor.

### Sol conversion
ISS budgets are per 24.0-h day. Mars surface crews will live on the 24.6597-h sol (as MER/MSL/InSight ops teams did). Two defensible conventions:
1. Scale all categories by 24.6597/24 = **1.0275** (used here for per-sol parameters), or
2. Keep category durations fixed and add the extra 0.66 h/sol to discretionary/margin time (more conservative for labor supply).

### Food preparation (BVAD §4.5.3)
- Fully **prepackaged** food system: **0.17 CM-h/CM-d** (1.0 CM-h/d for a crew of 6; Shuttle experience 45–90 min/d for crew up to 6).
- Half packaged / half crop-based: 0.50 CM-h/CM-d.
- Fully **bioregenerative** (crops grown on site): **0.83 CM-h/CM-d** (30 min per dish, 10 dishes/CM-d model); LMLSTP Phase III *measured* 4.6 CM-h/d on the 10-day BIO-Plex menu (≈1.15 CM-h/CM-d for crew of 4). Crop processing into ingredients is *additional and unquantified* in BVAD.
- Food-system choice is therefore worth ~0.7–1.0 crew-h/CM-sol — one of the largest discretionary labor levers in a settlement sim, and it grows with the fraction of grown food.

### Maintenance (ISS actuals and NASA models)
- ISS maintenance is folded inside the 6.5 h/d "scheduled ops" block; NASA's Exploration Crew Time Model (ECTM) was built from ISS timeline actuals (OPTimIS) and maintenance logs (MDC) (Stromgren et al., IEEE Aerospace 2021, NTRS 20210026843).
- **Corrective (unscheduled) maintenance**, lunar Surface Habitat case study (28-day, 2 crew, ISS-derived component data): expected value **24.7 crew-h/mission** → **≈0.44 crew-h/CM-sol**; 50th percentile 9.0 h, 80th 33.4 h, 90th 40.0 h, 99th **70.5 h** (≈1.26 h/CM-sol). ECLSS dominates the corrective total (Stromgren et al. 2021, Table 3 / Fig. 3). Skew matters: plan at 90–99th percentile if loss-of-function is mission-critical.
- **ECLSS-specific upkeep** (preventive + corrective) from ISS/Mir experience: **3.0–3.3 crew-h/day per habitat for crews of 2–3** (Russell & Klaus 2007, *Reliability Engineering & System Safety* 92:808–820). Per-habitat, not per-CM; scales sub-linearly with crew size.
- Mars-mission integrated result (Stromgren et al., "Developing a Crew Time Model for Human Exploration Missions to Mars", IEEE Aerospace 2015, NTRS 20150009469): on a 1,100-day conjunction mission (4 crew, 460-sol surface stay), after all liens only **≈8.5% of surface crew-hours** (≈2,667 crew-h) were available for utilization/science incl. rover driving; transit ≈1–2%. Surface work rate must *increase* +0.7 h/CM-d over ISS experience (long-stay). Also: 30 days post-landing reconditioning before EVA/excursions (long-stay assumption).

## 2. EVA cost

- **ISS actuals (microgravity, EMU + 101.3 kPa cabin):** average **58.25 crew-h of preparation + 15.83 crew-h post-EVA ≈ 74 crew-h of overhead per (typically 2-person, ~6.5 h) EVA** — procedure reviews, tool config, suit servicing, prebreathe, IV support, across several days (NASA figures via Griffin, AIAA 2020-4170, citing NASA EVA-EXP-0031). Work-efficiency index (EVA time / total invested time) ≈ 0.39–0.51; translation alone eats 12–34% of suited time (Looper & Ney, SAE 2005-01-3014).
- **ISS EVA gas cost:** prebreathe O₂ ≈ 9.12 kg/EVA (campout/ISLE protocols), in-suit metabolic O₂ ≈ 0.94 kg, airlock air loss ≈ 1.82 kg/EVA ("ISS EVA Gas Usage" via Griffin 2020).
- **Mars design case:** NASA's exploration atmosphere (56.5 kPa / 34% O₂, NASA-STD-3001-endorsed) cuts prebreathe to ~**15 min** (Abercromby et al. 2015, *Acta Astronautica* 109:76-87); suitports avoid airlock cycling. Estimated **per-crewmember EVA overhead 1–3 h/EVA (nominal 1.5)** — don/doff, checkout, prebreathe, dust mitigation, suit servicing amortized. This is an engineering estimate (confidence LOW): planetary-suit overhead has no flight data since Apollo (Apollo LM cabin depress ops were ~1 h/EVA but suits required massive post-mission servicing).
- **Suit consumables (BVAD Table 4-69, lunar surface values, ~Mars-applicable):** O₂ usage+leakage **0.069–0.110 (nominal 0.092) kg/CM-h**; xEMU suitport scenario 0.1 kg/CM-h + 0.54 kg/CM-EVA fixed (N₂ purge dominated). Cooling water (sublimator) **0.25–0.5 kg/CM-h** polar / **0.46–0.76 kg/CM-h** equatorial thermal environments; metabolic rate during EVA ~295–352 W (1.06 MJ/CM-h food energy).
- **Scheduling constraint:** NASA HAT baseline caps EVA at **24 crew-h per crewmember per 7 days with one full rest day** (Stromgren et al. 2015). BVAD lunar outpost planning: 80 CM-h/wk total EVA for the base at 7 sorties/wk (2-person sorties).

## 3. Habitable volume & psychological time/volume allocations

- **Minimum acceptable Net Habitable Volume: 25 m³/person** for long-duration (~Mars transit, up to ~912 d) missions — SME consensus, Whitmire et al. 2015 (NASA/TM-2015-218564; consensus report NTRS 20140016951). Caveats: assumes sensory/social countermeasures, individual private quarters, exercise and galley/wardroom function separation. NASA-STD-3001 Vol 2 requires NHV be validated by task analysis rather than a single number; 25 m³/p is the accepted planning floor.
- **Private crew quarters:** consensus report allocates **5.4 m³ per crewmember** for individual quarters on long missions; ISS USOS CQ flight actual is **2.1 m³** (Broyan et al., 08ICES-0222) — treated as an absolute floor, marginal for multi-year missions.
- **Psychological scheduling constraints (time, not psychometrics):** 2 contiguous weekend days off per week with ~6 h/d recreation (BVAD Table 3-28); 8 vacation d/yr; daily crew autonomy grows with Earth comm latency (4–24 min one-way; no real-time ground scheduling — plan-review time above already reflects autonomous ops per Stromgren 2015, who *increased* training and work-prep liens for Mars); weekly private family/psych conferences (~1 h/wk/CM, folded into recreation/pre-sleep in the table).

## 4. Radiation

### Measured environment (flight data)
| Quantity | Value | Source |
|---|---|---|
| Mars surface GCR dose equivalent (Gale, −4.4 km MOLA, solar max era) | **0.64 ± 0.12 mSv/day** (= 0.66 mSv/sol; 0.21 mGy/d absorbed) | Hassler et al. 2014, *Science* 343:1244797 (MSL RAD) |
| Surface annual GCR | ≈ 233 mSv/yr (Earth yr), ~0.5–0.8 mSv/d over solar cycle (higher at solar min; ~+ at higher elevation) | Hassler 2014; Guo et al. 2021 *Astron. Astrophys. Rev.* 29:8 |
| Interplanetary cruise GCR dose equivalent (2011–12, weak-moderate shielding, solar max) | **1.84 ± 0.33 mSv/day** | Zeitlin et al. 2013, *Science* 340:1080 (MSL RAD cruise) |
| Round-trip transit dose (~360 d total, chemical propulsion) | ≈ **0.66 Sv** | Zeitlin et al. 2013 |
| Mean vertical atmospheric column (datum) | ≈ **23 g/cm²** CO₂ (site/season dependent ~16–30; Gale ≈ 21–27) | Guo et al. 2021; Martinez-Sierra et al. 2024 (arXiv:2409.02001) |

### Dose limits (NASA-STD-3001 Vol 1 Rev B, 2022; per 2021 NAS review)
- **Career effective dose limit: 600 mSv**, universal (all ages/sexes), anchored to 3% mean REID for cancer mortality.
- Short-term deterministic limits: **30-day BFO 250 mGy-Eq**, 1-year BFO 500 mGy-Eq; eye/skin higher (OCHMO Radiation Technical Brief).

### Shielding — regolith (GCR)
Röstel et al. 2020 (*JGR Planets* 125:e2019JE006246, GEANT4/AtRIS, validated vs RAD; water-sphere phantom):
- **Neutron buildup:** equivalent dose *increases* with depth for dry regolith, peaking at **30–40 cm (88–102 g/cm²)**, and only falls back below the surface value beyond **≈200 g/cm² (0.7–0.9 m of dry rock)**. Anything thinner than ~0.7 m of dry regolith is *worse than nothing* — this is the single most model-critical shielding fact.
- **Halving GCR:** reducing water-sphere equivalent dose to 200 mSv/yr (less than half the dry-surface value of ~400+ mSv/yr in that metric) needs **≈1.26 m of andesite rock (ρ = 2.8 g/cm³, ≈350 g/cm²)**; sandstone 1.64 m.
- **To ≤100 mSv/yr: 0.8–2.5 m depending on composition** — hydrogen (water ice) content dominates effectiveness: 10–50 wt% water regolith shields far better (moderates the <10 MeV neutron flux); even *unexcavated* 10% subsurface water cuts above-ground equivalent dose ~36%.
- Zhang et al. 2022 (*JGR Planets*, 2021JE007157): 1–1.6 m regolith for the 100 mSv/yr threshold at representative low-elevation sites.
- **Metric mismatch warning:** RAD's 0.64 mSv/d (thin silicon detector, point dose equivalent) is *not* the same observable as Röstel's water-sphere equivalent dose (~1.1–1.3 mSv/d surface in dry scenarios) or ICRP effective dose (Martinez-Sierra 2024 finds effective dose ≈ 30% *lower* than RAD-style estimates once body self-shielding is included). A dose tracker must pick one metric (recommend: effective dose, with RAD as validation anchor) and apply consistent conversion factors; sources genuinely disagree at the ±30–50% level here.

### Shielding — SPE and shelter
- Mars's atmosphere alone (≥23 g/cm² vertical, much more at slant angles) removes most SPE protons <150 MeV; surface SPE doses are small (largest RAD-observed surface SPE, 2017-09-10: transient ~doubling of dose rate for <1 day). SPE shelters are principally a **transit** requirement.
- Transit storm shelter: **5 g/cm² Al is insufficient for BFO limits in an Aug-1972-class event; ~10 g/cm² Al meets 30-day BFO limits; ≥20 g/cm² (esp. hydrogenous material/water) gives comfortable margin for extreme (Carrington-class) events** (Kim et al. 2006; Townsend et al. 2018, *Life Sci. Space Res.* 17:32-39, PMID 29753411). Parameter baseline: 20 g/cm² dedicated shelter (10–40 range), occupancy up to 24–36 h/event, warning time minutes–hours.
- On surface, designating a ≥200 g/cm² regolith-covered module as shelter satisfies SPE needs trivially.

### Dose bookkeeping example (conjunction mission, unmitigated)
360 d transit × 1.84 + 500 sols × 0.66 ≈ 662 + 330 ≈ **1.0 Sv ≈ 165% of the 600 mSv career limit** — radiation is a *binding* constraint; the sim should treat transit shielding, surface habitat cover, and solar-cycle timing as first-class trade variables. (Transit at solar max and ~20 g/cm² hydrogenous shielding plausibly cuts transit dose 20–40%; buried surface habitat cuts surface dose 50–80%.)

## 5. Medical event rates

- **All clinical events (mostly minor):** **3.4 events per person-flight-year** on ISS (skin rash 1.12/py, URI 0.97/py leading; Crucian et al. 2016, "Incidence of clinical symptoms during long-duration orbital spaceflight", *Int. J. Gen. Med.* 9:383-391).
- **Infectious events:** ~0.6/person-year (46 ISS long-duration missions).
- **Medical evacuation-class events:** IMM-predicted **0.017 events/person-year** (ISS context; none has actually occurred).
- **Mars projections (IMM v4.1, 4 crew, 1,195 d):** hundreds of (mostly minor) medical events per mission; probability of an evacuation-level condition >10% even with full ISS-level capability; **loss-of-crew-life from medical causes has a ~0.01 floor** per mission (Antonsen et al. 2022, *npj Microgravity* 8:8).
- Labor coupling: routine health checks/private medical conferences are already inside the ISS 6.5 h ops block; a significant medical event costs both the patient's labor (days–weeks) and a caregiver fraction (~0.5–2 h/d, estimate).

## 6. Disagreements & gaps between sources

1. **Sleep:** scheduled 8.5 h vs measured 6.1 h (Barger). Use 8.5 h in the schedule; do not harvest the gap as labor.
2. **Radiation metric:** RAD point dose equivalent vs water-sphere equivalent dose vs ICRP effective dose differ by ±30–50%; Röstel's "surface ≈400 mSv/yr" and RAD's 233 mSv/yr are *both correct* in their own metrics.
3. **Maintenance scale-up:** Russell & Klaus (3.0–3.3 h/d/habitat ECLSS) vs Stromgren corrective-only 0.44 h/CM-sol expected — overlapping but differently-scoped categories (preventive+corrective per habitat vs corrective per CM). MIT's Mars One analysis argues maintenance/spares dominate at settlement scale; NASA models assume ISS-class reliability. Treat preventive as per-habitat fixed, corrective as per-CM stochastic.
4. **EVA overhead:** ISS actuals (~74 crew-h/EVA total overhead) vs planetary design targets (1–3 h/CM). The two-order-of-magnitude gap is real and unproven; sensitivity-test it.
5. **NHV:** 25 m³/p is an SME consensus floor, not a validated requirement; NASA-STD-3001 requires task-based derivation. Settlement-scale (permanent) habitation plausibly needs substantially more (no data).

## 7. Fidelity tiers (recommended model forms)

### L0 — single scalar averages
- Labor: `available_work_h_per_cm_sol = VST/7 × 1.0275 ≈ 10.2`, of which productive (after housekeeping, food prep, expected maintenance, EVA overhead amortization) ≈ **8 h/CM-sol on workdays, 5 workdays per 7-sol week** → ≈ 5.8 productive h/CM-sol averaged. Science/utilization sanity anchor: NASA integrated model got only ~8.5% of gross crew time for utilization on a 460-sol stay.
- Radiation: `dose_mSv = 1.84·d_transit + 0.66·S·sols_surface`, S = habitat shielding factor (1.0 unshielded, ~0.5 for 1.3 m dry regolith, ~0.2–0.3 for >2 m or water-rich cover), tracked against 600 mSv career.
- Medical: Poisson, λ = 3.4 minor + 0.017 evac-class /person-yr.

### L1 — analytic daily/seasonal models
- Labor: 7-sol repeating week (5 work + 2 rest), per-category liens from Table 3-28 (sol-scaled); stochastic corrective maintenance drawn from a right-skewed distribution (lognormal fit to {50%: 0.16, mean 0.44, 99%: 1.26} h/CM-sol); EVA campaigns consume 24 h/CM/wk max with 1.5 h/CM overhead + consumables per sortie; food-prep lien slides 0.17→0.83+ h/CM-sol with grown-food fraction; 30-sol reconditioning after landing (no EVA).
- Radiation: `D_surface(t) = D₀·[1 + 0.2·M(t)]·A(p(t))·S`, with M(t) = solar-cycle modulation (±20%, 11-yr, anti-correlated with sunspots), A(p) = atmospheric column correction (~ -2%/(g/cm²) around 23 g/cm² for dose equivalent, sign per RAD pressure anti-correlation), S = shielding transfer factor from a depth-lookup of Röstel Fig. 6/Table 3 (include the 0–200 g/cm² buildup hump: S(d) rises to ~1.1–1.25 at ~90 g/cm² before falling). SPEs: Poisson (λ ≈ 2–6 significant events/yr near solar max, ~0.2–1/yr at min), transit dose/event lognormal (1–100+ mSv behind 10 g/cm²), surface dose/event ~1–2 orders lower; shelter occupancy consumes crew time (12–36 h/event, all hands).
- Medical: category-split Poisson rates (rash/URI/injury/etc. per Crucian), each event drawing labor loss for patient and caregiver.

### L2 — physics-based sub-sol models
- Radiation: transport GCR spectra (Badhwar-O'Neill or DLR/Matthiä model, modulation parameter Φ(t) from sunspot proxy) through atmosphere column p(t, sol, season, site elevation from MCD) + habitat mass distribution with HZETRN/OLTARIS or GEANT4-derived response matrices; score ICRP effective dose on phantoms; diurnal pressure tide gives ~±5% sub-sol dose modulation (RAD-observed). Validate: 0.64 mSv/d at Gale, Φ ≈ 580 MV, 840 Pa. SPE: sample historical proton fluence spectra (Aug 1972, Oct 1989, Sep 2017), Weibull rigidity attenuation through slant column vs zenith.
- Labor: discrete-event scheduler with per-task durations, precedence, circadian constraints (sleep anchored to local solar time, performance modifier from sleep debt per Barger/SAFTE-class models), EVA prebreathe/don-doff/dust-servicing as explicit timeline blocks, stochastic component failures (MTBF catalogs) generating corrective tasks with crew-skill matching — i.e., the ECTM/EMAT architecture (Stromgren 2015/2021).
- Medical: IMM-style Monte Carlo over a condition list with per-condition incidence, treatability given carried medical capability mass, outcomes feeding labor and (rarely) crew-loss states.

## 8. Sources (primary)

1. NASA BVAD: *Life Support Baseline Values and Assumptions Document*, NASA/TP-2015-218570 Rev 2, Feb 2022 (NTRS 20210024855) — Tables 3-28, 3-29, 4-69/70/71, §4.5.3.
2. Hassler, D.M. et al. (2014), "Mars' Surface Radiation Environment Measured with MSL's Curiosity Rover", *Science* 343:1244797.
3. Zeitlin, C. et al. (2013), "Measurements of Energetic Particle Radiation in Transit to Mars on MSL", *Science* 340:1080-1084.
4. NASA-STD-3001 Vol 1 Rev B (2022) + OCHMO Radiation Protection Technical Brief; NAS 2021 report *Space Radiation and Astronaut Health*.
5. Whitmire, A. et al. (2015), *Minimum Acceptable Net Habitable Volume for Long-Duration Exploration Missions*, NASA/TM-2015-218564 (consensus report NTRS 20140016951).
6. Röstel, L., Guo, J., Banjac, S., Wimmer-Schweingruber, R., Heber, B. (2020), "Subsurface Radiation Environment of Mars and Its Implication for Shielding Protection of Future Habitats", *JGR Planets* 125:e2019JE006246.
7. Zhang, J. et al. (2022), *JGR Planets* e2021JE007157 (regolith depth vs 100 mSv/yr).
8. Guo, J. et al. (2021), "Radiation environment for future human exploration on the surface of Mars", *Astron. Astrophys. Rev.* 29:8.
9. Stromgren, C. et al. (2015), "Developing a Crew Time Model for Human Exploration Missions to Mars", IEEE Aerospace (NTRS 20150009469).
10. Stromgren, C., Lynch, C., Cho, J., Cirillo, W., Owens, A. (2021), "Assessment of Crew Time for Maintenance and Repair Activities for Lunar Surface Missions", IEEE Aerospace (NTRS 20210026843).
11. Barger, L.K. et al. (2014), "Prevalence of sleep deficiency and hypnotic use in astronauts", *Lancet Neurology* 13:904-912.
12. Russell, J.F., Klaus, D.M. (2007), "Maintenance, reliability and policies for orbital space station life support systems", *Reliab. Eng. Syst. Saf.* 92:808-820.
13. Crucian, B. et al. (2016), "Incidence of clinical symptoms during long-duration orbital spaceflight", *Int. J. Gen. Med.* 9:383-391 (PMID 27843335).
14. Antonsen, E. et al. (2022), "Estimating medical risk in human spaceflight", *npj Microgravity* 8:8 (PMC8971481).
15. Griffin, B. (2020), "The Wait-less EVA Solution: Single-Person Spacecraft", AIAA 2020-4170 (EVA overhead per NASA EVA-EXP-0031; ISS EVA gas usage).
16. Abercromby, A.F.J. et al. (2015), "Modeling a 15-min EVA prebreathe protocol using NASA's exploration atmosphere (56.5 kPa/34% O₂)", *Acta Astronautica* 109:76-87.
17. Townsend, L.W. et al. (2018), "Solar particle event storm shelter requirements for missions beyond low Earth orbit", *Life Sci. Space Res.* 17:32-39; Kim, M.-H.Y. et al. (2006), FLUKA/phantom SPE shielding studies.
18. Broyan, J.L. et al. (2008), "ISS USOS Crew Quarters Development", 08ICES-0222 (NTRS 20080013462).
19. Martinez-Sierra, L. et al. (2024), "Effective dose equivalent estimation for humans on Mars", arXiv:2409.02001.
