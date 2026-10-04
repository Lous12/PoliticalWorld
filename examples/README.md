# Political World API examples

These examples are intentionally small. Pick the one closest to what you want to build instead of copying half the core mod into an addon.

Current repository baseline: Political World 1.11.0, PoliticalWorldAPI 1.19.0, WorldBox 0.51.2 build 719, NeoModLoader 1.2.0.1.

| Example | What it shows | Minimum API used |
| --- | --- | ---: |
| `01_First_Addon` | Registering the smallest possible addon | 1.6 |
| `02_Custom_Ideology` | Registering an ideology | 1.6 |
| `03_Custom_Government` | Registering a government | 1.6 |
| `04_Event_Listener` | Subscribing to Political World events | 1.6 |
| `05_Rare_Event` | Registering a rare political event | 1.6 |
| `06_Scenario_Action` | Registering an explicit action | 1.6 |
| `07_Addon_Event` | Addon-owned custom events and payloads | 1.10 |
| `08_World_Data` | Capped world queries and addon-owned save data | 1.11 |
| `09_UI_Inspector` | Public inspector UI integration without patching PW windows | 1.12 |

## About the version numbers

`IsCompatible(1, 6)` does **not** mean the repository still uses API 1.6. It means that specific example only needs contracts that have existed since 1.6.

Use the lowest API version that actually provides every feature your addon uses. If you use a newer capability, raise the requirement. Do not blindly replace every minimum version with the current API number.

For new projects, start from `templates/PoliticalWorld-Addon-Template` and then copy only the relevant example code.

## Rules worth keeping

- Use `PoliticalWorldAPI` instead of reflecting into `Main`, `ScenarioBridge`, or private core classes.
- Namespace addon IDs and content IDs, for example `YourName.MyAddon.feature`.
- Prefer event hooks and registered actions over an addon `Update()` loop.
- World queries are capped on purpose. Do not turn an example into an unbounded actor scan.
- Addon-owned data should go through the public data helpers so keys do not collide with another addon.
- If the public API cannot do something safely, request a capability instead of bypassing it with reflection.
