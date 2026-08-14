# Creating an addon with AI

The repository is intentionally structured so ChatGPT, Codex, Claude, and other assistants can follow a public contract instead of guessing internals.

## Ready-to-use prompt

```text
Read Political World's AI_START_HERE.md and the relevant RU/EN API docs.
Create a NeoModLoader addon using only the public Lous12.PoliticalWorld.PoliticalWorldAPI.
Do not depend on Main, ScenarioBridge, or other internal Political World classes.
Use a stable namespaced addon GUID and namespaced content IDs.
Call PoliticalWorldAPI.IsCompatible and check optional capabilities when needed.
Use Event Bus / Rare Political Event Registry instead of a permanent world-scanning Update loop.
Use addon-private kingdom data/tags for internal state.
Follow the SDK addon template and report any API feature that is missing instead of reaching into internals.
```

## What to give the AI when something fails

1. your `mod.json`;
2. the full compile error from `Player.log`;
3. `PoliticalWorldAPI.GetDiagnosticsReport(AddonId)`;
4. the SDK file/example you were trying to follow.

Do not ask the AI to bypass a missing API with reflection. Record the missing capability and extend the public API instead.
