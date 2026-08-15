# PoliticalWorldAPI 1.7 — quick reference

Entry point:

```csharp
using Lous12.PoliticalWorld;
PoliticalWorldAPI....
```

## Version and capabilities

- `ApiVersion`, `ApiMajor`, `ApiMinor`, `CoreModId`
- `IsReady()`
- `IsCompatible(requiredMajor, requiredMinor)`
- `GetCapabilities()` / `HasCapability(...)`

API 1.7 exposes capabilities for addon/action/ideology/government/kingdom/party/event/diagnostics systems. For optional behavior, prefer `HasCapability` over guessing from a version string.

## Addon registration

- `ValidateAddon(AddonDefinition)`
- `RegisterAddon(AddonDefinition)`
- `IsAddonRegistered(addonId)`
- `GetRegisteredAddons()`

Register the addon before registering ideologies, governments, actions, events, or subscriptions.

## Ideologies

- `GetIdeologies()`
- `GetRootIdeologies()`
- `GetCurrentsForRoot(rootId)`
- `ValidateIdeology(addonId, definition)`
- `RegisterIdeology(addonId, definition)`
- `GetIdeologyTags(id, includeParents)`
- `HasIdeologyTag(id, tag, includeParents)`

## Governments

- `GetGovernmentForms()` / `GetGovernment(id)`
- `ValidateGovernment(...)` / `RegisterGovernment(...)`
- `GetGovernmentTags(...)` / `HasGovernmentTag(...)`
- `GetCoreGovernmentId(GovernmentArchetype)`

Archetypes: AbsoluteMonarchy, ConstitutionalMonarchy, ParliamentaryRepublic, PresidentialRepublic, OnePartyState, MilitaryDictatorship, CouncilRepublic, Oligarchy.

## Kingdom

Read:
- `GetKingdomState(kingdom)`
- `GetKingdomRuler(kingdom)`
- `RulerHasTrait`, `GetRulerRaceId`, `RulerIsImmortal`

Write:
- `SetKingdomIdeology`
- `SetKingdomCurrent`
- `SetKingdomGovernment`
- `SetKingdomStability` / `ChangeKingdomStability`

## Parties

- `GetKingdomParties(..., includeInactive)`
- `GetKingdomParty(...)`
- `GetKingdomRulingParty(...)`
- `GetKingdomPartyLeader(...)`
- `CreateKingdomParty(...)`
- `RenameKingdomParty(...)`
- `SetKingdomPartySupport/Radicalism/Ideology/Active/Leader`
- `DeactivateKingdomParty` / `ReactivateKingdomParty`
- `AssignBestKingdomPartyLeader`
- `SetKingdomRulingParty` / `ClearKingdomRulingParty`

There is intentionally no hard-delete party API: deactivation is safer for election history, split history, and save references.

## Tags and addon data

Shared tags: `Get/Has/Add/RemoveKingdomTag`.  
Addon-private tags: `Get/Has/Add/RemoveAddonKingdomTag`.  
Data: `Get/SetKingdomInt`, `Get/SetKingdomString`.

Use addon-private methods for internal addon state.

## Actions

- `ValidateAction`
- `RegisterAction` / `UnregisterAction`
- `GetActions(kingdom)`
- `ExecuteAction(actionId, kingdom)`

## Conditions

`All`, `Any`, `Not`, `GovernmentIs`, `IdeologyIs`, `CurrentIs`, `PoliticalSystemIs`, `StabilityAtLeast/AtMost`, `RulerHasTrait`, `RulerRaceIs`, `RulerIsImmortal`, `KingdomHasTag`, `IdeologyHasTag`.

## Event Bus

- `GetEventIds()`
- `Subscribe(addonId, eventId, handler)`
- `Unsubscribe(...)` / `UnsubscribeAll(addonId)`

`Events.All` (`"*"`) subscribes to all known core events.

## Rare Political Events

- `ValidateRarePoliticalEvent`
- `RegisterRarePoliticalEvent`
- `UnregisterRarePoliticalEvent`
- `GetRarePoliticalEvent` / `GetRarePoliticalEvents`
- `CanExecuteRarePoliticalEvent(eventId, kingdom)`
- `ExecuteRarePoliticalEvent(eventId, kingdom)`

`ExecuteRarePoliticalEvent` is intended for scenario/director tools. It ignores random chance, periodic check interval and existing cooldown, but still respects the registered `Condition`. A successful manual execution records the current year as the last fire year.

`ChancePermille`: 0..1000; 10 = 1%, 100 = 10%, 1000 = 100%.

## Diagnostics

- `GetAddonDiagnostics(addonId)`
- `GetDiagnosticsReport(addonId)`
- `GetAllDiagnosticsReports()`
- `LogDiagnosticsReport(addonId)`

## Publishing a WorldLog event

`PublishKingdomEvent(kingdom, text, eventId, cooldownSeconds)` is the public path for a visible political event.
