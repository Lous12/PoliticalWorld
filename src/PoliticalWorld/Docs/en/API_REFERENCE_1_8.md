# PoliticalWorldAPI 1.8 — quick reference

API 1.8 is a backward-compatible creator/tooling update for Political World 1.7.x.

```csharp
using Lous12.PoliticalWorld;
PoliticalWorldAPI....
```

## Version and capability checks

- `ApiVersion` = `1.8.0`
- `IsCompatible(requiredMajor, requiredMinor)`
- `GetCapabilities()` / `HasCapability(...)`

New 1.8 capabilities:

- `content.lookup`
- `content.filter-by-addon`
- `localization.safe`
- `action.inspect`
- `kingdom.addon-data.typed`
- `political-system.read`
- `operation.checks`

## Content lookup

Addons and developer tools no longer need to enumerate every registered item just to find one object.

- `GetAddon(addonId)`
- `GetIdeology(ideologyId)`
- `GetIdeologiesByAddon(addonId)`
- `GetGovernment(governmentId)`
- `GetGovernmentsByAddon(addonId)`
- `GetAction(actionId, kingdom)`
- `GetActionsByAddon(addonId, kingdom)`
- `GetRarePoliticalEvent(eventId)`
- `GetRarePoliticalEventsByAddon(addonId)`

Existing full-list methods remain available.

## Safe localization

- `HasLocalization(key)`
- `ResolveLocalization(key, fallback)`

These methods check whether a key exists before resolving it. Missing optional addon localization therefore falls back without producing WorldBox `missing text` log spam.

Political World's ideology, custom-government, action and rare-event API views now use the same safe behavior internally.

## Actions

- `GetAction(actionId, kingdom = null)`
- `GetActions(kingdom)`
- `GetActionsByAddon(addonId, kingdom = null)`
- `CanExecuteAction(actionId, kingdom)`
- `ExecuteAction(actionId, kingdom)`

Use `CanExecuteAction` when building buttons or director/scenario UIs.

## Typed addon-private kingdom data

Existing storage remains namespaced and save-compatible.

- `Get/SetKingdomInt`
- `Get/SetKingdomString`
- `Get/SetKingdomBool`
- `Get/SetKingdomFloat`

Float values are serialized with invariant culture.

## Political systems

Use `PoliticalWorldAPI.PoliticalSystems.*` instead of hardcoding the legacy save IDs.

Constants:

- `Competitive`
- `OneParty`
- `Soviet`
- `SovietOneParty`
- `NonElectoral`
- `Decentralized`

Read metadata with:

- `GetPoliticalSystems()`
- `GetPoliticalSystem(id)`

`PoliticalSystemInfo` exposes display name and high-level system traits.

## Operation checks

`CheckSetKingdomRulingParty(kingdom, partyId)` returns an `OperationCheck` with:

- `Allowed`
- stable `Code`
- developer-facing `Message`

Current ruling-party codes include `ok`, `invalid-kingdom`, `party-mandate-not-supported`, `party-id-required`, `party-not-found`, and `party-inactive`.

The existing `SetKingdomRulingParty(...)` bool API is unchanged.

## Rare political events

API 1.7 manual execution remains:

- `CanExecuteRarePoliticalEvent(eventId, kingdom)`
- `ExecuteRarePoliticalEvent(eventId, kingdom)`

API 1.8 adds addon filtering with `GetRarePoliticalEventsByAddon(addonId)`.

## Performance notes

API 1.8 ships with a behavior-preserving core performance pass:

- bounded actor collection reads for capped political leader/candidate searches;
- linear-time duplicate filtering for large actor collection reads;
- cached reflection lookup for city resource storage changes;
- kingdom-wide ideology economy calculations moved outside per-city loops;
- cached sorted rare-event registry views;
- direct ideology/action lookup paths instead of full-list enumeration;
- static hot-path member-name arrays to reduce transient allocations.

No additional Update loop or per-citizen persistent simulation was added.
