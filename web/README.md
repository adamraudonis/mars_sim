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
src/sim/       pure-TS simulation core (no DOM): kernel, 16 modules, scenario, distiller
src/three/     Three.js base view: procedural terrain, structures, sky, reconciler
src/ui/        React mission-control HUD: charts (canvas), systems, parameters, log
public/data/   parameters_master.json (415 sourced parameters) + scenarios/*.json
scripts/       headless runners
tests/         bun test suite (physics anchors, conservation, determinism, smoke)
```

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
