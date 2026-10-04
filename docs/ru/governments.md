# Формы правления

Пользовательская форма правления использует один из поддерживаемых механических archetype Political World, но сохраняет собственный публичный ID, название, локализацию и теги.

## Регистрация

```csharp
PoliticalWorldAPI.RegisterGovernment(
    AddonId,
    new PoliticalWorldAPI.GovernmentDefinition
    {
        Id = AddonId + ".dragon_monarchy",
        NameKey = AddonId + ".dragon_monarchy",
        DisplayName = "Dragon Monarchy",
        Description = "A custom hereditary government.",
        BaseArchetype = PoliticalWorldAPI.GovernmentArchetype.AbsoluteMonarchy,
        Tags = new[] { "dragon", "hereditary", "fantasy" }
    }
);
```

Поддерживаемые archetypes:

- `AbsoluteMonarchy`
- `ConstitutionalMonarchy`
- `ParliamentaryRepublic`
- `PresidentialRepublic`
- `OnePartyState`
- `MilitaryDictatorship`
- `CouncilRepublic`
- `Oligarchy`

`Unknown` нельзя использовать как основу custom government.

## Зачем нужен archetype

Custom government сохраняет свой публичный образ, а Political World переиспользует уже проверенную логику выборов, лидерства и политической системы. Аддону не приходится заново реализовывать core simulation.

Если аддон временно отсутствует, сохранённый базовый archetype может продолжить работать. После возвращения аддона custom ID снова может быть распознан.

## Чтение

```csharp
var all = PoliticalWorldAPI.GetGovernmentForms();
var one = PoliticalWorldAPI.GetGovernment(governmentId);
var mine = PoliticalWorldAPI.GetGovernmentsByAddon(AddonId);

var tags = PoliticalWorldAPI.GetGovernmentTags(governmentId);
bool hereditary = PoliticalWorldAPI.HasGovernmentTag(governmentId, "hereditary");
```

`GovernmentInfo.Source` показывает, пришла запись из core Political World или из аддона.

## Изменение государства

```csharp
PoliticalWorldAPI.SetKingdomGovernment(
    kingdom,
    governmentId,
    publishEvent: true
);
```

Используйте публичный метод, а не прямую запись raw government ID в save data.

## Political system — отдельное понятие

Форма правления и political system связаны, но это не одно и то же.

Для system metadata есть `GetPoliticalSystems()` / `GetPoliticalSystem(id)` и стабильные константы `PoliticalWorldAPI.PoliticalSystems`. Не хардкодьте legacy `ukiol_*`, если для нужного ID уже есть публичная константа.
