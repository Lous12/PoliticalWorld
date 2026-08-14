# What can you build with Political World?

Political World is both a WorldBox politics mod and a framework for other creators. You can use only the small part you need, or build a large addon ecosystem on top of it.

## 1. Very small beginner addons

Good first projects:

- one new ideology;
- one ideological current;
- one custom government;
- one political event;
- one Scenario Action;
- one listener that reacts to an election, crisis or government change;
- a tiny addon that stores one custom value for each kingdom.

These are useful because you can see a visible result quickly without understanding the whole Political World codebase.

## 2. Ideology and politics packs

You can create themed content packs such as:

- historical ideology expansions;
- fictional ideology trees;
- regional political systems;
- alternative-history political packs;
- cyberpunk, medieval, sci-fi or post-apocalyptic ideology sets;
- additional party behavior built around Political World events and state.

## 3. Custom governments

The Government Registry lets addons create their own government identities while reusing a safe Political World archetype.

Examples:

```text
Magocracy             → Oligarchy
Dragon Monarchy       → Absolute Monarchy
Arcane Parliament     → Parliamentary Republic
Necrocracy            → One-Party State
Military Junta        → Military Dictatorship
Council of Druids     → Council Republic
Technocracy           → Oligarchy / Republic archetype
```

The addon gets its own government ID, tags and name without having to rewrite the entire government simulation.

## 4. Political event mods

You can create event packs using the Event Bus and Rare Political Event Registry.

Examples:

- palace coup;
- constitutional crisis;
- succession dispute;
- party split;
- reform movement;
- military intervention in politics;
- anti-corruption campaign;
- religious-political conflict;
- magical catastrophe that changes political stability;
- election consequences;
- dynasty or ruler-specific events.

The goal is to register events instead of scanning the whole world every frame.

## 5. Fantasy Politics-style addons

A fantasy addon can register content and react to Political World state without modifying Political World internals.

Possible themes:

- Dragon Blood dynasties;
- Magocracy;
- Necropolitics;
- Divine authority;
- vampire aristocracies;
- elven councils;
- dwarven guild republics;
- immortal rulers;
- magical parties and ideological movements.

Actor traits can be combined with Political World conditions, kingdom tags and events.

## 6. Scenario / Director tools

The public action system makes it possible to build an external editor or director-style mod.

It could let the player:

- choose a kingdom;
- change ideology/current/government;
- edit stability;
- create or rename parties;
- modify party support/radicalism;
- assign a ruling party;
- run registered political actions or events;
- build presets for scenario creation.

This is the foundation for a future **Scenario Tools** addon.

## 7. Large overhauls

Political World can act as a dependency for a much larger mod.

Examples:

- total fantasy politics overhaul;
- alternate-history world politics;
- Cold War-style political blocs;
- dynastic politics expansion;
- religion + politics overhaul;
- economy + politics extension;
- deep election and party expansion;
- a roleplay/scenario framework that uses Political World state as one layer.

A large addon should still use the public API instead of depending on internal `Main` or `ScenarioBridge` code.

## 8. Completely standalone WorldBox mods

You do **not** have to depend on Political World.

The repository also contains a standalone NeoModLoader starter and NML cookbook. You can use those materials to learn how to create your own unrelated WorldBox mod.

Possible standalone projects:

- new powers;
- custom tabs and windows;
- gameplay utilities;
- world-generation tools;
- creature systems;
- scenario tools unrelated to politics;
- UI experiments;
- your own complete framework.

Political World is not meant to replace NeoModLoader. It provides examples and recipes in the context we learned while building the mod.

## 9. What can you do with the repository files?

Under the MIT License, you may generally:

- read and study the source code;
- copy the templates and examples;
- modify Political World for yourself;
- fork the repository;
- publish your own fork or continuation;
- use Political World as a dependency;
- create addons that require Political World;
- use the standalone NML starter for unrelated mods;
- use the documentation with AI coding assistants;
- contribute fixes, examples or documentation back to the project.

When redistributing MIT-licensed Political World code, preserve the required copyright and license notice. The repository's `LICENSE` file contains the exact license text.

## 10. Where should I start?

If you are new to modding:

1. Copy `templates/PoliticalWorld-Addon-Template`.
2. Read [Getting Started](GETTING_STARTED.md).
3. Add one small visible feature.
4. Run WorldBox and check the log.
5. Only then expand the project.

If you want a standalone mod instead, start from `templates/WorldBox-NeoMod-Starter` and read [Standalone NML mod](standalone-nml-starter.md).

You do not have to understand everything before you create something.
