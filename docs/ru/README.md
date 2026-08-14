# Political World — документация разработчика (RU)

**Core mod ID:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Текущая версия API:** `1.6.0`  
**Целевая PC-сборка WorldBox:** `0.51.2 / build 719`

Эта документация предназначена для двух разных задач:

1. **Аддоны Political World** — используют публичный `PoliticalWorldAPI` и зависят от `Lous12.PoliticalWorld`.
2. **Самостоятельные NeoModLoader-моды** — не обязаны зависеть от Political World; для них есть отдельный стартовый шаблон и NML Cookbook.

## Куда идти сначала

- [Быстрый старт: первый аддон за 10 минут](GETTING_STARTED.md)
- [Что можно создавать с помощью Political World?](what-you-can-build.md)
- [Справочник API 1.6](API_REFERENCE_1_6.md)
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

## Главное правило

Сторонний аддон должен работать через `PoliticalWorldAPI`. Не привязывайтесь к `Main`, `ScenarioBridge` и внутренним классам Political World: они считаются деталями реализации и могут меняться без сохранения совместимости.

Political World спроектирован событийно: регистрируйте контент при загрузке, подписывайтесь на события и используйте Rare Political Event Registry вместо собственного постоянного сканирования мира.
