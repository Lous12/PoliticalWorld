# PoliticalWorldAPI 1.8 — краткий справочник

API 1.8 — обратно совместимое обновление API и инструментов для Political World 1.7.x.

```csharp
using Lous12.PoliticalWorld;
PoliticalWorldAPI....
```

## Версия и capabilities

- `ApiVersion` = `1.8.0`
- `IsCompatible(requiredMajor, requiredMinor)`
- `GetCapabilities()` / `HasCapability(...)`

Новые capabilities 1.8:

- `content.lookup`
- `content.filter-by-addon`
- `localization.safe`
- `action.inspect`
- `kingdom.addon-data.typed`
- `political-system.read`
- `operation.checks`

## Поиск контента

Аддонам и инструментам больше не нужно получать полный список только ради одного объекта.

- `GetAddon(addonId)`
- `GetIdeology(ideologyId)`
- `GetIdeologiesByAddon(addonId)`
- `GetGovernment(governmentId)`
- `GetGovernmentsByAddon(addonId)`
- `GetAction(actionId, kingdom)`
- `GetActionsByAddon(addonId, kingdom)`
- `GetRarePoliticalEvent(eventId)`
- `GetRarePoliticalEventsByAddon(addonId)`

Старые методы получения полных списков остаются.

## Безопасная локализация

- `HasLocalization(key)`
- `ResolveLocalization(key, fallback)`

Сначала проверяется существование ключа. Если сторонний аддон забыл локализацию, используется fallback без спама `missing text` в логе WorldBox.

Такое же безопасное разрешение теперь используется внутри API для идеологий, пользовательских правительств, Actions и редких событий.

## Actions

- `GetAction(actionId, kingdom = null)`
- `GetActions(kingdom)`
- `GetActionsByAddon(addonId, kingdom = null)`
- `CanExecuteAction(actionId, kingdom)`
- `ExecuteAction(actionId, kingdom)`

`CanExecuteAction` удобно использовать для кнопок и director/scenario-интерфейсов.

## Типизированные приватные данные государства

Хранилище остаётся namespaced и совместимым с существующими сохранениями.

- `Get/SetKingdomInt`
- `Get/SetKingdomString`
- `Get/SetKingdomBool`
- `Get/SetKingdomFloat`

Float сохраняется через invariant culture.

## Политические системы

Используйте `PoliticalWorldAPI.PoliticalSystems.*`, а не прописывайте старые `ukiol_*` ID вручную.

Константы:

- `Competitive`
- `OneParty`
- `Soviet`
- `SovietOneParty`
- `NonElectoral`
- `Decentralized`

Метаданные:

- `GetPoliticalSystems()`
- `GetPoliticalSystem(id)`

`PoliticalSystemInfo` содержит отображаемое имя и основные свойства системы.

## Проверка операций

`CheckSetKingdomRulingParty(kingdom, partyId)` возвращает `OperationCheck`:

- `Allowed`
- стабильный `Code`
- понятный разработчику `Message`

Текущие коды: `ok`, `invalid-kingdom`, `party-mandate-not-supported`, `party-id-required`, `party-not-found`, `party-inactive`.

Старый bool-метод `SetKingdomRulingParty(...)` не изменён.

## Редкие политические события

Ручной запуск из API 1.7 остаётся:

- `CanExecuteRarePoliticalEvent(eventId, kingdom)`
- `ExecuteRarePoliticalEvent(eventId, kingdom)`

В 1.8 добавлен фильтр `GetRarePoliticalEventsByAddon(addonId)`.

## Оптимизация ядра

В 1.8 также входит проход по производительности без добавления новой симуляции:

- ограниченное чтение жителей там, где поиск лидера/кандидата всё равно имеет лимит;
- линейная дедупликация больших коллекций жителей вместо O(N²) `List.Contains`;
- кэширование reflection-поиска метода изменения городских ресурсов;
- расчёт общегосударственных идеологических экономических параметров один раз на государство, а не на каждый город;
- кэш отсортированного списка Rare Events;
- прямой поиск идеологий/Actions без лишней сборки полного списка;
- статические массивы имён для горячих reflection-путей.

Новый `Update()` и постоянная симуляция каждого жителя не добавлялись.
