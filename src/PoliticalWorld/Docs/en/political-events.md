# Event Bus and Rare Political Events — API 1.7

## Why

An addon should not scan every kingdom every frame just to detect political changes. Political World emits events from existing state transitions and provides a rare-event registry inside the existing yearly political pipeline.

## Core Event Bus

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);
```

The handler receives `PoliticalEventData`: `EventId`, `Kingdom`, old/new values, PartyId, Actor, names, SourceAddonId, Category, Year, Text, and EventKey.

Known API 1.7 events:

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

`Events.All` subscribes to every known event.

Each addon callback is isolated: exceptions are recorded in diagnostics and dispatch continues for other subscribers. The bus also has a recursion-depth guard.

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

`ChancePermille` is 0..1000: 30 = 3%. The registry runs from the existing yearly political pipeline and does not create an addon `Update()`.

Cooldown is stored per kingdom/event through Political World's namespaced state.


## Manual rare-event execution — API 1.7

Scenario/director tools can explicitly execute a registered rare political event without bypassing Political World's public API:

```csharp
if (PoliticalWorldAPI.CanExecuteRarePoliticalEvent(eventId, kingdom))
{
    PoliticalWorldAPI.ExecuteRarePoliticalEvent(eventId, kingdom);
}
```

Manual execution skips the random chance, normal check interval and current cooldown because the caller explicitly requested the event. The registered `Condition` is still evaluated. A successful execution records the current world year as the last fire year so the normal yearly pipeline respects the event cooldown afterwards.

Capability: `political-event.rare.execute`.
