# Warfare API

API 1.15+ даёт небольшой публичный warfare facade.

Главное различие — обычное объявление войны и намеренный bypass.

## Чтение состояния войны

```csharp
bool atWar =
    PoliticalWorldAPI.Warfare.IsAtWar(kingdom);

bool enemies =
    PoliticalWorldAPI.Warfare.AreAtWar(first, second);

var enemyKingdoms =
    PoliticalWorldAPI.Warfare.GetEnemies(kingdom);

int exhaustion =
    PoliticalWorldAPI.Warfare.GetWarExhaustion(kingdom);
```

## Обычное объявление

```csharp
bool started =
    PoliticalWorldAPI.Warfare.TryDeclareWar(
        attacker,
        defender
    );
```

Этот путь проходит через diplomacy/war interception Political World.

Если normal declaration вернул `false`, не надо автоматически повторять через force. Отказ может быть частью реальной игровой логики.

## Forced war

```csharp
bool started =
    PoliticalWorldAPI.Warfare.ForceWar(
        attacker,
        defender
    );
```

Этот путь намеренно обходит обычный interception PW.

Используйте его только если обход обычных правил действительно является функцией вашего аддона.

## Обновления WorldBox

Core warfare integration зависит от внутренних diplomacy методов WorldBox. Аддону лучше оставаться на публичном `Warfare` facade, чтобы изменения Harmony/signatures оставались проблемой Political World, а не каждого аддона.
