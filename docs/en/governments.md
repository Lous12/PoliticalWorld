# Governments

Custom governments reuse one of Political World's supported mechanical archetypes while keeping their own public ID, display name, localization and tags.

## Registration

```csharp
PoliticalWorldAPI.RegisterGovernment(
    AddonId,
    new PoliticalWorldAPI.GovernmentDefinition
    {
        Id = AddonId + ".dragon_monarchy",
        NameKey = AddonId + ".dragon_monarchy",
        DisplayName = "Dragon Monarchy",
        Description = "A custom hereditary government.",
        BaseArchetype = PoliticalWorldAPI.GovernmentArchetype.AbsoluteMonarchy,
        Tags = new[] { "dragon", "hereditary", "fantasy" }
    }
);
```

Supported archetypes are:

- `AbsoluteMonarchy`
- `ConstitutionalMonarchy`
- `ParliamentaryRepublic`
- `PresidentialRepublic`
- `OnePartyState`
- `MilitaryDictatorship`
- `CouncilRepublic`
- `Oligarchy`

`Unknown` is not a valid custom-government base.

## Why archetypes exist

The custom government keeps its own public identity, but Political World can reuse tested election/leadership/system behavior instead of every addon reimplementing the simulation.

If an addon is temporarily missing, the saved core archetype can still remain usable. When the addon is present again, the custom identity can be resolved again.

## Reading

```csharp
var all = PoliticalWorldAPI.GetGovernmentForms();
var one = PoliticalWorldAPI.GetGovernment(governmentId);
var mine = PoliticalWorldAPI.GetGovernmentsByAddon(AddonId);

var tags = PoliticalWorldAPI.GetGovernmentTags(governmentId);
bool hereditary = PoliticalWorldAPI.HasGovernmentTag(governmentId, "hereditary");
```

`GovernmentInfo.Source` tells you whether the entry comes from Political World core or an addon.

## Changing a kingdom

```csharp
PoliticalWorldAPI.SetKingdomGovernment(
    kingdom,
    governmentId,
    publishEvent: true
);
```

Use the public method instead of changing raw government IDs in save data. The public path keeps Political World's state/event handling in one place.

## Political systems are separate

Government form and political system are related but not identical.

Use `GetPoliticalSystems()` / `GetPoliticalSystem(id)` and the stable `PoliticalWorldAPI.PoliticalSystems` constants when you need system metadata. Do not hardcode legacy `ukiol_*` strings when a public constant exists.
