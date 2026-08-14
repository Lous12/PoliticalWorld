# Модульная структура Political World

Разбор старого монолитного `Main.cs` завершён. Новые игровые системы не должны добавляться в `Main.cs`: он намеренно оставлен маленькой точкой входа NeoModLoader.

Текущие области runtime-кода:

- `API/` — публичный фасад аддонов, Event Bus, diagnostics, реестры правительств и редких событий.
- `Core/Configuration/` — legacy-ID, настройки и общее runtime-состояние, важные для совместимости.
- `Core/Runtime/` — загрузка мода и staggered political pipeline.
- `Core/Integration/WorldBox/` — чувствительные к обновлениям WorldBox/Harmony-мосты.
- `Core/Persistence/` и `Core/Events/` — общие helpers сохранений и WorldLog.
- `Politics/` — правительства, идеологии, партии, выборы, руководство, советы, кризисы и стабильность.
- `International/` — блоки, синхронизация с vanilla Alliance и саммиты.
- `Warfare/` — интеграция войн и дипломатии.
- `Map/` — Political Map.
- `UI/` — Politics UI государств, окна и sandbox powers.

## Правило для новых изменений

Новая система должна жить в самом узком подходящем модуле. Если аддону требуется внутренний класс Political World, это считается недостающей возможностью Public API: лучше расширить `PoliticalWorldAPI`, чем раскрывать внутреннюю реализацию.
