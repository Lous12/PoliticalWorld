# PoliticalWorldAPI 1.19 Reference

Political World 1.11 ships **PoliticalWorldAPI 1.19.0**.

The canonical implementation is the public `Lous12.PoliticalWorld.PoliticalWorldAPI` facade under `src/PoliticalWorld/API/`. Addons should use that surface instead of depending on `Main`, `ScenarioBridge`, Harmony patches or private UI/runtime classes.

For a source-file map, see [`src/PoliticalWorld/API/README.md`](../../src/PoliticalWorld/API/README.md). For copyable code, see the [`examples/` index](../../examples/README.md).

## Version constants

```csharp
PoliticalWorldAPI.ApiVersion // "1.19.0"
PoliticalWorldAPI.ApiMajor   // 1
PoliticalWorldAPI.ApiMinor   // 19
```

## Registration and compatibility

A current addon can describe both its minimum API and the capabilities it needs:

```csharp
var definition = new PoliticalWorldAPI.AddonDefinition
{
    Id = "YourName.MyAddon",
    Name = "My Addon",
    Version = "0.1.0",
    Author = "YourName",
    RequiredApiMajor = 1,
    RequiredApiMinor = 19,
    RequiredCapabilities = new[]
    {
        "framework.requirements-check",
        "diagnostics.report"
    }
};

var check = PoliticalWorldAPI.Framework.CheckRequirements(definition);
if (!check.Compatible)
{
    // check.Summary is suitable for a log/error message.
    return;
}

PoliticalWorldAPI.RegisterAddon(definition);
```

Do **not** require 1.19 automatically if your addon only uses older 1.x contracts. The minimum version should describe the oldest API that actually contains everything you use.

## Main public areas

### Content and political state

- addon registration and validation;
- ideology registration/query;
- government registration/query and archetypes;
- party and ruling-party access;
- kingdom political state, stability and tags;
- actions, conditions and effects;
- race political profiles and country-name templates;
- monarchy rank access.

Monarchy rank example:

```csharp
string rank = PoliticalWorldAPI.Countries.GetMonarchyRank(kingdom);
```

### Events

Use the event bus instead of polling when possible.

Core event IDs live in `PoliticalWorldAPI.Events`. Addons can also register and publish namespaced custom events through `RegisterAddonEvent` / `PublishAddonEvent`.

See `examples/04_Event_Listener` and `examples/07_Addon_Event`.

### Addon-owned data and tags

`PoliticalWorldAPI.Data` stores namespaced values on Actor/City/Kingdom objects. Use these helpers instead of inventing raw core keys.

The migration registry in `PoliticalWorldAPI.Migrations` exists for addon schema changes and is applied explicitly to objects the addon actually touches.

See `examples/08_World_Data`.

### World access and lifecycle

`PoliticalWorldAPI.WorldQuery` exposes capped snapshots of live world objects. Queries are deliberately bounded.

Lifecycle state is available through `PoliticalWorldAPI.Lifecycle`, while lifecycle notifications use the normal event bus (`WorldChanged`, `WorldReady`, `WorldUnavailable`).

Do not replace capped queries with an unbounded per-frame world scan.

### UI integration

`PoliticalWorldAPI.UI` exposes inspector sections, context actions and hosted kingdom/settlement Politics pages.

Political World owns the host navigation and lifecycle. Addons should render through the public context instead of Harmony-patching private windows.

See `examples/09_UI_Inspector`.

### Warfare

`PoliticalWorldAPI.Warfare` exposes read helpers and war-start operations.

Normal declaration goes through Political World's diplomatic interception. The force path intentionally bypasses it. Do not use forced war simply because a normal declaration was rejected.

### Ecosystem / release / diagnostics

API 1.19 provides:

- framework release metadata;
- API/capability requirement checks;
- addon ecosystem snapshots;
- event metrics;
- diagnostics/support reports;
- runtime cleanup of detachable addon registrations;
- deprecation notices.

`PoliticalWorldAPI.Framework.GetSupportReport(addonId)` is useful in bug reports.

## Compatibility rules

PoliticalWorldAPI uses a major/minor model:

- a major bump may break contracts;
- a minor bump adds capabilities while preserving existing 1.x contracts where practical;
- private core implementation is not covered by this promise.

Use [API versioning](../../API_VERSIONING.md) for the repository policy.

## Related docs

- [Getting Started](GETTING_STARTED.md)
- [Ideologies](ideologies.md)
- [Governments](governments.md)
- [Parties](parties.md)
- [Events](political-events.md)
- [World lifecycle](world-lifecycle.md)
- [Data and migrations](data-storage.md)
- [General framework](general-framework.md)
- [UI integration](ui-integration.md)
- [Warfare](warfare.md)
- [Validation & diagnostics](validation-diagnostics.md)
- [Compatibility & versioning](compatibility-versioning.md)
- [Common mistakes](common-mistakes.md)
- [Framework Vision](FRAMEWORK_VISION.md)
- [What can you build?](what-you-can-build.md)
