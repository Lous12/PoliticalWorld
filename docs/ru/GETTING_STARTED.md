# Первый аддон Political World за 10 минут

Текущая база: Political World **1.11.0**, PoliticalWorldAPI **1.19.0**, WorldBox **0.51.2 build 719**, NeoModLoader **1.2.0.1**.

В этом гайде используется актуальный шаблон API 1.19. Когда аддон уже работает, минимальную версию API можно понизить, если все используемые вами контракты существовали в более старой версии 1.x.

## 1. Структура папки

Создайте отдельную папку рядом с Political World:

```text
Mods/
├── PoliticalWorld/
└── MyPoliticalAddon/
    ├── mod.json
    ├── Main.cs
    └── Locales/
        ├── en.json
        └── ru.json
```

Не помещайте исходники аддона внутрь `PoliticalWorld/`: NML рекурсивно компилирует `.cs` внутри каждой папки мода.

## 2. mod.json

```json
{
  "name": "My Political Addon",
  "GUID": "YourName.MyPoliticalAddon",
  "author": "YourName",
  "version": "0.1.0",
  "description": "Example Political World addon",
  "targetGameBuild": 719,
  "Dependencies": ["Lous12.PoliticalWorld"],
  "OptionalDependencies": [],
  "IncompatibleWith": []
}
```

`GUID` аддона должен быть стабильным. Этот же ID удобно использовать как `AddonId` и префикс всех публичных ID контента.

## 3. Минимальный Main.cs

```csharp
using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace YourName.MyPoliticalAddon
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "YourName.MyPoliticalAddon";

        protected override void OnModLoad()
        {
            PoliticalWorldAPI.AddonDefinition definition =
                new PoliticalWorldAPI.AddonDefinition
                {
                    Id = AddonId,
                    Name = "My Political Addon",
                    Version = "0.1.0",
                    Author = "YourName",
                    Description = "My first Political World addon",
                    RequiredApiMajor = 1,
                    RequiredApiMinor = 19,
                    RequiredCapabilities = new[]
                    {
                        "framework.requirements-check",
                        "diagnostics.report"
                    }
                };

            PoliticalWorldAPI.AddonRequirementCheck requirements =
                PoliticalWorldAPI.Framework.CheckRequirements(definition);

            if (!requirements.Compatible)
            {
                LogError(requirements.Summary);
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(definition))
            {
                return;
            }

            LogInfo("Loaded through PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion);
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
```

Шаблон требует API 1.19, потому что `Framework.CheckRequirements` появился в API 1.19. Более простой аддон может поддерживать старую minor-версию 1.x, если использует только старые контракты.

## 4. Добавьте первую идеологию

После `RegisterAddon`:

```csharp
PoliticalWorldAPI.RegisterIdeology(
    AddonId,
    new PoliticalWorldAPI.IdeologyDefinition
    {
        Id = AddonId + ".magical_reformism",
        ParentId = "",
        NameKey = AddonId + ".magical_reformism",
        SupportThreshold = -1,
        DiffusionMultiplier = 1.0f,
        RandomWeight = 0,
        Tags = new[] { "magic", "reformist" }
    }
);
```

`RandomWeight = 0` означает: идеология не будет сама случайно засеваться обычным механизмом Political World.

## 5. Проверка

Запустите WorldBox и проверьте `Player.log`. Успешная загрузка должна дать ваш лог и diagnostics без ошибок.

Если регистрация не прошла, сначала смотрите diagnostics или `Framework.GetSupportReport(AddonId)`, а не внутренний код Political World.

Для изменений, связанных с сейвами, UI, lifecycle мира, войнами или дипломатией, проверяйте и новый мир, и загрузку существующего сейва.

## 6. Не обходите API только потому, что что-то неудобно

Если вы не нашли публичный метод, сначала проверьте:

- [Справочник API 1.19](API_REFERENCE_1_19.md)
- `src/PoliticalWorld/API/README.md`
- `examples/`

Не считайте `Main`, `ScenarioBridge` или приватное поле поддерживаемым контрактом для аддонов. Если Public API действительно не умеет нужную вещь — лучше открыть запрос на новую capability.

## Дальше

- формы правления: `governments.md`
- партии: `parties.md`
- редкие события: `political-events.md`
- actions: `actions.md`
- безопасные сохранения: `data-storage.md`
- lifecycle мира и queries: `API_REFERENCE_1_19.md`
- маленькие копируемые примеры: `../../examples/README.md`
