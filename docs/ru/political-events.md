# Event Bus, custom events и Rare Political Events

Political World использует один Event Bus для core-переходов, событий аддонов и lifecycle мира.

По возможности реагируйте на события вместо постоянного сканирования государств/городов каждый кадр.

## Подписка на core event

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);
```

Callback получает `PoliticalEventData`.

Полезные поля:

- `EventId`
- `Kingdom`, `KingdomName`
- `OldValue`, `NewValue`, `OldNumber`, `NewNumber`
- `PartyId`
- `IdeologyId`, `CurrentId`, `GovernmentId`
- `TargetKingdom`, `TargetKingdomName`
- `WarSource`
- `Actor`, identity/name актёра
- `City`
- `SourceAddonId`
- `Category`
- `Year`
- `Text`, `EventKey`
- `Payload` для custom addon events

Не каждое событие заполняет каждое поле.

## Текущие семейства core events

API 1.19 публикует события для:

- смены идеологии/current/government;
- создания, активации, деактивации и переименования партий;
- смены идеологии/лидера/поддержки/радикализма партии;
- смены ruling party и правителя;
- выборов;
- динамических названий стран;
- начала/конца войн;
- политических и leadership crises;
- сепаратизма, автономии и secession lifecycle поселений;
- Rare Political Events;
- опубликованных political events;
- lifecycle мира: `WorldChanged`, `WorldReady`, `WorldUnavailable`.

Если инструменту нужен актуальный список, используйте `PoliticalWorldAPI.GetEventIds()`.

`Events.All` подписывает на все dispatch-события.

## Собственные события аддона

Сначала зарегистрируйте namespaced ID:

```csharp
PoliticalWorldAPI.RegisterAddonEvent(
    AddonId,
    AddonId + ".mana_crisis"
);
```

Затем опубликуйте событие с optional payload и world context:

```csharp
PoliticalWorldAPI.PublishAddonEvent(
    AddonId,
    AddonId + ".mana_crisis",
    new Dictionary<string, string>
    {
        ["severity"] = "high"
    },
    kingdom: kingdom,
    city: city,
    category: "magic"
);
```

Другой аддон может подписаться на этот ID обычным `Subscribe(...)`.

## Изоляция callback

Исключение в callback одного аддона не останавливает dispatch другим подписчикам. Ошибка записывается в diagnostics.

Также есть ограничение глубины рекурсивного dispatch. Не стройте бесконечные event loops, которые перепубликуют друг друга.

## Отписка

Есть `Unsubscribe(...)` для конкретного handler и `UnsubscribeAll(AddonId)` для снятия всех подписок аддона.

## Rare Political Event Registry

```csharp
PoliticalWorldAPI.RegisterRarePoliticalEvent(
    AddonId,
    new PoliticalWorldAPI.RarePoliticalEventDefinition
    {
        Id = AddonId + ".palace_crisis",
        DisplayName = "Palace Crisis",
        CheckIntervalYears = 1,
        CooldownYears = 10,
        ChancePermille = 30,
        Condition = PoliticalWorldAPI.Conditions.StabilityAtMost(45),
        Handler = kingdom =>
            PoliticalWorldAPI.ChangeKingdomStability(kingdom, -5)
    }
);
```

`ChancePermille` использует диапазон `0..1000`, то есть `30 = 3%`.

Rare events проверяются существующим yearly political pipeline Political World и не создают новый addon `Update()`.

Смотрите также:

- `examples/04_Event_Listener`
- `examples/05_Rare_Event`
- `examples/07_Addon_Event`
- [Lifecycle мира](world-lifecycle.md)
