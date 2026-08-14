<p align="center">
  <img src="assets/icon.png" width="128" alt="Political World icon">
</p>

<h1 align="center">Political World</h1>

<p align="center"><strong>Political framework & modding SDK for WorldBox</strong></p>

<p align="center">
  <a href="en/README.md"><strong>English documentation</strong></a> ·
  <a href="ru/README.md"><strong>Русская документация</strong></a> ·
  <a href="https://github.com/Lous12/PoliticalWorld"><strong>GitHub</strong></a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869"><strong>Steam Workshop</strong></a>
</p>

---

## What is Political World?

Political World adds a lightweight political layer to WorldBox: ideologies, parties, governments, elections, councils, crises, blocs, summits, war-related politics and a Political Map.

Starting with 1.7, it is also a **public framework for other modders**. You can extend Political World through a documented API instead of editing its internal code.

```text
WorldBox + NeoModLoader
        ↓
Political World Core
        ↓
PoliticalWorldAPI 1.6
        ↓
Your addon / community mod / Scenario Tools / Fantasy Politics
```

## Start creating

You do not need to begin with a huge project.

| I want to... | Start here |
|---|---|
| Make my first Political World addon | [First addon in 10 minutes](en/GETTING_STARTED.md) |
| Сделать первый аддон на русском | [Первый аддон за 10 минут](ru/GETTING_STARTED.md) |
| Add an ideology | [Ideologies](en/ideologies.md) |
| Add a government | [Governments](en/governments.md) |
| React to political events | [Political events](en/political-events.md) |
| Make a standalone WorldBox/NML mod | [Standalone NML starter](en/standalone-nml-starter.md) |
| Create with ChatGPT / Claude / Codex | [Using AI](en/using-ai.md) |

## What can you build?

Political World can be used for tiny learning projects or large overhauls.

- one new ideology or ideological branch;
- custom governments such as Magocracy, Dragon Monarchy or Technocracy;
- political event packs;
- party and election extensions;
- fantasy politics systems;
- scenario/director tools;
- ideology packs based on fictional settings;
- large political overhauls that use Political World as a dependency;
- completely standalone NeoModLoader mods using the included NML starter and cookbook.

**[See the full list of things you can build →](en/what-you-can-build.md)**  
**[Что можно создавать — полная страница →](ru/what-you-can-build.md)**

## For AI-assisted development

The repository is intentionally structured so coding assistants can understand it without reading the entire codebase first.

Start with:

- [`AI_START_HERE.md`](../AI_START_HERE.md)
- [Using AI — English](en/using-ai.md)
- [Разработка с ИИ — Русский](ru/using-ai.md)

Recommended instruction for an AI assistant:

```text
Read Political World's AI_START_HERE.md and the relevant API docs.
Use only the public PoliticalWorldAPI for Political World addons.
Do not depend on internal Main or ScenarioBridge classes.
Use namespaced IDs and validate API capabilities.
```

## Open for everyone

Political World is released under the **MIT License**.

That means people may study the source, fork the project, modify it, build addons, create their own versions, learn from the examples, and continue the project if development ever stops — while preserving the MIT license/copyright notice where required.

The goal is simple:

> **Give people a place to learn by creating. Start small, break things, understand them, fix them, and make something that is yours.**

---

**Political World 1.7 · Public API 1.6 · WorldBox PC 0.51.2 · NeoModLoader 1.2.0.1**
