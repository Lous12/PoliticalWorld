# Совместимость и версионирование API

Версия Political World и версия его Public API существуют отдельно.

- Версия мода: `1.7.0` public beta candidate.
- Public API: `1.6.0`.
- Целевая версия игры: WorldBox PC build `719` (`0.51.2`).

Аддон, которому нужен API 1.6, должен проверить:

```csharp
if (!PoliticalWorldAPI.IsCompatible(1, 6)) return;
```

## Правило API 1.x

- **Major** меняется при несовместимых изменениях публичного API.
- **Minor** добавляет совместимые возможности и новые публичные методы.
- **Patch** исправляет поведение без намеренного нарушения документированного контракта.

Для необязательных возможностей используйте capability check:

```csharp
if (PoliticalWorldAPI.HasCapability("political-event.rare"))
{
    // register optional rare events
}
```

Не используйте reflection и внутренние `Main` / `ScenarioBridge` вместо Public API. Внутренние модули могут меняться без гарантии совместимости.

`targetGameBuild` относится к совместимости с WorldBox и не заменяет проверку версии Political World API.
