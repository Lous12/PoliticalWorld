# AI_START_HERE — Political World

You are looking at Political World **1.11.0** with Public PoliticalWorldAPI **1.19.0**.

## If you are creating an addon

1. Read `docs/en/GETTING_STARTED.md` or `docs/ru/GETTING_STARTED.md`.
2. Use `Lous12.PoliticalWorld.PoliticalWorldAPI`.
3. Use a stable addon GUID and namespaced public IDs.
4. Check the minimum API version you actually require with `IsCompatible(...)`.
5. Use `HasCapability(...)` for optional systems.
6. Prefer events / rare events over permanent polling.
7. Keep private state in addon-owned data/tags.
8. If the API is missing something, report the missing capability instead of reaching into internals.

Do not depend on `Main`, `ScenarioBridge`, private partial methods or reflection into Political World internals.

## If you are editing Political World itself

Read `AGENTS.md`, `ARCHITECTURE.md`, `KNOWN_RISKS.md` and `DEVELOPMENT.md`, then inspect the module you are changing.

Old-looking compatibility code is not automatically dead code.

## Ready prompt

```text
Read AI_START_HERE.md and the current Political World API documentation.
Create a NeoModLoader addon using only public Lous12.PoliticalWorld.PoliticalWorldAPI.
Use stable namespaced IDs, API compatibility/capability checks, addon-private state and event-driven APIs where possible.
Do not use reflection into Political World internals.
If the public API cannot support the feature, explain the missing capability instead of inventing one.
```
