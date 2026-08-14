# Political World API 1.2 — Foundation

This internal API revision focuses on making addon mistakes easier to diagnose and addon data safer to store.

## New capabilities

- `validation`
- `kingdom.addon-tags`
- `kingdom.addon-data.v2`

## Validation

Before registration you can call:

```csharp
var check = PoliticalWorldAPI.ValidateAddon(definition);
var ideologyCheck = PoliticalWorldAPI.ValidateIdeology(AddonId, ideology);
var actionCheck = PoliticalWorldAPI.ValidateAction(AddonId, action);
```

`ValidationResult.IsValid` is `false` only when at least one error exists. Warnings are kept in `Issues` but do not block registration.

Registration methods use the same validation automatically and write a readable `[Political World API]` warning to the Unity log when validation fails.

## Collision-safe addon storage

API 1.1 replaced punctuation in addon IDs with `_`, which could make different IDs collide. API 1.2 stores addon data under a reversible collision-safe UTF-8 hex namespace.

Reads automatically fall back to the old API 1.1 key and migrate found values to the new key. Old values are not deleted.

## Private addon kingdom tags

Use these for implementation details that belong only to your addon:

```csharp
PoliticalWorldAPI.AddAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
bool hasTag = PoliticalWorldAPI.HasAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
```

Use `AddKingdomTag` only for intentionally shared/global tags that other addons are expected to understand.
