# UI integration

PoliticalWorldAPI даёт публичные UI extension points, чтобы аддонам не приходилось Harmony-патчить приватную реализацию окон Political World для обычных задач.

## Inspector sections

Через `PoliticalWorldAPI.UI.RegisterInspectorSection(...)` можно зарегистрировать declarative fields для Actor, City или Kingdom.

Section может содержать:

- namespaced ID;
- target kind;
- display/localization text;
- sort order;
- visibility predicate;
- fields с value providers.

Смотрите `examples/09_UI_Inspector`.

## Context actions

`PoliticalWorldAPI.UI.RegisterContextAction(...)` регистрирует действие над выбранным inspector target.

Definition может задавать:

- visibility;
- `CanExecute`;
- `Execute`.

Это нормальный способ добавить selected-object action без приватного патча PW UI.

## Hosted Politics pages

API 1.16+ умеет регистрировать полноценные страницы в kingdom/settlement Politics host:

```csharp
PoliticalWorldAPI.UI.RegisterKingdomPoliticsPage(
    AddonId,
    new PoliticalWorldAPI.PoliticsPageDefinition
    {
        Id = AddonId + ".overview",
        DisplayName = "Addon",
        SortOrder = 100,
        Render = context =>
        {
            // Рендерить в context.Content.
        }
    }
);
```

Для поселений есть `RegisterSettlementPoliticsPage(...)`.

Навигацией и lifecycle хоста владеет Political World. Аддон получает `PoliticsPageContext` с target objects и content transform.

## Cleanup

`UI.UnregisterInspectorSection`, `UI.UnregisterContextAction` и `UI.UnregisterPoliticsPage` снимают отдельные регистрации.

Framework runtime cleanup также умеет убрать detachable UI registrations аддона.

## Правило

Перед патчем `KingdomWindow`, `CityWindow`, tab navigation или private UI methods PW проверьте, нельзя ли сделать это через public UI host.

Если нельзя — лучше запросить capability, чем зависеть от приватной реализации.
