# Справочник PoliticalWorldAPI 1.19

Political World 1.11 поставляется с **PoliticalWorldAPI 1.19.0**.

Каноническая публичная поверхность — `Lous12.PoliticalWorld.PoliticalWorldAPI` в `src/PoliticalWorld/API/`. Аддонам лучше работать через неё, а не зависеть от `Main`, `ScenarioBridge`, Harmony-патчей и приватных UI/runtime-классов.

Карта исходников есть в [`src/PoliticalWorld/API/README.md`](../../src/PoliticalWorld/API/README.md), а маленькие копируемые примеры — в [индексе `examples/`](../../examples/README.md).

## Версия

```csharp
PoliticalWorldAPI.ApiVersion // "1.19.0"
PoliticalWorldAPI.ApiMajor   // 1
PoliticalWorldAPI.ApiMinor   // 19
```

## Регистрация и совместимость

Современный аддон может явно указать и минимальную версию API, и нужные capabilities:

```csharp
var definition = new PoliticalWorldAPI.AddonDefinition
{
    Id = "YourName.MyAddon",
    Name = "My Addon",
    Version = "0.1.0",
    Author = "YourName",
    RequiredApiMajor = 1,
    RequiredApiMinor = 19,
    RequiredCapabilities = new[]
    {
        "framework.requirements-check",
        "diagnostics.report"
    }
};

var check = PoliticalWorldAPI.Framework.CheckRequirements(definition);
if (!check.Compatible)
{
    // check.Summary можно вывести в лог/ошибку.
    return;
}

PoliticalWorldAPI.RegisterAddon(definition);
```

Не нужно автоматически требовать 1.19, если аддон использует только старые контракты 1.x. Минимальная версия должна означать самую старую версию API, где реально есть всё нужное вашему коду.

## Основные публичные области

### Контент и политическое состояние

- регистрация и валидация аддонов;
- регистрация/чтение идеологий;
- регистрация/чтение правительств и archetypes;
- партии и правящая партия;
- политическое состояние государства, стабильность и теги;
- actions, conditions и effects;
- политические профили рас и шаблоны названий стран;
- ранги монархий.

Пример ранга монархии:

```csharp
string rank = PoliticalWorldAPI.Countries.GetMonarchyRank(kingdom);
```

### События

По возможности используйте Event Bus вместо polling.

Core event IDs находятся в `PoliticalWorldAPI.Events`. Аддоны могут регистрировать и публиковать собственные namespaced events через `RegisterAddonEvent` / `PublishAddonEvent`.

Смотрите `examples/04_Event_Listener` и `examples/07_Addon_Event`.

### Данные и теги аддона

`PoliticalWorldAPI.Data` хранит namespaced-значения на Actor/City/Kingdom. Лучше использовать эти helpers, чем придумывать свои сырые core-ключи.

Для изменения собственной схемы данных есть `PoliticalWorldAPI.Migrations`; миграции применяются явно к тем объектам, которые аддон реально использует.

Смотрите `examples/08_World_Data`.

### Доступ к миру и lifecycle

`PoliticalWorldAPI.WorldQuery` отдаёт ограниченные snapshots живых объектов мира. Лимиты сделаны специально.

Текущее состояние lifecycle доступно через `PoliticalWorldAPI.Lifecycle`, а уведомления идут через обычный Event Bus (`WorldChanged`, `WorldReady`, `WorldUnavailable`).

Не заменяйте ограниченный query на бесконечный скан всего мира каждый кадр.

### UI integration

`PoliticalWorldAPI.UI` даёт inspector sections, context actions и регистрацию полноценных kingdom/settlement Politics pages в хосте PW.

Навигацией и lifecycle хоста владеет Political World. Аддон должен рисовать через публичный context, а не Harmony-патчить приватные окна.

Смотрите `examples/09_UI_Inspector`.

### Warfare

`PoliticalWorldAPI.Warfare` даёт чтение состояния войн и операции старта войны.

Обычное объявление проходит через дипломатический interception Political World. Force-путь намеренно его обходит. Не используйте force просто потому, что обычное объявление было отклонено.

### Ecosystem / release / diagnostics

API 1.19 включает:

- metadata текущего framework release;
- проверку API/capability requirements;
- snapshots экосистемы аддонов;
- event metrics;
- diagnostics/support reports;
- runtime cleanup снимаемых регистраций аддона;
- deprecation notices.

`PoliticalWorldAPI.Framework.GetSupportReport(addonId)` удобно прикладывать к bug report.

## Правила совместимости

PoliticalWorldAPI использует major/minor-модель:

- major-версия может ломать контракт;
- minor-версия добавляет возможности и по возможности сохраняет старые 1.x контракты;
- приватная реализация core в это обещание не входит.

Политика репозитория: [API versioning](../../API_VERSIONING.md).

## Связанные документы

- [Первый аддон](GETTING_STARTED.md)
- [Идеологии](ideologies.md)
- [Формы правления](governments.md)
- [Партии](parties.md)
- [События](political-events.md)
- [Lifecycle мира](world-lifecycle.md)
- [Данные и миграции](data-storage.md)
- [Общий framework](general-framework.md)
- [UI integration](ui-integration.md)
- [Warfare](warfare.md)
- [Validation и diagnostics](validation-diagnostics.md)
- [Совместимость и версии](compatibility-versioning.md)
- [Частые ошибки](common-mistakes.md)
- [Куда развивается API](FRAMEWORK_VISION.md)
- [Что можно создавать?](what-you-can-build.md)
