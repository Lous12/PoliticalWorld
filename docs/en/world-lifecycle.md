# World access and lifecycle

API 1.11+ exposes explicit, capped world queries and lifecycle notifications.

This exists so addons can inspect live WorldBox objects without inventing their own unbounded manager scans.

## Availability

```csharp
bool available =
    PoliticalWorldAPI.Lifecycle.IsWorldAvailable();

bool ready =
    PoliticalWorldAPI.Lifecycle.IsWorldReady();

int session =
    PoliticalWorldAPI.Lifecycle.GetWorldSessionId();

int year =
    PoliticalWorldAPI.Lifecycle.GetWorldYear();
```

`IsWorldAvailable` and `IsWorldReady` are useful gates, but they are not a promise that every unrelated mod has completed every custom restore step.

For complex addon restore logic, also react to lifecycle events and test save/load.

## Lifecycle events

Subscribe through the normal event bus:

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.WorldReady,
    OnWorldReady
);
```

Available lifecycle events:

- `WorldChanged`
- `WorldReady`
- `WorldUnavailable`

See `examples/08_World_Data`.

## Capped queries

`PoliticalWorldAPI.WorldQuery` provides explicit snapshots of kingdoms, cities and actors.

The implementation deliberately caps how many raw objects a query may inspect. A predicate does not turn the query into an unlimited scan.

Use a sensible limit and call the query when you actually need a snapshot.

Do not put large `WorldQuery` calls in an addon `Update()` every frame.

## Session IDs

`GetWorldSessionId()` changes with world/session transitions. It is useful for invalidating addon caches that should not survive switching worlds.

Do not treat runtime object references from an old session as valid in a new one.

## Persistence

For addon-owned persistent values, use `PoliticalWorldAPI.Data` and `PoliticalWorldAPI.Migrations`.

WorldQuery returns live runtime objects; it is not a persistence format.
