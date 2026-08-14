# Political World API 1.2 — фундамент

Эта внутренняя версия API делает ошибки аддонов понятнее, а их данные — безопаснее.

## Новые возможности

- `validation`
- `kingdom.addon-tags`
- `kingdom.addon-data.v2`

## Проверка до регистрации

Перед регистрацией можно вызвать:

```csharp
var check = PoliticalWorldAPI.ValidateAddon(definition);
var ideologyCheck = PoliticalWorldAPI.ValidateIdeology(AddonId, ideology);
var actionCheck = PoliticalWorldAPI.ValidateAction(AddonId, action);
```

`ValidationResult.IsValid` становится `false` только при наличии ошибки. Предупреждения остаются в `Issues`, но не запрещают регистрацию.

Сами методы регистрации используют ту же проверку автоматически. При ошибке в лог попадает понятное сообщение с префиксом `[Political World API]`.

## Безопасное пространство сохранений

В API 1.1 символы вроде `.` и `-` заменялись на `_`. Из-за этого два разных addon ID теоретически могли получить одинаковый внутренний ключ.

API 1.2 кодирует addon ID и ключ в UTF-8 hex, поэтому пространства данных не сталкиваются. При чтении API сначала проверяет новый ключ, а затем старый ключ API 1.1 и при необходимости автоматически копирует значение в новый формат. Старое значение не удаляется.

## Приватные теги аддона

Для внутренних состояний конкретного аддона используйте:

```csharp
PoliticalWorldAPI.AddAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
bool hasTag = PoliticalWorldAPI.HasAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
```

`AddKingdomTag` оставлен для намеренно общих тегов, которые должны понимать разные аддоны.
