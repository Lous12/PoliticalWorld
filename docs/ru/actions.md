# Actions, Conditions и Effects

Action Registry позволяет аддону выставить явное действие над государством без хардкода этого аддона внутри Scenario Tools или другого host.

## Регистрация action

```csharp
PoliticalWorldAPI.RegisterAction(
    AddonId,
    new PoliticalWorldAPI.ActionDefinition
    {
        Id = AddonId + ".stabilize",
        Category = "politics",
        DisplayName = "Stabilize kingdom",
        Description = "Adds 5 stability.",
        SortOrder = 100,
        Condition =
            PoliticalWorldAPI.Conditions.StabilityAtMost(95),
        Handler = kingdom =>
            PoliticalWorldAPI.ChangeKingdomStability(kingdom, 5)
    }
);
```

Handler не запускается автоматически. Он выполняется только когда action явно вызывается через API/host.

## Conditions

Kingdom conditions можно собирать через helpers вроде `All`, `Any`, `Not` и конкретные политические проверки.

General framework также даёт Actor/City condition builders для addon-owned data/tags.

Лучше вынести eligibility в Condition, чем дублировать одну и ту же проверку в каждой UI-кнопке.

## Effects

Публичные helpers PW умеют менять политическое состояние государства, а `WorldEffects` даёт object-scoped эффекты для Actor/City data/tags.

Это всё равно явные операции. Регистрация/создание effect не запускает автоматический simulation loop.

## Inspection

Host может перечислить registered actions и проверить, доступны ли они выбранному государству.

Это нормальный паттерн для creator tools: аддон один раз регистрирует поведение, а host находит его через Public API.

## Ownership

Action ID должен принадлежать namespace аддона:

```text
YourName.MyAddon.stabilize
```

Не используйте чужой/core namespace.

Смотрите `examples/06_Scenario_Action`.
