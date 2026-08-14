# Создание аддона с помощью ИИ

Репозиторий специально проектируется так, чтобы ChatGPT, Codex, Claude и другие ассистенты могли работать по публичному контракту, а не угадывать внутренности.

## Готовый промт

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

## Что дать ИИ при ошибке

1. ваш `mod.json`;
2. полный compile error из `Player.log`;
3. `PoliticalWorldAPI.GetDiagnosticsReport(AddonId)`;
4. файл/пример из SDK, который вы пытались повторить.

Не просите ИИ «обойти API через reflection», если нужного метода нет. Лучше зафиксировать недостающую capability и расширить публичный API.
