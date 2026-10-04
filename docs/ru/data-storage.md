# Данные, теги и миграции аддона

API 1.19 даёт addon-owned typed storage на `Actor`, `City` и `Kingdom`.

Используйте его вместо собственных raw save keys во внутренних структурах Political World.

## Typed data

Основная поверхность — `PoliticalWorldAPI.Data`.

```csharp
int mana = PoliticalWorldAPI.Data.GetInt(
    kingdom,
    AddonId,
    "mana",
    0
);

PoliticalWorldAPI.Data.SetInt(
    kingdom,
    AddonId,
    "mana",
    mana + 1
);
```

Тот же API работает с `Actor`, `City` и `Kingdom`.

Поддерживаются:

- `GetInt` / `SetInt`
- `GetString` / `SetString`
- `GetBool` / `SetBool`
- `GetFloat` / `SetFloat`

Ключи namespaced по addon ID. Физическую кодировку/save key контролирует API.

Старые kingdom-only методы вроде `GetKingdomInt` / `SetKingdomInt` остаются ради совместимости.

## Приватные теги

`PoliticalWorldAPI.Tags` работает с addon-owned tags на Actor/City/Kingdom:

```csharp
PoliticalWorldAPI.Tags.Add(city, AddonId, "holy_site");

if (PoliticalWorldAPI.Tags.Has(city, AddonId, "holy_site"))
{
    // ...
}
```

Для внутреннего состояния используйте private tags.

Legacy kingdom helpers вроде `AddAddonKingdomTag(...)` остаются рабочими, но новый общий код может использовать `PoliticalWorldAPI.Tags`.

## Общие kingdom tags

`AddKingdomTag` / `HasKingdomTag` — общие договорённости, а не приватное состояние аддона.

Используйте shared tag только если другой мод действительно должен понимать тот же тег.

## Save lifecycle

Actor/City data хранится через vanilla object data container. Kingdom data проходит через save-backed helpers Political World.

Не сохраняйте live object reference так, будто это стабильный persistent ID.

Если значение должно переживать save/load — обязательно проверяйте реальный roundtrip.

## Schema migrations

API 1.11+ даёт lazy migrations для addon-owned Actor/City/Kingdom data.

Регистрация шага:

```csharp
PoliticalWorldAPI.Migrations.RegisterKingdom(
    AddonId,
    fromVersion: 0,
    toVersion: 1,
    handler: (kingdom, fromVersion, toVersion) =>
    {
        PoliticalWorldAPI.Data.SetInt(
            kingdom,
            AddonId,
            "mana",
            0
        );
        return true;
    }
);
```

Миграция применяется явно только к объектам, которые аддон реально использует:

```csharp
PoliticalWorldAPI.Migrations.Apply(kingdom, AddonId);
```

Версия схемы хранится в том же addon-owned namespace.

Registry **не** запускает автоматический full-world scan.

## Простое правило

Если другой аддон не обязан понимать значение/тег — оставляйте его приватным и namespaced.

Если вы меняете смысл или структуру сохранённых данных, добавьте migration вместо тихого переосмысления старых сейвов.

Смотрите `examples/08_World_Data`.
