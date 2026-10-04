# Creating Political World addons with AI

ChatGPT, Codex, Claude and other assistants are welcome here. The repository is structured so they can read a real contract instead of guessing internals.

## Start with

- repository root: `AI_START_HERE.md`
- current API reference: `API_REFERENCE_1_19.md`
- the topic page relevant to the addon

## Ready prompt

```text
Read Political World's AI_START_HERE.md and the current API docs.
Create a NeoModLoader addon using only public Lous12.PoliticalWorld.PoliticalWorldAPI.
Use a stable namespaced addon GUID and namespaced content IDs.
Check the minimum API version actually required and optional capabilities when needed.
Prefer Event Bus / registered events / rare events over permanent world-scanning Update loops.
Use addon-owned data/tags for private state.
If the API cannot support the requested feature, explain the missing capability instead of using reflection into Political World internals.
```

## When something fails

Give the AI your `mod.json`, the complete error from `Player.log`, API diagnostics when available, and the exact example/doc page you followed.

Do not "fix" a missing addon capability by reaching into private Political World classes.
