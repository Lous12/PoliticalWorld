# Первый аддон Political World за 10 минут

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
            if (!PoliticalWorldAPI.IsCompatible(1, 6))
            {
                LogError("Political World API 1.6+ is required.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(
                new PoliticalWorldAPI.AddonDefinition
                {
                    Id = AddonId,
                    Name = "My Political Addon",
                    Version = "0.1.0",
                    Author = "YourName",
                    Description = "My first Political World addon"
                }))
            {
                return;
            }

            LogInfo("Loaded through PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion);
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
        }
    }
}
```

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

Запустите WorldBox и проверьте `Player.log`. Успешная загрузка должна дать ваш лог и diagnostics без ошибок. Если регистрация не прошла, сначала смотрите `ValidationResult`/diagnostics, а не внутренний код Political World.

## Дальше

- формы правления: `governments.md`
- партии: `parties.md`
- редкие события: `political-events.md`
- действия для Scenario Tools: `actions.md`
- безопасные сохранения: `data-storage.md`
