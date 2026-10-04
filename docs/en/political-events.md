# Event Bus, custom events and Rare Political Events

Political World exposes one event bus for core transitions, addon events and world lifecycle notifications.

Use events when possible instead of scanning every kingdom/city every frame.

## Subscribe to a core event

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);
```

The callback receives `PoliticalEventData`.

Useful fields include:

- `EventId`
- `Kingdom`, `KingdomName`
- `OldValue`, `NewValue`, `OldNumber`, `NewNumber`
- `PartyId`
- `IdeologyId`, `CurrentId`, `GovernmentId`
- `TargetKingdom`, `TargetKingdomName`
- `WarSource`
- `Actor`, actor identity/name
- `City`
- `SourceAddonId`
- `Category`
- `Year`
- `Text`, `EventKey`
- `Payload` for custom addon events

Not every event populates every field.

## Current core event families

API 1.19 exposes events for:

- ideology/current/government changes;
- party creation, activation, deactivation, rename, ideology, leader, support and radicalism;
- ruling party and ruler changes;
- elections;
- dynamic country-name changes;
- war start/end;
- political and leadership crises;
- settlement separatism/autonomy/secession lifecycle;
- rare political events;
- published political events;
- world lifecycle: `WorldChanged`, `WorldReady`, `WorldUnavailable`.

Use `PoliticalWorldAPI.GetEventIds()` when a tool needs the current list.

`Events.All` subscribes to all dispatched events.

## Custom addon events

Register a namespaced event ID:

```csharp
PoliticalWorldAPI.RegisterAddonEvent(
    AddonId,
    AddonId + ".mana_crisis"
);
```

Publish it with an optional payload and typed world context:

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

Another addon can subscribe to that event ID through the same `Subscribe(...)` method.

## Callback isolation

One addon callback throwing an exception does not stop dispatch to other subscribers. Political World records the error in diagnostics.

There is also a recursion-depth guard, so do not build event loops that endlessly republish each other.

## Unsubscribe

Use `Unsubscribe(...)` for one handler or `UnsubscribeAll(AddonId)` when your runtime registration needs to be detached.

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

`ChancePermille` uses `0..1000`, so `30 = 3%`.

Rare events run from Political World's existing yearly political pipeline. They do not create a new addon `Update()` loop.

See also:

- `examples/04_Event_Listener`
- `examples/05_Rare_Event`
- `examples/07_Addon_Event`
- [World lifecycle](world-lifecycle.md)
