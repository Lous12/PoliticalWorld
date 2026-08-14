# Political World API 1.3 — Event Bus и диагностика разработчика

API 1.3 добавляет событийную основу, чтобы аддоны могли реагировать на Political World
без собственного вечного polling/`Update()`.

## Новые capabilities

- `event.subscribe`
- `diagnostics`

Validation, приватные теги аддонов и безопасные сохранения из API 1.2 остаются.

## Подписка на события

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);

private static void OnGovernmentChanged(
    PoliticalWorldAPI.PoliticalEventData data
)
{
    if (data == null || data.Kingdom == null)
        return;

    // data.OldValue -> прошлое правительство
    // data.NewValue -> новое правительство
}
```

Список событий установленной версии можно получить через
`PoliticalWorldAPI.GetEventIds()`.

События API 1.3:

- `kingdom.ideology.changed`
- `kingdom.current.changed`
- `kingdom.government.changed`
- `party.created`
- `party.deactivated`
- `party.renamed`
- `party.radicalism.changed`
- `party.support.changed`
- `political.event.published`

Для инструментов разработчика можно подписаться на
`PoliticalWorldAPI.Events.All` (`"*"`).

### Что именно означает событие

Изменение идеологии, течения, правительства, создание и деактивация партии
подключены к центральным переходам ядра. Поэтому аддон увидит и естественные
изменения Political World.

Переименование партии, изменение радикализма и поддержки в API 1.3 уведомляют об
явной записи через Public API. Мы специально не генерируем событие на каждый
фоновый перерасчёт поддержки, чтобы не получить спам и лишнюю нагрузку.

Новые hooks будем добавлять только после проверки реальной точки перехода в ядре.

## Изоляция callback

Каждый callback чужого аддона выполняется через `try/catch`.

Если аддон A сломался внутри обработчика:

- Political World продолжает работать;
- аддон B всё равно получает событие;
- ошибка записывается в лог;
- диагностика аддона A показывает callback error.

Метаданные события копируются для каждого подписчика, поэтому один аддон не может
испортить поля события для следующего.

Также есть ограничитель глубины рекурсии, чтобы ошибочный
`event -> изменение -> event` не ушёл в бесконечный цикл.

## Отписка

```csharp
PoliticalWorldAPI.Unsubscribe(
    AddonId,
    PoliticalWorldAPI.Events.GovernmentChanged,
    OnGovernmentChanged
);

PoliticalWorldAPI.UnsubscribeAll(AddonId);
```

## Developer Diagnostics

```csharp
PoliticalWorldAPI.AddonDiagnostics info =
    PoliticalWorldAPI.GetAddonDiagnostics(AddonId);

string report =
    PoliticalWorldAPI.GetDiagnosticsReport(AddonId);

PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

Отчёт показывает:

- сколько зарегистрировано идеологий;
- actions;
- подписок на события;
- ошибок callback;
- warnings;
- errors;
- последние диагностические сообщения.

Пример:

```text
[Political World API]
API: 1.3.0
Addon: Vampire Politics [Author.VampirePolitics]
Registered ideologies: 3
Registered actions: 2
Event subscriptions: 4
Callback errors: 0
Warnings: 0
Errors: 0
```

## Производительность

Event Bus не имеет собственного `Update()`. Он вызывается только в проверенных
точках изменения политического состояния. Аддонам следует использовать подписки,
а не постоянно сканировать королевства и жителей.
