<p align="center">
  <img src="docs/assets/icon.png" width="128" alt="Political World icon">
</p>

<h1 align="center">Political World</h1>

<p align="center"><strong>A political framework for WorldBox — built so anyone can create.</strong></p>

<p align="center">
  <a href="README_RU.md">Русский</a> ·
  <a href="docs/en/GETTING_STARTED.md">Addon quick start</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.7.0%20Beta-blue">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.6.0-blueviolet">
  <img alt="WorldBox" src="https://img.shields.io/badge/WorldBox%20PC-0.51.2-informational">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NeoModLoader-1.2.0.1-informational">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green">
</p>

Political World expands WorldBox with ideologies, parties, governments, political crises, elections, blocs, summits, political warfare consequences and a Political Map. Starting with 1.7, it is also a public addon platform: third-party mods can extend the political simulation through a documented API instead of editing Political World internals.

The project follows one idea: **modding should be a place where people learn by creating**. You should be able to start with one ideology or one event and gradually grow into a complete political overhaul.

## For players

Political World adds a lightweight, event-driven political layer without trying to simulate every citizen every frame. Major systems include:

- ideology trees and currents;
- political parties, leaders, support and radicalism;
- multiple government and political-system archetypes;
- elections, councils, party congresses and leadership changes;
- political stability and crises;
- international blocs, vanilla-alliance synchronization and physical leader summits;
- war preparation, political crises, war exhaustion and peace logic;
- Political Map modes for parties, ideologies and tension.

## For modders

The public entry point is:

```csharp
using Lous12.PoliticalWorld;

if (!PoliticalWorldAPI.IsCompatible(1, 6))
    return;
```

Addons can register or control ideologies, custom governments, parties, kingdom state, Scenario Actions, Event Bus subscriptions, rare political events, private addon data/tags and diagnostics. Internal classes such as `Main` and `ScenarioBridge` are not part of the public contract.

```text
WorldBox + NeoModLoader
        ↓
Political World Core
        ↓
PoliticalWorldAPI 1.6
        ↓
Your addon / Scenario Tools / Fantasy Politics / community mods
```

Start here: **[Create your first addon](docs/en/GETTING_STARTED.md)**.

## Repository layout

```text
src/PoliticalWorld/    Runtime mod source
examples/              Complete small Political World addon examples
templates/             Political World addon + standalone NML starter
docs/en/               English developer documentation
docs/ru/               Russian developer documentation
AI_START_HERE.md       Entry point for AI coding assistants
ARCHITECTURE.md        Project boundaries and module layout
API_VERSIONING.md      Public API compatibility policy
```

## Compatibility

| Component | Target |
|---|---|
| Political World | 1.7.0 public beta candidate |
| Public API | 1.6.0 |
| WorldBox PC | 0.51.2 / build 719 |
| NeoModLoader | 1.2.0.1 |

WorldBox updates can require changes in `Core/Integration/WorldBox/`. Public API versioning is separate from WorldBox compatibility.

## Documentation

English: [documentation index](docs/en/README.md) · [API reference](docs/en/API_REFERENCE_1_6.md) · [NML UI recipes](docs/en/nml-ui-recipes.md) · [Using AI](docs/en/using-ai.md)

Russian: [индекс документации](docs/ru/README.md) · [справочник API](docs/ru/API_REFERENCE_1_6.md) · [NML UI recipes](docs/ru/nml-ui-recipes.md) · [разработка с ИИ](docs/ru/using-ai.md)

## Contributing

Contributions, examples, documentation fixes and addon API proposals are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) and [ARCHITECTURE.md](ARCHITECTURE.md) first. Keep Russian and English developer documentation equivalent when changing public behavior.

## License

Political World is released under the [MIT License](LICENSE). Fork it, study it, change it, build on it, and keep creating.

WorldBox belongs to Maxim Karpenko / its respective rights holders. Political World is a community mod and is not an official WorldBox project.
