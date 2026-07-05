# Answering the big questions with Mars Habitat Sim

How each headline trade decision maps onto the tool. Every study below can be run three
ways: **live** (load a scenario, edit parameters in the inspector, watch the charts),
**menu** (MarsSim → Studies → …), or **headless** (`-executeMethod
MarsSim.EditorTools.TradeStudyMenu.RunHeadless -study <name>` → CSV in `studies/out/`).

What the research campaign already tells us is noted per question (full citations in
`research/RESEARCH_REPORT.md`); the sim is where those numbers meet each other.

---

## 1. Nuclear vs solar — or both?

**Run:** `baseline.json` vs `nuclear.json` (same campaign, different power line), or the
`solar_vs_nuclear` study (5 Monte Carlo weather seeds each).

**What the physics says:** solar+storage lands at ~300–450 kg per continuous kW at
mid-latitudes vs ~230 kg/kW for FSP-class fission — but Mars dust is forward-scattering,
so even a τ=10 global storm leaves ~5–10 % of clear output, and the sim's two-tier battery
reserve keeps life support alive while ISRU throttles. Watch `power.unmet`,
`fleet.return_prop` and battery SoC through the sol ≈1347 global storm in the default seed:
the solar base loses ~130 sols of propellant production; the fission base doesn't blink.
The interesting hybrid question — a fission backbone sized to critical + keep-alive loads
with a solar ISRU farm on top — is one scenario file away.

## 2. How will ISRU ice mining work? CO₂-plus-imported-H₂?

**Run:** `baseline.json` (water-based: electrolysis supplies H₂ *and* all O₂; 2.25 kg fresh
water per kg CH₄) vs `h2_import.json` (Sabatier on Earth H₂, product water re-electrolyzed
to recycle half the H₂, remaining O₂ from MOXIE-style SOXE; **zero mining**).

**The stoichiometry the sim enforces:** 0.489 kg water per kg of O:F-3.6 propellant mix
(water route) vs 0.054 kg imported H₂ per kg mix + ~15 % more plant energy (SOXE route).
Refueling one ship (1,200 t) = ~590 t mined water vs ~65 t Earth-side LH₂ — but LH₂ needs
20 K storage for years (`isru_atmosphere.lh2_boiloff_passive` is in the database at
0.3 %/day; the depot module charges cryocooler power). Ice mining also buys settlement
water for free. The `isru.kwh_per_kg` series shows both chains live.

## 3. What will the mass of solar panels be?

**Run:** the `solar_sizing` study, or just edit `power_solar.specific_mass_kg_m2`
(1.5 kg/m², ROSA-class, sourced) and the manifest's `solar_area_m2` and read the Systems
panel's "Array mass". Baseline: 60,000 m² ≈ **90 t** of arrays + 46 t of batteries buys
~1.9 MW-continuous-equivalent at 40°N — roughly one dedicated cargo Starship of power
hardware per crew ship refueled per window.

## 4. How many flights for the initial base?

**Run:** the manifests are the scenario file; `ManifestMassFitsShipCount` (test suite)
enforces mass-vs-lift feasibility per wave. The shipped baseline: 5 cargo (2031) +
5 crew-wave (2033) + 3 resupply (2035). The resupply wave is not optional — remove it and
watch the sol-1800 spares collapse (we did; the crew does not survive it). Behind each
Mars-bound ship stand ~12 tanker flights to LEO (sourced), so the baseline campaign is
~150 Earth launches — pad cadence, not astrodynamics, is the bottleneck.

## 5. How do we simulate hardware failures? What spares mass?

**Run:** the `spares_vs_kfactor` study (failure-rate uncertainty ×1–4, 8 seeds each), and
watch `maint.awaiting_spares`. The failure engine samples per-operating-hour
exponential/Weibull failures per component group (ECLSS ORUs, SOXE stacks, excavators,
robot fleet…), repairs consume crew/robot hours + class-pooled spares, cannibalization
merges dead units. ISS flight experience (14 of 20 ECLSS ORUs ran worse than predicted,
some 22×) is why the k-factor default is 2.0. Baseline provisioning is ~25 t of spares
across three waves for ~4 years — consistent with Owens & de Weck's 12–17 t per 4-crew
transit once uncertainty is carried. The debug runs in `studies/out/` show exactly what a
spares shortfall looks like: quiet for 500 sols, then everything at once.

## 6. How much food can we bring? Energy economics of growing?

**Run:** the `food_closure` study, or edit `greenhouse_m2` in a manifest. The database
carries both sides: packaged food at 1.83 kg/CM-day as-shipped (≈ 8 t per crew per
1,000 sols) vs LED-grown potatoes at ~76 kcal/m²/sol for ~6.2 kWh/m²/sol of lighting.
That is ~0.08 kWh(e)/kcal — feeding one person 100 % from LEDs is a 10–20 kW line item,
the single largest life-support power draw. The baseline's 700 m² greenhouse supplies
~25 % of calories for 12 crew and doubles as an oxygen plant (surplus O₂ banks to the
reserve — which the sol ≈1790 emergency purges then spend). Grow-vs-bring crosses over
only when power is cheap (fission) or the base is old.

## 7. What do Optimus robots change about the labor schedule?

**Run:** the `robot_count` study (fleet availability 0 → 0.8), and watch `labor.unmet`.
Labor is a real market in the sim: crew supply ~6.5 productive h/sol each (ISS-derived),
robots supply ~12 h/sol × availability × task-specific effectiveness (0.5–0.6 structured
work, 0.25 diagnosis-heavy repair — the research is emphatic that a single "robot = X %
human" scalar is wrong). The pre-crew phase is the decisive case: 40 robots deploy the
entire 90 t solar farm and commission ISRU before any human lands, which is what makes the
uncrewed-first campaign architecture possible at all. Set robots to zero and the first
crew spend their surface stay as construction workers.

## 8. What's the overall launch schedule?

**Run:** it's data — `flights` in the scenario JSON, with real window timing baked into
the shipped campaign (Feb 2031 launch → Sep 2031 landing; Apr 2033 crew → Nov 2033;
May 2035 resupply → early 2036; Earth-return windows at sols ≈1380 and ≈2140). The
`campaign.*` series and mission log narrate it. C3 per window (2026–2037) is in the
database — 2033 is the cheap window (7.78 km²/s²), 2037 the expensive one (14.84, ~2×
payload penalty), so "which window do we skip?" is itself a study the tool can run.

---

## The fidelity workflow in practice

1. Run `baseline.json` with solar at **L2** through a couple of dusty seasons.
2. Press **Distill → Solar** — the sol-integrated yield (≈5 kWh/sol per rated kW at 40°N,
   CV reported) installs itself as the L0 coefficient with provenance.
3. Switch solar to **L0** in the Systems panel and run 20-year campaign sweeps at ~10×
   speed; drill back to L2 whenever a distilled number turns out to drive the answer
   (e.g. storm-season battery sizing — the one thing L0 deliberately erases).
