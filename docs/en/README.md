# Political World — developer documentation (EN)

**Core mod ID:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Current tested API:** `1.9.0`  
**Target PC WorldBox build:** `0.51.2 / build 719`

Political World is both:

1. a politics mod for WorldBox;
2. a growing general addon framework.

Political APIs remain fully supported, but creators are **not expected to limit themselves to politics**. The framework direction is to expose reusable registration, localization, data, tags, events, conditions, effects, actions, UI integration and safe WorldBox object access.

## Start here

- [Getting started: first addon in 10 minutes](GETTING_STARTED.md)
- [Framework vision](FRAMEWORK_VISION.md)
- [What can you build?](what-you-can-build.md)
- [API 1.9 quick reference](API_REFERENCE_1_9.md)
- [Ideologies](ideologies.md)
- [Governments](governments.md)
- [Parties](parties.md)
- [Scenario Actions](actions.md)
- [Core events and rare political events](political-events.md)
- [Addon data and tags](data-storage.md)
- [Validation and diagnostics](validation-diagnostics.md)
- [Compatibility and versioning](compatibility-versioning.md)
- [NeoModLoader UI Recipes](nml-ui-recipes.md)
- [Standalone NML mod](standalone-nml-starter.md)
- [Using AI for development](using-ai.md)
- [Common mistakes](common-mistakes.md)

Older API references remain useful for historical/version-specific behavior:
- [API 1.6 reference](API_REFERENCE_1_6.md)

## Main rule

Third-party addons should work through `PoliticalWorldAPI`.

Do not depend on `Main`, `ScenarioBridge` or Political World's internal classes. They are implementation details and may change without compatibility guarantees.

If a first-party addon needs an internal shortcut, the preferred response is to improve the public API instead of creating a private backdoor.

## Performance rule

Prefer registration and events over continuous polling.

Political World is intentionally event-driven and aggregate-first. Addons should avoid scanning the whole world every frame when an event, Action, Condition, Rare Event or cached addon state can express the same behavior.
