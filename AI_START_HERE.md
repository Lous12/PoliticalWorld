# AI_START_HERE — Political World

You are working with Political World 1.7 and Public PoliticalWorldAPI 1.6.

## If creating an addon

1. Read `docs/en/GETTING_STARTED.md` or `docs/ru/GETTING_STARTED.md`.
2. Use only `Lous12.PoliticalWorld.PoliticalWorldAPI`.
3. Never depend on `Main`, `ScenarioBridge`, reflection into internals, or private Political World methods.
4. Give the addon a stable GUID and namespace all content IDs under that GUID.
5. Check `PoliticalWorldAPI.IsCompatible(1, 6)`.
6. Use `HasCapability(...)` for optional systems.
7. Prefer Event Bus / Rare Political Event Registry over permanent polling.
8. Use addon-private kingdom data and tags for private state.
9. If the API lacks something important, report the missing capability instead of bypassing the API.

## If editing Political World itself

1. Read `ARCHITECTURE.md`.
2. Keep `Main.cs` tiny. Put code in the narrowest fitting module.
3. Preserve legacy `ukiol_*` identifiers unless an explicit migration exists.
4. Treat `Core/Integration/WorldBox/` as compatibility-sensitive.
5. Avoid combining structural refactors with gameplay redesigns.
6. Keep RU and EN public documentation equivalent.
7. Maintain API 1.x compatibility unless intentionally planning a new major API.

## Ready prompt

```text
Read AI_START_HERE.md, ARCHITECTURE.md, and the relevant Political World API documentation.
Create a NeoModLoader addon using only public Lous12.PoliticalWorld.PoliticalWorldAPI.
Do not depend on internal Political World classes.
Use namespaced IDs, API compatibility/capability checks, addon-private state, and Event Bus/Rare Event APIs instead of permanent polling where possible.
If the public API cannot support a requested feature, explain the missing capability instead of using reflection.
```
