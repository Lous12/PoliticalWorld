# Political World 1.7 / SDK status

Political World 1.7 is prepared as a public beta candidate with Public API `1.6.0`.

The legacy monolithic `Main.cs` refactor is complete. Runtime systems live in focused `API/`, `Core/`, `Politics/`, `International/`, `Warfare/`, `Map/`, and `UI/` modules. `Main.cs` is intentionally only the NeoModLoader entry declaration.

Developer material includes equal Russian and English documentation, addon and standalone NeoModLoader templates, complete examples, Event Bus and Rare Political Event guides, diagnostics, versioning rules, UI recipes, and AI-oriented entry points.

The external SDK is distributed separately so example `.cs` files are never placed inside the `PoliticalWorld` runtime folder and accidentally compiled into the core mod.
