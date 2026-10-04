# Political World architecture

## Public boundary

Third-party addons have one supported facade:

`Lous12.PoliticalWorld.PoliticalWorldAPI`

Everything else is implementation detail unless explicitly documented.

## Dependency direction

```text
WorldBox / NeoModLoader
        ↓
Political World runtime modules
        ↓
PoliticalWorldAPI
        ↓
Community / first-party addons
```

If a real addon needs something missing, prefer a safe API capability over a private backdoor.

## Runtime modules

- `API/` — public facade, Event Bus, diagnostics, registries, actions and addon-facing helpers.
- `Core/Configuration/` — tuning, legacy identifiers and shared configuration/state.
- `Core/Runtime/` — bootstrap and staggered simulation pipeline.
- `Core/Integration/WorldBox/` — compatibility-sensitive WorldBox/Harmony bridges.
- `Core/Persistence/` — save/load helpers and persisted state.
- `Core/Events/` — shared event/WorldLog integration.
- `Politics/` — governments, ideologies, parties, elections, leadership, councils, crises and stability.
- `International/` — blocs, vanilla alliance synchronization and summits.
- `Warfare/` — war and diplomacy integration.
- `Map/` — Political Map.
- `UI/` — kingdom/city politics UI, windows and sandbox powers.

`Main.cs` is intentionally tiny. Keep it that way.

## Runtime philosophy

Prefer events, cached aggregate state, staggered updates, explicit lifecycle hooks and rare checks.

Avoid permanent full-world polling when the same result can be reached another way.

## Cross-system edges

### Persistence

Persist stable IDs/data, not temporary runtime references. Changing stored field names/meaning needs a migration plan.

### Dynamic country names

Visible country names are derived state. Base/manual name, political wrapper, ideology/government/rank and manual rename behavior must stay separate.

### World load

Some systems wait until kingdoms/cities/topology are stable before autonomous behavior resumes. Do not move simulation earlier just because objects exist.

### International systems

Political World blocs and vanilla alliances are related but not identical. Startup synchronization must not accidentally mutate gameplay while loading.

### Warfare

War/diplomacy Harmony patches are WorldBox-version-sensitive.

### Native UI

CityWindow and KingdomWindow tabs share vanilla containers with other mods. Do not assume the first active ScrollRect/tab/child belongs to Political World.

## Compatibility-sensitive IDs

Historical save/gameplay identifiers still use `ukiol_*`.

Ugly old ID + working saves > pretty rename + broken worlds.
