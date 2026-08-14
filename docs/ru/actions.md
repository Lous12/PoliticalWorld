# Scenario Actions

Action Registry позволяет аддону зарегистрировать явное действие над выбранным государством. Scenario Tools сможет перечислять такие действия через `GetActions(kingdom)` без жёсткой зависимости от конкретного аддона.

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

`Condition` может быть `null` или собрана через `Conditions.All/Any/Not/...`.

Регистрация не запускает никакой симуляции: handler выполняется только при `ExecuteAction`.

Для ID действует ownership rule — используйте namespace вашего AddonId.
