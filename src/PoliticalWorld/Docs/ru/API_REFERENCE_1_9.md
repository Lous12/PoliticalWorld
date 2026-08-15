# PoliticalWorldAPI 1.9 — обновление для создателей и локализации

API 1.9 — обратно совместимое расширение API для Political World 1.7.x.

## Главное правило: локализация необязательна

Контент аддона больше не должен зависеть от языка игры пользователя.

Порядок получения текста:

1. перевод, зарегистрированный для текущего языка;
2. текущая локализация NeoModLoader, если она есть;
3. зарегистрированный английский/default fallback;
4. обычный `DisplayName` / `Description`;
5. ключ или ID контента.

Для самого простого аддона достаточно английского текста:

```csharp
new PoliticalWorldAPI.IdeologyDefinition {
    Id = AddonId + ".technocracy",
    DisplayName = "Technocracy"
}
```

Игрок с русским языком всё равно увидит `Technocracy`, даже если у аддона вообще нет русского перевода.

Для многоязычного аддона переводы можно зарегистрировать прямо через API:

```csharp
PoliticalWorldAPI.RegisterEnglishLocalization(AddonId,
    new Dictionary<string, string> {
        { AddonId + ".technocracy.name", "Technocracy" }
    });

PoliticalWorldAPI.RegisterLocalizationPack(AddonId, "ru",
    new Dictionary<string, string> {
        { AddonId + ".technocracy.name", "Технократия" }
    });
```

Обычные locale-файлы NeoModLoader тоже продолжают работать.

## Новые capabilities 1.9

- `localization.fallback`
- `localization.register`
- `content.metadata`
- `content.batch-register`
- `content.query`
- `effect.helpers`
- `condition.helpers.v2`
- `operation.result`
- `party.addon-data`
- `diagnostics.report`

## Более богатые метаданные

`IdeologyDefinition` / `IdeologyInfo` теперь поддерживают:

- `DisplayName`
- `DescriptionKey`
- `Description`
- `Icon`
- `SortOrder`

`GovernmentDefinition` / `GovernmentInfo` также получили описание, иконку и порядок сортировки.

## Пакетная регистрация

- `RegisterIdeologies(...)`
- `RegisterGovernments(...)`
- `RegisterActions(...)`
- `RegisterRarePoliticalEvents(...)`

Возвращается `BatchRegistrationResult`.

## Поиск контента

- `GetAddonContentSummary(addonId)`
- `GetIdeologiesByTag(tag, includeParentTags)`
- `GetGovernmentsByTag(tag)`
- `GetActionsByCategory(category, kingdom)`

## Результаты операций

Инструменты теперь могут получать причину ошибки, а не только `false`:

- `TryExecuteAction(...)`
- `TryExecuteRarePoliticalEvent(...)`
- `TrySetKingdomIdeology(...)`
- `TrySetKingdomCurrent(...)`
- `TrySetKingdomGovernment(...)`
- `TrySetKingdomStability(...)`

`OperationResult` содержит `Success`, код и понятное разработчику сообщение.

## Conditions v2

Новые готовые условия:

- `GovernmentHasTag`
- `HasRulingParty`
- `RulingPartyIdeologyIs`
- `HasActivePartyIdeology`
- `PartySupportAtLeast`
- `AddonIntAtLeast`
- `AddonIntAtMost`
- `AddonBoolIs`
- `KingdomHasAddonTag`

## Effects

`PoliticalWorldAPI.Effects` даёт готовые event-driven эффекты без собственного `Update()`:

- `Sequence`
- `ChangeStability`
- `SetStability`
- `SetIdeology`
- `SetCurrent`
- `SetGovernment`
- `AddTag` / `RemoveTag`
- `AddAddonTag` / `RemoveAddonTag`
- `SetAddonInt` / `ChangeAddonInt`
- `SetAddonBool`
- `PublishEvent`
- `SetPartySupport`
- `SetPartyRadicalism`

Их можно прямо передавать в `Handler` Action или Rare Political Event.

## Приватные данные аддона для партий

Аддон может сохранять своё состояние, привязанное к конкретной партии:

- `Get/SetPartyInt`
- `Get/SetPartyString`
- `Get/SetPartyBool`
- `Get/SetPartyFloat`

Отдельная система сохранений не создаётся — всё идёт через namespaced addon-data Political World.

## Диагностика

`ReportDiagnostic(addonId, severity, code, message)` позволяет аддону добавлять сообщения в developer diagnostics Political World.

## Второй проход оптимизации

API 1.9 не добавляет новый `Update()` и сохраняет event-driven архитектуру.

Дополнительно:

- O(1) проверка capabilities;
- кэш сортировки пользовательских правительств;
- проверка government tags без лишнего клонирования массива;
- `HashSet` для дедупликации при регистрации;
- фильтрация пользовательских правительств по аддону без сборки полного списка;
- единый безопасный путь локализации вместо повторных запросов отсутствующих ключей.

Оптимизации API 1.8 для больших популяций и ограниченных actor-scan остаются.
