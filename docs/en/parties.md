# Party API 1.4+

The Party API is primarily intended for Scenario Tools, political addons, and event-driven content.

## Reading

```csharp
var parties = PoliticalWorldAPI.GetKingdomParties(kingdom, includeInactive: true);
var ruling = PoliticalWorldAPI.GetKingdomRulingParty(kingdom);
```

`PartyInfo` includes ID, name, ideology, leader, founder, founded year, support, radicalism, active/ruling state, traits, position, strategy, foreign stance, origin, and parent party.

## Writing

Creation/rename, support, radicalism, ideology, active state, leadership, and ruling-party assignment are exposed.

```csharp
string id = PoliticalWorldAPI.CreateKingdomParty(kingdom, ideologyId, 25, "Arcane League");
PoliticalWorldAPI.SetKingdomPartySupport(kingdom, id, 40);
PoliticalWorldAPI.AssignBestKingdomPartyLeader(kingdom, id);
```

## Deactivate instead of deleting

Use `DeactivateKingdomParty` / `ReactivateKingdomParty`. Hard delete is intentionally not public because party IDs may remain in election history, split history, and regional support data.

## Events

Listen to `PartyCreated`, `PartyActivated`, `PartyDeactivated`, `PartyRenamed`, `PartyIdeologyChanged`, `PartyLeaderChanged`, `RulingPartyChanged`, `PartySupportChanged`, and `PartyRadicalismChanged`.
