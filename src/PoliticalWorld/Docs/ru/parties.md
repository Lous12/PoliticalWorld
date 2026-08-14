# Party API 1.4+

Party API предназначен прежде всего для Scenario Tools, политических аддонов и событий.

## Чтение

```csharp
var parties = PoliticalWorldAPI.GetKingdomParties(kingdom, includeInactive: true);
var ruling = PoliticalWorldAPI.GetKingdomRulingParty(kingdom);
```

`PartyInfo` содержит ID, имя, идеологию, лидера, основателя, год основания, поддержку, радикализм, active/ruling, traits, позицию, стратегию, внешнеполитическую позицию, происхождение и родительскую партию.

## Запись

Доступны создание/переименование, поддержка, радикализм, идеология, active state, лидер и правящая партия.

```csharp
string id = PoliticalWorldAPI.CreateKingdomParty(kingdom, ideologyId, 25, "Arcane League");
PoliticalWorldAPI.SetKingdomPartySupport(kingdom, id, 40);
PoliticalWorldAPI.AssignBestKingdomPartyLeader(kingdom, id);
```

## Деактивация вместо удаления

Используйте `DeactivateKingdomParty` / `ReactivateKingdomParty`. Hard delete не является публичной операцией, потому что ID партии может находиться в истории выборов, расколов и региональной поддержке.

## События

Слушайте `PartyCreated`, `PartyActivated`, `PartyDeactivated`, `PartyRenamed`, `PartyIdeologyChanged`, `PartyLeaderChanged`, `RulingPartyChanged`, `PartySupportChanged`, `PartyRadicalismChanged`.
