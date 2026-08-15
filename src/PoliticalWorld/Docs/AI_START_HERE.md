# AI_START_HERE — Political World 1.7 / Public API 1.7

For AI coding assistants working on Political World or a third-party addon:

1. Read `ARCHITECTURE.md`.
2. Addons must use only `Lous12.PoliticalWorld.PoliticalWorldAPI`.
3. Do not depend on `Main`, `ScenarioBridge`, reflection into internals, or private Political World implementation classes.
4. Use a stable addon GUID and namespaced content IDs.
5. Check `PoliticalWorldAPI.IsCompatible(1, 7)` and capability gates before optional features.
6. Prefer Event Bus and Rare Political Event Registry over permanent polling/Update loops.
7. Use addon-private kingdom data/tags for private state.
8. Preserve legacy `ukiol_*` core IDs unless an explicit migration plan exists.
9. Prefer NeoModLoader feature APIs for UI before Harmony or repeated scene searches.
10. If a required operation is missing from the public API, report the missing capability instead of bypassing the API.

Documentation indexes:
- Russian: `ru/README.md`
- English: `en/README.md`
- API reference: `ru/API_REFERENCE_1_7.md` / `en/API_REFERENCE_1_7.md`

Target for this candidate: WorldBox PC `0.51.2 / build 719`, NeoModLoader `1.2.0.1`, PoliticalWorldAPI `1.6.0`.
