# Addon data and tags

## Addon-private data

```csharp
int value = PoliticalWorldAPI.GetKingdomInt(kingdom, AddonId, "mana", 0);
PoliticalWorldAPI.SetKingdomInt(kingdom, AddonId, "mana", value + 1);

string dynasty = PoliticalWorldAPI.GetKingdomString(kingdom, AddonId, "dynasty", "");
PoliticalWorldAPI.SetKingdomString(kingdom, AddonId, "dynasty", "Draconis");
```

API 1.2+ uses a collision-safe namespace and can read/migrate the legacy 1.1 key format.

## Private tags

```csharp
PoliticalWorldAPI.AddAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
bool has = PoliticalWorldAPI.HasAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
```

Use these for your addon's internal logic.

## Shared tags

`AddKingdomTag` / `HasKingdomTag` are only for intentionally shared conventions between mods. They are not owned by one addon namespace.

## Rule

If another mod does not need to understand your tag/key, use the addon-private variant.
