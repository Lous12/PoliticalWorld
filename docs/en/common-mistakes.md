# Common mistakes

## The addon is inside the PoliticalWorld folder

NeoModLoader recursively compiles `.cs` files inside a mod folder. Put the addon in its own sibling mod folder.

## Missing dependency

A PW addon should declare:

```json
"Dependencies": ["Lous12.PoliticalWorld"]
```

Otherwise compile/load ordering and the assembly reference are not guaranteed.

## Treating the current API as the minimum API

`1.19.0` is current, not automatically required.

If your addon only uses older contracts, a lower honest `IsCompatible(1, x)` check is correct.

## Skipping RegisterAddon

Register the addon before owned content, custom events, UI registrations, tags/data or subscriptions.

## Content/event IDs are not namespaced

Use IDs such as:

```text
YourName.MyAddon.feature
YourName.MyAddon.event_name
```

Validation intentionally rejects content owned by another addon.

## Hardcoding legacy `ukiol_*` IDs

Some legacy IDs are part of save compatibility. Addon code should use public constants/accessors where available instead of copying internal IDs from core source.

## Polling the whole world every frame

Use:

- Event Bus for transitions;
- Rare Political Events for low-frequency political effects;
- `WorldQuery` for explicit capped snapshots;
- addon-owned cached data when possible.

Do not turn one missing event into an unbounded permanent world scan.

## Treating WorldQuery.IsReady as "the entire save is definitely restored"

The public lifecycle tells you when the world/query surface is available. Complex addon restore logic should still be conservative around load transitions and use lifecycle events instead of racing world initialization.

## Using shared tags for private state

Use `PoliticalWorldAPI.Tags` / addon-owned data for private state. Shared kingdom tags are for intentional interop conventions.

## Saving live object references

Actor/City/Kingdom runtime references are not persistent IDs. Store stable data, then resolve live objects again after load when necessary.

## Changing persisted data without a migration

If the meaning/schema of addon-owned data changes, use `PoliticalWorldAPI.Migrations` instead of silently reinterpreting old saves.

## Harmony-patching PW windows for addon UI

Before patching private windows, check the public UI surfaces:

- inspector sections;
- context actions;
- hosted kingdom/settlement Politics pages.

If the public host cannot express the feature, request a capability.

## Reaching into Main or ScenarioBridge

They are implementation details, not addon contracts.

If a public capability is missing, document/request it instead of building reflection glue around core internals.

## Using ForceWar as the normal path

`Warfare.TryDeclareWar` goes through Political World's diplomacy interception. `ForceWar` intentionally bypasses it.

Use the force path only when bypassing normal rules is actually the feature.

## Only sending a screenshot after a failure

For useful bug reports include the complete compile/runtime error or `Player.log`, version/build information and exact reproduction steps.
