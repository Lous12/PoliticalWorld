# Validation и Developer Diagnostics

Перед регистрацией сложного объекта можно вызвать `Validate...` и показать человеку понятную причину ошибки.

```csharp
var validation = PoliticalWorldAPI.ValidateGovernment(AddonId, definition);
if (!validation.IsValid)
{
    LogError(validation.Summary);
    return;
}
```

Коды вида `PW203`, `PW403`, `PW508` предназначены для стабильной диагностики. Warning не обязан блокировать регистрацию; error блокирует.

После загрузки аддона полезно:

```csharp
PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
```

Отчёт включает количество зарегистрированных идеологий, правительств, actions, rare events, subscriptions, callback errors, warnings и errors.

Если callback Event Bus или rare event бросает исключение, Political World ловит его, записывает diagnostics и продолжает работу остальных аддонов.
