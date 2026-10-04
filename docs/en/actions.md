# Actions, Conditions and Effects

The Action Registry lets an addon expose an explicit kingdom action without hard-coding that addon into Scenario Tools or another host.

## Register an action

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

The handler does not run automatically. It runs only when the action is explicitly executed through the API/host.

## Conditions

Kingdom conditions can be composed through helpers such as `All`, `Any`, `Not` and specific political checks.

The general framework also exposes Actor/City condition builders for addon-owned data/tags.

Use a condition instead of duplicating the same eligibility logic in every UI button.

## Effects

Political World's public helpers include reusable state operations for kingdom politics, while `WorldEffects` provides object-scoped Actor/City data/tag effects.

Effects are still explicit code. Registering an effect does not create an automatic simulation loop.

## Inspection

Hosts can enumerate registered actions and inspect whether they are enabled for a selected kingdom.

This is the preferred pattern for creator tools: addon registers behavior once, host discovers it through the public contract.

## Ownership

Action IDs must be namespaced to the addon:

```text
YourName.MyAddon.stabilize
```

Do not reuse another addon/core namespace.

See `examples/06_Scenario_Action`.
