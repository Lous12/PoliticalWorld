# Government Registry — API 1.5

Register a custom government without reimplementing Political World's core government simulation.

```csharp
PoliticalWorldAPI.RegisterGovernment(AddonId, new PoliticalWorldAPI.GovernmentDefinition
{
    Id = AddonId + ".dragon_monarchy",
    NameKey = "dragon_monarchy_name",
    DisplayName = "Dragon Monarchy",
    BaseArchetype = PoliticalWorldAPI.GovernmentArchetype.AbsoluteMonarchy,
    Tags = new[] { "dragon", "hereditary", "fantasy" }
});
```

`SetKingdomGovernment()` accepts both core and registered custom government IDs. The kingdom keeps the custom public identity, while Political World uses the selected archetype for existing optimized mechanics. If the addon is temporarily missing, the saved core archetype remains usable; when the addon returns, the custom identity can be resolved again.
