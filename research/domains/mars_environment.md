# Mars Surface Environment — Parameter Research

**Domain key:** `env`
**Purpose:** the environment model that every other module (power, ECLSS, thermal, ISRU, EVA, agriculture) reads.
**Compiled:** July 2026. All values SI; energy in kWh where natural; time in sols (1 sol = 88,775 s = 24.6597 h; 1 Mars year = 668.6 sols).

---

## 1. Astronomical & orbital constants

| Quantity | Value | Source | Confidence |
|---|---|---|---|
| Sol (mean solar day) | 88,775.244 s = 24.6597 h = 1.0274912517 Earth days | Allison & McEwen (2000), GISS Mars24 | High |
| Mars year | 668.5991 sols = 686.9725 d | Allison & McEwen (2000) | High |
| Semi-major axis a | 1.523679 AU | NSSDC Mars Fact Sheet | High |
| Eccentricity e | 0.0934 | NSSDC Mars Fact Sheet | High |
| Obliquity ε | 25.19° | NSSDC Mars Fact Sheet; A&M 2000 (0.42565 = sin 25.19°) | High |
| Ls of perihelion | 251.0° (≈ southern-summer solstice at Ls 270°) | Allison & McEwen (2000) | High |
| Surface gravity | 3.711 m/s² (equatorial 3.69–3.73 with rotation/shape) | NSSDC Mars Fact Sheet | High |

Seasons are indexed by **areocentric solar longitude Ls**: Ls 0° = N-spring equinox, 90° = N-summer solstice, 180° = N-autumn equinox, 270° = N-winter solstice (= southern summer). Because perihelion (Ls 251°) nearly coincides with southern-summer solstice, **southern summer is short, hot, and dusty; aphelion (Ls 71°) makes northern summer long and clear.** Season lengths (in sols): Ls 0–90: 193.30; 90–180: 178.64; 180–270: 141.90; 270–360: 154.36.

---

## 2. Sun-position equations (the sim's astronomical core)

All from Allison & McEwen (2000) / NASA GISS Mars24 algorithm (accuracy ~0.01° in Ls over 1874–2127).

With Δt = (Julian Date TT) − 2451545.0 (days from J2000 epoch):

```
Mean anomaly:          M   = 19.3871° + 0.52402073° Δt
Fictitious mean sun:   αFMS = 270.3863° + 0.52403840° Δt
Equation of center:    ν − M = (10.691° + 3.0e-7 Δt) sin M + 0.623° sin 2M
                              + 0.050° sin 3M + 0.005° sin 4M + 0.0005° sin 5M  (+ minor planetary terms PBS)
Solar longitude:       Ls  = αFMS + (ν − M)                                  [mod 360°]
Equation of time:      EOT = 2.861° sin 2Ls − 0.071° sin 4Ls + 0.002° sin 6Ls − (ν − M)
                       (hours: EOT_h = EOT / 15°·h)
Coordinated Mars time: MTC = 24 h · frac[ (JD_TT − 2451549.5)/1.0274912517 + 44796.0 − 0.0009626 ]
Local mean solar time: LMST = MTC − Λ_W/15°·h     (Λ_W = west longitude)
Local true solar time: LTST = LMST + EOT_h
```

**Solar declination vs Ls** (this is the equation the sim should use for sun position at any lat/sol/time):

```
δ = arcsin(0.42565 · sin Ls) + 0.25° · sin Ls          [0.42565 = sin 25.19°]
```

**Hour angle and elevation:**

```
h  = 15° · (LTST − 12)                                  [degrees, + afternoon]
cos z = sin φ sin δ + cos φ cos δ cos h                 [z = solar zenith angle, φ = latitude]
```

**Sunrise/sunset hour angle and daylength:**

```
cos h_ss = − tan φ · tan δ
daylength = (2 h_ss / 360°) sols = (h_ss / 180°) · 24.6597 h  (true solar)
```

No sunrise when tan φ·tan δ < −1 (polar night), no sunset when > +1 (midnight sun); on Mars these occur poleward of |φ| = 64.81°.

**Heliocentric distance and top-of-atmosphere flux:**

```
ν (true anomaly) = Ls − 251°   [mod 360°]
r = a(1 − e²)/(1 + e cos ν) = 1.523679 · (1.00436 − 0.09309 cos M − 0.004336 cos 2M − …) AU
S_TOA(Ls) = S_mean · [ (1 + e cos ν) / (1 − e²) ]²
```

with **S_mean = 586.2 W/m²** (1361/1.523679² — modern TSI; Appelbaum & Flood used 1371 → 590 W/m²). Extremes: **~493 W/m² at aphelion, ~717 W/m² at perihelion** (Appelbaum & Flood 1990, NASA TM-102299) — a **±19% (45%) annual swing**, vs ±3.5% for Earth. This eccentricity effect is first-order for solar power: perihelion insolation boost coincides with the dust-storm season, which more than cancels it in dusty years.

**Direct beam at surface (Beer–Lambert):**

```
G_dir(normal) = S_TOA · exp(−τ / cos z)
G_horiz ≈ S_TOA · cos z · f(τ, z)
```

where f is the Appelbaum & Flood (1990) "normalized net flux function" including diffuse. Approximate f at z = 0 (dust single-scattering albedo ~0.93 keeps a large diffuse component alive at high τ): τ 0.4 → ~0.88; τ 1.0 → ~0.72; τ 2 → ~0.50; τ 3 → ~0.33; τ 5 → ~0.17. Even in the 1977 global storms ~15–25% of TOA flux reached the surface as diffuse light; Opportunity kept operating (barely) through τ ≈ 4.7 in 2007.

**Daily TOA insolation on a horizontal surface** (analytic, for L1):

```
H_sol = (88775/π) · S_TOA · [ h_ss sin φ sin δ + cos φ cos δ sin h_ss ]   [J/m², h_ss in rad]
```

Example: equator, Ls = 0: H ≈ 15.9 MJ/m² = **4.4 kWh/m²/sol** TOA; at τ = 0.4 the surface global horizontal is ~3.0–3.3 kWh/m²/sol.

---

## 3. Surface pressure — annual CO₂ condensation cycle

~25–30% of the atmosphere seasonally condenses onto/sublimes from the polar caps, producing a **double-humped annual pressure wave** (two minima/maxima; global, synchronous in Ls). Flight data:

| Site (elevation) | Annual min | Annual max | Timing | Source |
|---|---|---|---|---|
| Viking Lander 1 (−3.63 km) | ~680 Pa (Ls ≈ 145–155) | ~900 Pa (Ls ≈ 255–260) | Primary min: S winter cap growth; max after perihelion | Hess et al. 1980; Haberle et al. 2014 |
| Viking Lander 2 (−4.5 km) | ~730 Pa | ~1080 Pa | same phasing | Hess et al. 1980 |
| MSL/REMS, Gale (−4.5 km) | ~700–740 Pa (Ls ≈ 150) | ~925–955 Pa (Ls ≈ 250–260) | secondary max ~890 Pa near Ls 50–60 | Harri et al. 2014; Martínez et al. 2017 |
| Reference datum | 610.5 Pa (definition; ≈ triple point of H₂O) | — | — | convention |

Landmark values for a 2-harmonic L1 fit at Gale (regress exact coefficients from PDS REMS PS data): (Ls, p in Pa) ≈ (0, 860), (55, 890), (150, 730), (255, 950), (330, 855).

**Site elevation scaling:** p(z) ≈ p_ref · exp(−(z − z_ref)/H), **H = 11.1 km** (scale height, NSSDC). The often-quoted "600–750 Pa" range applies near datum elevation (z ≈ 0); settlement candidate sites at −3 to −5 km see 700–1000+ Pa — relevant margin for compressor/ISRU sizing and EDL.

**Diurnal cycle:** thermal tides give ±2–5% diurnal pressure swings; Gale's crater circulation amplifies this to nearly 10% peak-to-peak (largest measured on Mars) — noise the ISRU compressor model should tolerate but not a driver.

**Derived air density** (ideal gas, mean molar mass M = 43.34 g/mol, R_spec = 191.8 J/kg·K):

```
ρ = p·M/(R·T)   →  Gale mean (840 Pa, 210 K): 0.021 kg/m³
                   cold night (860 Pa, 190 K): 0.024 kg/m³ ; warm midday (820 Pa, 250 K): 0.017 kg/m³
```

(~1.6% of Earth sea-level density.) CO₂ frost point at 700–900 Pa: ~148–150 K — the floor for nighttime polar/surface temperatures. Speed of sound ~240–250 m/s.

---

## 4. Atmospheric composition (MSL/SAM, reevaluated)

Franz et al. (2017, Planet. Space Sci.), quadrupole MS on Curiosity — the values the prompt cites:

| Species | Volume fraction | Notes |
|---|---|---|
| CO₂ | **0.951** | condensable — seasonal ±~2% absolute |
| N₂ | **0.0259** | Mahaffy et al. (2013) originally reported 1.89%; reevaluation raised it |
| ⁴⁰Ar | **0.0194** | inert tracer; enriched/depleted ±~10–20% seasonally as CO₂ condenses (Trainer et al. 2019) |
| O₂ | 0.00161 | ISRU-irrelevant but chemically interesting (seasonal anomalies) |
| CO | 0.00058 | |

Viking gave 2.7% N₂ / 1.6% Ar — the SAM reevaluation swapped the N₂:Ar ranking. For ISRU buffer-gas trades use N₂+Ar ≈ **4.5%** of intake; mean molar mass 43.34 g/mol. Water vapor: ~2–70 pr-µm column, RH near saturation before dawn, <5% midday.

---

## 5. Temperature (mid-latitude site; REMS at Gale, 4.6°S, −4.5 km)

**Air temperature (1.6 m):**

| Season | Daily max | Daily min | Diurnal range |
|---|---|---|---|
| S summer (Ls 250–300, perihelion) | 270–283 K (record ~+6 to +10 °C) | 195–205 K | 65–80 K |
| S winter (Ls 70–120, aphelion) | 240–250 K | 178–190 K | 55–65 K |

Extremes at Gale over 5+ Mars years: air ~178 K to ~283 K. Diurnal range 50–80 K is the rule everywhere on Mars (thin atmosphere, low thermal inertia) — the dominant thermal-cycling load on all surface hardware. Higher latitudes are colder: at 40–50°N winter nights reach 150–170 K air.

**Ground (surface brightness) temperature (REMS GTS, range 150–300 K):**

| Season | Midday max | Pre-dawn min |
|---|---|---|
| S summer | 285–300 K | 175–185 K |
| S winter | 255–270 K | 150–165 K |

Ground leads/exceeds air by 15–30 K at midday and undershoots it by ~10 K at night. NASA MSFC worst-case design analysis (TFAWS 2024, MarsWRF-based, lat −50…+50°, τ 0.02–5): global hottest sol ground T_max ≈ 290 K (−25° lat, clear), coldest sol T_min ≈ 130 K (−50° lat).

**Frost:** H₂O frost events when ground <170 K with high RH (observed at Gale); CO₂ frost only if ground reaches ~148 K.

---

## 6. Dust

### 6.1 Background climatology (column optical depth τ)

Convention warning: orbital climatologies (Montabone et al. 2015) report 9.3 µm absorption CDOD; rovers report **880 nm visible extinction** — visible ≈ 2.6× IR-absorption value. All values below are **visible extinction**, the quantity solar-power models need.

- **Clear season (Ls 0–135, aphelion):** τ ≈ 0.25–0.65, typical **0.4**. Very repeatable year-to-year.
- **Dusty season (Ls 135–360):** background τ ≈ 0.6–1.2 (typical ~0.9), punctuated by regional storms; MER record over 5 Mars years spans τ ≈ 0.3–4.7 (Lemmon et al. 2015).
- Four-phase annual structure with distinct A (Ls ~210–240), B (~250–270), C (~300–330) regional storm sequences in non-GDS years (Montabone et al. 2015; regional-storm interannual studies MY24–36).

### 6.2 Storms

| Class | Frequency | Duration | Peak τ (visible) |
|---|---|---|---|
| Local (<1.6·10⁶ km²) | ~hundreds/year globally | sols | +0.2–1 locally |
| Regional | 2–4 per Mars year affecting any given site's region; A/B/C sequence every year | ~15–40 sols (weeks) | 2–5 |
| **Global (planet-encircling)** | ~1 per 3–4 Mars years (8–9 confirmed events 1956–2018); P(any given MY) ≈ 0.25–0.4 | onset weeks; high-τ phase 1–2 months; decay tail 2–4 months (total ~100–150 sols) | 5–11 (MY34/2018: τ ≈ 8.5 at Gale, >10.8 at Meridiani — killed Opportunity) |

GDS onset windows: Ls ≈ 185–310 (perihelion/southern summer only). Design case for a solar settlement: **τ ≥ 5 sustained for ≥ 60 sols, with τ ≥ 8 for ~30 sols**, arriving with ≤ ~1 week warning, once per ~2–4 Mars years (conservative bound).

### 6.3 Deposition on surfaces (solar arrays)

Flight histories (Landis & Jenkins Pathfinder; MER; Lorenz et al. 2021 synthesis):

- Pathfinder: **0.28%/sol** power loss (first 30 sols).
- MER Spirit/Opportunity: median **0.16–0.18%/sol**, means 0.28–0.39%/sol; observed envelope 0.05–2%/sol (highest during/after storms).
- Best single number: **~0.2%/sol** obscuration rate, episodically reversed by wind/dust-devil **cleaning events** (tens of % recovery, a few times per Mars year at MER sites — cannot be scheduled, so bank no credit at L0/L1).
- InSight (no cleaning events of note): arrays degraded from ~5000 Wh/sol to <500 Wh/sol over ~4 Mars years — the pessimistic bound.

### 6.4 Dust devils

- Measured areal rates: Jezero (MEDA) 1.3–3.4 km⁻²·sol⁻¹; Pathfinder ~19 km⁻²·sol⁻¹ (optical extrapolation); Spirit seasonal peak up to ~50 km⁻²·sol⁻¹; InSight: 502 vortex pressure drops >0.3 Pa in 151 sols at the station, but few visible dust devils. Strongly site- and season-dependent (summer midday peak).
- Encounter with core pressure drop ≥8 Pa: once per several hundred sols at a fixed point. Mechanical risk negligible (q < 1 Pa typically); electrostatics and sensor contamination are the real considerations, plus beneficial array cleaning.

---

## 7. Wind

- Mean near-surface (1.5–2 m) winds: **2–10 m/s** typical (Viking 0–9.5; InSight 0–7.5; Perseverance 0–12; mean ~5 m/s), diurnally modulated (calm nights, convective afternoons).
- Gusts: 22 m/s measured (Perseverance, in a storm); Viking-era storm-front estimates 25–30 m/s; design gust **30–40 m/s**.
- **Dynamic pressure is tiny (the point low density makes):** q = ½ρv² = 0.5 · 0.020 · 30² ≈ **9 Pa** at a 30 m/s design gust — ~a light Earth breeze. Structures are not wind-driven designs; dust ingress and abrasion, not force, are the issues.
- Convective heat transfer at habitat scale: JPL MSR-derived guideline h ≈ **0.4 (still, worst-hot) to 4.0 (windy, worst-cold) W/m²·K** (TFAWS 2024). Convection is a secondary but non-negligible term in habitat/radiator heat balance.

---

## 8. Regolith thermal & subsurface (InSight HP³, flight-measured)

| Quantity | Value | Source |
|---|---|---|
| Thermal conductivity k | **0.039 ± 0.002 W/m·K** (0.03–0.37 m depth) | Grott et al. 2021 |
| Thermal inertia I | **160–230 J m⁻² K⁻¹ s⁻¹ᐟ²** (site); global map 24–800; MCD engineering range 42–394 | Grott 2021; MCD v6.1 |
| Bulk density | **1470 (10–20 cm) – 1730 (deeper) kg/m³** | HP³/Spohn et al. |
| Specific heat c_p | ~630 J/kg·K (consistency check: √(kρc_p) ≈ 198 ✓) | derived |
| Mean soil T at 10–20 cm | 217.5 K, diurnal ±3 K, seasonal ±6.7 K | Spohn et al. 2024 |
| **T at 1 m depth** | **≈ 217 K (Elysium, 4.5°N); nearly isothermal over a sol** | Spohn et al. 2024, extrapolated |

Damping depths (k = 0.039, ρc = 1.0·10⁶ J/m³K): **diurnal ≈ 3.3 cm, seasonal ≈ 0.9 m**. One metre of regolith kills the diurnal wave entirely and attenuates the seasonal wave to ~30% — buried habitats/tanks see a stable ~210–225 K boundary (site-latitude dependent). Excellent free cold sink; poor conductor (0.039 W/m·K ≈ good insulation), so ground-coupled heat rejection needs large contact area or heat pipes.

Surface albedo: 0.105–0.315 (MCD v6.1 global range); typical dark regolith 0.20 (raised toward 0.3 after dust deposition).

---

## 9. Sky temperature (radiator sizing)

Effective IR sky sink temperature (MarsWRF/JPL boundary conditions, TFAWS 2024, equator, τ = 1): **pre-dawn ≈ 160 K, midday ≈ 186 K**; clear-sky (τ ≈ 0.02–0.3) pre-dawn drops to ~**140 K** (MARSTHERM). During a global dust storm the sky warms toward air temperature (~210–230 K), degrading radiator performance exactly when solar arrays are also starved — couple these in trade studies.

Radiator sizing form:

```
q_net = ε σ (T_rad⁴ − T_sky⁴) · F_sky + ε σ (T_rad⁴ − T_ground⁴) · F_ground − α G_solar − h (T_air − T_rad)
```

A horizontal 290 K, ε = 0.9 radiator at night (T_sky 160 K): ~330 W/m² net — Mars nights are superb for heat rejection; midday dusty conditions can halve that.

---

## 10. UV flux

REMS 10+ Mars-year record at Gale (PNAS 2025 — first surface UV dosimetry on another planet), noon values across seasons/dust:

| Band | Range (W/m²) |
|---|---|
| UVA 320–380 nm | 21.4–33.7 |
| UVB 280–320 nm | 4.21–6.45 |
| UVC 200–280 nm | 1.39–2.21 (no ozone shield — reaches the surface) |
| **Total 200–380 nm** | **26.9–42.3** |

Attenuation scales ~exp(−τ/cos z) (dust roughly grey across UV–vis). Consequences: rapid polymer degradation (design UV-hard exteriors), surface sterilization (planetary protection, but also free tool sterilization), and total UV dose ~2 orders of magnitude above Earth's surface in UVC-weighted biological dose.

---

## 11. Disagreements & pitfalls between sources

1. **τ conventions:** Montabone CDOD (9.3 µm absorption) vs rover 880 nm extinction differ by ~×2.6. Mixing them silently is the most common Mars-power modeling error.
2. **Composition:** Viking (2.7% N₂, 1.6% Ar) vs SAM 2013 (1.89% N₂, 1.93% Ar) vs SAM reevaluated 2017 (2.59% N₂, 1.94% Ar). Use Franz 2017. Non-condensables also vary ±10–20% with season (Trainer 2019).
3. **Solar constant:** 586 W/m² (TSI 1361) vs 590 (older 1371) — pick one convention (we use 586.2).
4. **MY34 GDS peak τ:** 8.5 (Gale, measured) vs "">10.8" (Opportunity — instrument-saturated estimate). Treat >10 as possible, 8–9 as the measured design point.
5. **Dust deposition:** Pathfinder 0.28%/sol vs MER medians 0.16–0.18%/sol vs InSight long-term (worse, no cleaning). Spread is a full order of magnitude (0.05–2%/sol) — carry as a distribution, not a scalar.
6. **Wind speeds:** lander anemometry at 1.5–2 m under-samples gusts; dust-devil migration and lander-tilt studies suggest stronger episodic winds. Dynamic pressure conclusion (negligible force) is robust regardless.
7. **GDS statistics:** "1 in 3" vs "1 in 4" Mars years; 8–9 events in ~35 Mars years of records with clustering. Poisson with λ ≈ 0.3/MY confined to Ls 185–310 is defensible.
8. **"600–750 Pa" folklore range** applies at datum elevation; actual settlement sites (low-lying) see 700–1000 Pa. Always elevation-correct with H = 11.1 km.

---

## 12. Fidelity tiers (recommended model forms)

### L0 — single-scalar averages (system sizing spreadsheets)
Use the JSON values directly: S_surface_mean ≈ 0.72 × 586 × (mean cos z) → global horizontal ≈ **2.9 kWh/m²/sol** at low latitude, τ 0.5; p = 840 Pa; T_air = 215 K mean / ΔT_diurnal = 65 K; τ = 0.5 with a GDS contingency case (τ = 9, 100 sols, storage/backup sizing); dust loss 0.2%/sol linear; T_sky = 170 K; T_ground(1 m) = 217 K; wind h = 2 W/m²K.

### L1 — analytic daily/seasonal model (Ls-driven; recommended default)
State variable: t in sols → Ls via Section 2 series (or the Kepler-exact version). Per sol:
1. **Insolation:** S_TOA(Ls) eccentricity formula; δ(Ls); daily energy by the H_sol integral; surface via f(τ) table (A&F 1990).
2. **τ(Ls):** climatology lookup (clear 0.4 → dusty-season 0.9 curve from Montabone shape) + stochastic storms: Poisson regional storms (rate ~3/MY inside Ls 135–330; Δτ ~ +2–4, duration 15–40 sols, exponential decay) and GDS (Bernoulli p ≈ 0.33/MY in Ls 185–310; peak τ 5–11, ~100–150 sol profile).
3. **Pressure:** 2-harmonic fit to the Gale landmark table, scaled exp(−Δz/11.1 km) for site.
4. **Temperature:** parametrize T_max(Ls), T_min(Ls) sinusoids anchored to Section 5 table; intra-sol shape = asymmetric (fast sunrise rise, exponential night decay).
5. **Dust deposition:** dm/dt = 0.2%/sol × (1 + k·τ); optional Poisson cleaning events (skip for conservatism).
6. Sky temp: T_sky = T_air − ΔT(τ) with ΔT ≈ 85 K clear-day, 20 K night, → 0 in storms.

### L2 — physics-based sub-sol timestep (10–60 s steps)
1. **Sun position each step:** full Section 2 chain (M → ν → Ls → EOT → LTST → h → cos z).
2. **Radiation:** direct Beer–Lambert exp(−τ/μ) + 2-stream (or δ-Eddington) diffuse with dust ω₀ ≈ 0.93, g ≈ 0.7; band-split solar/IR.
3. **Surface energy balance PDE** (what MarsWRF/MCD and the TFAWS ground model do):
   `ρc ∂T/∂t = ∂/∂z(k ∂T/∂z)` on a 15-node, 1 m biased grid; surface BC:
   `k ∂T/∂z|₀ = (1−A)·G_solar + ε σ(T_sky⁴ − T_s⁴) + h(T_air − T_s) + L·dm_CO2/dt (frost)`
   This reproduces ground/air diurnal curves to ~5 K vs REMS.
4. **Pressure:** annual harmonic + tide harmonics (diurnal ~2–5%, semi-diurnal ~1–2%, amplified in dust storms).
5. **Wind:** Weibull(mean 5 m/s) diurnally modulated; gust spectrum to 30 m/s; convective h = f(ρ, v) via forced-convection correlation rather than fixed 0.4/4.0.
6. **Dust events:** as L1 but with intra-sol τ ramps; couple τ → T_sky ↑, ΔT_diurnal ↓ (storms cut daytime highs ~10–20 K and raise night lows ~5–10 K — REMS MY34 observations, Viúdez-Moreiras 2019).
7. Optionally replace all of the above with **Mars Climate Database v6.1** lookups (the professional baseline) and keep the analytic model as the fast path.

---

## 13. Sources (primary)

- Allison, M. & McEwen, M. (2000), *A post-Pathfinder evaluation of areocentric solar coordinates…*, Planet. Space Sci. 48, 215–235; and NASA GISS Mars24 algorithm notes. https://www.giss.nasa.gov/tools/mars24/help/algorithm.html
- NSSDC Mars Fact Sheet (NASA GSFC). https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html
- Appelbaum, J. & Flood, D. (1990), *Solar radiation on Mars*, NASA TM-102299 / Solar Energy 45(6).
- Hess, S. et al. (1980), *The annual cycle of pressure on Mars measured by Viking Landers 1 and 2*, GRL 7, 197.
- Haberle, R. et al. (2014), *Preliminary interpretation of the REMS pressure data…*, JGR Planets 119. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1002/2013JE004488
- Harri, A.-M. et al. (2014), *Pressure observations by the Curiosity rover*, JGR Planets 119.
- Martínez, G. et al. (2017), *The Modern Near-Surface Martian Climate*, Space Sci. Rev. 212, 295. https://link.springer.com/article/10.1007/s11214-017-0360-x
- Mahaffy, P. et al. (2013), *Abundance and Isotopic Composition of Gases in the Martian Atmosphere*, Science 341. https://www.science.org/doi/10.1126/science.1237966
- Franz, H. et al. (2017), *Reevaluated martian atmospheric mixing ratios…*, Planet. Space Sci. https://www.sciencedirect.com/science/article/abs/pii/S0032063315000495
- Trainer, M. et al. (2019), *Seasonal Variations in Atmospheric Composition…*, JGR Planets 124. https://agupubs.onlinelibrary.wiley.com/doi/10.1029/2019JE006175
- Montabone, L. et al. (2015), *Eight-year climatology of dust optical depth on Mars*, Icarus 251, 65. https://arxiv.org/abs/1409.4841 (climatology maps: LMD dust climatology archive, MY24–36)
- Lemmon, M. et al. (2015), *Dust aerosol, clouds, and the atmospheric optical depth record over 5 Mars years of MER*, Icarus 251. https://arxiv.org/abs/1403.4234
- Guzewich, S. et al. (2019), *MSL observations of the 2018/MY34 global dust storm*, GRL 46. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2018GL080839
- Viúdez-Moreiras, D. et al. (2019), *Effects of the MY34/2018 GDS as measured by MSL REMS*, JGR Planets 124. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2019je005985
- Zurek, R. & Martin, L. (1993), *Interannual variability of planet-encircling dust storms on Mars*, JGR 98 (NTRS 19930046754).
- Landis, G. & Jenkins, P. (2000), *Measurement of the settling rate of atmospheric dust on Mars Pathfinder*, JGR 105; NASA Mars Solar Power NTRS 20040191326.
- Lorenz, R. et al. (2021), *Lander and rover histories of dust accumulation on and removal from solar arrays on Mars*, Planet. Space Sci. 207. https://www.sciencedirect.com/science/article/pii/S0032063321001768
- Stella, P. & Herman, J. (2005/2015), *Dust accumulation and MER solar array performance*, NTRS 20050186821.
- Toledo, D. et al. (2023), *Dust devil frequency of occurrence at Jezero Crater (MEDA/RDS)*, JGR Planets 128. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2022JE007494
- Banfield, D. et al. (2020), *The atmosphere of Mars as observed by InSight*, Nature Geoscience 13 (502 vortices >0.3 Pa in 151 sols).
- Grott, M. et al. (2021), *Thermal conductivity of the Martian soil at the InSight landing site from HP³*, JGR Planets 126. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2021JE006861
- Spohn, T. et al. (2024), *Mars soil temperature and thermal properties from InSight HP³ data*, GRL 51. https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2024GL108600
- Popok, D. (2024), *Modeling Martian Surface Thermal Environments in Thermal Desktop*, NASA TFAWS 2024, MSFC (MarsWRF-derived sky temperatures, h = 0.4/4.0 W/m²K JPL MSR guideline, MCD albedo 0.105–0.315, TI 41.7–393.9). https://tfaws.nasa.gov/wp-content/uploads/TFAWS2024-PT-2.pdf
- *Ultraviolet and biological effective dose observations at Gale Crater, Mars*, PNAS (2025), REMS UV record. https://www.pnas.org/doi/10.1073/pnas.2426611122
- MARSTHERM thermal model documentation (PSI): predawn clear-sky T_sky ≈ 140 K. https://marstherm.psi.edu/model_documentation.php
