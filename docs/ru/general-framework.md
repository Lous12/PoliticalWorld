# Общий addon framework

PoliticalWorldAPI не ограничен только политическим контентом.

API 1.10+ содержит общие primitives, которые могут использовать независимые аддоны.

## Generic content

Аддон может зарегистрировать собственный content type:

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

А затем контент этого типа:

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

Political World не начинает автоматически симулировать spell/resource/religion только потому, что запись появилась в registry. Generic content — это публичный discoverable registry.

## Cross-addon capabilities

Аддон может объявить semantic feature:

```csharp
PoliticalWorldAPI.AddonFeatures.Register(
    AddonId,
    "magic.mana"
);
```

Другие аддоны могут проверить, предоставляется ли capability и кем.

Capabilities не эксклюзивны: несколько аддонов могут объявлять одну и ту же возможность.

## Custom events

Для loose event-driven interop используйте `RegisterAddonEvent` / `PublishAddonEvent`.

Смотрите [Events](political-events.md).

## Data, tags, conditions и effects

Используйте:

- `PoliticalWorldAPI.Data`
- `PoliticalWorldAPI.Tags`
- Actor/City condition helpers
- `PoliticalWorldAPI.WorldEffects`

Эти primitives позволяют строить магию, религии, экономику, профессии, болезни или creator tools без необходимости добавлять каждую такую систему в core Political World.

## Маленький контракт лучше

Лучше несколько стабильных capabilities/events, чем прямой доступ одного аддона к приватным классам другого.
