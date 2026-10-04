# Political World Framework Vision

> **Political World should not decide what kind of mod you are allowed to create.**

Political World began as a politics mod. Its public API began by exposing political systems because those were the systems the project already had.

The long-term goal is wider: a reusable addon framework on top of WorldBox + NeoModLoader.

Political gameplay remains Political World's own feature set. The framework should provide safe building blocks that other creators can combine in ways the maintainer did not predict.

## What stays the same

- The mod is still called **Political World**.
- The stable public facade remains `Lous12.PoliticalWorld.PoliticalWorldAPI`.
- Existing 1.x addons should keep working when possible.
- Politics remains a first-class module.
- Performance stays event-driven and aggregate-first.
- First-party addons should follow the same public API rules as community addons.

## General direction

The API should not assume every addon is political.

Reusable primitives matter more than pre-building every imaginable game system:

- addon registry;
- capability discovery;
- localization + fallback;
- data and tags;
- Event Bus;
- Actions / Conditions / Effects;
- diagnostics;
- safe world object access;
- UI integration;
- addon-owned content.

## Addons should cooperate

Independent addons should be able to publish namespaced events and react to each other without hard dependencies.

Example:

```text
Magic addon
    publishes: mymagic.storm_started
            ↓
Religion addon reacts
Economy addon reacts
Political addon reacts
```

## Localization should fail gracefully

A creator should not need every WorldBox language before releasing a working addon.

Recommended fallback:

1. current-language translation;
2. registered/default localization;
3. English fallback;
4. literal DisplayName / Description;
5. safe readable ID.

## API 1.19 today

API 1.19 already exposes a broad reusable surface: addon registration, localization, data/tags, events, actions, conditions/effects, diagnostics, ideology/government/party access, country and warfare helpers, UI integration, lifecycle helpers and more.

The exact public shape will continue evolving, but the direction is deliberate.

## Next direction

Future API work should keep focusing on reusable systems without turning Political World core into a mod that tries to simulate everything.

If a community or first-party addon cannot build something safely through the public API, that is useful feedback about a missing capability.
