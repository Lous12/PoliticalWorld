# Event Bus и Rare Political Events — API 1.7

## Зачем

Аддон не должен каждый кадр сканировать государства ради проверки политических изменений. Political World публикует события из уже существующих переходов состояния и отдельно предлагает редкий event registry в годовом политическом pipeline.

## Core Event Bus

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);
```

Handler получает `PoliticalEventData`: `EventId`, `Kingdom`, старые/новые значения, PartyId, Actor, имена, SourceAddonId, Category, Year, Text, EventKey.

Известные события API 1.7:

- `kingdom.ideology.changed`
- `kingdom.current.changed`
- `kingdom.government.changed`
- `party.created`, `party.activated`, `party.deactivated`, `party.renamed`
- `party.ideology.changed`, `party.leader.changed`, `party.radicalism.changed`, `party.support.changed`
- `kingdom.ruling-party.changed`
- `kingdom.ruler.changed`
- `kingdom.election.finished`
- `kingdom.crisis.started`, `kingdom.crisis.ended`
- `kingdom.leadership-crisis.started`, `kingdom.leadership-crisis.resolved`
- `kingdom.rare-political-event.fired`
- `political.event.published`

`Events.All` подписывает на все известные события.

Callback одного аддона изолирован: исключение записывается в diagnostics, после чего dispatch остальных подписчиков продолжается. Event Bus также ограничивает глубину рекурсивного dispatch.

## Rare Political Event Registry

```csharp
PoliticalWorldAPI.RegisterRarePoliticalEvent(AddonId,
    new PoliticalWorldAPI.RarePoliticalEventDefinition
    {
        Id = AddonId + ".palace_crisis",
        DisplayName = "Palace Crisis",
        CheckIntervalYears = 1,
        CooldownYears = 10,
        ChancePermille = 30,
        Condition = PoliticalWorldAPI.Conditions.StabilityAtMost(45),
        Handler = kingdom => PoliticalWorldAPI.ChangeKingdomStability(kingdom, -5)
    });
```

`ChancePermille` — вероятность 0..1000: 30 = 3%. Registry использует существующий годовой политический pipeline и не создаёт addon `Update()`.

Cooldown хранится по государству и событию через namespaced данные Political World.


## Ручной запуск редких событий — API 1.7

Сценарные/director-инструменты могут вручную запускать зарегистрированное редкое политическое событие через публичный API:

```csharp
if (PoliticalWorldAPI.CanExecuteRarePoliticalEvent(eventId, kingdom))
{
    PoliticalWorldAPI.ExecuteRarePoliticalEvent(eventId, kingdom);
}
```

Ручной запуск пропускает случайный шанс, обычный интервал проверки и текущий cooldown, потому что событие запрашивается явно. Зарегистрированный `Condition` всё ещё проверяется. После успешного запуска текущий игровой год записывается как последний год срабатывания, поэтому обычный годовой pipeline дальше учитывает cooldown.

Capability: `political-event.rare.execute`.
