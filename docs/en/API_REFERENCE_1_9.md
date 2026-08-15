# PoliticalWorldAPI 1.9 — quick reference

Entry point:

```csharp
using Lous12.PoliticalWorld;
PoliticalWorldAPI....
```

Compatibility:

```csharp
if (!PoliticalWorldAPI.IsCompatible(1, 9))
    return;
```

## Core and capabilities

- `ApiVersion`, `ApiMajor`, `ApiMinor`, `CoreModId`
- `IsReady()`
- `IsCompatible(major, minor)`
- `GetCapabilities()`
- `HasCapability(...)`

Use capabilities for optional features instead of guessing from a version string.

## Addons and content discovery

- `ValidateAddon(...)`
- `RegisterAddon(...)`
- `IsAddonRegistered(...)`
- `GetAddon(...)`
- `GetRegisteredAddons()`
- `GetAddonContentSummary(...)`

Batch registration is available for ideologies, governments, Actions and Rare Events.

## Localization

Localization is optional.

- `RegisterLocalization(...)`
- `RegisterLocalizationPack(...)`
- `RegisterEnglishLocalization(...)`
- `HasRegisteredLocalization(...)`
- `HasLocalization(...)`
- `ResolveLocalization(key, fallback)`

Recommended behavior is current-language text → registered/default/English fallback → literal display text.

## Ideologies

- `GetIdeologies()`
- `GetIdeology(id)`
- `GetIdeologiesByAddon(addonId)`
- `GetIdeologiesByTag(...)`
- `GetRootIdeologies()`
- `GetCurrentsForRoot(rootId)`
- `ValidateIdeology(...)`
- `RegisterIdeology(...)`
- `RegisterIdeologies(...)`
- ideology tags and metadata

Definitions can provide literal `DisplayName`/`Description`, localization keys, icon, sort order and tags.

## Governments and political systems

- `GetGovernmentForms()`
- `GetGovernment(id)`
- `GetGovernmentsByAddon(addonId)`
- `GetGovernmentsByTag(...)`
- `ValidateGovernment(...)`
- `RegisterGovernment(...)`
- `RegisterGovernments(...)`
- government tags
- core archetype lookup
- `GetPoliticalSystems()` / `GetPoliticalSystem(id)`

## Kingdom and ruler state

Read:
- `GetKingdomState(...)`
- `GetKingdomRuler(...)`
- ruler trait/race/immortality helpers

Write:
- ideology/current/government
- stability
- `TrySet...` operation-result variants

## Parties

Read:
- kingdom parties
- one party by ID
- ruling party
- party leader

Write:
- create/rename
- support/radicalism/ideology
- activate/deactivate/reactivate
- leader assignment
- ruling-party assignment

`CheckSetKingdomRulingParty(...)` provides a structured reason when the operation is rejected.

Party addon-private data:
- `Get/SetPartyInt`
- `Get/SetPartyString`
- `Get/SetPartyBool`
- `Get/SetPartyFloat`

## Addon data and tags

Kingdom addon-private values:
- `int`
- `string`
- `bool`
- `float`

Shared kingdom tags and addon-private kingdom tags are supported.

Use namespaced/private storage for addon internals.

## Actions

- `ValidateAction(...)`
- `RegisterAction(...)`
- `RegisterActions(...)`
- `UnregisterAction(...)`
- `GetAction(...)`
- `GetActionsByAddon(...)`
- `GetActionsByCategory(...)`
- `GetActions(kingdom)`
- `CanExecuteAction(...)`
- `ExecuteAction(...)`
- `TryExecuteAction(...)`

## Conditions

Core combinators:
- `All`
- `Any`
- `Not`

Political/kingdom helpers include:
- government/ideology/current/political-system checks
- stability
- ruler trait/race/immortality
- kingdom and ideology tags

API 1.9 creator helpers add conditions such as:
- `GovernmentHasTag`
- `HasRulingParty`
- `RulingPartyIdeologyIs`
- `HasActivePartyIdeology`
- `PartySupportAtLeast`
- addon integer/bool checks
- addon-private tag checks

## Effects

Reusable effects include:

- sequences
- stability changes
- ideology/current/government changes
- shared/addon-private tags
- addon integer/bool state
- WorldLog publication
- party support/radicalism changes

Effects are intended to reduce boilerplate in event/action addons.

## Event Bus

- `GetEventIds()`
- `Subscribe(...)`
- `Unsubscribe(...)`
- `UnsubscribeAll(...)`

Political World core events remain available. General cross-addon events are a direction for the next framework work.

## Rare Events

- validate/register/unregister
- list by ID / list all / list by addon
- test whether an event can execute
- manual execution for creator/scenario tools
- `TryExecuteRarePoliticalEvent(...)`

Automatic Rare Events still use Political World's event-driven yearly pipeline.

## Diagnostics

- addon diagnostics reports
- all diagnostics
- log a report
- `ReportDiagnostic(...)`

## Performance guidance

Prefer:

- registration on load;
- Event Bus;
- Actions;
- Rare Events;
- addon-owned cached state.

Avoid whole-world scans every frame unless they are genuinely necessary.

## Public boundary

Use only `PoliticalWorldAPI` as the Political World contract.

`Main`, `ScenarioBridge` and internal implementation classes are not public API.
