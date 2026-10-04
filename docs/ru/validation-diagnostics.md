# Validation, requirements и diagnostics

В PoliticalWorldAPI есть три связанных слоя:

1. проверить definition до регистрации;
2. проверить API/capability requirements;
3. получить diagnostics/support data после загрузки.

## Проверка контента перед регистрацией

```csharp
var validation =
    PoliticalWorldAPI.ValidateGovernment(AddonId, definition);

if (!validation.IsValid)
{
    LogError(validation.Summary);
    return;
}
```

`ValidationResult.Issues` содержит diagnostic code, понятное сообщение и `IsError`.

Warning не обязательно блокирует регистрацию. Error блокирует.

Сами `Register...` тоже валидируют definitions, поэтому явный `Validate...` особенно полезен, когда хочется показать нормальную ошибку ещё до регистрации.

## Проверка framework requirements

Современный аддон может указать минимальную API-версию и нужные capabilities в `AddonDefinition`.

```csharp
var definition = new PoliticalWorldAPI.AddonDefinition
{
    Id = AddonId,
    Name = "My Addon",
    RequiredApiMajor = 1,
    RequiredApiMinor = 19,
    RequiredCapabilities = new[]
    {
        "world.lifecycle.events",
        "diagnostics.report"
    }
};

var check =
    PoliticalWorldAPI.Framework.CheckRequirements(definition);

if (!check.Compatible)
{
    LogError(check.Summary);
    return;
}
```

Не требуйте 1.19, если код реально не использует возможности уровня 1.19.

## Diagnostics

После загрузки:

```csharp
PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

Диагностика хранит количество регистраций, subscriptions, callback errors, warnings и errors.

Исключения Event Bus / Rare Event callbacks изолируются и записываются, а не ломают dispatch всем остальным аддонам.

## Support report

API 1.19 умеет собирать удобный отчёт:

```csharp
string report =
    PoliticalWorldAPI.Framework.GetSupportReport(AddonId);
```

Его удобно прикладывать к баг-репорту вместе с:

- версией/build WorldBox;
- версией NeoModLoader;
- версией Political World;
- версией аддона;
- точным repro;
- `Player.log`;
- информацией, использовался ли старый save.

## Ecosystem diagnostics

`PoliticalWorldAPI.Ecosystem` даёт snapshots framework/addon state, compatibility, event metrics и framework issues.

Это инструменты разработчика/диагностики, а не повод опрашивать их каждый кадр ради gameplay.
