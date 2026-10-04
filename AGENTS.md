# AGENTS.md — working on Political World

This file is for both humans and AI assistants touching the Political World repository.

Current baseline:

- Political World: **1.11.0**
- Public API: **1.19.0**
- WorldBox: **0.51.2 / build 719**
- NeoModLoader: **1.2.0.1**

## Read order

Before changing the core:

1. `AGENTS.md`
2. `ARCHITECTURE.md`
3. `KNOWN_RISKS.md`
4. `DEVELOPMENT.md`
5. the files in the module you are changing

Before creating an addon, use `AI_START_HERE.md` and the public API docs instead.

## Core rules

- Keep `Main.cs` tiny. Do not rebuild the old monolith.
- Put code in the narrowest module that owns the behavior.
- Do not mass-rename legacy `ukiol_*` identifiers. Some of that ugly stuff is already inside saves.
- Do not assume a WorldBox method, field, lifecycle callback or UI object behaves the way its name suggests. Verify it.
- Do not combine a structural refactor with a gameplay redesign unless there is a very good reason.
- Do not turn rare/event-driven logic into a permanent per-frame world scan.
- Preserve API 1.x compatibility unless the break is explicit, documented and intentional.
- If an addon needs internals, prefer adding a safe public capability instead of teaching it reflection.

## Dangerous parts

Treat these as high-risk until proven otherwise:

- persistence and save migration;
- world-load timing and topology stabilization;
- dynamic country names and cached/base names;
- native CityWindow/KingdomWindow tabs and scroll state;
- vanilla Alliance <-> Political World bloc synchronization;
- war/diplomacy Harmony patches;
- legacy IDs;
- public API contracts used by addons.

Read `KNOWN_RISKS.md` before touching them.

## Comment style

Comments should explain **why the weird code exists**, not narrate obvious syntax.

They may be informal and direct. This repository does not need corporate comments.

Good:

```csharp
// Do not rename ukiol_* just because the prefix is ancient.
// This crap already lives in saves. Rename it without migration and old worlds are gone.
```

Good:

```csharp
// WorldBox can call this before the window is fully alive.
// null here is not "impossible". We already stepped on this.
```

Bad:

```csharp
// Increment counter by one.
counter++;
```

An occasional swear word in an internal comment is fine if it makes a real warning clearer. Do not turn every file into a meme.