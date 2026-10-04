# API compatibility and versioning

Political World and its Public API have separate versions.

- Core mod: **1.11.0**
- Public API: **1.19.0**
- Target game build: WorldBox PC **0.51.2 / build 719**

An addon should check the minimum API version it actually needs.

Example for an addon that only requires API 1.6-era capabilities:

```csharp
if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
```

Do not blindly require 1.19 if your addon does not use anything added that late.

## API 1.x policy

- **Major** changes are reserved for breaking public API changes.
- **Minor** releases add compatible capabilities/public surface.
- **Patch** releases fix behavior without intentionally breaking documented contracts.

For optional behavior, prefer capability checks:

```csharp
if (PoliticalWorldAPI.HasCapability("political-event.rare"))
{
    // optional behavior
}
```

Internal Political World modules are not part of the addon compatibility contract.

`targetGameBuild` describes WorldBox compatibility and is separate from the Political World API version.
