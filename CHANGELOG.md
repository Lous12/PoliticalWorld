# Changelog

## 1.7.0 — Public Beta Candidate

### Developer platform
- Public `PoliticalWorldAPI 1.6.0`.
- Addon registry, validation and diagnostics.
- Collision-safe addon-owned kingdom data and tags.
- Event Bus with isolated third-party callbacks.
- Expanded Party API.
- Custom Government Registry with base archetypes.
- Rare Political Event Registry integrated into Political World's staggered political cycle.
- Scenario Action registration.
- Separate addon and standalone NeoModLoader templates.
- Equal RU/EN developer documentation and AI-oriented entry points.

### Architecture
- Replaced the old ~43k-line monolithic `Main.cs` with focused runtime modules.
- `Main.cs` is now only the NeoModLoader entry declaration.
- Compatibility-sensitive WorldBox/Harmony code is isolated under `Core/Integration/WorldBox/`.
- Political Map, governments, parties, ideologies, elections, crises, international blocs/summits, warfare and UI live in dedicated modules.

### Identity and compatibility
- Project GUID/namespace standardized on `Lous12.PoliticalWorld`.
- Historical `ukiol_*` gameplay/save identifiers remain intentionally preserved for compatibility.

## 1.6.0

- Expanded ideology and political-control systems.
- City political identity and ideology effects.
- Party customization and political map.
- Political World bloc ↔ vanilla Alliance synchronization.
- Quality-of-life and compatibility work for WorldBox PC 0.51.2 / build 719.
