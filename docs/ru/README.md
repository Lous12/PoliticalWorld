# Документация Political World для разработчиков

**Core mod:** `1.11.0`  
**ID основного мода:** `Lous12.PoliticalWorld`  
**Public API:** `Lous12.PoliticalWorld.PoliticalWorldAPI`  
**Текущая версия Public API:** `1.19.0`  
**Целевая версия WorldBox на ПК:** `0.51.2 / build 719`  
**NeoModLoader:** `1.2.0.1`

Political World — это одновременно политический мод для WorldBox и публичная платформа для аддонов.

Активная разработка больших новых функций сейчас на паузе. Репозиторий при этом продолжает обслуживаться: критические фиксы, документация, понятность API, форки, PR и аддоны сообщества никуда не делись.

## С чего начать

- [Первый аддон за 10 минут](GETTING_STARTED.md)
- [Справочник API 1.19](API_REFERENCE_1_19.md)
- [Индекс `examples/`](../../examples/README.md)
- [Шаблон аддона](../../templates/PoliticalWorld-Addon-Template)
- [Куда развивается API](FRAMEWORK_VISION.md)
- [Что можно создавать](what-you-can-build.md)
- [Совместимость и версии](compatibility-versioning.md)
- [Частые ошибки](common-mistakes.md)

Документация по отдельным частям:

- [Идеологии](ideologies.md)
- [Формы правления](governments.md)
- [Партии](parties.md)
- [Scenario Actions](actions.md)
- [Core events, custom events и Rare Events](political-events.md)
- [Доступ к миру и lifecycle](world-lifecycle.md)
- [Данные, теги и миграции аддона](data-storage.md)
- [Общий addon framework](general-framework.md)
- [UI integration](ui-integration.md)
- [Warfare API](warfare.md)
- [Проверка данных и диагностика](validation-diagnostics.md)
- [Рецепты интерфейса NeoModLoader](nml-ui-recipes.md)
- [Самостоятельный мод на NML](standalone-nml-starter.md)
- [Разработка с ИИ](using-ai.md)

## Главное правило

Для связи с Political World используй `PoliticalWorldAPI`.

`Main`, `ScenarioBridge` и другие внутренние классы не являются частью публичного контракта. Если Public API чего-то не умеет, лучше добавить/запросить безопасную capability, чем строить reflection-обход.

## Минимальная версия API и текущая версия — не одно и то же

`1.19.0` — текущая версия Public API. Это **не означает**, что любой аддон обязан требовать 1.19.

Нужно указывать минимальную версию API, которая реально нужна проекту. Маленький аддон, использующий старые возможности, вполне может честно проверять `IsCompatible(1, 6)`.

## Производительность

Старайся реагировать на события, а не постоянно сканировать весь мир. Если задачу можно решить через Event Bus, Action, Condition, Rare Event или сохранённое состояние аддона, это почти всегда лучше собственного `Update()` на каждый кадр.

## Работа с core / ИИ

Перед изменением самого Political World прочитайте в корне репозитория:

- `AGENTS.md`
- `ARCHITECTURE.md`
- `KNOWN_RISKS.md`
- `DEVELOPMENT.md`

## Сообщество

- Discord: https://discord.gg/kYH5GadndE
- Issues: https://github.com/Lous12/PoliticalWorld/issues
- Pull Requests: https://github.com/Lous12/PoliticalWorld/pulls
- Форки: https://github.com/Lous12/PoliticalWorld/forks
