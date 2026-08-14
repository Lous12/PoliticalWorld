# Реестр форм правления — API 1.5

Теперь аддон может зарегистрировать собственную форму правления, не переписывая внутреннюю политическую симуляцию Political World.

```csharp
PoliticalWorldAPI.RegisterGovernment(AddonId, new PoliticalWorldAPI.GovernmentDefinition
{
    Id = AddonId + ".dragon_monarchy",
    NameKey = "dragon_monarchy_name",
    DisplayName = "Драконья монархия",
    BaseArchetype = PoliticalWorldAPI.GovernmentArchetype.AbsoluteMonarchy,
    Tags = new[] { "dragon", "hereditary", "fantasy" }
});
```

`SetKingdomGovernment()` принимает как встроенные, так и зарегистрированные ID. Для игрока и API сохраняется собственное имя режима, а механика использует выбранный проверенный архетип. Если аддон временно отсутствует, базовый архетип государства остаётся рабочим; после возвращения аддона пользовательский ID снова может быть распознан.
