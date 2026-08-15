# Political World — developer documentation (EN)

**Core mod ID:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Current API version:** `1.6.0`  
**Target PC WorldBox build:** `0.51.2 / build 719`

This documentation supports two different workflows:

1. **Political World addons** — use the public `PoliticalWorldAPI` and depend on `Lous12.PoliticalWorld`.
2. **Standalone NeoModLoader mods** — do not have to depend on Political World; a separate starter and NML cookbook are provided.

## Start here

- [Getting started: first addon in 10 minutes](GETTING_STARTED.md)
- [API 1.7 quick reference](API_REFERENCE_1_7.md)
- [API 1.6 quick reference](API_REFERENCE_1_6.md) — previous minor reference
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

## Main rule

Third-party addons should work through `PoliticalWorldAPI`. Do not depend on `Main`, `ScenarioBridge`, or Political World's internal classes: they are implementation details and may change without compatibility guarantees.

Political World is designed around events: register content on load, subscribe to core events, and use the Rare Political Event Registry instead of continuously scanning the world from your own `Update()` loop.
