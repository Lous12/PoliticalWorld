# Validation, requirements and diagnostics

PoliticalWorldAPI has three related layers:

1. validate definitions before registration;
2. check API/capability requirements;
3. collect diagnostics/support information after load.

## Validate content before registration

```csharp
var validation =
    PoliticalWorldAPI.ValidateGovernment(AddonId, definition);

if (!validation.IsValid)
{
    LogError(validation.Summary);
    return;
}
```

`ValidationResult.Issues` contains stable-style diagnostic codes, human-readable messages and `IsError`.

Warnings do not necessarily block registration. Errors do.

Registration methods also run their own validation, so explicit `Validate...` is mainly useful when you want to show a better error before calling the registration method.

## Check framework requirements

Modern addons can describe minimum API/capability requirements in `AddonDefinition`.

```csharp
var definition = new PoliticalWorldAPI.AddonDefinition
{
    Id = AddonId,
    Name = "My Addon",
    RequiredApiMajor = 1,
    RequiredApiMinor = 19,
    RequiredCapabilities = new[]
    {
        "world.lifecycle.events",
        "diagnostics.report"
    }
};

var check =
    PoliticalWorldAPI.Framework.CheckRequirements(definition);

if (!check.Compatible)
{
    LogError(check.Summary);
    return;
}
```

Do not require 1.19 if you do not actually use 1.19-era capabilities.

## Diagnostics

After load:

```csharp
PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

The addon diagnostics track registration counts, subscriptions, callback errors, warnings and errors.

Event/Rare Event callback exceptions are isolated and recorded instead of stopping dispatch for every other addon.

## Support report

API 1.19 adds a copy-paste-friendly framework support report:

```csharp
string report =
    PoliticalWorldAPI.Framework.GetSupportReport(AddonId);
```

This is useful for GitHub/Discord bug reports together with:

- WorldBox version/build;
- NeoModLoader version;
- Political World version;
- addon version;
- exact reproduction steps;
- `Player.log`;
- whether an existing save was involved.

## Ecosystem diagnostics

`PoliticalWorldAPI.Ecosystem` exposes framework/addon snapshots, compatibility state, event metrics and recorded framework issues.

Use these for developer tools and diagnostics, not for per-frame gameplay polling.
