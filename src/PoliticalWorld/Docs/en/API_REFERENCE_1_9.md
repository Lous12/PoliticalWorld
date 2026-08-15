# PoliticalWorldAPI 1.9 — creator & localization update

API 1.9 is a backward-compatible creator-focused update for Political World 1.7.x.

## Main rule: localization is optional

Addon content no longer has to depend on the player's game language.

Resolution order for API content is:

1. translation registered for the current language;
2. current NeoModLoader localization, if present;
3. registered English/default fallback;
4. literal `DisplayName` / `Description`;
5. content key or ID.

For the simplest addon, literal English text is enough:

```csharp
new PoliticalWorldAPI.IdeologyDefinition {
    Id = AddonId + ".technocracy",
    DisplayName = "Technocracy"
}
```

A Russian player can still see `Technocracy` even when the addon has no Russian translation.

For multilingual addons, localization can be registered directly through the API:

```csharp
PoliticalWorldAPI.RegisterEnglishLocalization(AddonId,
    new Dictionary<string, string> {
        { AddonId + ".technocracy.name", "Technocracy" }
    });

PoliticalWorldAPI.RegisterLocalizationPack(AddonId, "ru",
    new Dictionary<string, string> {
        { AddonId + ".technocracy.name", "Технократия" }
    });
```

Normal NeoModLoader locale files remain supported.

## New 1.9 capabilities

- `localization.fallback`
- `localization.register`
- `content.metadata`
- `content.batch-register`
- `content.query`
- `effect.helpers`
- `condition.helpers.v2`
- `operation.result`
- `party.addon-data`
- `diagnostics.report`

## Richer content metadata

`IdeologyDefinition` / `IdeologyInfo` now support:

- `DisplayName`
- `DescriptionKey`
- `Description`
- `Icon`
- `SortOrder`

`GovernmentDefinition` / `GovernmentInfo` now also support description, icon and sort metadata.

## Batch registration

- `RegisterIdeologies(...)`
- `RegisterGovernments(...)`
- `RegisterActions(...)`
- `RegisterRarePoliticalEvents(...)`

They return `BatchRegistrationResult`.

## Content discovery

- `GetAddonContentSummary(addonId)`
- `GetIdeologiesByTag(tag, includeParentTags)`
- `GetGovernmentsByTag(tag)`
- `GetActionsByCategory(category, kingdom)`

## Operation results

Creator tools can use structured results instead of a bare `false`:

- `TryExecuteAction(...)`
- `TryExecuteRarePoliticalEvent(...)`
- `TrySetKingdomIdeology(...)`
- `TrySetKingdomCurrent(...)`
- `TrySetKingdomGovernment(...)`
- `TrySetKingdomStability(...)`

`OperationResult` contains `Success`, stable-ish `Code`, and a developer-facing `Message`.

## Conditions v2

New helper conditions include:

- `GovernmentHasTag`
- `HasRulingParty`
- `RulingPartyIdeologyIs`
- `HasActivePartyIdeology`
- `PartySupportAtLeast`
- `AddonIntAtLeast`
- `AddonIntAtMost`
- `AddonBoolIs`
- `KingdomHasAddonTag`

## Effects

`PoliticalWorldAPI.Effects` provides zero-tick action builders:

- `Sequence`
- `ChangeStability`
- `SetStability`
- `SetIdeology`
- `SetCurrent`
- `SetGovernment`
- `AddTag` / `RemoveTag`
- `AddAddonTag` / `RemoveAddonTag`
- `SetAddonInt` / `ChangeAddonInt`
- `SetAddonBool`
- `PublishEvent`
- `SetPartySupport`
- `SetPartyRadicalism`

They can be plugged directly into `ActionDefinition.Handler` or `RarePoliticalEventDefinition.Handler`.

## Party-scoped addon data

Addons can attach their own save-safe state to a Political World party without creating a new persistence system:

- `Get/SetPartyInt`
- `Get/SetPartyString`
- `Get/SetPartyBool`
- `Get/SetPartyFloat`

The data is still stored through Political World's namespaced kingdom addon-data layer.

## Diagnostics

`ReportDiagnostic(addonId, severity, code, message)` lets an addon put useful information into Political World's developer diagnostics.

## Performance pass 2

API 1.9 keeps the event-driven design and adds no new `Update()` loop.

This pass also:

- uses O(1) capability lookup;
- caches sorted custom-government IDs;
- avoids cloning government tags for simple tag checks;
- uses `HashSet` for registration-time tag/ID deduplication;
- avoids building the full government list when filtering custom governments by addon;
- centralizes localization resolution instead of repeatedly calling missing localization keys.

API 1.8's bounded actor scans and large-population optimizations remain in place.
