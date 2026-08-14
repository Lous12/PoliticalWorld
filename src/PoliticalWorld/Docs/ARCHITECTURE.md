# Political World architecture (internal draft)

## Public boundary

Third-party addons should depend on one public facade only:

`Lous12.PoliticalWorld.PoliticalWorldAPI`

Everything under `Core/`, `Politics/`, `International/`, `UI/`, `Map/`, `Persistence/`, and `Integration/` should be treated as implementation detail unless explicitly documented otherwise.

## Direction of dependencies

Recommended direction:

`WorldBox / NeoModLoader -> Political World Core -> PoliticalWorldAPI -> Addons`

Internal modules may communicate through shared services/events rather than calling every other subsystem directly.

## Planned module layout

- `API/` — stable public addon API, including `Events/` and `Diagnostics/` modules.
- `Core/` — bootstrap, validation, diagnostics, event bus, internal bridges.
- `Politics/` — ideologies, governments, parties, elections, crises.
- `International/` — blocs, alliance synchronization, summits.
- `Warfare/` — political consequences and war integration.
- `Map/` — Political Map modes and visuals.
- `UI/` — Political World UI only.
- `Persistence/` — saves, migrations, addon-owned data.
- `Integration/` — WorldBox and NeoModLoader integration helpers.

## Refactor rule

Do not rewrite mechanics during structural refactors. Related proven subsystems may move together in a FAST package, but each package must keep behavior identical and receive a runtime checkpoint before the next high-risk group.

## Public API rule

If Scenario Tools or another first-party addon needs to reach into `Main.cs`, treat that as a missing Public API capability and improve the facade instead of teaching addons to use internals.


## Current modularization status

Foundation Step 9A extracted the first complete legacy subsystem from `Main.cs`:

`Map/PoliticalMap/`

The extraction uses C# partial-class files and intentionally keeps behavior and private visibility unchanged. Future steps should follow the same one-subsystem-at-a-time rule.

## Extracted runtime modules (1.7 internal)

- `Map/PoliticalMap/` — Political Map rendering/state/patches (Step 9A)
- `Politics/Governments/` — government forms and political-system core (Step 9B)

`Main.cs` remains a temporary legacy host while systems are moved incrementally. Step 9C FAST additionally extracts parties/movements, crises, councils, party congresses, leadership, elections, native political WorldLog helpers and shared kingdom persistence helpers. Unrelated high-risk groups still remain separate checkpoints.


## Refactor status — Step 9F FAST

Large runtime systems are now separated into Politics, International, Warfare, Map and UI folders. `Main.cs` is being reduced toward bootstrap/integration responsibilities while public API stays isolated under `API/`.

## Refactor status — Step 9G FINAL

The legacy monolithic `Main.cs` refactor is structurally complete. `Main.cs` now only declares the NeoModLoader entry type; runtime bootstrap/pipeline, legacy IDs/runtime state, and WorldBox/Harmony integration live under focused `Core/` modules.

Current major internal areas:

- `API/` — public addon facade, event bus, diagnostics, government and rare-event registries.
- `Core/Configuration/` — legacy IDs, tuning and shared runtime state kept for save/API compatibility.
- `Core/Runtime/` — mod bootstrap and staggered political simulation pipeline.
- `Core/Integration/WorldBox/` — compatibility-sensitive Harmony bridges.
- `Core/Persistence/` and `Core/Events/` — shared persistence and WorldLog support.
- `Politics/` — governments, ideologies, parties, elections, leadership, councils, crises and stability.
- `International/` — blocs/alliance synchronization and summits.
- `Warfare/` — political war/diplomacy integration.
- `Map/` — Political Map.
- `UI/` — kingdom politics UI, windows and sandbox powers.

`Main.cs` is intentionally kept tiny so contributors and AI tools are not encouraged to add new gameplay systems to the entry file. New systems should be placed in the appropriate module instead.
