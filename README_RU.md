<p align="center">
  <img src="docs/assets/icon.png" width="128" alt="Иконка Political World">
</p>

<h1 align="center">Political World</h1>

<p align="center"><strong>Политический фреймворк для WorldBox — чтобы каждый мог творить.</strong></p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="docs/ru/GETTING_STARTED.md">Первый аддон</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.7.0%20Beta-blue">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.6.0-blueviolet">
  <img alt="WorldBox" src="https://img.shields.io/badge/WorldBox%20PC-0.51.2-informational">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NeoModLoader-1.2.0.1-informational">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green">
</p>

Political World добавляет в WorldBox идеологии, партии, формы правления, политические кризисы, выборы, международные блоки, саммиты, политические последствия войн и Political Map. Начиная с 1.7 это ещё и открытая платформа для аддонов: сторонние моды могут расширять политическую систему через документированный Public API, не залезая во внутренний код Political World.

В основе проекта простая идея: **моддинг должен быть местом, где человек учится, создавая что-то своё**. Можно начать с одной идеологии или события, а потом дорасти до полноценного политического overhaul-мода.

## Для игроков

Political World создаёт ощущение глубокой политики без тяжёлой симуляции каждого жителя каждый кадр. Основные системы:

- деревья идеологий и течений;
- политические партии, лидеры, поддержка и радикализм;
- разные формы правления и политические системы;
- выборы, советы, партийные съезды и смена руководства;
- политическая стабильность и кризисы;
- международные блоки, синхронизация с vanilla Alliance и физические саммиты лидеров;
- подготовка к войне, дипломатические кризисы, военная усталость и мирные условия;
- Political Map с режимами партий, идеологий и политического напряжения.

## Для разработчиков

Единая публичная точка входа:

```csharp
using Lous12.PoliticalWorld;

if (!PoliticalWorldAPI.IsCompatible(1, 6))
    return;
```

Аддон может добавлять и изменять идеологии, пользовательские правительства, партии, состояние государств, Scenario Actions, подписки Event Bus, редкие политические события, приватные данные/теги аддона и diagnostics. Внутренние `Main` и `ScenarioBridge` не являются частью публичного контракта.

```text
WorldBox + NeoModLoader
        ↓
Political World Core
        ↓
PoliticalWorldAPI 1.6
        ↓
Твой аддон / Scenario Tools / Fantasy Politics / моды сообщества
```

Начать: **[Создать первый аддон](docs/ru/GETTING_STARTED.md)**.

## Структура репозитория

```text
src/PoliticalWorld/    Исходники самого мода
examples/              Маленькие законченные примеры аддонов
templates/             Шаблон PW-аддона + standalone NML starter
docs/en/               Английская документация
docs/ru/               Русская документация
AI_START_HERE.md       Точка входа для ИИ-ассистентов
ARCHITECTURE.md        Архитектура и границы модулей
API_VERSIONING.md      Правила совместимости Public API
```

## Совместимость

| Компонент | Версия |
|---|---|
| Political World | 1.7.0 public beta candidate |
| Public API | 1.6.0 |
| WorldBox PC | 0.51.2 / build 719 |
| NeoModLoader | 1.2.0.1 |

После обновлений WorldBox чаще всего внимания требует `Core/Integration/WorldBox/`. Версия Public API существует отдельно от версии игры.

## Документация

Русский: [индекс](docs/ru/README.md) · [API 1.6](docs/ru/API_REFERENCE_1_6.md) · [NML UI recipes](docs/ru/nml-ui-recipes.md) · [разработка с ИИ](docs/ru/using-ai.md)

English: [documentation index](docs/en/README.md) · [API 1.6](docs/en/API_REFERENCE_1_6.md) · [NML UI recipes](docs/en/nml-ui-recipes.md) · [Using AI](docs/en/using-ai.md)

## Участие в разработке

Исправления, примеры, документация и предложения по Public API приветствуются. Сначала прочитайте [CONTRIBUTING.md](CONTRIBUTING.md) и [ARCHITECTURE.md](ARCHITECTURE.md). При изменениях публичного поведения русская и английская документация должны оставаться равноценными.

## Лицензия

Political World распространяется под [MIT License](LICENSE). Форкайте, изучайте, меняйте, продолжайте и создавайте своё.

WorldBox принадлежит Maxim Karpenko / соответствующим правообладателям. Political World — мод сообщества и не является официальным проектом WorldBox.
