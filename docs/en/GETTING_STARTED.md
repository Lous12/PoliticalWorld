# Your first Political World addon in 10 minutes

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
            if (!PoliticalWorldAPI.IsCompatible(1, 6))
            {
                LogError("Political World API 1.6+ is required.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(
                new PoliticalWorldAPI.AddonDefinition
                {
                    Id = AddonId,
                    Name = "My Political Addon",
                    Version = "0.1.0",
                    Author = "YourName",
                    Description = "My first Political World addon"
                }))
            {
                return;
            }

            LogInfo("Loaded through PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion);
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
```

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

Start WorldBox and check `Player.log`. A successful load should show your log entry and a diagnostics report without errors. If registration fails, inspect `ValidationResult`/diagnostics before reaching into Political World internals.

## Next

- governments: `governments.md`
- parties: `parties.md`
- rare events: `political-events.md`
- Scenario Tools actions: `actions.md`
- safe persistence: `data-storage.md`
