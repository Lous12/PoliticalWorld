# Доступ к миру и lifecycle

API 1.11+ даёт явные capped world queries и lifecycle notifications.

Это нужно, чтобы аддон мог работать с живыми объектами WorldBox без собственного бесконечного скана manager-коллекций.

## Availability

```csharp
bool available =
    PoliticalWorldAPI.Lifecycle.IsWorldAvailable();

bool ready =
    PoliticalWorldAPI.Lifecycle.IsWorldReady();

int session =
    PoliticalWorldAPI.Lifecycle.GetWorldSessionId();

int year =
    PoliticalWorldAPI.Lifecycle.GetWorldYear();
```

`IsWorldAvailable` и `IsWorldReady` полезны как gates, но это не обещание, что каждый сторонний мод уже закончил каждую свою restore-операцию.

Для сложной addon restore logic также реагируйте на lifecycle events и проверяйте save/load.

## Lifecycle events

Подписка идёт через обычный Event Bus:

```csharp
PoliticalWorldAPI.Subscribe(
    AddonId,
    PoliticalWorldAPI.Events.WorldReady,
    OnWorldReady
);
```

Доступны:

- `WorldChanged`
- `WorldReady`
- `WorldUnavailable`

Смотрите `examples/08_World_Data`.

## Capped queries

`PoliticalWorldAPI.WorldQuery` отдаёт явные snapshots государств, городов и Actor.

Реализация специально ограничивает число raw objects, которые один query может просмотреть. Predicate не превращает запрос в бесконечный scan.

Используйте разумный limit и вызывайте query только когда snapshot реально нужен.

Не запускайте большие `WorldQuery` каждый кадр в addon `Update()`.

## Session ID

`GetWorldSessionId()` меняется при переходах между world/session. Его удобно использовать для сброса addon caches.

Runtime references из старой session нельзя считать валидными в новой.

## Persistence

Для persistent addon values используйте `PoliticalWorldAPI.Data` и `PoliticalWorldAPI.Migrations`.

WorldQuery возвращает live runtime objects, а не формат сохранения.
