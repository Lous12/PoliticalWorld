# Political World API 1.4 — расширенный Party API

API 1.4 открывает безопасный Party API, пригодный для редакторов, сторонних
аддонов и будущего Scenario Tools. Он использует уже существующую партийную
систему Political World и не создаёт вторую параллельную симуляцию.

## Новые capabilities

- `party.lifecycle`
- `party.ideology.write`
- `party.leadership`
- `party.ruling`

Старые `party.read` и `party.write` сохраняются.

## Чтение партий

```csharp
List<PoliticalWorldAPI.PartyInfo> active =
    PoliticalWorldAPI.GetKingdomParties(kingdom);

List<PoliticalWorldAPI.PartyInfo> all =
    PoliticalWorldAPI.GetKingdomParties(
        kingdom,
        includeInactive: true
    );

PoliticalWorldAPI.PartyInfo party =
    PoliticalWorldAPI.GetKingdomParty(
        kingdom,
        partyId,
        includeInactive: true
    );

PoliticalWorldAPI.PartyInfo ruling =
    PoliticalWorldAPI.GetKingdomRulingParty(kingdom);
```

`PartyInfo` теперь содержит:

- стабильный ID и название партии;
- ID/название идеологии;
- identity/имя лидера;
- identity/имя основателя;
- год основания;
- поддержку и радикализм;
- активна ли партия;
- `IsRuling`;
- цветовой seed;
- позицию, стратегию и внешнеполитическую линию;
- партийные traits;
- город происхождения;
- родительскую партию/историю раскола.

## Жизненный цикл

```csharp
PoliticalWorldAPI.DeactivateKingdomParty(
    kingdom,
    partyId
);

PoliticalWorldAPI.ReactivateKingdomParty(
    kingdom,
    partyId
);

// То же самое универсальным методом:
PoliticalWorldAPI.SetKingdomPartyActive(
    kingdom,
    partyId,
    false
);
```

Деактивация — намеренно основной публичный способ «убрать» партию из активной
политики.

### Почему нет жёсткого Delete

Старый ID партии может использоваться в:

- истории выборов;
- истории расколов и родительских связях;
- биографии партии;
- локальной поддержке в городах;
- данных правящей партии и других сохранённых политических данных.

Полное удаление всех ключей сделало бы сохранения хрупкими. Неактивную партию
можно посмотреть через `includeInactive: true`, а при необходимости вернуть.

## Смена идеологии

```csharp
bool ok = PoliticalWorldAPI.SetKingdomPartyIdeology(
    kingdom,
    partyId,
    AddonId + ".ideology_arcane"
);
```

Правила:

- идеология должна быть зарегистрированной корневой идеологией;
- лимиты активных партий соблюдаются;
- стабильный ID, история и основатель сохраняются;
- пользовательское название сохраняется;
- автоматически созданное название меняется под новую идеологию;
- региональная поддержка согласуется существующей системой Political World;
- несовместимый лидер по возможности заменяется обычным алгоритмом Political World.

## Лидер партии

Получить живого Actor:

```csharp
Actor leader = PoliticalWorldAPI.GetKingdomPartyLeader(
    kingdom,
    partyId
);
```

Назначить конкретного персонажа:

```csharp
bool ok = PoliticalWorldAPI.SetKingdomPartyLeader(
    kingdom,
    partyId,
    actor
);
```

Персонаж должен:

- быть жив;
- принадлежать этому королевству;
- иметь ту же гражданскую идеологию, что партия;
- не возглавлять другую активную партию.

Либо можно доверить выбор Political World:

```csharp
PoliticalWorldAPI.AssignBestKingdomPartyLeader(
    kingdom,
    partyId
);
```

Используется уже существующий алгоритм выбора партийного лидера, а не копия в
аддоне.

## Правящая партия

```csharp
PoliticalWorldAPI.SetKingdomRulingParty(
    kingdom,
    partyId
);

PoliticalWorldAPI.ClearKingdomRulingParty(
    kingdom
);
```

Ручное назначение разрешено только там, где партийный мандат имеет смысл:
в системах с конкурентными выборами или однопартийных режимах.

При ручном назначении API **не подделывает запись о выборах** в истории.

## Новые события

API 1.4 добавляет:

- `party.activated`
- `party.ideology.changed`
- `party.leader.changed`
- `kingdom.ruling-party.changed`

`party.leader.changed` приходит как при ручном назначении через API, так и при
автоматической замене лидера самим Political World.

`kingdom.ruling-party.changed` вызывается из центральной точки смены правящей
партии — включая обычные выборы и однопартийную смену.

Аддонам не нужно постоянно опрашивать эти данные через `Update()`.

## Совместимость

Старые методы API 1.3 продолжают работать:

- `CreateKingdomParty`
- `RenameKingdomParty`
- `SetKingdomPartyRadicalism`
- `SetKingdomPartySupport`
- `GetKingdomParties(kingdom)`

Новые методы расширяют ту же партийную систему.
