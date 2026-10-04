# Warfare API

API 1.15+ exposes a small public warfare facade.

The important distinction is between a normal declaration and an intentional bypass.

## Read war state

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

## Normal declaration

```csharp
bool started =
    PoliticalWorldAPI.Warfare.TryDeclareWar(
        attacker,
        defender
    );
```

This goes through Political World's diplomacy/war interception.

If normal declaration returns `false`, do not automatically retry with force. The rejection may be meaningful gameplay logic.

## Forced war

```csharp
bool started =
    PoliticalWorldAPI.Warfare.ForceWar(
        attacker,
        defender
    );
```

This intentionally bypasses Political World's normal interception path.

Use it only when bypassing normal rules is explicitly the feature you are implementing.

## WorldBox updates

The core war integration depends on WorldBox diplomacy internals. Addons should stay on the public `Warfare` facade so internal Harmony/signature changes remain Political World's problem instead of becoming every addon's problem.
