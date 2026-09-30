# PoliticalWorldAPI 1.19 Reference

Political World 1.11 ships **PoliticalWorldAPI 1.19.0**.

The API is intentionally split across partial files under `src/PoliticalWorld/API/`. Addons should use the public `Lous12.PoliticalWorld.PoliticalWorldAPI` facade instead of internal `Main` classes whenever possible.

## Main areas

- Addon registration, compatibility checks and capability discovery
- Ideology and government registration
- Party, kingdom and political-state access
- Events and rare political events
- Actions, conditions and effects
- Addon data, tags and localization helpers
- World lifecycle / world access helpers
- Warfare helpers
- UI integration and addon pages
- Release metadata and diagnostics
- Politics expansion helpers, including monarchy rank access

## Version constants

```csharp
PoliticalWorldAPI.ApiVersion // "1.19.0"
PoliticalWorldAPI.ApiMajor   // 1
PoliticalWorldAPI.ApiMinor   // 19
```

## Monarchy ranks

1.11 exposes the current monarchy rank through:

```csharp
string rank = PoliticalWorldAPI.Countries.GetMonarchyRank(kingdom);
```

Rank-aware addon definitions can also use rank IDs where supported.

## Compatibility

Prefer declaring the minimum API version your addon actually needs rather than requiring the newest version without reason. The API is designed to grow additively where possible.

For concrete signatures, use the source files in `src/PoliticalWorld/API/`; they are the canonical reference for 1.19.

See also:

- [Getting Started](GETTING_STARTED.md)
- [Compatibility & versioning](compatibility-versioning.md)
- [Common mistakes](common-mistakes.md)
- [Framework Vision](FRAMEWORK_VISION.md)
