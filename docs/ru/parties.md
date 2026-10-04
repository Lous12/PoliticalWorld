# Партии

Party API рассчитан на редакторы, Scenario Tools, политические аддоны и event-driven контент. Он даёт стабильные ID партий и мягкое управление lifecycle без доступа к внутреннему хранилищу Political World.

## Чтение

```csharp
var active = PoliticalWorldAPI.GetKingdomParties(kingdom);
var all = PoliticalWorldAPI.GetKingdomParties(kingdom, includeInactive: true);
var one = PoliticalWorldAPI.GetKingdomParty(kingdom, partyId);
var ruling = PoliticalWorldAPI.GetKingdomRulingParty(kingdom);
var leader = PoliticalWorldAPI.GetKingdomPartyLeader(kingdom, partyId);
```

`PartyInfo` содержит:

- стабильный ID и название;
- идеологию;
- лидера и основателя;
- год основания;
- поддержку и радикализм;
- active/ruling state;
- color seed;
- позицию, стратегию и foreign stance;
- traits;
- origin city;
- parent/split metadata.

Для редакторов, миграций и исторического просмотра используйте `includeInactive: true`.

## Создание и редактирование

```csharp
string id = PoliticalWorldAPI.CreateKingdomParty(
    kingdom,
    ideologyId,
    25,
    "Arcane League"
);

PoliticalWorldAPI.SetKingdomPartySupport(kingdom, id, 40);
PoliticalWorldAPI.SetKingdomPartyRadicalism(kingdom, id, 30);
PoliticalWorldAPI.SetKingdomPartyIdeology(kingdom, id, ideologyId);
PoliticalWorldAPI.SetKingdomPartyColorSeed(kingdom, id, 3);
PoliticalWorldAPI.RenameKingdomParty(kingdom, id, "New Arcane League");
```

## Лидерство

```csharp
PoliticalWorldAPI.AssignBestKingdomPartyLeader(kingdom, id);
```

Конкретного живого Actor можно назначить через `SetKingdomPartyLeader(...)`; Political World проверит, подходит ли он.

## Деактивация вместо удаления

Используйте:

```csharp
PoliticalWorldAPI.DeactivateKingdomParty(kingdom, id);
PoliticalWorldAPI.ReactivateKingdomParty(kingdom, id);
```

Hard delete специально не является публичной операцией. Стабильный party ID может оставаться в истории выборов, split lineage и данных поддержки поселений/регионов.

## Правящая партия

Перед ручной сменой мандата проверьте операцию:

```csharp
var check = PoliticalWorldAPI.CheckSetKingdomRulingParty(kingdom, id);
if (check.Allowed)
{
    PoliticalWorldAPI.SetKingdomRulingParty(kingdom, id);
}
```

Также есть `ClearKingdomRulingParty(...)`.

Ручное назначение ruling party не подделывает запись в истории выборов.

## События

Полезные core events:

- `PartyCreated`
- `PartyActivated`
- `PartyDeactivated`
- `PartyRenamed`
- `PartyIdeologyChanged`
- `PartyLeaderChanged`
- `RulingPartyChanged`
- `PartySupportChanged`
- `PartyRadicalismChanged`

Лучше слушать эти события, чем каждый кадр опрашивать состояние партий.
