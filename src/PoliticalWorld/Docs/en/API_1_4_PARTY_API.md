# Political World API 1.4 — Expanded Party API

API 1.4 exposes a safer, editor-grade party surface for third-party addons and
future Scenario Tools. It reuses Political World's existing party storage and
simulation; it does not introduce a second party system.

## New capabilities

- `party.lifecycle`
- `party.ideology.write`
- `party.leadership`
- `party.ruling`

Existing `party.read` and `party.write` remain compatible.

## Reading parties

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

`PartyInfo` now includes:

- stable party ID and name;
- ideology ID/display name;
- leader identity/name;
- founder identity/name;
- founded year;
- support and radicalism;
- active state;
- `IsRuling`;
- color seed;
- position, strategy and foreign stance;
- party traits;
- origin city;
- parent party/split lineage.

## Lifecycle

```csharp
PoliticalWorldAPI.DeactivateKingdomParty(
    kingdom,
    partyId
);

PoliticalWorldAPI.ReactivateKingdomParty(
    kingdom,
    partyId
);

// Equivalent general form:
PoliticalWorldAPI.SetKingdomPartyActive(
    kingdom,
    partyId,
    false
);
```

Deactivation is intentionally the public equivalent of removal.

### Why there is no hard delete

Political World can reference an old party ID from:

- election history;
- split/parent lineage;
- party biographies;
- city-local party support;
- ruling-party records and other saved political metadata.

Deleting every key would be fragile and could corrupt historical references.
Inactive parties remain inspectable with `includeInactive: true` and can later
be reactivated.

## Changing ideology

```csharp
bool ok = PoliticalWorldAPI.SetKingdomPartyIdeology(
    kingdom,
    partyId,
    AddonId + ".ideology_arcane"
);
```

Rules:

- the ideology must be a registered root ideology;
- active-party limits are enforced;
- the stable party ID/history/founder are preserved;
- a custom party name is preserved;
- a generated/default party name follows the new ideology;
- regional support is reconciled through Political World's existing system;
- an incompatible leader is replaced using the normal Political World leader
  selection logic when possible.

## Leadership

Read the resolved actor:

```csharp
Actor leader = PoliticalWorldAPI.GetKingdomPartyLeader(
    kingdom,
    partyId
);
```

Assign a specific actor:

```csharp
bool ok = PoliticalWorldAPI.SetKingdomPartyLeader(
    kingdom,
    partyId,
    actor
);
```

The actor must:

- be alive;
- belong to the kingdom;
- have the same citizen ideology as the party;
- not already lead another active party.

Or let Political World choose:

```csharp
PoliticalWorldAPI.AssignBestKingdomPartyLeader(
    kingdom,
    partyId
);
```

This uses the existing Political World leader-selection logic rather than
duplicating it inside the addon.

## Ruling party

```csharp
PoliticalWorldAPI.SetKingdomRulingParty(
    kingdom,
    partyId
);

PoliticalWorldAPI.ClearKingdomRulingParty(
    kingdom
);
```

Manual assignment is allowed only where a party mandate makes sense:
competitive-election systems or one-party systems.

The API does **not** write a fake election-history entry when Scenario Tools or
an addon manually changes the ruling party.

## New events

API 1.4 adds:

- `party.activated`
- `party.ideology.changed`
- `party.leader.changed`
- `kingdom.ruling-party.changed`

`party.leader.changed` is emitted both for explicit API assignments and for
Political World's automatic leader replacement.

`kingdom.ruling-party.changed` is emitted from the core ruling-party transition
itself, including normal elections and one-party succession.

This means addons should subscribe instead of polling party leadership/election
state.

## Compatibility

The old API 1.3 party methods still work:

- `CreateKingdomParty`
- `RenameKingdomParty`
- `SetKingdomPartyRadicalism`
- `SetKingdomPartySupport`
- `GetKingdomParties(kingdom)`

The new overloads and methods extend the same system.
