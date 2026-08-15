<p align="center">
  <img src="docs/assets/github-banner.svg" alt="Баннер Political World" width="100%">
</p>

<h1 align="center">Political World</h1>
<p align="center"><strong>Мод про политику для WorldBox • Public API • платформа для аддонов</strong></p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="https://lous12.github.io/PoliticalWorld/">Сайт</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/discussions">Discussions</a>
</p>

<p align="center">
  <img alt="Political World" src="https://img.shields.io/badge/Political%20World-1.7.0%20Public%20Beta-2563eb">
  <img alt="Public API" src="https://img.shields.io/badge/Public%20API-1.9.0-8b5cf6">
  <img alt="WorldBox" src="https://img.shields.io/badge/WorldBox%20PC-0.51.2-0ea5e9">
  <img alt="NeoModLoader" src="https://img.shields.io/badge/NML-1.2.0.1-10b981">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-22c55e">
</p>

> **Играй с политикой. Или возьми API и сделай что-то своё.**

Political World начинался как мод про политику для WorldBox.  
Сейчас у проекта две стороны: сам мод остаётся политическим, а `PoliticalWorldAPI` постепенно становится открытой платформой, на которой другие авторы могут делать свои аддоны.

---

## Быстрая навигация

| Что нужно | Куда идти |
|---|---|
| Скачать мод | [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869) |
| Почитать документацию | [Сайт](https://lous12.github.io/PoliticalWorld/) |
| Сделать первый аддон | [Быстрый старт](docs/ru/GETTING_STARTED.md) |
| Понять направление API | [Видение фреймворка](docs/ru/FRAMEWORK_VISION.md) |
| Задать вопрос / выложить аддон | [Discussions](https://github.com/Lous12/PoliticalWorld/discussions) |

---

## Две стороны одного проекта

### 🎮 Political World для игроков
Мод добавляет в WorldBox политический слой:

- идеологии и течения;
- партии, лидеры, поддержка и радикализм;
- формы правления и политические системы;
- выборы, советы и смена руководства;
- стабильность, кризисы, мятежи, перевороты и революции;
- международные блоки и интеграция с альянсами;
- физические саммиты правителей;
- подготовка к войне, дипломатические кризисы и военная усталость;
- режимы Political Map.

### 🧩 PoliticalWorldAPI для разработчиков
API уже умеет:

- регистрировать аддоны и проверять их данные;
- работать без обязательной локализации;
- хранить данные и теги аддонов;
- передавать события через Event Bus;
- поддерживать Actions и Rare Events;
- использовать готовые Conditions и Effects;
- выдавать диагностику и понятные результаты операций;
- работать с идеологиями, правительствами, партиями и государствами.

**Перевод не обязателен.**  
Если аддон написан только на английском, игрок всё равно увидит понятный английский текст вместо `missing text`.

---

## Куда всё это развивается

Сам Political World остаётся модом про политику.

Но `PoliticalWorldAPI` мы делаем шире, чтобы на нём можно было собирать не только политические дополнения, но и другие идеи:

- фэнтези-системы;
- религии и культы;
- экономику и торговлю;
- болезни и катастрофы;
- системы персонажей;
- инструменты для сценариев и разработки;
- и другие аддоны, которым пригодятся общие данные, события и интеграция.

Подробнее: [Видение фреймворка](docs/ru/FRAMEWORK_VISION.md)

---

## Структура репозитория

```text
src/PoliticalWorld/    Исходники мода и Public API
docs/en/               Документация на английском
docs/ru/               Документация на русском
examples/              Небольшие примеры аддонов
templates/             Шаблоны + standalone NML starter
AI_START_HERE.md       Точка входа для ИИ-ассистентов
```

---

## Сообщество

Полезные ссылки:

- [Discussions](https://github.com/Lous12/PoliticalWorld/discussions)
- [Аддоны сообщества](docs/ru/community-addons.md)
- [Что можно создавать?](docs/ru/what-you-can-build.md)

Не обязательно начинать с огромного проекта. Одно событие, одна идеология или маленький инструмент — уже хороший старт.

---

## Лицензия

Проект распространяется под [MIT License](LICENSE).
