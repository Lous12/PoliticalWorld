<p align="center">
  <img src="docs/assets/hero-banner.png" alt="Political World banner" width="100%">
</p>

<h1 align="center">Political World</h1>
<p align="center"><strong>Политический мод для WorldBox • Public API • открытая разработка</strong></p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="https://lous12.github.io/PoliticalWorld/">Сайт</a> ·
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869">Steam Workshop</a> ·
  <a href="https://discord.gg/kYH5GadndE">Discord</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/issues">Issues</a> ·
  <a href="https://github.com/Lous12/PoliticalWorld/pulls">Pull Requests</a>
</p>

# Текущий статус

- **Political World:** 1.11.0
- **Public API:** 1.19.0
- **Целевой build WorldBox:** 719
- **NeoModLoader:** 1.2.0.1
- **Статус разработки:** активная разработка больших новых функций сейчас на паузе
- **Текущий фокус:** поддержка, критические фиксы, уборка репозитория, документация и работа сообщества

## Что появилось в 1.11

- Прогрессия рангов монархии от малых титулов до королевства и империи.
- Исправлена смена правителя после выборов в республиках.
- Внутренние партийные фракции и динамические расколы.
- Отколовшиеся партии могут переходить к близким идеологиям, а при тяжёлых кризисах — переживать более резкий идеологический разрыв.
- Политические альянсы / избирательные блоки, которые объединяют поддержку на выборах и могут со временем распадаться.
- Улучшено долгосрочное появление партий.

Спасибо участникам Discord-сервера, чьи идеи и баг-репорты вошли в 1.11: **@Asriel**, **@Mauro**, **@Mars**.

## Быстрые ссылки

- [Сайт проекта](https://lous12.github.io/PoliticalWorld/)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3780484869)
- [Discord](https://discord.gg/kYH5GadndE)
- [Первый аддон](docs/ru/GETTING_STARTED.md)
- [Справочник API 1.19](docs/ru/API_REFERENCE_1_19.md)
- [Примеры](examples/README.md)
- [Шаблон аддона](templates/PoliticalWorld-Addon-Template)
- [Аддоны сообщества](https://lous12.github.io/PoliticalWorld/ru/community-addons.html)
- [Issues](https://github.com/Lous12/PoliticalWorld/issues)
- [Pull Requests](https://github.com/Lous12/PoliticalWorld/pulls)
- [Форки](https://github.com/Lous12/PoliticalWorld/forks)

## Для игроков

Political World добавляет идеологии, партии, правительства, выборы, политическую стабильность, сепаратизм, политические кризисы, международные блоки, саммиты, подготовку к войне, политические режимы карты, динамические названия стран, политику поселений и долгосрочную эволюцию партий.

## Для разработчиков

PoliticalWorldAPI 1.19 включает регистрацию аддонов, capability discovery, локализацию, данные и теги, события, редкие политические события, actions, реестры идеологий и правительств, доступ к партиям и государствам, warfare helpers, UI integration, lifecycle мира, release helpers и диагностику.

Исходники публичного API находятся в `src/PoliticalWorld/API/`. Если текстовая документация и публичный исходник расходятся, исходник API считается каноническим контрактом.

Начать лучше отсюда:

- [AI_START_HERE.md](AI_START_HERE.md)
- [AGENTS.md](AGENTS.md)
- [Карта исходников API](src/PoliticalWorld/API/README.md)
- [Примеры](examples/README.md)

## Open source, форки и вклад в проект

Political World открыт под MIT License. Форки, небольшие патчи, PR, эксперименты, исправления документации и помощь с ИИ приветствуются.

Для работы с core сначала прочитайте:

- [AGENTS.md](AGENTS.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [KNOWN_RISKS.md](KNOWN_RISKS.md)
- [DEVELOPMENT.md](DEVELOPMENT.md)

Форки должны явно указывать, что они неофициальные, и сохранять исходный текст MIT License. Официальные релизы Political World по-прежнему выпускаются основным проектом под поддержкой Lous12.

## Сообщество

Для обычного общения, тестов, идей и экспериментов с аддонами удобнее Discord. GitHub Issues/PR лучше подходят для воспроизводимых багов и изменений кода.

## Поддержать проект

Political World, Public API, исходники и документация остаются бесплатными. Поддержка полностью добровольная.

- [DonationAlerts](https://www.donationalerts.com/r/lous12)
- **USDT — TRC20 / TRON:** `TAooa2bwstvhrSPnTaDZjBNGHZ1j5zDB4p`
- **USDT — TON:** `UQCppGv_A8uf07Ws_zyPw_U7XRnhafM2TDd1ABR1DQrfGA73`

> Перед отправкой проверьте сеть. USDT в TRC20/TRON и USDT в TON — разные сети.

## Лицензия

Проект распространяется под [MIT License](LICENSE).
