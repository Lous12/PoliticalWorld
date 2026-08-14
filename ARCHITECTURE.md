# Political World architecture

## Public boundary

Third-party addons have one supported facade:

`Lous12.PoliticalWorld.PoliticalWorldAPI`

Everything else is an implementation detail unless explicitly documented. In particular, addons must not depend on `Main`, `ScenarioBridge`, private partial-class methods, or reflection into internal modules.

## Dependency direction

```text
WorldBox / NeoModLoader
        ↓
Political World runtime modules
        ↓
PoliticalWorldAPI
        ↓
Third-party addons
```

The public API is a boundary, not a convenience wrapper. When an addon genuinely needs missing functionality, add a safe capability to the API instead of teaching consumers to bypass it.

## Runtime modules

- `API/` — public facade, Event Bus, diagnostics, government registry, rare political event registry.
- `Core/Configuration/` — legacy identifiers, tuning and shared runtime state.
- `Core/Runtime/` — NeoModLoader bootstrap and staggered simulation pipeline.
- `Core/Integration/WorldBox/` — compatibility-sensitive WorldBox/Harmony integration.
- `Core/Persistence/` — shared save helpers.
- `Core/Events/` — Political World → WorldLog bridge.
- `Politics/` — governments, ideologies, parties, elections, leadership, councils, crises and stability.
- `International/` — blocs, vanilla alliance synchronization and summits.
- `Warfare/` — war/diplomacy integration.
- `Map/` — Political Map.
- `UI/` — kingdom politics UI, windows and sandbox powers.

`Main.cs` is intentionally tiny. Do not turn it back into a monolith.

## Performance philosophy

Political World aims to create the feeling of deep politics without simulating unnecessary detail. Prefer event-driven aggregate state and rare/staggered checks over permanent per-citizen polling. Addons should prefer the Event Bus and Rare Political Event Registry to their own continuous `Update()` loops.

## Compatibility-sensitive IDs

The project identity is `Lous12.PoliticalWorld`, but many historical gameplay/save identifiers still use the `ukiol_*` prefix. They are intentionally retained as legacy compatibility IDs. Do not mass-rename them without a save migration plan.

## First-party addons

Scenario Tools and Fantasy Politics should consume the same public API available to community addons wherever practical. If a first-party addon requires an internal reach-through, that is a signal to improve the public API.
