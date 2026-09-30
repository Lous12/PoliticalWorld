# Справочник PoliticalWorldAPI 1.19

Political World 1.11 поставляется с **PoliticalWorldAPI 1.19.0**.

API разделён на partial-файлы в `src/PoliticalWorld/API/`. По возможности аддоны должны использовать публичный фасад `Lous12.PoliticalWorld.PoliticalWorldAPI`, а не внутренние классы `Main`.

## Основные области

- регистрация аддонов, проверка совместимости и capability discovery;
- регистрация идеологий и правительств;
- доступ к партиям, государствам и политическому состоянию;
- события и редкие политические события;
- actions, conditions и effects;
- данные аддонов, теги и локализация;
- lifecycle мира и world access;
- warfare helpers;
- UI integration и addon pages;
- release metadata и диагностика;
- politics expansion helpers, включая доступ к рангу монархии.

## Версия

```csharp
PoliticalWorldAPI.ApiVersion // "1.19.0"
PoliticalWorldAPI.ApiMajor   // 1
PoliticalWorldAPI.ApiMinor   // 19
```

## Ранги монархий

В 1.11 текущий ранг монархии доступен через:

```csharp
string rank = PoliticalWorldAPI.Countries.GetMonarchyRank(kingdom);
```

Там, где это поддерживается, определения аддонов также могут использовать rank IDs.

## Совместимость

Лучше указывать минимальную версию API, которая действительно нужна вашему аддону, а не требовать новейшую без необходимости. По возможности API развивается аддитивно.

Точные сигнатуры методов смотрите в `src/PoliticalWorld/API/` — это канонический источник для API 1.19.

См. также:

- [Первый аддон](GETTING_STARTED.md)
- [Совместимость и версии](compatibility-versioning.md)
- [Частые ошибки](common-mistakes.md)
- [Куда развивается API](FRAMEWORK_VISION.md)
