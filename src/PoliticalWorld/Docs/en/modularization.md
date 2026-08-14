# Political World module layout

The legacy monolithic `Main.cs` refactor is complete. New gameplay code should not be added to `Main.cs`; it is intentionally kept as a tiny NeoModLoader entry declaration.

Current runtime areas:

- `API/` — public addon facade, Event Bus, diagnostics, government and rare-event registries.
- `Core/Configuration/` — compatibility-sensitive legacy IDs, tuning and shared runtime state.
- `Core/Runtime/` — bootstrap and staggered political simulation pipeline.
- `Core/Integration/WorldBox/` — WorldBox/Harmony bridges that may need attention after game updates.
- `Core/Persistence/` and `Core/Events/` — shared save and WorldLog helpers.
- `Politics/` — governments, ideologies, parties, elections, leadership, councils, crises and stability.
- `International/` — blocs, vanilla alliance synchronization and summits.
- `Warfare/` — war and diplomacy integration.
- `Map/` — Political Map.
- `UI/` — kingdom politics UI, windows and sandbox powers.

## Contribution rule

Place a new system in the narrowest fitting module. If a first- or third-party addon needs an internal class, treat that as a missing Public API capability and improve `PoliticalWorldAPI` rather than exposing implementation details.
