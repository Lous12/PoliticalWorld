# Частые ошибки

## Аддон лежит внутри папки PoliticalWorld

NeoModLoader рекурсивно компилирует `.cs` внутри папки мода. Аддон должен лежать отдельной sibling-папкой.

## Забыта dependency

PW-аддон должен объявить:

```json
"Dependencies": ["Lous12.PoliticalWorld"]
```

Иначе порядок компиляции/загрузки и assembly reference не гарантированы.

## Текущая API-версия считается обязательной минимальной

`1.19.0` — current API, а не автоматический minimum requirement.

Если аддон использует только более старые контракты, честный `IsCompatible(1, x)` с меньшим minor — нормально.

## RegisterAddon не вызван первым

Сначала зарегистрируйте аддон, затем owned content, custom events, UI, tags/data и subscriptions.

## ID контента/событий не namespaced

Используйте формат вроде:

```text
YourName.MyAddon.feature
YourName.MyAddon.event_name
```

Validation специально не даёт одному аддону регистрировать чужой ID.

## Хардкодятся legacy `ukiol_*`

Часть старых ID уже является частью save compatibility. В аддоне используйте публичные constants/accessors, если они есть, а не копируйте внутренние ID из core source.

## Каждый кадр сканируется весь мир

Используйте:

- Event Bus для переходов;
- Rare Political Events для редких политических эффектов;
- `WorldQuery` для явных capped snapshots;
- cached addon state, когда это возможно.

Не заменяйте один отсутствующий event бесконечным full-world scan.

## WorldQuery.IsReady воспринимается как «весь save уже точно полностью восстановлен»

Публичный lifecycle говорит, что world/query surface доступен. Сложная restore-логика аддона всё равно должна осторожно относиться к load transition и реагировать на lifecycle events, а не соревноваться с инициализацией мира.

## Shared tags используются как приватное состояние

Для private state используйте `PoliticalWorldAPI.Tags` / addon-owned data. Shared kingdom tags нужны для осознанного interop.

## Сохраняются live object references

Runtime Actor/City/Kingdom reference — не persistent ID. Сохраняйте стабильные данные и после load заново находите живые объекты при необходимости.

## Схема сохранённых данных меняется без migration

Если меняется смысл/структура addon-owned data, используйте `PoliticalWorldAPI.Migrations`, а не тихое переосмысление старых сейвов.

## Harmony-патчится окно PW ради UI аддона

Сначала проверьте публичные UI surfaces:

- inspector sections;
- context actions;
- hosted kingdom/settlement Politics pages.

Если public host реально не умеет нужное — запросите capability.

## Аддон лезет в Main/ScenarioBridge

Это implementation details, а не addon contract.

Если Public API чего-то не умеет, зафиксируйте/request capability вместо reflection glue вокруг internals.

## ForceWar используется как обычный способ начать войну

`Warfare.TryDeclareWar` проходит через diplomacy interception PW. `ForceWar` намеренно его обходит.

Force path нужен только когда обход обычных правил действительно является функцией.

## После ошибки присылается только скрин

Для нормального bug report нужен полный compile/runtime error или `Player.log`, версии/build и точный repro.
