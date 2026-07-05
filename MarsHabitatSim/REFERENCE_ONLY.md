# ⚠️ Reference implementation only

This Unity project is the **original implementation** of Mars Habitat Sim and is kept as a
reference — the physics models here were debugged, reviewed, and validated (24/24 tests),
and the C# core served as the specification for the port.

**The maintained, primary implementation is the web app in [`../web/`](../web/)**
(TypeScript sim core + Three.js view + React UI). Make model changes there; only consult
this project for history or to cross-check a ported model's behavior.
