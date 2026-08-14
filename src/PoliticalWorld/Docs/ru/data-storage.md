# Данные и теги аддона

## Addon-private данные

```csharp
int value = PoliticalWorldAPI.GetKingdomInt(kingdom, AddonId, "mana", 0);
PoliticalWorldAPI.SetKingdomInt(kingdom, AddonId, "mana", value + 1);

string dynasty = PoliticalWorldAPI.GetKingdomString(kingdom, AddonId, "dynasty", "");
PoliticalWorldAPI.SetKingdomString(kingdom, AddonId, "dynasty", "Draconis");
```

API 1.2+ использует collision-safe namespace и умеет подхватывать старый формат 1.1 при чтении.

## Приватные теги

```csharp
PoliticalWorldAPI.AddAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
bool has = PoliticalWorldAPI.HasAddonKingdomTag(kingdom, AddonId, "dragon_dynasty");
```

Используйте их для внутренней логики вашего аддона.

## Общие теги

`AddKingdomTag` / `HasKingdomTag` предназначены только для осознанно общих договорённостей между модами. Их namespace не принадлежит одному аддону.

## Правило

Если другой мод не обязан понимать ваш тег/ключ — используйте addon-private вариант.
