# Идеологии

Political World 1.11 / API 1.19 позволяет регистрировать идеологии, читать их иерархию и менять идеологическое состояние государства через Public API.

## Регистрация

Используйте `IdeologyDefinition`. Публичный ID контента должен принадлежать namespace вашего аддона.

```csharp
PoliticalWorldAPI.RegisterIdeology(
    AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".arcane_reformism",
        ParentId = "",
        NameKey = AddonId + ".arcane_reformism",
        DisplayName = "Arcane Reformism",
        Description = "A small example ideology.",
        HighSupportStability = 2,
        SupportThreshold = 55,
        LowSupportStability = -2,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Family = "reformist",
        Rarity = "uncommon",
        Radicalism = 25,
        Tags = new[] { "magic", "reformist" }
    }
);
```

`DisplayName` и `Description` работают как читаемый fallback. Переводы можно дать через locale-файлы или `RegisterLocalization(...)`.

## Иерархия

`ParentId = ""` создаёт корневую идеологию.

Для течения/дочерней идеологии укажите существующий `ParentId`. `IdeologyInfo` отдаёт `ParentId`, `RootId` и `Tier`, поэтому дерево можно строить без доступа к внутренним классам PW.

## RandomWeight

`RandomWeight` отвечает за участие в generic random seeding Political World.

- `0` — идеология сама не засевается generic-механизмом;
- положительное значение — автор явно разрешает такое появление.

Для большинства контентных аддонов безопасный старт — `0`.

## Метаданные

API 1.15+ добавляет:

- `Family`
- `Rarity`
- `Radicalism`
- `IsSecret`
- `Tags`

Это метаданные. Произвольный тег или rarity сам по себе не создаёт механику core, пока какая-то система явно их не читает.

## Чтение

Полезные публичные методы:

```csharp
PoliticalWorldAPI.GetIdeologies();
PoliticalWorldAPI.GetIdeology(id);
PoliticalWorldAPI.GetIdeologiesByAddon(AddonId);
PoliticalWorldAPI.GetRootIdeologies();
PoliticalWorldAPI.GetCurrentsForRoot(rootId);
PoliticalWorldAPI.GetIdeologyTags(id, includeParents: true);
PoliticalWorldAPI.HasIdeologyTag(id, "magic", includeParents: true);
```

## Изменение государства

Используйте публичные операции:

```csharp
PoliticalWorldAPI.SetKingdomIdeology(kingdom, ideologyId);
PoliticalWorldAPI.SetKingdomCurrent(kingdom, currentId);
```

Они проходят через существующую логику Political World и публикуют соответствующие core events.

Не записывайте внутренние ideology/current keys напрямую в `kingdom.data`.
