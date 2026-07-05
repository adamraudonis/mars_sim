# Hardware Reliability & Spares Logistics for a Mars Base

**Domain key:** `reliability`
**Date:** July 2026
**Purpose:** Parameter database + model-form recommendations for the stochastic failure / spares-logistics engine of a Mars habitat/settlement trade-study simulation.

Time conventions: 1 sol = 88,775 s = 24.6597 h; 1 Mars year = 668.6 sols. MTBF values are kept in **operating hours** (the native unit of all NASA reliability datasets); divide by 24.6597 to get sols of continuous operation.

---

## 1. What flight experience actually says

### 1.1 ISS maintenance crew time (the best available analog)

| Quantity | Value | Source |
|---|---|---|
| Scheduled **maintenance** crew time, ISS average Oct 2017–Dec 2023 (OPTimIS dataset) | **6.0 h per crewmember-week** | Lynch et al., ICES-2024-132, Table 4 |
| Adjacent "Routine Ops" (upkeep/housekeeping) | 4.5 h/CM-wk | ICES-2024-132, Table 4 |
| Logistics handling (stowage, inventory) | 2.3 h/CM-wk | ICES-2024-132, Table 4 |
| **ECLSS-only** maintenance & repair, whole station | **13–15 h/week** (design documents predicted ~1 h/week — an order-of-magnitude underestimate) | Owens et al., AIAA 2017-5124, citing Bagdigian et al., ICES-2015-094 |
| Planned IVA maintenance, long-term average (planning era) | 2,536 crew-h/yr (plus 421 EVA, 777 EVR crew-h/yr) | Patterson, NASA JSC, NTRS 20100042525 |
| ISS Increments 4–8: maintenance time per crewmember | 1.9 h/workday, 1.8 h/rest day; ECLSS maintenance exceeded design estimates by 60.4% in analyzed categories | Russell & Klaus, Rel. Eng. & Sys. Safety 92(6), 2007 |

Cross-check: 6.0 h/CM-wk × ~6.5 crew ≈ 39 crew-h/wk station-wide scheduled maintenance, consistent with the 2,536 h/yr (≈49 h/wk) planning figure once corrective peaks and Russian-segment work are included. For a Mars sim, maintenance consumes roughly **10–20% of the nominal 40–48 h crew work-week per crewmember** (6.0/40 ≈ 15%), *before* communication-delay penalties (Mars adds ~22 min one-way light time; ICES-2024-95 estimates two ground consultations can cost ~1 h of a maintenance task).

### 1.2 Observed vs. predicted failure rates — the headline result

Two datasets, superficially in tension (see §6 Disagreements):

- **Jones, ICES-2019-14** (ECLSS ORUs, MADS pre-flight estimates vs ~8 yr of flight data): of 20 ORUs whose estimates changed, **14 of 20 (70%) were worse than predicted**. 7 of 20 had failure rates **1.9–6.3×** the estimate; **4 of 20 (all UPA ORUs) had failure rates 7.5–22×** the estimate. Had the original spares manifest been flown on a 450-day Mars transit, the UPA probability-of-insufficient-spares would have been **0.995** (the FCPA pump alone: 0.983).
- **Stromgren et al., AIAA 2016-5307** (all ISS USOS Criticality 1&2 components, Bayesian MADS updates through June 2016): of components whose MTBF was updated, **~85% were adjusted upward** (better than predicted) and **~15% downward**; ~3% ended at ≤0.25× the original MTBF, and ~14% of the improved ones ≥10×. Asymmetry matters: the small tail of much-worse components dominates system risk because their spares are the ones that run out.

Confidence-building is brutally slow: **~3× MTBF of accumulated operating time is needed for 95% confidence** in an MTBF estimate, ~5× for 99% (AIAA 2016-5307). Through 2016 only 15.8% of ISS Crit-1/2 components had >1.0× MTBF of accumulated time.

### 1.3 MTBF anchors for known problem hardware (flight data)

From Jones ICES-2019-14 (MADS estimate vs flight-derived MTBF, hours; flight values capped at 70,000 h = 8 yr no-failure):

| ORU | MADS est. (h) | Flight (h) | Flight (sols) | Ratio |
|---|---|---|---|---|
| UPA Fluids Control & Pump Assembly (FCPA) | 22,759 | **1,000** | 41 | 22.8× worse |
| UPA Pressure Control & Pump Assembly (PCPA) | 59,221 | **3,000** | 122 | 19.7× worse |
| UPA Separator Plumbing Assembly (SPA) | 88,993 | **4,000** | 162 | 22.2× worse |
| UPA Distillation Assembly (DA) | 41,376 | **5,500** | 223 | 7.5× worse |
| OGA Pump ORU | 189,433 | **35,000** | 1,419 | 5.4× worse |
| OGA Hydrogen Dome | 29,551 | 35,000 | 1,419 | ~same |
| OGA H₂ Sensor | 57,803 | **2,600** | 105 | 22× worse (life-limited) |
| OGA ACTEX recirc. bed | — | 11,000 | 446 | life-limited item |
| WPA Catalytic Reactor | 27,077 | **14,000** | 568 | 1.9× worse |
| WPA Pump Separator | 39,429 | 70,000 | 2,839 | better |

Current (2025) MADS-derived dataset (Chen, ICES-2025-127, Appendix Tables 2–11) spans **MTBF ≈ 8.2×10³ h (MCA mass spectrometer) to 2×10⁸ h (passive vents)** across ~90 ECLSS ORUs; the median "process" ORU sits near **1×10⁵ h**. Note the 2025 MADS values for the UPA pumps (e.g., FCPA 22,759 h) reflect post-redesign bookkeeping and *do not* show the Jones flight-derived degradation — see §6. A 2025 review (ScienceDirect, S2950616625000452) summarizes ISS-heritage hardware as: pumps/rotating machinery 5,000–20,000 h; **CDRA valves and heaters 800–1,500 h**; catalytic oxidizers and filters >10,000 h.

Repair times: ICES-2025-127 MTTR values run 0.3–17.7 h with median ≈ 1.5 h (tool time); major-ORU R&R crew time including access runs **1–19 h, typically 2–5 h** (its Table 1: CDRA blower/precooler 6 h, OGA hydrogen dome 14 h, most others 2–4 h), *excluding* gather/stow overhead.

---

## 2. The sparing-to-availability math NASA actually uses

### 2.1 Demand model (L1 canonical form)

Failures of ORU *i* are modeled as a Poisson process with annual rate (ISS program formula, ICES-2025-127):

```
λ_i [1/yr] = DutyCycle_i × Quantity_i × K_i × 8760 / MTBF_i
```

- `DutyCycle` = fraction of calendar time operating (non-operating hardware is conventionally assumed not to fail — an explicit, optimistic assumption; ICES-2024-95).
- `K_i` = **K-factor ≥ 1**, multiplying the intrinsic rate to capture *induced* failures (crew error, environment, false alarms, damage from other systems). Flight-derived K-factors in the 2025 MADS tables run **1.01–3.26, median ≈ 1.2** (e.g., WPA pump/separator 3.26, avionics air assembly 3.2, SPA 2.63, most items 1.0–1.4).

Probability of sufficiency (POS) for n_i spares over mission time T (cold spares, Poisson):

```
POS_i(n_i) = Σ_{x=0}^{n_i} (λ_i T)^x e^{-λ_i T} / x!
System POS = Π_i POS_i(n_i)
```

Because system POS is a product over N ≈ 100–300 repairable item types, **per-item POS must vastly exceed the system target**: a 0.99 system POS over 150 items requires per-item POS ≈ 0.99^(1/150) ≈ **0.99993**. This is why Mars sparing is so mass-expensive and why single bad actors (UPA FCPA) destroy system POS.

### 2.2 Epistemic uncertainty layer (gamma-Poisson / negative binomial)

Failure-rate estimates are uncertain. NASA supportability practice (Owens 2019 MIT PhD; Vega et al., ICES-2024-95) treats λ as gamma-distributed, making the number of failures **negative binomial** ("gamma-Poisson"), parameterized by the point-estimate rate and an **Error Factor**:

```
EF = (95th percentile of λ) / (50th percentile),  lognormal convention (Anderson et al., AIAA 2012-5320)
σ_ln = ln(EF) / 1.645
```

ISS S&MA sparing works in (POS, confidence) pairs; historical ISS confidence targets ranged **85–99.5%** (AIAA 2012-5320). For Mars (no resupply), plan at **POS ≥ 0.99 at ≥ 75–95% confidence** per critical system.

### 2.3 Optimization

Spares allocation = marginal analysis: repeatedly add the spare with the greatest ΔPOS/Δkg until the system POS target is hit (EMAT, Stromgren et al. AIAA-2012-5323; identical logic in ICES-2024-95). Alternative closed forms: binomial per-day trials (ICES-2015-288) and semi-Markov analytic models (Owens & de Weck, ICES-2014-116) agree with Monte Carlo within a few percent for series systems.

### 2.4 Availability (for continuous-process equipment)

```
A_i = MTBF_i / (MTBF_i + MDT_i)
```

MDT = MTTR + logistics/crew-availability delay. With spares on-shelf and median MTTR ≈ 1.5–3 h against MTBF ≈ 10⁴–10⁶ h, hardware availability is ≥0.999 **as long as a spare and crew time exist** — on Mars, availability risk is dominated by stock-out (POS) and by *crew time contention*, not by repair duration. Buffer sizing (O₂, H₂O tankage) must cover the outage tail, including the case where the failure occurs during sleep or EVA and diagnosis takes ground-loop time.

---

## 3. Spares mass: the anchor numbers

| Case | Value | Source |
|---|---|---|
| ISS spares posture (LEO, resupply available) | >13,000 kg on-orbit + ~18,000 kg on ground = **>31,000 kg** staged | Owens & de Weck, AIAA 2016-5394 |
| Fraction of ISS corrective spares never expected to be used | >95% | AIAA 2016-5394 / 2017-5124 |
| Mars transit DSH (≈4 crew, 1,100 d, critical systems incl. ECLSS, TCS, EPS, C&DH, comm): spares for R=0.99, deterministic MTBFs | **5,984 kg** | Stromgren et al., AIAA 2016-5307 |
| Same, holding 0.99 at 95th-pct confidence, "low uncertainty" bound | **>12,000 kg** | AIAA 2016-5307 |
| Same, "high uncertainty" bound | **>17,000 kg** | AIAA 2016-5307 |
| Mars transit DSH: manifest for median P(LoC contribution)=10⁻³ incl. MTBF uncertainty | 17,232 kg; +4,124 kg per additional 10× risk reduction | AIAA 2016-5394 |
| Historical heuristic (Salyut/Mir/ISS): spares per year | **5% of system dry mass/yr** (Larson & Pranke, quoted in ICES-2015-288); for a 20,000 kg 4-crew transit hab ⇒ ~1,000 kg/yr | ICES-2015-288 |
| Mars settlement steady state (Mars One assessment, ISS-derived failure rates, growing base) | spares = **62% of all mass shipped from Earth by month 130** | Do et al., Acta Astronautica 120, 2016 |
| ECLS spares share of ECLS logistics, long endurance | >50% | AIAA 2016-5394 (citing EMC analysis) |

**Normalized planning rates** (derived): deterministic-POS-0.99 sparing of a 20 t habitat ≈ 5,984 kg/3.01 yr ≈ **10% of supported dry mass per Earth-year** (≈5.3%/Mars-year × 2 — careful with units); with epistemic uncertainty at high confidence this rises to **20–28%/yr**. The classical 5%/yr heuristic is a *lower bound* appropriate only with ISS-grade resupply and risk posture.

---

## 4. Mitigations: commonality, cannibalization, ISM

- **Commonality/reconfigurability:** same availability with **33–50% fewer spares** when parts are common across mission elements (Siddiqi & de Weck, J. Spacecraft & Rockets 44(1) 2007, quoted in Jones ICES-2015-044). Caveat (Jones, ICES-2017-83): commonality concentrates common-cause exposure — a design defect propagates to every copy; "in space, diversity trumps commonality" for *new* designs.
- **Cannibalization:** shared spares pools across co-located habs/rovers behave like commonality (Jones ICES-2015-044 shows shared cold spares across 5 crews cut required spares ~50%). Harvesting parts from failed ORUs (sub-ORU level of repair) is credited in NASA studies but unquantified in flight; model as a recoverable fraction ~0.1–0.5 (speculative).
- **In-Space Manufacturing (3D printing):** with ~33% of MEL line items printable from common feedstock (fluid-flow class: plumbing, ducting, fans, tanks, valves), maintenance-logistics mass falls **28%** (17,232 → 12,323 kg); adding feedstock recycling: **34%** (→11,254 kg). For the *covered items alone*, feedstock replaces spares at **78.3% mass reduction** (97.7% with recycling). ISM also cuts the *sensitivity* to MTBF uncertainty (median P(LoC) contribution 0.027 → 0.0167 at 33% coverage, → 0.0084 at 50%). (All: Owens & de Weck, AIAA 2016-5394.) Production-rate feasibility: AM processes span **0.01–10 kg/h**; printable set is limited by time-to-effect vs manufacture time (Moraguez & de Weck, ICES-2020). Packaging overhead: 1.5% for conventional spares, ~0.015% for feedstock.

---

## 5. Recommended stochastic model per equipment class

ISS program practice assumes constant failure rate (exponential; memoryless) for random failures, with life-limited items handled by deterministic replacement clocks and K-factors for induced failures (ICES-2025-127). For an L2 engine, use Weibull hazard `h(t) = (β/η)(t/η)^(β-1)`:

| Equipment class | Distribution | Shape β | Basis / confidence |
|---|---|---|---|
| Electronics, controllers, sensors, avionics | Exponential (β=1.0) | 1.0 | ISS CFR assumption, ICES-2025-127 — medium |
| Pumps, fans, blowers, compressors, distillation drives (rotating) | Weibull, wear-out | 1.5–3.0 (rec. 1.8) | generic rotating-machinery reliability practice (Ebeling; OREDA-type data); ISS pump flight history — low |
| Valves, mechanisms, actuators | Weibull | 1.0–2.0 (rec. 1.3) | mixed wear/random; CDRA valve dust-driven wear — low |
| Sorbent beds, membranes, filters, softgoods, cell stacks | Deterministic life limit / throughput limit + small random term | n/a (+β≈2–3 wear if modeled) | MADS tracks these by shelf life/throughput, not MTBF — medium |
| Newly manufactured/installed units (first 500–1,000 h) | Weibull infant mortality | 0.6–0.9 | bathtub curve; "first model year" effect, AIAA 2016-5307 — speculative |
| Common-cause failures across identical units | Beta-factor model on top of independent failures | β_CCF ≈ 0.01–0.10 (rec. 0.05) | generic PRA (NUREG/CR-5485-class values); OGA cell-stack, CDRA dust, UPA CaSO₄ are documented ECLSS CCFs (Jones ICES-2019-14) — speculative |

Sampling recipe (L1/L2): draw λ_i ~ Lognormal(median = K_i/MTBF_i, EF_i); within a replication, generate failures as Poisson(λ_i·DC_i·t) (L1) or Weibull renewals with matched mean (L2); apply CCF beta-factor; apply repair only if spare + crew-hours available.

---

## 6. Disagreements & pitfalls between sources

1. **"85% got better" (Stromgren) vs "70% got worse" (Jones).** Different populations and estimators. Stromgren: *all* USOS Crit-1/2 ORUs with Bayesian updates (initial estimates were conservative for most avionics-like items). Jones: *regenerative ECLSS* ORUs only, using raw observed lifetimes, where fluid-handling hardware genuinely underperformed. For a Mars sim: apply the Stromgren distribution station-wide, but bias ECLSS/fluid-handling classes toward the Jones tail (this is exactly what the error-factor + class-specific K approach achieves).
2. **2025 MADS tables vs Jones flight-derived MTBFs** (e.g., FCPA 22,759 h vs 1,000 h). MADS reflects redesigned hardware and Bayesian smoothing; Jones reflects raw early-life flight experience including design-flaw epochs. Use MADS for mature-hardware baselines; use Jones ratios to build the "new hardware first deployment on Mars" uncertainty case.
3. **POS ≠ reliability.** All sparing math assumes independent random failures, perfect repair, no CCF, no design error. Jones ICES-2019-14 and Owens/Stromgren agree POS is an **upper bound** (Do 2016: "probability of having sufficient spares," a lower bound on risk). Add explicit CCF and unknown-unknown margins rather than inflating POS targets alone.
4. **Crew-time accounting varies**: OPTimIS "Maintenance" (6.0 h/CM-wk) excludes some upkeep captured in "Routine Ops"; Russell & Klaus counted differently (per-day per-CM); design documents historically undercounted by 10×+ (ECLSS) and 60% (category coverage). Prefer the 2024 OPTimIS numbers, carry ±35% uncertainty.
5. **Dormancy**: standard models assume non-operating systems don't fail and all dormancy failures are repairable on crew return (ICES-2024-95) — both optimistic; a Mars settlement sim with pre-deployed assets should stress-test both assumptions.

---

## 7. Fidelity tiers

### L0 — single-scalar averages (campaign sizing)
- Spares upmass = **0.10 × (supported dry mass) per Earth-year** (range 0.05 deterministic-optimistic → 0.28 high-confidence/high-uncertainty). Apply ×(1−0.28) if ISM(33%) is bought; ×(1−0.35) commonality across co-located elements.
- Maintenance crew time = **6.0 h/CM-week** (+4.5 h/CM-wk routine upkeep), i.e., ~15% of work time.
- System hardware availability = 0.99 (spares assumed sufficient).

### L1 — analytic probabilistic model (per-subsystem, per-resupply-window)
- Per ORU class *i*: λ_i = DC_i·Q_i·K_i/MTBF_i; failures over window T ~ NegBinomial via λ ~ Lognormal(EF_i).
- Spares by marginal-ΔPOS/Δkg greedy allocation to system POS target (0.99 baseline, 0.999 crew-critical) at chosen confidence (75–95%).
- Crew time = Σ failures × (R&R time sampled 1–19 h, median 3 h + 30–60 min Mars ground-loop overhead) + scheduled PM from life-limit clocks (MTBPMRR tables). Flag any week where maintenance demand > crew work-opportunity time (LoM precursor per ICES-2024-95 Figure 3 logic).
- Seasonality hook: dust season modifies K for EVA-exposed and thermal-cycled equipment (no flight basis — sensitivity parameter).

### L2 — physics-informed sub-sol discrete-event engine
- Per-ORU renewal processes: Weibull(β class-dependent, η matched to K/MTBF); infant-mortality β<1 segment for newly installed/printed units; deterministic throughput/life-limit clocks for beds/filters/bladders (e.g., BPA bladder: 24 L brine/26 d, 35-wk disposal clock).
- Failure draw → functional-state propagation through reliability block diagram (series ECLSS strings, k-of-n fans, cold-spare switchover) → buffer drawdown ODEs (O₂ partial pressure, H₂O tanks) against MTTR + diagnosis + crew-schedule queue (crew asleep 8.5 h/sol; EVA days block IVA repair) → LoC clock if buffer empties.
- CCF via beta-factor across identical units; cannibalization queue (failed ORUs yield recoverable sub-parts with p≈0.3); ISM job-shop (feedstock inventory, 0.01–10 kg/h rate, printer itself has MTBF); spares mass/volume ledger per launch window.
- Governing equations: Poisson/NB demand (§2), Weibull hazard (§5), availability A=MTBF/(MTBF+MDT), POS product rule, buffer ODE dB/dt = production − consumption with production gated by functional state.

---

## 8. Parameter table (machine-readable copy in reliability.json)

See `reliability.json`. Confidence legend: high = flight data or primary NASA analysis; medium = single study or planning value; low = engineering estimate anchored to data; speculative = expert-judgment placeholder.

---

## 9. Key findings

1. ISS crews average **6.0 h/CM-week of scheduled maintenance** (2017–2023 flight data); ECLSS alone consumes 13–15 h/wk station-wide vs ~1 h/wk predicted at design — plan Mars crew time accordingly (~15% of work time, with upside risk).
2. Failure-rate prediction error is the dominant sparing risk: ECLSS fluid-handling ORUs ran **1.9–22× worse** than estimates; four UPA ORUs alone would have made a 450-day mission 99.5% likely to run out of spares under the original manifest.
3. Poisson/negative-binomial POS math with per-item targets ≥0.9999 (for 0.99 system POS over ~150 items) is the NASA-standard approach; Mars missions need POS ≥0.99 at high confidence *with epistemic uncertainty carried explicitly* (lognormal error factor, gamma-Poisson demand).
4. Spares mass anchor: **~6 t for a 4-crew, 1,100-day Mars transit habitat at R=0.99 with perfect knowledge; 12–17 t once MTBF uncertainty is included** — roughly 10%→30% of supported dry mass per year, vs the 5%/yr historical heuristic.
5. At settlement scale, spares dominate logistics: **62% of all Earth-sourced mass by month 130** (Mars One assessment) — reliability growth, commonality (−33–50% spares), ISM (−28–34% maintenance mass; −78% on covered items), and sub-ORU repair are the only levers that bend this curve.
6. For the failure engine: exponential (β=1) for electronics per ISS practice; Weibull β≈1.8 (1.2–3.0) for rotating machinery; life-limit clocks for beds/filters; K-factor 1.0–3.3 (median 1.2) for induced failures; beta-factor ~0.05 for common cause; error factor ~3 (2–10) on all new-hardware failure rates.

---

## 10. Sources

1. Jones, H. W., "High Reliability Requires More Than Providing Spares," ICES-2019-14, 2019. https://ntrs.nasa.gov/citations/20190027319
2. Stromgren, C., Goodliff, K., Cirillo, W., Owens, A., "The Threat of Uncertainty – Why Using Traditional Approaches for Evaluating Spacecraft Reliability Are Insufficient for Future Human Mars Missions," AIAA 2016-5307. https://ntrs.nasa.gov/citations/20160011578
3. Owens, A. C., de Weck, O. L., "Systems Analysis of In-Space Manufacturing Applications for the International Space Station and the Evolvable Mars Campaign," AIAA 2016-5394. https://ntrs.nasa.gov/citations/20160011570
4. Owens, A. C., de Weck, O. L., Stromgren, C., Cirillo, W., Goodliff, K., "Supportability Challenges, Metrics, and Key Decisions for Future Human Spaceflight," AIAA 2017-5124. https://ntrs.nasa.gov/citations/20170009115
5. Anderson, L., Carter-Journet, K., et al., "Challenges of Sustaining the International Space Station through 2020 and Beyond: Including Epistemic Uncertainty in Reassessing Confidence Targets," AIAA 2012-5320. https://ntrs.nasa.gov/citations/20120014060
6. Vega, J. M., Kulikowski, J. D., Drake, A., Stromgren, C., Lynch, C. S., Owens, A. C., Piontek, N. E., Cirillo, W. M., "Modeling Logistics and Supportability for Crewed Missions Beyond Low Earth Orbit," ICES-2024-95. https://ntrs.nasa.gov/citations/20240005642
7. Lynch, C. S., Owens, A. C., Piontek, N. E., Cirillo, W., Stromgren, C., Vega, J., Kulikowski, J., "A Historical Review of Logistics Mass and Crew Time Demands for ISS Operations," ICES-2024-132. https://ntrs.nasa.gov/citations/20240005648
8. Chen, L., "Advancing ECLSS Reliability Modeling: Integrating ISS Data for Sustainable Long-Duration Mission Planning," ICES-2025-127. https://ntrs.nasa.gov/citations/20250003955
9. Patterson, L. P., "On-Orbit Maintenance Operations Strategy for the International Space Station – Concept and Implementation," NASA JSC. https://ntrs.nasa.gov/citations/20100042525
10. Owens, A., de Weck, O., Mattfeld, B., Stromgren, C., Cirillo, W., "Comparison of Spares Logistics Analysis Techniques for Long Duration Human Spaceflight," ICES-2015-288. https://ntrs.nasa.gov/citations/20160006509
11. Do, S., Owens, A., Ho, K., Schreiner, S., de Weck, O., "An independent assessment of the technical feasibility of the Mars One mission plan – Updated analysis," Acta Astronautica 120 (2016) 192–228. https://dspace.mit.edu/handle/1721.1/103973
12. Jones, H. W., "Comments on the MIT Assessment of the Mars One Plan," ICES-2015-044. https://ntrs.nasa.gov/citations/20160001251
13. Russell, J. F., Klaus, D. M., "Maintenance, reliability and policies for orbital space station life support systems," Reliability Engineering & System Safety 92(6), 2007, 808–820. https://www.sciencedirect.com/science/article/abs/pii/S0951832006001050
14. Siddiqi, A., de Weck, O. L., "Spare Parts Requirements for Space Missions with Reconfigurability and Commonality," Journal of Spacecraft and Rockets 44(1), 2007 (as quoted in ICES-2015-044).
15. Ewert, M. K., Chen, T. T., Powell, C. D., "Life Support Baseline Values and Assumptions Document," NASA/TP-2015-218570/REV2, Feb 2022.
16. Moraguez, M., de Weck, O., "In-Space Manufacturing Production Rate and Reliability Targets for On-Demand Fabrication of ECLSS Spares," ICES-2020. https://ttu-ir.tdl.org/handle/2346/84476
17. "Toward sustainable living in space: A review of environmental control and life support system technologies," 2025. https://www.sciencedirect.com/science/article/pii/S2950616625000452
