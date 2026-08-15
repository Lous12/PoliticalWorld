<p align="center">
  <img src="docs/assets/icon.png" width="128" alt="Political World icon">
</p>

<h1 align="center">Political World</h1>

<p align="center"><strong>A WorldBox politics mod and open addon framework — built so anyone can create.</strong></p>

<p align="center">
  <a href="README_RU.md">Русский</a> ·
  <a href="https://lous12.github.io/PoliticalWorld/">Website</a> ·
  <a href="docs/en/GETTING_STARTED.md">Addon quick start</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/discussions">Discussions</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.7.0%20Beta-blue">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.9.0-blueviolet">
  <img alt="WorldBox" src="https://img.shields.io/badge/WorldBox%20PC-0.51.2-informational">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NeoModLoader-1.2.0.1-informational">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green">
</p>

> **Play it as a politics mod. Use it as a framework. Build something we never planned.**

Political World started as a political expansion for WorldBox. It adds ideologies, parties, governments, elections, crises, international blocs, summits, war-related politics and a Political Map.

It is now also becoming an **open addon framework**. The public `PoliticalWorldAPI` exists so creators can build on a stable contract instead of editing Political World internals or depending on fragile implementation details.

The framework is intentionally growing **beyond politics**. Politics remains Political World's own gameplay layer, but third-party addons should not be forced to be political. General systems such as addon registration, localization fallback, data storage, tags, events, conditions, effects, actions and diagnostics are being developed as reusable building blocks for any kind of addon.

## For players

Political World creates the feeling of deep politics without trying to simulate every citizen every frame.

Current gameplay includes:

- ideology trees and currents;
- political parties, leaders, support and radicalism;
- government and political-system archetypes;
- elections, councils, party congresses and leadership changes;
- stability, crises, rebellions, coups and revolutions;
- international blocs and vanilla Alliance synchronization;
- physical leader summits;
- war preparation, diplomatic crises and war exhaustion;
- Political Map modes for parties, ideologies and political tension.

## For creators

The public entry point remains stable:

```csharp
using Lous12.PoliticalWorld;

if (!PoliticalWorldAPI.IsCompatible(1, 9))
    return;
```

API 1.9 provides creator-facing systems for:

- addon registration and validation;
- optional localization with readable fallback text;
- ideologies and custom governments;
- parties, kingdom political state and rulers;
- Actions and Rare Events;
- Event Bus subscriptions;
- Conditions and reusable Effects;
- addon-private `int`, `string`, `bool` and `float` data;
- kingdom and party addon data;
- shared and addon-private tags;
- diagnostics and structured operation results;
- batch registration and addon content discovery.

Localization files are **not mandatory**. An English-only addon can still display readable English text for a player using another language when no translation exists.

Political-specific APIs are only one module of the project. The next direction is a more general framework for world objects, custom content, UI/inspectors and cross-addon events.

```text
WorldBox + NeoModLoader
        ↓
Political World
  ├─ Political gameplay
  └─ Public framework
        ↓
PoliticalWorldAPI 1.9
        ↓
Your addon
  ├─ politics
  ├─ fantasy
  ├─ religion
  ├─ economy
  ├─ events
  ├─ tools
  └─ whatever you build next
```

The important rule is simple:

> If an addon needs to bypass the public API to reach Political World internals, that is a signal that the public API should probably be improved.

## Why a general framework?

We do not want Political World to decide what kind of mod you are allowed to make.

A creator may want to build:

- one ideology or government;
- a fantasy politics pack;
- religions or cults;
- magic systems;
- economy or trade extensions;
- disease and disaster systems;
- dynasties and character mechanics;
- scenario/director tools;
- custom events;
- creator utilities;
- integrations between several independent addons.

Not every example above is a built-in Political World system today. The goal of the framework is to provide reusable primitives so creators can implement systems without waiting for Political World itself to add them.

Read: **[Framework vision](docs/en/FRAMEWORK_VISION.md)**.

## Repository layout

```text
src/PoliticalWorld/    Runtime mod + public API
examples/              Small complete addon examples
templates/             Political World addon + standalone NML starter
docs/en/               English documentation
docs/ru/               Russian documentation
AI_START_HERE.md       Entry point for AI coding assistants
ARCHITECTURE.md        Project boundaries and module layout
API_VERSIONING.md      Public API compatibility policy
```

## Compatibility

| Component | Target |
|---|---|
| Political World | 1.7.0 Public Beta |
| Public API | 1.9.0 |
| WorldBox PC | 0.51.2 / build 719 |
| NeoModLoader | 1.2.0.1 |

Public API versioning is separate from both Political World gameplay versions and WorldBox compatibility.

## Community

Creators can use **GitHub Discussions** to ask questions, show work in progress and submit addons for testing.

- [Discussions](https://github.com/Lous12/PoliticalWorld/discussions)
- [Community Addons](docs/en/community-addons.md)
- [What can you build?](docs/en/what-you-can-build.md)

Small experiments are welcome. A project does not need to be a giant overhaul to matter.

## AI-assisted development

The repository is intentionally structured so AI coding assistants can work from documented public interfaces instead of reading and modifying the whole core.

Start with:

- [`AI_START_HERE.md`](AI_START_HERE.md)
- [Using AI](docs/en/using-ai.md)
- [Getting Started](docs/en/GETTING_STARTED.md)

## License

Political World is released under the [MIT License](LICENSE).

Study it, fork it, modify it, build addons, create tools, learn from it, and continue the project if development ever pauses — while preserving the required MIT copyright/license notice for MIT-covered code.

WorldBox belongs to Maxim Karpenko / its respective rights holders. Political World is a community mod and is not an official WorldBox project.
