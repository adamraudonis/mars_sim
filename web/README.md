# Mars Habitat Sim — web app (primary implementation)

TypeScript + Three.js + React, built with Vite, tested with `bun test`. Fully static —
deploy `dist/` to any static host (GitHub Pages, Netlify, Cloudflare Pages).

```bash
bun install
bun run dev        # dev server (Vite, HMR)
bun test           # 19 tests incl. a 1,000-sol mission smoke (~1 s total)
bun run build      # type-check + production build → dist/ (~210 KB gzipped)
bun run baseline   # headless 2,200-sol mission validation with vitals table
```

## Layout

```
src/sim/       pure-TS simulation core (no DOM): kernel, 16 modules, scenario, distiller, recorder
src/three/     Three.js base view: procedural terrain, structures, sky, frame-driven reconciler
src/ui/        React mission-control HUD: charts (canvas), systems, parameters, log, timeline
public/data/   parameters_master.json (sourced parameters) + scenarios/*.json
public/data/cache/  precomputed mission recordings (one per preset) — instant scrubbing
scripts/       headless runners, cache precompute, favicon generator
tests/         bun test suite (physics anchors, conservation, determinism, smoke)
```

## Cached playback (instant scrubbing)

The app is a **player over a precomputed mission Recording**, not a live stepper. Each
preset ships a cached recording (`public/data/cache/*.json`, ~155 KB gzipped) capturing
per-sol series, display frames, and events for the whole mission — so you can **scrub
straight to year 5** (or any moment) instantly, with the charts always showing the full
multi-year arc and a moving playhead. The timeline at the bottom has year quick-jumps and
event markers. `bun run precompute` regenerates the cache (also runs inside `bun run
build`). Editing a parameter (or a fidelity level) flips a **RE-RUN** button that
regenerates the recording client-side in ~1 s — so custom what-ifs stay live.

Every parameter's citation is a **clickable link** in the Parameters tab (sourced from the
research campaign; fetch-verified). Purely-derived quantities show their derivation instead.

## Performance notes

The full 2,200-sol baseline mission (52,800 steps × 16 modules) runs in **~0.6 s**
(≈85k steps/s) in plain TypeScript — the 50 sols/s timelapse needs 1,200 steps/s, so the
engine runs on the main thread with a 25 ms/frame budget and never hitches the UI. The
sim is driven from `setInterval` (rAF is throttled in background tabs), rendering from
rAF. Solar tables and crew/robot markers are `InstancedMesh` (one draw call each).
Rust/WASM was evaluated and deliberately skipped: at this workload it buys nothing user
-visible; if Monte Carlo studies grow to ~10k runs, port the step kernel behind the same
`SimulationEngine` interface and/or fan runs out to Web Workers.

The Unity project in `../MarsHabitatSim/` is the validated reference implementation
(see its `REFERENCE_ONLY.md`); the C# core served as the spec for this port, and the
test suite pins the same physics anchors (ISRU stoichiometry, Allison & McEwen Ls,
Appelbaum & Flood insolation bands, exact mass-ledger conservation).
