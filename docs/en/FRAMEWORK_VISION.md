# Political World Framework Vision

> **Political World should not decide what kind of mod you are allowed to create.**

Political World began as a politics mod. Its public API began by exposing political systems because those were the systems the project already had.

That is no longer the final goal.

The long-term goal is a reusable addon framework on top of WorldBox + NeoModLoader. Political gameplay remains Political World's own feature set, while the framework provides safe building blocks other creators can combine in ways the Political World maintainer did not predict.

## What stays the same

- The mod is still called **Political World**.
- The stable public facade remains `Lous12.PoliticalWorld.PoliticalWorldAPI`.
- Existing 1.x addons should keep working when possible.
- Politics remains a first-class module.
- Performance stays event-driven and aggregate-first.
- First-party addons must follow the same public API rules as community addons.

## What changes

The API should gradually stop assuming every addon is political.

The framework should expose general primitives:

```text
Core
├─ addon registry
├─ capability discovery
├─ localization + fallback
├─ data storage
├─ tags
├─ Event Bus
├─ Actions
├─ Conditions
├─ Effects
└─ diagnostics

World
├─ kingdoms
├─ cities
├─ actors
├─ clans
├─ cultures
├─ alliances
└─ other safe WorldBox objects

Content
├─ generic definitions
├─ metadata
├─ categories
├─ icons
└─ addon-owned content registries

UI
├─ inspector sections
├─ context actions
├─ windows
└─ creator-tool integration

Politics
├─ ideologies
├─ governments
├─ parties
├─ elections
├─ crises
└─ blocs
```

The exact public shape may evolve during API 1.x, but the direction is deliberate.

## Addons should be able to cooperate

Independent addons should be able to publish namespaced events and react to each other without direct hard dependencies.

Example:

```text
Magic addon
    publishes: mymagic.storm_started
            ↓
Religion addon reacts
Economy addon reacts
Political addon reacts
```

This allows an ecosystem to grow from small independent projects.

## Localization must be optional

A creator should not need to translate every WorldBox language before releasing a working addon.

Recommended fallback:

1. current-language translation;
2. registered/default localization;
3. English fallback;
4. literal `DisplayName` / `Description`;
5. safe readable ID.

Translation improves an addon. Missing translation should not make it unusable.

## We should not become the bottleneck

Political World cannot pre-build a special API for every imaginable concept:

- religion;
- spell;
- technology;
- disease;
- profession;
- crime;
- education;
- custom resource;
- custom social system;
- and thousands of ideas we cannot predict.

The framework should therefore expose generic registration/data/event primitives in addition to convenient specialized APIs.

## First-party addons are API tests

Scenario Tools, Fantasy Politics and future first-party addons should never receive secret access to internals.

If they cannot build a feature with the public API, that is useful feedback:

> the public framework may be missing a capability.

This is how the API grows based on real addon needs instead of speculative complexity.

## API 1.9 today

API 1.9 already provides reusable pieces beyond simple ideology registration:

- optional localization and English fallback;
- addon metadata;
- namespaced data/tags;
- Event Bus;
- Actions;
- Conditions;
- reusable Effects;
- diagnostics;
- content lookup/batch registration;
- structured operation checks.

Most concrete world-state APIs are still political/kingdom-oriented today.

## Next direction: General Framework

The next API work should focus on reusable systems rather than adding more gameplay to the Political World core.

Likely areas:

- generic object-scoped addon data/tags;
- city and actor access;
- generic content registries;
- custom addon events/payloads;
- extensible Conditions and Effects;
- UI/inspector registration;
- cross-addon capability discovery;
- safe lifecycle and save migration tools;
- performance diagnostics.

No addon should need to wait for Political World to become a religion mod, magic mod or economy mod before it can build those things itself.
