# What can you build with Political World?

Political World is both a politics mod and a growing addon framework.

The important distinction is:

> **Political World contains politics. PoliticalWorldAPI does not need to limit your imagination to politics.**

## What works especially well today

API 1.9 already provides strong support for:

- ideologies and ideological currents;
- custom governments;
- political parties and ruling-party tools;
- kingdom political state;
- Event Bus listeners;
- registered Actions;
- Rare Events, including manual execution for creator tools;
- Conditions and reusable Effects;
- addon-private data and tags;
- optional localization with English/readable fallback;
- diagnostics and capability checks.

That makes these projects practical today:

- ideology packs;
- government packs;
- political event packs;
- election/party extensions;
- fantasy politics;
- dynasty/ruler events;
- scenario/director tools;
- alternate-history politics;
- creator/debug utilities around Political World state.

## What the framework is growing toward

The general framework direction is intended to make projects outside pure politics possible without the core mod implementing those systems first.

Examples:

- religions and cults;
- magic or supernatural systems;
- economy and trade extensions;
- diseases and epidemics;
- technologies;
- professions;
- character/RPG mechanics;
- dynasties;
- city-level systems;
- custom world events;
- education or social systems;
- creator tools;
- integrations between several independent addons.

These are **framework targets and addon ideas**, not a claim that Political World already ships all of those gameplay systems.

## Start tiny

A useful addon can be very small:

- one event;
- one ideology;
- one government;
- one Action;
- one new custom value stored on a kingdom;
- one listener that reacts to another addon event;
- one creator utility.

Small projects are important because they are easy to understand, test and improve.

## Build with reusable primitives

The direction of the framework is:

```text
Addon
  ↓
Register content
  ↓
Store addon-owned data
  ↓
React to events
  ↓
Check Conditions
  ↓
Apply Effects / Actions
  ↓
Publish events for other addons
```

The less a creator needs to touch Political World internals, the healthier the ecosystem becomes.

## Cross-addon ecosystems

A future ecosystem does not need one giant overhaul that implements everything.

It can be several mods:

```text
Religion addon ─┐
Magic addon ────┼─→ shared events / capabilities
Economy addon ──┤
Politics addon ─┘
```

Each addon can stay small and focused.

## Scenario Tools

Scenario Tools is a first-party addon and a test of the public framework.

It intentionally uses the same Public API rules as third-party addons. When Scenario Tools needs something the API cannot express, the preferred fix is to improve PoliticalWorldAPI.

This makes it useful both as a player tool and as a real-world API test.

## Completely standalone WorldBox mods

You still do **not** have to depend on Political World.

The repository contains a standalone NeoModLoader starter and NML UI cookbook for unrelated WorldBox mods.

Political World is not intended to replace NeoModLoader. It adds a higher-level framework and examples on top of it.

## Where should I start?

If you want a Political World dependent addon:

1. Copy `templates/PoliticalWorld-Addon-Template`.
2. Read [Getting Started](GETTING_STARTED.md).
3. Read [Framework Vision](FRAMEWORK_VISION.md).
4. Build one visible feature.
5. Test it.
6. Ask for a missing API capability instead of bypassing internals.

If you want a standalone mod, use `templates/WorldBox-NeoMod-Starter`.
