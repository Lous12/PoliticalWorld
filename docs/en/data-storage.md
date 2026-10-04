# Addon data, tags and migrations

API 1.19 provides addon-owned typed storage on `Actor`, `City` and `Kingdom` objects.

Use this instead of inventing raw save keys in Political World internals.

## Typed data

The general surface is `PoliticalWorldAPI.Data`.

```csharp
int mana = PoliticalWorldAPI.Data.GetInt(
    kingdom,
    AddonId,
    "mana",
    0
);

PoliticalWorldAPI.Data.SetInt(
    kingdom,
    AddonId,
    "mana",
    mana + 1
);
```

The same API works with `Actor`, `City` and `Kingdom`.

Supported value helpers:

- `GetInt` / `SetInt`
- `GetString` / `SetString`
- `GetBool` / `SetBool`
- `GetFloat` / `SetFloat`

Keys are namespaced by addon ID. The API owns the physical encoding/storage key.

For older kingdom-only code, methods such as `GetKingdomInt` / `SetKingdomInt` remain available for compatibility.

## Private tags

`PoliticalWorldAPI.Tags` provides addon-owned tags for Actor/City/Kingdom:

```csharp
PoliticalWorldAPI.Tags.Add(city, AddonId, "holy_site");

if (PoliticalWorldAPI.Tags.Has(city, AddonId, "holy_site"))
{
    // ...
}
```

Use addon-private tags for internal state.

Legacy kingdom helpers such as `AddAddonKingdomTag(...)` remain available, but new general code can use `PoliticalWorldAPI.Tags`.

## Shared kingdom tags

`AddKingdomTag` / `HasKingdomTag` are shared conventions, not private addon state.

Only use a shared tag when another mod is intentionally expected to understand the same tag.

## Save lifecycle

Actor/City data uses the vanilla object's data container. Kingdom data goes through Political World's save-backed kingdom helpers.

Do not store live object references as if they were stable persistent IDs.

If a value must survive save/load, test an actual roundtrip.

## Schema migrations

API 1.11+ exposes lazy addon-owned migrations for Actor, City and Kingdom data.

Register steps:

```csharp
PoliticalWorldAPI.Migrations.RegisterKingdom(
    AddonId,
    fromVersion: 0,
    toVersion: 1,
    handler: (kingdom, fromVersion, toVersion) =>
    {
        PoliticalWorldAPI.Data.SetInt(
            kingdom,
            AddonId,
            "mana",
            0
        );
        return true;
    }
);
```

Apply migrations explicitly to objects your addon actually uses:

```csharp
PoliticalWorldAPI.Migrations.Apply(kingdom, AddonId);
```

The registry stores the schema version inside the same addon-owned namespace.

It does **not** automatically scan the entire world.

## Rule of thumb

If another addon does not need to understand the value/tag, keep it private and namespaced.

If you change the meaning or structure of persisted data, add a migration instead of silently reinterpreting old saves.

See `examples/08_World_Data`.
