# Validation and Developer Diagnostics

Before registering a complex object, call `Validate...` when you want a human-readable failure reason.

```csharp
var validation = PoliticalWorldAPI.ValidateGovernment(AddonId, definition);
if (!validation.IsValid)
{
    LogError(validation.Summary);
    return;
}
```

Codes such as `PW203`, `PW403`, and `PW508` are designed for stable diagnostics. A warning does not have to block registration; an error does.

After addon load, a useful call is:

```csharp
PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

The report includes registered ideologies, governments, actions, rare events, subscriptions, callback errors, warnings, and errors.

If an Event Bus or rare-event callback throws, Political World catches it, records diagnostics, and continues running other addons.
