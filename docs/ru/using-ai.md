# Создание аддонов Political World с помощью ИИ

AI-assisted разработка приветствуется. Репозиторий специально устроен так, чтобы ассистент читал реальный публичный контракт, а не угадывал внутренности Political World.

## Дайте ИИ правильные стартовые файлы

Для аддона:

1. `AI_START_HERE.md`
2. `docs/ru/GETTING_STARTED.md`
3. `docs/ru/API_REFERENCE_1_19.md`
4. нужную topic page
5. ближайший пример из `examples/`

Для работы с core используется другой путь:

1. `AGENTS.md`
2. `ARCHITECTURE.md`
3. `KNOWN_RISKS.md`
4. `DEVELOPMENT.md`
5. исходники нужного модуля

Не смешивайте эти два режима. Аддон не должен лезть во внутренности core просто потому, что исходники открыты.

## Готовый промт для аддона

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

## Не доверяйте правдоподобным названиям методов

ИИ очень хорошо выдумывает API, которое выглядит настоящим.

Перед использованием метода PoliticalWorldAPI проверьте его в:

- `src/PoliticalWorld/API/`
- `docs/ru/API_REFERENCE_1_19.md`
- актуальном example.

Если метода там нет — считайте, что его не существует, пока исходники не докажут обратное.

## Если что-то сломалось

Дайте ассистенту:

- `mod.json`;
- точный source file;
- полный compile/runtime error;
- нужную часть `Player.log`;
- версии Political World/API/NML/WorldBox;
- точный repro;
- информацию, ломается ли fresh world, existing save или оба.

Одного скрина обычно мало.

## Большие AI-rewrite

Не делайте гигантский «почисти всё» rewrite.

Для save data, dynamic naming, UI tabs, alliances и warfare patches нужен маленький diff и runtime evidence. Код, который выглядит древним, может существовать ради совместимости.

Для стороннего аддона ответ «используй reflection во внутренности PW» почти всегда неправильный.
