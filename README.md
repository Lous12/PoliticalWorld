<p align="center">
  <img src="docs/assets/github-banner.svg" alt="Political World banner" width="100%">
</p>

<h1 align="center">Political World</h1>
<p align="center"><strong>Politics mod for WorldBox • Public API • Addon framework</strong></p>

<p align="center">
  <a href="README_RU.md">Русский</a> ·
  <a href="https://lous12.github.io/PoliticalWorld/">Website</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/discussions">Discussions</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.7.0%20Public%20Beta-2563eb">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.9.0-8b5cf6">
  <img alt="WorldBox" src="https://img.shields.io/badge/WorldBox%20PC-0.51.2-0ea5e9">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NML-1.2.0.1-10b981">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-22c55e">
</p>

> **Play politics. Build anything.**

Political World started as a political expansion for WorldBox.  
Today it is also becoming an **open addon framework**: the core mod remains focused on politics, while `PoliticalWorldAPI` gives creators stable tools to build addons on top of it.

---

## Quick navigation

| I want to... | Go here |
|---|---|
| Play the mod | [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869) |
| Read the docs | [Website](https://lous12.github.io/PoliticalWorld/) |
| Make my first addon | [Getting Started](docs/en/GETTING_STARTED.md) |
| Understand the framework direction | [Framework Vision](docs/en/FRAMEWORK_VISION.md) |
| Ask questions / post addons | [Discussions](https://github.com/Lous12/PoliticalWorld/discussions) |

---

## Two sides of one project

### 🎮 Political World for players
Political World adds a lightweight but deep-feeling political layer:

- ideologies and currents;
- parties, leaders, support and radicalism;
- governments and political systems;
- elections, councils and leadership changes;
- stability, crises, rebellions, coups and revolutions;
- international blocs and alliance integration;
- physical ruler summits;
- war preparation, diplomatic crises and war exhaustion;
- Political Map modes.

### 🧩 PoliticalWorldAPI for creators
The public API already provides:

- addon registration and validation;
- optional localization with readable fallback;
- addon data and tags;
- Event Bus;
- Actions and Rare Events;
- reusable Conditions and Effects;
- diagnostics and structured operation results;
- ideologies, governments, parties and kingdom state.

**Localization is optional.**  
If an addon only ships English text, players using another language can still see readable English instead of `missing text`.

---

## Framework direction

Political World itself stays a politics mod.

`PoliticalWorldAPI` is being expanded into a more general addon framework so creators can build:

- political extensions;
- fantasy systems;
- religions and cults;
- economy and trade;
- diseases and disasters;
- character systems;
- scenario / creator tools;
- and other ideas that do not need to wait for the core mod.

Read more: [Framework Vision](docs/en/FRAMEWORK_VISION.md)

---

## Repository layout

```text
src/PoliticalWorld/    Core mod + Public API
docs/en/               English docs
docs/ru/               Russian docs
examples/              Small example addons
templates/             Addon templates + standalone NML starter
AI_START_HERE.md       Entry point for AI assistants
```

---

## Community

Useful links:

- [Discussions](https://github.com/Lous12/PoliticalWorld/discussions)
- [Community Addons](docs/en/community-addons.md)
- [What can you build?](docs/en/what-you-can-build.md)

Small projects matter. One event, one ideology, one tool or one experimental addon is already worth sharing.

---

## License

Released under the [MIT License](LICENSE).
