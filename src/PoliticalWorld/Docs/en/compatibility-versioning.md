# API compatibility and versioning

Political World and its Public API have separate versions.

- Core mod: `1.7.0` public beta candidate.
- Public API: `1.6.0`.
- Target game build: WorldBox PC build `719` (`0.51.2`).

An addon that needs API 1.6 should check:

```csharp
if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
```

## API 1.x policy

- **Major** changes are reserved for breaking public API changes.
- **Minor** changes add compatible capabilities and public surface.
- **Patch** changes fix behavior without intentionally breaking documented contracts.

For optional behavior, use capability checks instead of guessing from the version string:

```csharp
if (PoliticalWorldAPI.HasCapability("political-event.rare"))
{
    // register optional rare events
}
```

Do not inspect private classes or use reflection into `Main` or `ScenarioBridge` as a substitute for the public API. Internal modules may change without compatibility guarantees.

`targetGameBuild` is WorldBox compatibility and is separate from the Political World API version.
