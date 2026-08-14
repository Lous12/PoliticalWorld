# Идеологии

## Регистрация

Идеология задаётся через `IdeologyDefinition`. ID должен принадлежать вашему namespace, например `Author.Mod.magocracy`.

```csharp
PoliticalWorldAPI.RegisterIdeology(AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".magocracy",
        ParentId = "",
        NameKey = AddonId + ".magocracy",
        HighSupportStability = 2,
        SupportThreshold = 55,
        LowSupportStability = -2,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Tags = new[] { "magic", "elitist" }
    });
```

## Иерархия

`ParentId = ""` создаёт корневую идеологию. Для течения укажите ID существующего родителя. Political World хранит `ParentId`, `RootId` и `Tier`, поэтому внешние инструменты могут строить дерево без знания внутренних классов.

## RandomWeight

- `0` — не участвует в обычном случайном засеве;
- положительное значение — автор явно разрешает естественное появление через generic seeding.

Для контентных аддонов безопасный default — `0`.

## Tags

Теги — метаданные для совместимости и условий между аддонами. Используйте короткие стабильные строки: `magic`, `dragon`, `theocratic`, `reformist`. Не рассчитывайте, что произвольный тег автоматически создаст механику ядра.

## Чтение

`GetIdeologies`, `GetRootIdeologies`, `GetCurrentsForRoot`, `GetIdeologyTags`, `HasIdeologyTag`.

## Изменение государства

`SetKingdomIdeology` и `SetKingdomCurrent` используют существующую логику Political World и публикуют соответствующие core events.
