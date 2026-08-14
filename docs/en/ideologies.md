# Ideologies

## Registration

Use `IdeologyDefinition`. The ID must belong to your namespace, for example `Author.Mod.magocracy`.

```csharp
PoliticalWorldAPI.RegisterIdeology(AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".magocracy",
        ParentId = "",
        NameKey = AddonId + ".magocracy",
        HighSupportStability = 2,
        SupportThreshold = 55,
        LowSupportStability = -2,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Tags = new[] { "magic", "elitist" }
    });
```

## Hierarchy

`ParentId = ""` creates a root ideology. For a current/child, use an existing ideology ID as the parent. Political World exposes `ParentId`, `RootId`, and `Tier`, so external tools can render the tree without internal classes.

## RandomWeight

- `0` — does not participate in generic random seeding;
- positive value — the author explicitly opts into natural generic seeding.

For content addons, `0` is the safe default.

## Tags

Tags are metadata for interoperability and conditions between addons. Prefer short stable strings such as `magic`, `dragon`, `theocratic`, `reformist`. Arbitrary tags do not automatically create core mechanics.

## Reading

`GetIdeologies`, `GetRootIdeologies`, `GetCurrentsForRoot`, `GetIdeologyTags`, `HasIdeologyTag`.

## Changing a kingdom

`SetKingdomIdeology` and `SetKingdomCurrent` reuse Political World's existing state logic and publish the related core events.
