# Creating Political World addons with AI

AI-assisted development is welcome. The repository is structured so an assistant can inspect a real public contract instead of guessing Political World internals.

## Give the assistant the right starting files

For an addon:

1. `AI_START_HERE.md`
2. `docs/en/GETTING_STARTED.md`
3. `docs/en/API_REFERENCE_1_19.md`
4. the relevant topic page
5. the closest example under `examples/`

For core work, use the root maintainer path instead:

1. `AGENTS.md`
2. `ARCHITECTURE.md`
3. `KNOWN_RISKS.md`
4. `DEVELOPMENT.md`
5. the actual module source

Do not mix those two workflows. An addon should not be taught to use core internals just because the source is visible.

## Ready prompt for addon work

```text
Read Political World's AI_START_HERE.md, API 1.19 docs and the closest example.
Create a NeoModLoader addon using only public Lous12.PoliticalWorld.PoliticalWorldAPI.

Use a stable addon GUID and namespaced content/event IDs.
Declare the minimum API version actually required.
Use capability checks for optional features.
Prefer Event Bus, Rare Events and capped WorldQuery calls over permanent world scanning.
Use addon-owned Data/Tags for private persistent state and Migrations when the schema changes.
Use PoliticalWorldAPI.UI / hosted Politics pages before patching PW windows.
Do not use Main, ScenarioBridge or reflection into Political World internals.

If the public API cannot express the requested feature, name the missing capability instead of inventing a method.
```

## Do not trust plausible method names

AI models are very good at inventing APIs that look reasonable.

Before using a PoliticalWorldAPI method, verify it in:

- `src/PoliticalWorld/API/`
- `docs/en/API_REFERENCE_1_19.md`
- a current example.

If it is not there, treat it as nonexistent until proven otherwise.

## When something fails

Give the assistant:

- addon `mod.json`;
- the exact source file;
- complete compile/runtime error;
- relevant `Player.log`;
- Political World/API/NML/WorldBox versions;
- exact repro;
- whether the problem appears on a fresh world, existing save, or both.

A screenshot without the complete error is usually not enough.

## AI-generated rewrites

Avoid giant "clean up everything" rewrites.

For save data, dynamic naming, UI tabs, alliances and warfare patches, require a focused diff and runtime evidence. Old-looking code can be compatibility code.

For third-party addons, if the answer is "use reflection into PW internals", the answer is usually wrong.
