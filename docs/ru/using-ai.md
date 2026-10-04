# Создание аддонов Political World с помощью ИИ

ChatGPT, Codex, Claude и другие ассистенты здесь нормальны. Репозиторий специально устроен так, чтобы ИИ мог читать реальный контракт, а не угадывать внутренности.

## С чего начать

- в корне репозитория: `AI_START_HERE.md`;
- текущий справочник: `API_REFERENCE_1_19.md`;
- тематическая страница по нужной системе.

## Готовый промт

```text
Read Political World's AI_START_HERE.md and the current API docs.
Create a NeoModLoader addon using only public Lous12.PoliticalWorld.PoliticalWorldAPI.
Use a stable namespaced addon GUID and namespaced content IDs.
Check the minimum API version actually required and optional capabilities when needed.
Prefer Event Bus / registered events / rare events over permanent world-scanning Update loops.
Use addon-owned data/tags for private state.
If the API cannot support the requested feature, explain the missing capability instead of using reflection into Political World internals.
```

## Если что-то сломалось

Дайте ИИ `mod.json`, полный error из `Player.log`, diagnostics PoliticalWorldAPI и точный пример/страницу документации, по которой работали.

Не надо "чинить" отсутствующую capability через reflection во внутренние классы Political World.
