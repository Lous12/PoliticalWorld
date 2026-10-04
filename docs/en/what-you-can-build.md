# What can you build with Political World?

Political World is both a politics mod and an addon framework.

The important distinction is:

> **Political World contains politics. PoliticalWorldAPI does not need to limit your imagination to politics.**

Current repository baseline: Political World **1.11.0**, PoliticalWorldAPI **1.19.0**.

## What the framework already exposes

API 1.19 includes public surfaces for:

- ideologies and ideological currents;
- custom governments and government archetypes;
- political parties and ruling-party tools;
- kingdom political state and stability;
- Event Bus listeners and addon-owned custom events;
- registered Actions and Rare Events;
- reusable Conditions and Effects;
- addon-owned Actor/City/Kingdom data and tags;
- optional localization with readable fallback text;
- capped world queries and world lifecycle events;
- lazy addon-data migrations;
- declarative inspector UI and context actions;
- hosted kingdom/settlement Politics pages;
- race political profiles and dynamic country-name templates;
- warfare helpers;
- capability discovery, ecosystem diagnostics and support reports.

That makes projects like these practical without copying PW internals:

- ideology or government packs;
- political event packs;
- election/party extensions;
- fantasy politics and alternate-history systems;
- creator/debug utilities;
- city or kingdom metadata systems;
- addon-to-addon integrations;
- UI pages that live inside PW's Politics hosts;
- external systems that react to PW events instead of polling the world.

## What this does not mean

A public primitive is not the same thing as a finished gameplay system.

For example, the framework can provide data, events, world access and UI hooks that an economy addon can use, but Political World does not magically become a complete economy/trade mod because those primitives exist.

Possible independent projects include:

- religions and cults;
- magic or supernatural systems;
- economy and trade extensions;
- diseases and epidemics;
- technologies;
- professions;
- character/RPG mechanics;
- dynasties;
- education or social systems;
- creator tools;
- integrations between several independent addons.

## Start tiny

A useful addon can be very small:

- one event;
- one ideology;
- one government;
- one Action;
- one custom value stored on a kingdom;
- one listener that reacts to another addon event;
- one inspector section;
- one creator utility.

Small projects are easier to understand, test and finish.

## Build with reusable primitives

A healthy addon usually looks more like this:

```text
Addon
  ↓
Register content/capabilities
  ↓
Store addon-owned data
  ↓
React to lifecycle/core/addon events
  ↓
Check Conditions
  ↓
Apply Effects / Actions
  ↓
Publish events for other addons
```

and less like this:

```text
Update()
  ↓
scan the entire world every frame
  ↓
reflect into Main
  ↓
hope nothing changes next release
```

## Cross-addon ecosystems

An ecosystem does not need one giant overhaul that owns everything.

```text
Religion addon ─┐
Magic addon ────┼─→ shared events / capabilities
Economy addon ──┤
Politics addon ─┘
```

Each addon can stay focused and expose only what the others need.

## Scenario Tools

Scenario Tools is a first-party addon and an API test bed.

The intended rule is the same as for third-party addons: when a useful feature cannot be expressed through the public API, improve the API instead of giving one addon a permanent private backdoor.

## Completely standalone WorldBox mods

You still do **not** have to depend on Political World.

The repository contains a standalone NeoModLoader starter and NML UI documentation for unrelated WorldBox mods. Political World does not replace NeoModLoader; it adds a higher-level framework where that framework is useful.

## Where should I start?

If you want a Political World dependent addon:

1. Copy `templates/PoliticalWorld-Addon-Template`.
2. Read [Getting Started](GETTING_STARTED.md).
3. Open the [`examples/` index](../../examples/README.md) and copy the smallest relevant pattern.
4. Read [API 1.19 Reference](API_REFERENCE_1_19.md).
5. Build one visible feature.
6. Test it in WorldBox and check `Player.log`.
7. Ask for a missing API capability instead of bypassing internals.

If you want a standalone mod, use `templates/WorldBox-NeoMod-Starter`.
