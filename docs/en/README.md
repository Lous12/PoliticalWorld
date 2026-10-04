# Political World — developer documentation (EN)

**Core mod:** `1.11.0`  
**Core mod ID:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Current public API:** `1.19.0`  
**Target PC WorldBox build:** `0.51.2 / build 719`  
**NeoModLoader:** `1.2.0.1`

Political World is both a politics mod for WorldBox and a public addon framework.

Active large feature development is currently on a break. The repository is still maintained for critical fixes, documentation, API clarity, forks, PRs and community addon work.

## Start here

- [Getting Started: first addon in 10 minutes](GETTING_STARTED.md)
- [API 1.19 Reference](API_REFERENCE_1_19.md)
- [`examples/` index](../../examples/README.md)
- [Addon template](../../templates/PoliticalWorld-Addon-Template)
- [Framework vision](FRAMEWORK_VISION.md)
- [What can you build?](what-you-can-build.md)
- [Compatibility and versioning](compatibility-versioning.md)
- [Common mistakes](common-mistakes.md)

Topic docs:

- [Ideologies](ideologies.md)
- [Governments](governments.md)
- [Parties](parties.md)
- [Scenario Actions](actions.md)
- [Core events, custom events and rare political events](political-events.md)
- [World access and lifecycle](world-lifecycle.md)
- [Addon data, tags and migrations](data-storage.md)
- [General addon framework](general-framework.md)
- [UI integration](ui-integration.md)
- [Warfare API](warfare.md)
- [Validation and diagnostics](validation-diagnostics.md)
- [NeoModLoader UI Recipes](nml-ui-recipes.md)
- [Standalone NML mod](standalone-nml-starter.md)
- [Using AI for development](using-ai.md)

## Main rule

Third-party addons should work through `PoliticalWorldAPI`.

Do not depend on `Main`, `ScenarioBridge` or Political World's internal classes. They are implementation details and may change without compatibility guarantees.

If the public API is missing a capability, request/add a safe public capability instead of creating a reflection backdoor.

## Minimum API version vs current API

`1.19.0` is the current public API. That does **not** mean every addon must require 1.19.

An addon should declare the minimum API version it actually needs. A tiny addon using only older capabilities can legitimately check `IsCompatible(1, 6)`.

## Performance rule

Prefer registration and events over continuous polling. Avoid scanning the whole world every frame when an event, Action, Condition, Rare Event or cached addon state can express the same behavior.

## Core contributors / AI assistants

Before changing Political World itself, read the repository root files:

- `AGENTS.md`
- `ARCHITECTURE.md`
- `KNOWN_RISKS.md`
- `DEVELOPMENT.md`

## Community

- Discord: https://discord.gg/kYH5GadndE
- Issues: https://github.com/Lous12/PoliticalWorld/issues
- Pull Requests: https://github.com/Lous12/PoliticalWorld/pulls
- Forks: https://github.com/Lous12/PoliticalWorld/forks
