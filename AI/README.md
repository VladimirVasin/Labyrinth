# AI Memory

Shared notes for AI agents working on `Labyrinth`.

Read order:

1. `AI/work-log.md`
2. `AI/system-tree.md`
3. `AI/systems-map.md`
4. `TECHNICAL_SPEC.md`
5. Relevant files under `Assets/Scripts/`

Rules:

- Treat code as source of truth.
- Consult `AI/system-tree.md` before broad, ambiguous, architectural, or cross-system work, then use it to identify affected systems before searching code.
- Consult `AI/systems-map.md` before broad code searches, implementation, bugfixes, investigations, and refactors.
- Use the owner cards in `AI/systems-map.md` as starting points, then follow cross-system links when a task crosses systems.
- Update `AI/system-tree.md` when system hierarchy, subsystem responsibilities, player-facing features, simulation features, UI surfaces, rendering layers, generated-map features, or cross-system dependencies change.
- Update `AI/systems-map.md` when source files, ownership, responsibilities, or cross-system dependencies change.
- Keep `AI/system-tree.md` conceptual; keep file ownership details in `AI/systems-map.md`.
- Keep implementation files under 1000 lines.
- Use UTF-8 for all text files.
- Update `AI/work-log.md` after meaningful implementation work.
