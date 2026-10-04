# PoliticalWorldAPI source map

This folder is the public addon boundary of Political World.

Current baseline: Political World **1.11.0**, PoliticalWorldAPI **1.19.0**, WorldBox **0.51.2 build 719**, NeoModLoader **1.2.0.1**.

If you are writing an addon, prefer this folder and `docs/en/API_REFERENCE_1_19.md` over searching through `Main` or copying core implementation code.

## Where to look

- `PoliticalWorldAPI.cs` — main facade: addon registration, validation, ideologies, governments, kingdom/party access, actions, addon-owned kingdom data and the capability list.
- `PoliticalWorldAPI.Creator.cs` — creator conveniences added around API 1.9: localization fallback, operation results and batch helpers.
- `PoliticalWorldAPI.GeneralFramework.cs` — addon-owned Actor/City/Kingdom data, tags, generic content, addon capabilities and custom addon events.
- `PoliticalWorldAPI.WorldAccessLifecycle.cs` — capped world queries, lifecycle state/events and lazy addon-data migrations.
- `PoliticalWorldAPI.UIIntegration.cs` — declarative inspector sections and context actions.
- `PoliticalWorldAPI.UIPages.cs` — full addon pages hosted inside Political World's kingdom/settlement Politics UI.
- `PoliticalWorldAPI.PoliticsExpansion.cs` — race political profiles and dynamic country-name templates.
- `PoliticalWorldAPI.Warfare.cs` — public warfare facade. Read the difference between normal declaration and forced war before using it.
- `PoliticalWorldAPI.Ecosystem.cs` — addon/capability overview, event metrics, diagnostics and safe cleanup of runtime registrations.
- `PoliticalWorldAPI.Release.cs` — API 1.19 release information, requirement checks and copy-paste support reports.
- `Diagnostics/DeveloperDiagnostics.cs` — addon diagnostics bookkeeping.
- `Events/PoliticalEventBus.cs` — core event IDs, subscriptions and event payloads.
- `Events/RarePoliticalEventRegistry.cs` — rare political events evaluated by PW's existing pipeline.
- `Governments/GovernmentRegistry.cs` — custom governments backed by a stable public archetype.

## Topic docs

- `docs/en/ideologies.md` / `docs/ru/ideologies.md`
- `docs/en/governments.md` / `docs/ru/governments.md`
- `docs/en/parties.md` / `docs/ru/parties.md`
- `docs/en/political-events.md` / `docs/ru/political-events.md`
- `docs/en/world-lifecycle.md` / `docs/ru/world-lifecycle.md`
- `docs/en/data-storage.md` / `docs/ru/data-storage.md`
- `docs/en/general-framework.md` / `docs/ru/general-framework.md`
- `docs/en/ui-integration.md` / `docs/ru/ui-integration.md`
- `docs/en/warfare.md` / `docs/ru/warfare.md`
- `docs/en/validation-diagnostics.md` / `docs/ru/validation-diagnostics.md`

## The important boundary

`PoliticalWorldAPI` is the contract. `Main`, `ScenarioBridge`, Harmony patches, runtime caches and UI internals are implementation details.

If an addon needs something that only exists behind the public boundary, do not solve that by reflection unless you are deliberately making a fragile private experiment. For a normal addon, request or add a narrow public capability instead.

## Version requirements

The repository currently exposes API **1.19.0**, but an addon does not need to require 1.19 just because that is the newest version.

Require the oldest API minor that actually contains every contract you use. For example, an addon using only old ideology registration can keep a lower 1.x minimum, while one using `Framework.CheckRequirements` must require API 1.19.

Record the minimum in `AddonDefinition.RequiredApiMajor` / `RequiredApiMinor`. For newer integrations, also list required capabilities. This makes diagnostics useful and avoids fake incompatibility with older PW builds.

## Performance rules

The API intentionally prefers registration, explicit actions and events over polling.

- Do not add a full-world `Update()` scan when an event exists.
- `WorldQuery` methods are capped for a reason.
- Keep expensive work outside event callbacks when possible.
- Do not retain vanilla manager collections returned through reflection/internal access. Public queries return snapshots.

## Save-data rules

Use addon-owned data helpers rather than inventing raw `ukiol_*` keys.

Public data helpers namespace keys by addon ID and are meant to follow the save lifecycle of the Actor/City/Kingdom object. If your own schema changes, use the public migration registry instead of rewriting every object in the world during startup.

## UI rules

Use `PoliticalWorldAPI.UI` / Politics page registration before touching Political World's private windows.

PW owns navigation, scroll containers and host lifecycle. Addons should render through the public context they are given instead of cloning private tabs or Harmony-patching window methods.

## Warfare warning

Warfare is one of the easiest areas to misuse.

Normal public war declaration goes through Political World's diplomacy/interception path. Forced war intentionally bypasses it. Do not use the force path as a lazy replacement for understanding why a normal declaration was rejected.

## AI-assisted addon work

When an AI writes against this API:

1. make it inspect the actual public source or API 1.19 reference;
2. make it state the minimum API/capabilities it is using;
3. reject invented method names;
4. reject reflection into core internals unless the project is intentionally experimental;
5. test in WorldBox and check `Player.log` before treating generated code as working.

See `examples/` for small patterns that are safer to copy than core implementation files.
