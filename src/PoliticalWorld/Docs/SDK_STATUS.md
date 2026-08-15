# Political World 1.7 / SDK status

Political World 1.7 remains the mod release line. This internal candidate exposes Public API `1.8.0`.

The legacy monolithic `Main.cs` refactor is complete. Runtime systems live in focused `API/`, `Core/`, `Politics/`, `International/`, `Warfare/`, `Map/`, and `UI/` modules. `Main.cs` remains only the NeoModLoader entry declaration.

API 1.8 is creator-focused and backward-compatible: direct content lookup/filtering, safe localization, action inspection, typed addon-private kingdom data, public political-system constants/metadata, and structured ruling-party operation checks.

The same candidate also includes a behavior-preserving performance pass focused on large populations and hot reflection paths. No new Update loop or per-citizen persistent simulation is introduced.

See `Docs/en/API_REFERENCE_1_8.md` or `Docs/ru/API_REFERENCE_1_8.md`.
