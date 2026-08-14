# Political World API 1.3 — Event Bus and Developer Diagnostics

API 1.3 adds an event-driven extension layer so addons can react to Political World
without creating their own permanent polling loop.

## New capabilities

- `event.subscribe`
- `diagnostics`

Existing API 1.2 validation, addon-private tags and collision-safe saved data remain unchanged.

## Subscribe to events

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);

private static void OnGovernmentChanged(
    PoliticalWorldAPI.PoliticalEventData data
)
{
    if (data == null || data.Kingdom == null)
        return;

    // data.OldValue -> previous government id
    // data.NewValue -> new government id
}
```

Use `PoliticalWorldAPI.GetEventIds()` to query the events supported by the installed API.

API 1.3 audited event ids:

- `kingdom.ideology.changed`
- `kingdom.current.changed`
- `kingdom.government.changed`
- `party.created`
- `party.deactivated`
- `party.renamed`
- `party.radicalism.changed`
- `party.support.changed`
- `political.event.published`

`PoliticalWorldAPI.Events.All` (`"*"`) can be used for developer tools that need
all Political World API events.

### Important event semantics

Ideology/current/government changes and party creation/deactivation are emitted
from core transition points, so natural Political World changes are visible.

Party rename/radicalism/support events currently describe explicit writes through
the public Scenario/Addon API. They are intentionally not emitted for every
background support calculation, which avoids event spam.

More event hooks will be added only after their core transition points are audited.

## Callback isolation

Every addon callback is wrapped in `try/catch`.

If addon A throws an exception while handling an event:

- Political World continues running;
- addon B still receives the event;
- the error is logged;
- addon A's diagnostics records the callback failure.

Event metadata is copied for each subscriber so one addon cannot rewrite the event
fields seen by another addon.

A recursion depth guard also prevents a broken callback from creating an unlimited
event -> write -> event loop.

## Unsubscribe

```csharp
PoliticalWorldAPI.Unsubscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);

PoliticalWorldAPI.UnsubscribeAll(AddonId);
```

## Developer Diagnostics

```csharp
PoliticalWorldAPI.AddonDiagnostics info =
    PoliticalWorldAPI.GetAddonDiagnostics(AddonId);

string report =
    PoliticalWorldAPI.GetDiagnosticsReport(AddonId);

PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

The report includes:

- registered ideologies;
- registered actions;
- event subscriptions;
- callback errors;
- warnings;
- errors;
- recent diagnostic messages.

Example:

```text
[Political World API]
API: 1.3.0
Addon: Vampire Politics [Author.VampirePolitics]
Registered ideologies: 3
Registered actions: 2
Event subscriptions: 4
Callback errors: 0
Warnings: 0
Errors: 0
```

## Performance rule

The Event Bus has no `Update()` loop. It only runs when Political World reaches an
audited state transition. Addons should prefer subscriptions over repeatedly
scanning kingdoms or actors.
