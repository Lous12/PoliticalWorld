# Parties

The Party API is intended for editors, Scenario Tools, political addons and event-driven content. It exposes stable party IDs and soft lifecycle operations without requiring access to Political World's internal party storage.

## Reading

```csharp
var active = PoliticalWorldAPI.GetKingdomParties(kingdom);
var all = PoliticalWorldAPI.GetKingdomParties(kingdom, includeInactive: true);
var one = PoliticalWorldAPI.GetKingdomParty(kingdom, partyId);
var ruling = PoliticalWorldAPI.GetKingdomRulingParty(kingdom);
var leader = PoliticalWorldAPI.GetKingdomPartyLeader(kingdom, partyId);
```

`PartyInfo` includes:

- stable ID and name;
- ideology;
- leader and founder identity/name;
- founded year;
- support and radicalism;
- active/ruling state;
- color seed;
- position, strategy and foreign stance;
- traits;
- origin city;
- parent/split party metadata.

Use `includeInactive: true` for editors, migration tools or historical inspection.

## Creating and editing

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

## Leadership

```csharp
PoliticalWorldAPI.AssignBestKingdomPartyLeader(kingdom, id);
```

Or assign a specific live actor with `SetKingdomPartyLeader(...)`. Political World validates whether that actor is suitable.

## Deactivate instead of deleting

Use:

```csharp
PoliticalWorldAPI.DeactivateKingdomParty(kingdom, id);
PoliticalWorldAPI.ReactivateKingdomParty(kingdom, id);
```

Hard delete is intentionally not public. Stable party IDs may still be referenced by election history, split lineage and settlement/regional support data.

## Ruling party

Before manually changing the mandate, ask the API whether the operation is allowed:

```csharp
var check = PoliticalWorldAPI.CheckSetKingdomRulingParty(kingdom, id);
if (check.Allowed)
{
    PoliticalWorldAPI.SetKingdomRulingParty(kingdom, id);
}
```

`ClearKingdomRulingParty(...)` is also available.

Manual ruling-party assignment is not the same as forging an election result.

## Events

Relevant core events include:

- `PartyCreated`
- `PartyActivated`
- `PartyDeactivated`
- `PartyRenamed`
- `PartyIdeologyChanged`
- `PartyLeaderChanged`
- `RulingPartyChanged`
- `PartySupportChanged`
- `PartyRadicalismChanged`

Prefer listening to these events instead of polling party state every frame.
