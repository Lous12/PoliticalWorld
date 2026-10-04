# Political World Addon Template

Starter project for a new addon using the current Political World public API.

Baseline used by this template:
- Political World 1.11.0
- PoliticalWorldAPI 1.19.0
- WorldBox 0.51.2 build 719
- NeoModLoader 1.2.0.1

Before publishing:
1. rename the folder and namespace;
2. replace `YourName.MyPoliticalAddon` in both `Main.cs` and `mod.json`;
3. replace the addon name/author/version;
4. keep `Lous12.PoliticalWorld` as a dependency;
5. set the **minimum** API version and capabilities your actual code needs.

The template starts at API 1.19 because it demonstrates `Framework.CheckRequirements`. A simple addon may support an older 1.x minor version if it only uses older contracts.

Start with `docs/en/GETTING_STARTED.md`, then use the matching folder under `examples/`.

Do not depend on `Main`, `ScenarioBridge`, private fields, or reflection into Political World internals. If something useful is missing from the public surface, open an API capability request instead.
