# Political World — документация разработчика (RU)

**Core mod ID:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Текущая протестированная версия API:** `1.9.0`  
**Целевая PC-сборка WorldBox:** `0.51.2 / build 719`

Political World теперь одновременно:

1. политический мод для WorldBox;
2. развивающийся универсальный фреймворк аддонов.

Политические API остаются полноценной частью проекта, но **авторы не обязаны ограничиваться политикой**. Направление фреймворка — универсальные регистрация, локализация, данные, теги, события, Conditions, Effects, Actions, UI-интеграция и безопасная работа с объектами WorldBox.

## Куда идти сначала

- [Быстрый старт: первый аддон за 10 минут](GETTING_STARTED.md)
- [Видение фреймворка](FRAMEWORK_VISION.md)
- [Что можно создавать?](what-you-can-build.md)
- [Справочник API 1.9](API_REFERENCE_1_9.md)
- [Идеологии](ideologies.md)
- [Формы правления](governments.md)
- [Партии](parties.md)
- [Scenario Actions](actions.md)
- [События ядра и редкие политические события](political-events.md)
- [Данные и теги аддона](data-storage.md)
- [Validation и Diagnostics](validation-diagnostics.md)
- [Совместимость и версионирование](compatibility-versioning.md)
- [NeoModLoader UI Recipes](nml-ui-recipes.md)
- [Самостоятельный NML-мод](standalone-nml-starter.md)
- [Разработка с помощью ИИ](using-ai.md)
- [Частые ошибки](common-mistakes.md)

Старые справочники остаются полезны для конкретных версий:
- [API 1.6](API_REFERENCE_1_6.md)

## Главное правило

Сторонний аддон должен работать через `PoliticalWorldAPI`.

Не привязывайтесь к `Main`, `ScenarioBridge` и внутренним классам Political World. Это детали реализации без публичной гарантии совместимости.

Если даже наш first-party аддон хочет использовать внутренний обходной путь, правильнее сначала улучшить Public API.

## Производительность

Предпочитайте регистрацию и события постоянному polling.

Political World специально строится событийно и агрегированно. Если механику можно реализовать через Event Bus, Action, Condition, Rare Event или сохранённое addon-state, не нужно сканировать весь мир каждый кадр.
