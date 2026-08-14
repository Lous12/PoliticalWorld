# PoliticalWorldAPI 1.6 — краткий справочник

Точка входа:

```csharp
using Lous12.PoliticalWorld;
PoliticalWorldAPI....
```

## Версия и возможности

- `ApiVersion`, `ApiMajor`, `ApiMinor`, `CoreModId`
- `IsReady()`
- `IsCompatible(requiredMajor, requiredMinor)`
- `GetCapabilities()` / `HasCapability(...)`

API 1.6 объявляет capabilities для addon/action/ideology/government/kingdom/party/event/diagnostics систем. Для необязательных возможностей лучше проверять `HasCapability`, а не угадывать версию.

## Регистрация аддона

- `ValidateAddon(AddonDefinition)`
- `RegisterAddon(AddonDefinition)`
- `IsAddonRegistered(addonId)`
- `GetRegisteredAddons()`

Всегда регистрируйте аддон до его идеологий, правительств, действий, событий и подписок.

## Идеологии

- `GetIdeologies()`
- `GetRootIdeologies()`
- `GetCurrentsForRoot(rootId)`
- `ValidateIdeology(addonId, definition)`
- `RegisterIdeology(addonId, definition)`
- `GetIdeologyTags(id, includeParents)`
- `HasIdeologyTag(id, tag, includeParents)`

## Правительства

- `GetGovernmentForms()` / `GetGovernment(id)`
- `ValidateGovernment(...)` / `RegisterGovernment(...)`
- `GetGovernmentTags(...)` / `HasGovernmentTag(...)`
- `GetCoreGovernmentId(GovernmentArchetype)`

Архетипы: AbsoluteMonarchy, ConstitutionalMonarchy, ParliamentaryRepublic, PresidentialRepublic, OnePartyState, MilitaryDictatorship, CouncilRepublic, Oligarchy.

## Государство

Чтение:
- `GetKingdomState(kingdom)`
- `GetKingdomRuler(kingdom)`
- `RulerHasTrait`, `GetRulerRaceId`, `RulerIsImmortal`

Запись:
- `SetKingdomIdeology`
- `SetKingdomCurrent`
- `SetKingdomGovernment`
- `SetKingdomStability` / `ChangeKingdomStability`

## Партии

- `GetKingdomParties(..., includeInactive)`
- `GetKingdomParty(...)`
- `GetKingdomRulingParty(...)`
- `GetKingdomPartyLeader(...)`
- `CreateKingdomParty(...)`
- `RenameKingdomParty(...)`
- `SetKingdomPartySupport/Radicalism/Ideology/Active/Leader`
- `DeactivateKingdomParty` / `ReactivateKingdomParty`
- `AssignBestKingdomPartyLeader`
- `SetKingdomRulingParty` / `ClearKingdomRulingParty`

Hard-delete партии намеренно отсутствует: деактивация безопаснее для истории выборов, расколов и ссылок сохранения.

## Теги и данные

Общие теги: `Get/Has/Add/RemoveKingdomTag`.  
Приватные теги аддона: `Get/Has/Add/RemoveAddonKingdomTag`.  
Данные: `Get/SetKingdomInt`, `Get/SetKingdomString`.

Для внутреннего состояния аддона используйте addon-private методы.

## Actions

- `ValidateAction`
- `RegisterAction` / `UnregisterAction`
- `GetActions(kingdom)`
- `ExecuteAction(actionId, kingdom)`

## Conditions

`All`, `Any`, `Not`, `GovernmentIs`, `IdeologyIs`, `CurrentIs`, `PoliticalSystemIs`, `StabilityAtLeast/AtMost`, `RulerHasTrait`, `RulerRaceIs`, `RulerIsImmortal`, `KingdomHasTag`, `IdeologyHasTag`.

## Event Bus

- `GetEventIds()`
- `Subscribe(addonId, eventId, handler)`
- `Unsubscribe(...)` / `UnsubscribeAll(addonId)`

`Events.All` (`"*"`) подписывает на все известные события.

## Rare Political Events

- `ValidateRarePoliticalEvent`
- `RegisterRarePoliticalEvent`
- `UnregisterRarePoliticalEvent`
- `GetRarePoliticalEvent` / `GetRarePoliticalEvents`

`ChancePermille`: 0..1000; 10 = 1%, 100 = 10%, 1000 = 100%.

## Diagnostics

- `GetAddonDiagnostics(addonId)`
- `GetDiagnosticsReport(addonId)`
- `GetAllDiagnosticsReports()`
- `LogDiagnosticsReport(addonId)`

## Публикация WorldLog

`PublishKingdomEvent(kingdom, text, eventId, cooldownSeconds)` — безопасный публичный путь для заметного политического события.
