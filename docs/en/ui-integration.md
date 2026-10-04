# UI integration

PoliticalWorldAPI exposes public UI extension points so addons do not need to Harmony-patch Political World's private window implementation for common cases.

## Inspector sections

Register declarative fields for Actor, City or Kingdom targets through `PoliticalWorldAPI.UI.RegisterInspectorSection(...)`.

A section can provide:

- namespaced ID;
- target kind;
- display/localization text;
- sort order;
- visibility predicate;
- fields with value providers.

See `examples/09_UI_Inspector`.

## Context actions

`PoliticalWorldAPI.UI.RegisterContextAction(...)` registers an explicit action for an inspector target.

The definition can provide:

- visibility;
- `CanExecute`;
- `Execute`.

Use this for selected-object actions instead of adding a private button by patching PW internals.

## Hosted Politics pages

API 1.16+ can register full pages in Political World's kingdom or settlement Politics host:

```csharp
PoliticalWorldAPI.UI.RegisterKingdomPoliticsPage(
    AddonId,
    new PoliticalWorldAPI.PoliticsPageDefinition
    {
        Id = AddonId + ".overview",
        DisplayName = "Addon",
        SortOrder = 100,
        Render = context =>
        {
            // Render into context.Content.
        }
    }
);
```

Settlement pages use `RegisterSettlementPoliticsPage(...)`.

Political World owns navigation/host lifecycle. The addon receives a `PoliticsPageContext` with target objects and the host content transform.

## Cleanup

`UI.UnregisterInspectorSection`, `UI.UnregisterContextAction` and `UI.UnregisterPoliticsPage` remove individual registrations.

Framework runtime cleanup can detach runtime-owned UI registrations for an addon.

## Rule

Before patching `KingdomWindow`, `CityWindow`, tab navigation or PW private UI methods, check whether the public UI host can express the feature.

If it cannot, request a public capability instead of relying on private implementation details.
