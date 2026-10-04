# Your first Political World addon in 10 minutes

Current baseline: Political World **1.11.0**, PoliticalWorldAPI **1.19.0**, WorldBox **0.51.2 build 719**, NeoModLoader **1.2.0.1**.

This guide uses the current API 1.19 template. After your addon works, you can lower its minimum API requirement if every contract you use existed in an older 1.x release.

## 1. Folder structure

Create a separate folder next to Political World:

```text
Mods/
├── PoliticalWorld/
└── MyPoliticalAddon/
    ├── mod.json
    ├── Main.cs
    └── Locales/
        ├── en.json
        └── ru.json
```

Do not place addon source files inside `PoliticalWorld/`: NML recursively compiles `.cs` files inside each mod folder.

## 2. mod.json

```json
{
  "name": "My Political Addon",
  "GUID": "YourName.MyPoliticalAddon",
  "author": "YourName",
  "version": "0.1.0",
  "description": "Example Political World addon",
  "targetGameBuild": 719,
  "Dependencies": ["Lous12.PoliticalWorld"],
  "OptionalDependencies": [],
  "IncompatibleWith": []
}
```

Keep the addon `GUID` stable. The same ID is a good `AddonId` and prefix for every public content ID.

## 3. Minimal Main.cs

```csharp
using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace YourName.MyPoliticalAddon
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "YourName.MyPoliticalAddon";

        protected override void OnModLoad()
        {
            PoliticalWorldAPI.AddonDefinition definition =
                new PoliticalWorldAPI.AddonDefinition
                {
                    Id = AddonId,
                    Name = "My Political Addon",
                    Version = "0.1.0",
                    Author = "YourName",
                    Description = "My first Political World addon",
                    RequiredApiMajor = 1,
                    RequiredApiMinor = 19,
                    RequiredCapabilities = new[]
                    {
                        "framework.requirements-check",
                        "diagnostics.report"
                    }
                };

            PoliticalWorldAPI.AddonRequirementCheck requirements =
                PoliticalWorldAPI.Framework.CheckRequirements(definition);

            if (!requirements.Compatible)
            {
                LogError(requirements.Summary);
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(definition))
            {
                return;
            }

            LogInfo("Loaded through PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion);
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
```

The template requires API 1.19 because `Framework.CheckRequirements` is an API 1.19 feature. A simpler addon can support an older 1.x minor version if it only uses older contracts.

## 4. Add a first ideology

After `RegisterAddon`:

```csharp
PoliticalWorldAPI.RegisterIdeology(
    AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".magical_reformism",
        ParentId = "",
        NameKey = AddonId + ".magical_reformism",
        SupportThreshold = -1,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Tags = new[] { "magic", "reformist" }
    }
);
```

`RandomWeight = 0` means the ideology will not be randomly seeded by Political World's generic seeding logic.

## 5. Test

Start WorldBox and check `Player.log`. A successful load should show your log entry and a diagnostics report without errors.

If registration fails, inspect the diagnostics or `Framework.GetSupportReport(AddonId)` before reaching into Political World internals.

For changes involving saves, UI, world lifecycle, wars or diplomacy, test both a fresh world and an existing save.

## 6. Do not bypass the API because something is annoying

If you cannot find a public method, first check:

- [API 1.19 Reference](API_REFERENCE_1_19.md)
- `src/PoliticalWorld/API/README.md`
- `examples/`

Do not assume `Main`, `ScenarioBridge` or a private field is a supported addon contract. If the API really cannot do what you need, open a capability request.

## Next

- governments: `governments.md`
- parties: `parties.md`
- rare events: `political-events.md`
- actions: `actions.md`
- safe persistence: `data-storage.md`
- world lifecycle and queries: `API_REFERENCE_1_19.md`
- small copyable patterns: `../../examples/README.md`
