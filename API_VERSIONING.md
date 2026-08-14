# Public API versioning

Political World core and PoliticalWorldAPI are versioned independently.

Current candidate:
- Core: `1.7.0`
- Public API: `1.6.0`

## Compatibility rule

For API 1.x:
- major = breaking public contract;
- minor = backward-compatible public additions;
- patch = fixes that do not intentionally break documented public contracts.

An addon should request the minimum API it needs:

```csharp
if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
```

For optional functionality, prefer:

```csharp
PoliticalWorldAPI.HasCapability("political-event.rare")
```

Internal classes and folder layout are not covered by API compatibility guarantees.

## Deprecation

Before removing a public 1.x member, prefer to:
1. add the replacement;
2. document the old member as deprecated;
3. keep the old member functional for a migration window when practical;
4. remove it only in a future breaking major API unless there is a severe correctness/safety reason.
