# Standalone NeoModLoader mod

Not every project should depend on Political World. The SDK contains `WorldBox-NeoMod-Starter`, which only depends on NML.

Minimal entry class:

```csharp
using NeoModLoader.api;

namespace YourName.MyWorldBoxMod
{
    public class Main : BasicMod<Main>
    {
        protected override void OnModLoad()
        {
            LogInfo("Loaded.");
        }
    }
}
```

`BasicMod<T>` is NML's base class for simple mods. NML calls `OnModLoad` and then the normal Unity lifecycle; `BasicMod` also creates a feature manager and loads the `Locales` directory.

For UI, start with the NML feature API described in `nml-ui-recipes.md`.

Only add Political World as a dependency when the mod actually needs political integration. Avoid unnecessary hard dependencies.
