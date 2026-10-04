# Political World Framework Vision

> **Political World should not decide what kind of mod you are allowed to create.**

Political World began as a politics mod. Its public API began by exposing political systems because those were the systems the project already had.

The broader goal is a reusable addon framework on top of WorldBox + NeoModLoader. Political gameplay remains Political World's own feature set; the framework should provide safe building blocks that other creators can combine in ways the maintainer did not predict.

## Current project state

Political World 1.11 / API 1.19 is the current baseline. Active large feature development is currently on a break.

That does not mean the framework is abandoned. The current priority is maintenance, critical fixes, documentation, safer public boundaries and making the repository easier for community contributors, forks and addon authors to work with.

## What stays the same

- The mod is still called **Political World**.
- The public facade remains `Lous12.PoliticalWorld.PoliticalWorldAPI`.
- Existing 1.x addons should keep working when practical.
- Politics remains a first-class module.
- Performance stays event-driven and aggregate-first.
- First-party addons should follow the same public API rules as community addons.

## General direction

The API should not assume every addon is political. Reusable primitives matter more than pre-building every imaginable game system:

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

```text
Magic addon
    publishes: mymagic.storm_started
            ↓
Religion addon reacts
Economy addon reacts
Political addon reacts
```

## API 1.19 today

API 1.19 already exposes addon registration, localization, data/tags, events, actions, conditions/effects, diagnostics, ideology/government/party access, country and warfare helpers, UI integration, world lifecycle helpers and more.

The current priority is to document and stabilize how people use that surface instead of pretending every useful next step has to be another giant core feature.

## Missing capabilities

If a community or first-party addon cannot safely build something through the public API, that is useful feedback. Prefer a focused public capability request/PR over a private reflection dependency.
