# Scenario Actions

The Action Registry lets an addon register an explicit action for a selected kingdom. Scenario Tools can enumerate these through `GetActions(kingdom)` without hard-coding dependencies on each addon.

```csharp
PoliticalWorldAPI.RegisterAction(AddonId,
    new PoliticalWorldAPI.ActionDefinition
    {
        Id = AddonId + ".stabilize",
        Category = "politics",
        DisplayName = "Stabilize kingdom",
        Description = "Adds 5 stability.",
        SortOrder = 100,
        Condition = PoliticalWorldAPI.Conditions.StabilityAtMost(95),
        Handler = kingdom => PoliticalWorldAPI.ChangeKingdomStability(kingdom, 5)
    });
```

`Condition` may be `null` or composed using `Conditions.All/Any/Not/...`.

Registration creates no simulation loop: the handler only runs when `ExecuteAction` is called.

IDs follow the ownership rule — use your AddonId namespace.
