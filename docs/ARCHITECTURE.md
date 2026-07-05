# Mars Habitat Sim — Architecture

A composable, multi-fidelity simulation of a Mars surface settlement built around SpaceX
Starship v3 logistics, designed for **trade studies**: solar vs nuclear, ISRU architectures,
sparing strategy, food closure, robotic labor, and launch-campaign design.

Not a game. Every default parameter is sourced (see `research/parameters_master.json`),
every value is tunable at runtime, and any subsystem can be swapped between fidelity levels.

---

## 1. Layering

```
┌────────────────────────────────────────────────────────────┐
│  MarsSim.Unity      3D base view, timelapse, charts, UI    │  (UnityEngine)
├────────────────────────────────────────────────────────────┤
│  MarsSim.Core       kernel + all physics/logistics models  │  (pure C#, no UnityEngine)
├────────────────────────────────────────────────────────────┤
│  research/          sourced parameter DB + scenario JSONs  │  (data)
└────────────────────────────────────────────────────────────┘
```

* **MarsSim.Core** is an `asmdef` with `noEngineReferences: true`. It can run headless
  (EditMode tests, `-batchmode` trade studies, or even outside Unity). It contains its own
  minimal JSON parser so it has zero engine dependencies.
* **MarsSim.Unity** owns presentation only: it *reads* engine state, never mutates physics.
* **Data** lives in `Assets/StreamingAssets/`: `parameters_master.json` (values + citations)
  and `scenarios/*.json` (mission definitions).

## 2. Simulation kernel

### Time
* Fixed timestep, default **1/24 sol** (3699 s, a "Mars hour"); configurable per scenario.
* `SimClock` tracks: sol number (mission elapsed), time-of-sol, areocentric solar longitude
  **Ls** (via Mars orbit propagation), Mars year, and maps sols → Earth UTC dates from the
  scenario epoch (1 sol = 88,775.244 s).

### Tick pipeline (each step)
1. **Environment** — sun elevation/azimuth, top-of-atmosphere flux, optical depth τ
   (climatology + stochastic storms), air/ground temperature, pressure.
2. **PreTick** — every module declares intent: power offers (generators), power demands
   (with `LoadPriority`), labor demands.
3. **PowerBus resolve** — allocation by priority; battery charges from surplus / discharges
   to cover deficit; unmet demand → modules run at reduced throughput this step
   (`GrantedFraction`), lowest priority shed first.
4. **LaborPool resolve** — crew-hours and robot-hours allocated to maintenance, ISRU ops,
   agriculture, construction, science by priority; robots substitute at a per-task
   effectiveness ratio.
5. **Tick** — modules exchange mass/energy with `Store`s and the `Habitat` atmosphere.
   All exchanges go through the store API so **mass conservation is auditable**.
6. **Failures** — `FailureEngine` samples component failures (per-class MTBF, seeded RNG),
   queues repairs (crew/robot hours + spares from inventory), applies degraded states.
7. **Record** — `History` samples every tracked series; `EventLog` collects discrete events
   (landings, failures, storms, milestones, warnings).

### Resources
Bulk commodities are `Store`s (mass-based, kg, with capacity): O₂, N₂, H₂, CH₄, LOX,
water (potable/wastewater), food (kg & kcal), biomass, spares (by equipment class),
regolith, propellant-grade cryo in depot tanks.
The **cabin atmosphere is state inside `Habitat`** (total pressure, ppO₂, ppCO₂, ppN₂,
temperature, humidity) — crew and ECLSS exchange gas with it directly, so the UI can chart
partial pressures the crew actually breathes.

Electricity is not a store; it is resolved instantaneously by the `PowerBus` each step,
with `BatteryBank` as the only energy buffer (round-trip efficiency, DoD limits).

### Determinism
One master seed per run; each module gets an independent xorshift stream keyed by module id.
Identical scenario + seed ⇒ identical history (required for Monte Carlo and regression tests).

## 3. Multi-fidelity design (the core idea)

Every module implements its behavior at up to three levels, selected per-module in the
scenario (or live from the UI):

| Level | Meaning | Example (solar) |
|-------|---------|-----------------|
| **L0 — Distilled** | single averaged coefficients | `kWh/sol per kW installed` × installed kW |
| **L1 — Analytic** | daily/seasonal closed-form | Appelbaum–Flood daily insolation vs Ls, τ climatology, dust accumulation |
| **L2 — Physics** | sub-sol resolved mechanism | sun-angle geometry per step, direct+diffuse split vs τ, panel temperature coefficient, stochastic storm process, cleaning events |

**Distillation** makes the fidelity ladder round-trip: `Distiller` runs any single module at
L2 over N sols in an isolated sub-simulation, regresses the L0/L1 coefficients (e.g., mean
kWh/sol/kW and its seasonal amplitude), writes them into the `ParameterRegistry` as an
override layer, and reports the fit error. Workflow: *explore at L2 → distill → run
campaign-scale studies at L0 → drill back down when a parameter turns out to matter.*

## 4. Parameters

`ParameterRegistry` loads `parameters_master.json`. Each `Param` carries:
`id`, `name`, `value`, `unit`, `range`, `source` (citation), `source_url`, `confidence`,
`notes`. Layered overrides: **database → scenario → UI edits → distillation results**, all
non-destructive and inspectable. Modules never hard-code numbers; they resolve
`Params.Get("power_solar.array_specific_mass")` once at init and re-resolve on change.
The in-app Parameter Inspector shows the value *and its citation* side by side.

## 5. Modules (initial set)

| Module | What it models | Fidelity notes |
|--------|----------------|----------------|
| `MarsEnvironment` | orbit → Ls, insolation, τ (climatology + storm process), T_air/T_ground, pressure cycle | L1 analytic default; L2 adds diurnal detail |
| `SolarFarm` | installed kW, geometry, dust deposition & cleaning, temp derating | L0/L1/L2 |
| `NuclearPlant` | Kilopower/FSP-class units: rated kWe, mass, lifetime, thermal margin | L0/L1 |
| `BatteryBank` | kWh, DoD, round-trip η, fade | L0/L1 |
| `Habitat` | pressurized volume, cabin gas state, leakage, N₂ makeup, thermal load | L1 |
| `Crew` | metabolic O₂/CO₂/H₂O/kcal per activity level, schedule, EVA, radiation dose | L1 |
| `Eclss` | OGA electrolysis, CDRA CO₂ removal, Sabatier, water recovery %, TCC; power + maintenance burden per unit | L1; L0 = closure fractions |
| `Greenhouse` | crop batches (BVAD crop table): area, growth cycle, kcal yield, LED kWh, transpiration | L0 = kcal/m²/sol; L1 = per-batch growth |
| `FoodSystem` | packaged reserves + grown food, kcal accounting | L0 |
| `IsruPropellantPlant` | CO₂ capture → Sabatier + electrolysis (or imported H₂) → CH₄/LOX, liquefaction, kWh/kg, feed water | L0/L1 |
| `IceMine` | excavation/Rodwell water production: kWh/kg, labor, haul | L0/L1 |
| `PropellantDepot` | cryo tanks, boiloff %/sol, cryocooler power | L0/L1 |
| `StarshipFleet` | landed ships as habitats/tankage, return-propellant goal vs window | L0 |
| `RobotFleet` | Optimus count, duty cycle, charge power, work-rate ratio, own maintenance | L0/L1 |
| `LaborScheduler` | supply (crew, robots) vs demand (maintenance, ISRU, agriculture, construction, science); backlog effects | L1 |
| `MaintenanceSystem` + `FailureEngine` | stochastic failures per class, spares inventory, repair queue, availability | L1 (Monte Carlo capable) |
| `LaunchCampaign` | scenario flights: window dates, manifests; landing events instantiate modules/stores after assembly labor | L0 |

Adding a subsystem = subclass `SimModule`, declare stores/params, register in
`ModuleFactory`. Nothing else changes; UI, charts, failures, and trade runner pick it up
automatically (modules self-describe their tracked series and health).

## 6. Scenarios & launch campaign

Scenario JSON defines: site (lat/elevation), epoch, seed, timestep, per-module fidelity,
parameter overrides, **flights** (Earth-date or sol, ship count, manifest: kW solar,
reactors, ISRU plants, hab modules, food t, spares t, H₂ t, robots, crew), and policies
(propellant target & return window, load-shed order, diet closure target).
The default campaign follows current public SpaceX statements (uncrewed cargo at the
2028/2029 window in this sim's baseline, crew at the following window) and is trivially
editable — that schedule is itself a study output, not an assumption.

## 7. Trade studies

`TradeStudyRunner` (headless, EditMode or `-batchmode -executeMethod`): grid/list sweeps
over any parameter ids × N Monte Carlo seeds, running the full engine per point, exporting
tidy CSV (`studies/out/*.csv`) plus a summary of objective metrics (mass landed, kWh
produced, crew survival margin, propellant-by-window, spares shortfalls, dose).
Canned studies ship for: solar-vs-nuclear power architecture, ice-mining vs imported-H₂,
spares mass vs failure-rate uncertainty, food closure fraction, robot fleet size, and
launch-manifest feasibility.

## 8. Unity presentation

* **Timelapse**: `SimRunner` advances N sim-steps per frame (0.001–50 sols/s), decoupled
  from render rate. The base visibly grows: ships land, arrays unfurl (and dust over),
  greenhouses glow at night, storms darken the sky, crew/robot markers move between sites.
* **3D**: all meshes procedural (no third-party assets): terrain from layered noise with
  crater dressing, URP lit materials, physically-driven sun (elevation/azimuth/intensity
  from the environment model), fog/particles tied to τ.
* **UI (UI Toolkit)**: top bar (Earth date, sol, Ls, speed); dockable chart panel
  (multi-series vs sol: ppO₂, water, kWh, food days, propellant %, dose…); system health
  tree; event ticker; **Parameter Inspector** (search any param, edit live, see citation);
  fidelity switcher; scenario picker.

## 9. Validation

EditMode tests assert: mass conservation per resource across long runs, deterministic
replay, module-level anchors from the research DB (e.g., MOXIE-class specific energy,
BVAD metabolic rates, Starship refuel water/energy totals), and scenario smoke runs
(≥2000 sols) without NaNs or negative stores.

## 10. Known modeling limitations (accepted, documented)

Findings from the adversarial review that were triaged as acceptable at current fidelity —
listed so users know where the model floor is:

* **Sun geometry ignores the Martian equation of time** (±~50 min ≈ ±12° hour angle).
  Daily-integrated insolation is unaffected; instantaneous sunrise/sunset timing shifts.
* **Temperature/pressure seasonal wave phases are approximate** (single/double sinusoids
  vs Viking/REMS records; extrema within ~±30° Ls of observation). Amplitudes are sourced.
* **Crew radiation dose is a single cohort-average scalar**; it does not track individuals
  across crew rotations. Fine while crew stay; wrong for rotation studies.
* **Global-storm onset rate treats the Ls 180–330 season at the mean angular rate**;
  perihelion-speeding of Ls makes true storm-season sol-length ~10 % shorter.
* **Spares are pooled by equipment class in units**, so deposits/withdrawals with
  different per-unit masses can drift the aggregate spares-mass store (ledger stays
  consistent; unit counts are exact).
* **Habitat thermal is a power line-item, not a thermal network** — insulation/heater
  sizing is folded into the baseline W/m³ parameter.
* **Crew departure is not modeled** (a fueled return ship departs uncrewed in the shipped
  scenarios; add a `crew` field to a departure policy if rotation studies need it).
