# Ideologies

Political World 1.11 / API 1.19 exposes ideology registration, hierarchy metadata and kingdom ideology changes through the public API.

## Registration

Use `IdeologyDefinition`. Public content IDs must belong to your addon namespace.

```csharp
PoliticalWorldAPI.RegisterIdeology(
    AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".arcane_reformism",
        ParentId = "",
        NameKey = AddonId + ".arcane_reformism",
        DisplayName = "Arcane Reformism",
        Description = "A small example ideology.",
        HighSupportStability = 2,
        SupportThreshold = 55,
        LowSupportStability = -2,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Family = "reformist",
        Rarity = "uncommon",
        Radicalism = 25,
        Tags = new[] { "magic", "reformist" }
    }
);
```

`DisplayName` and `Description` are readable fallbacks. Locale files or `RegisterLocalization(...)` can provide translated text.

## Hierarchy

`ParentId = ""` creates a root ideology.

For a current/child ideology, set `ParentId` to an existing ideology ID. Read-side `IdeologyInfo` exposes `ParentId`, `RootId` and `Tier`, so an addon or tool can render the tree without touching internal classes.

## RandomWeight

`RandomWeight` controls participation in Political World's generic random seeding.

- `0` — no generic random seeding;
- positive value — the addon explicitly allows generic natural seeding.

For most content addons, `0` is the safe starting point.

## Metadata

API 1.15+ ideology metadata includes:

- `Family`
- `Rarity`
- `Radicalism`
- `IsSecret`
- `Tags`

These fields are metadata. A custom tag or rarity does not magically create new core gameplay unless some system explicitly reads it.

## Reading

Useful public methods include:

```csharp
PoliticalWorldAPI.GetIdeologies();
PoliticalWorldAPI.GetIdeology(id);
PoliticalWorldAPI.GetIdeologiesByAddon(AddonId);
PoliticalWorldAPI.GetRootIdeologies();
PoliticalWorldAPI.GetCurrentsForRoot(rootId);
PoliticalWorldAPI.GetIdeologyTags(id, includeParents: true);
PoliticalWorldAPI.HasIdeologyTag(id, "magic", includeParents: true);
```

## Changing a kingdom

Use the public state operations:

```csharp
PoliticalWorldAPI.SetKingdomIdeology(kingdom, ideologyId);
PoliticalWorldAPI.SetKingdomCurrent(kingdom, currentId);
```

These go through Political World's existing state path and publish the corresponding core events.

Do not write raw internal ideology/current keys directly into `kingdom.data`.
