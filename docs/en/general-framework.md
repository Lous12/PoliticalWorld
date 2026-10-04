# General addon framework

PoliticalWorldAPI is not limited to political content.

API 1.10+ includes general primitives that can be reused by independent addons.

## Generic content

An addon can register its own content type:

```csharp
PoliticalWorldAPI.Content.RegisterType(
    AddonId,
    new PoliticalWorldAPI.GenericContentTypeDefinition
    {
        Id = AddonId + ".spell",
        DisplayName = "Spell",
        Tags = new[] { "magic" }
    }
);
```

Then register content under that type:

```csharp
PoliticalWorldAPI.Content.Register(
    AddonId,
    new PoliticalWorldAPI.GenericContentDefinition
    {
        Id = AddonId + ".spell.fireball",
        TypeId = AddonId + ".spell",
        DisplayName = "Fireball",
        Tags = new[] { "fire" },
        Metadata = new Dictionary<string, string>
        {
            ["power"] = "10"
        }
    }
);
```

Political World does not automatically simulate a spell/resource/religion just because it was registered. Generic content is a discoverable public registry.

## Cross-addon capabilities

An addon can advertise a semantic feature:

```csharp
PoliticalWorldAPI.AddonFeatures.Register(
    AddonId,
    "magic.mana"
);
```

Other addons can ask whether the capability is provided and which addons provide it.

Capabilities are not exclusive: several addons may advertise the same capability.

## Custom events

Use `RegisterAddonEvent` / `PublishAddonEvent` for loose event-driven interop without a hard dependency.

See [Events](political-events.md).

## Data, tags, conditions and effects

Use:

- `PoliticalWorldAPI.Data`
- `PoliticalWorldAPI.Tags`
- Actor/City condition helpers
- `PoliticalWorldAPI.WorldEffects`

These primitives are useful for systems such as magic, religion, economy, professions, diseases or creator tools without requiring Political World core to implement those systems itself.

## Keep the contract small

Prefer a few stable capabilities/events over one addon reaching into another addon's private classes.
