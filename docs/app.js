const menuToggle = document.getElementById('menuToggle');
const menuClose = document.getElementById('menuClose');
const menu = document.getElementById('mobileMenu');
const backdrop = document.getElementById('menuBackdrop');
const langToggle = document.getElementById('langToggle');

const translations = {
  en: {
    brand_sub: 'Politics • API • Framework',
    nav_status: 'Status',
    nav_update: 'Latest update',
    nav_players: 'Players',
    nav_creators: 'Creators',
    nav_community: 'Community',
    nav_roadmap: 'Roadmap',
    menu_all: 'Everything in one menu',
    menu_project: 'Project',
    menu_direction: 'Development direction',
    menu_play: 'Play',
    menu_help: 'Help / bug reports',
    menu_build: 'Build',
    menu_get_started: 'Getting Started',
    menu_framework: 'Framework Vision',
    menu_what_build: 'What can you build?',
    menu_templates: 'Templates',
    menu_examples: 'Examples',
    menu_community: 'Community',
    menu_addons: 'Community Addons',
    menu_language: 'Language',
    hero_eyebrow: 'Politics mod • Public API • Addon Framework',
    hero_title: 'Play politics. Build anything.',
    hero_lead: 'Political World is a politics mod for WorldBox and a growing framework for creators who want to build addons on top of a stable public API.',
    hero_steam: 'Open on Steam',
    hero_github: 'Open GitHub',
    status_kicker: 'Current status',
    status_title: 'What is live right now',
    status_public: 'Public Beta',
    status_beta: 'Public Beta',
    status_tested: 'runtime-tested base',
    status_supported: 'supported setup',
    latest_kicker: 'Latest framework update',
    latest_title: 'Public API 1.9 — Creator & Localization Update',
    latest_body: 'API 1.9 improves the creator side of Political World: optional localization with readable English fallback, stronger addon data, reusable Conditions and Effects, better diagnostics, creator conveniences and performance work.',
    latest_api: 'Read API 1.9 →',
    latest_vision: 'See the framework direction →',
    players_kicker: 'For players',
    players_title: 'Political World as a mod',
    players_politics_title: 'Politics that evolves',
    players_politics_body: 'Ideologies, parties, governments, elections, crises, revolutions and leadership changes grow with the world.',
    players_world_title: 'World-level politics',
    players_world_body: 'International blocs, vanilla Alliance integration, physical ruler summits and war-related political consequences.',
    players_map_body: 'Dedicated map modes for parties, ideologies and political tension without turning the mod into a heavy per-frame simulation.',
    creators_kicker: 'For creators',
    creators_title: 'Start small, then build outward',
    creator_start_title: 'Make your first addon',
    creator_start_body: 'Use the template and quick-start guide. One event or one ideology is enough for a first project.',
    creator_start_link: 'Getting Started →',
    creator_api_title: 'Use the public contract',
    creator_api_body: 'Build through PoliticalWorldAPI instead of depending on internal Main or ScenarioBridge implementation details.',
    creator_request_title: 'Ask for missing capabilities',
    creator_request_body: 'If the API cannot express something useful, request the capability instead of creating a private workaround.',
    creator_request_link: 'Open Discussions →',
    community_kicker: 'Community',
    community_title: 'A place for addons to grow',
    community_catalog: 'Verified addon catalog',
    community_empty_title: 'No verified community addons listed yet',
    community_empty_body: 'That is fine. The catalog is intentionally curated: an addon is listed after it has been tested on a supported Political World setup.',
    community_submit: 'Submit an addon for testing',
    community_view: 'Open addon catalog',
    community_testing_body: 'Compatibility testing before an addon is added to the public catalog.',
    community_requests_title: 'Ideas & API requests',
    community_requests_body: 'Tell us what the framework is missing before you have to bypass it.',
    community_help_title: 'Modding help',
    community_help_body: 'Questions, experiments and small projects are welcome.',
    direction_kicker: 'Development direction',
    direction_title: 'The mod is not abandoned — the foundation is the priority',
    direction_body: 'For a while, large gameplay updates may be less frequent because the focus is on expanding the Public API. The goal is to let other creators build systems and addons without waiting for every idea to be implemented in the core mod.',
    direction_link: 'Read the full framework vision →',
    roadmap_kicker: 'Roadmap',
    roadmap_title: 'From political API to general framework',
    roadmap_done: 'Current stable base',
    roadmap_next: 'Next focus',
    roadmap_future: 'Later',
    roadmap_19: 'Creator conveniences, localization fallback, addon data, Conditions / Effects, diagnostics and performance work.',
    roadmap_110: 'Generic data and tags for more world objects, safer Actor / City access, custom events, extensible Conditions / Effects, generic content and better cross-addon integration.',
    roadmap_ui_title: 'UI & ecosystem hooks',
    roadmap_ui: 'Inspector sections, context actions, creator-tool integration, lifecycle helpers and more ways for addons to work together.',
    directory_kicker: 'Directory',
    directory_title: 'Everything important in one place',
    directory_project: 'Project',
    directory_docs: 'Documentation',
    directory_build: 'Build',
    directory_community: 'Community',
    directory_requests: 'Help / Ideas / API requests',
    footer_tagline: 'Politics mod • Public API • Addon Framework'
  },
  ru: {
    brand_sub: 'Политика • API • Фреймворк',
    nav_status: 'Статус',
    nav_update: 'Последнее обновление',
    nav_players: 'Игрокам',
    nav_creators: 'Разработчикам',
    nav_community: 'Сообщество',
    nav_roadmap: 'Планы',
    menu_all: 'Всё важное в одном меню',
    menu_project: 'Проект',
    menu_direction: 'Направление разработки',
    menu_play: 'Играть',
    menu_help: 'Помощь / баги',
    menu_build: 'Создавать',
    menu_get_started: 'Первый аддон',
    menu_framework: 'Куда развивается API',
    menu_what_build: 'Что можно создавать?',
    menu_templates: 'Шаблоны',
    menu_examples: 'Примеры',
    menu_community: 'Сообщество',
    menu_addons: 'Аддоны сообщества',
    menu_language: 'Язык',
    hero_eyebrow: 'Мод про политику • Public API • платформа для аддонов',
    hero_title: 'Играй с политикой. Создавай что угодно.',
    hero_lead: 'Political World — мод про политику для WorldBox и развивающаяся платформа для авторов, которые хотят делать свои аддоны через стабильный Public API.',
    hero_steam: 'Открыть в Steam',
    hero_github: 'Открыть GitHub',
    status_kicker: 'Текущий статус',
    status_title: 'Что уже доступно',
    status_public: 'Public Beta',
    status_beta: 'Public Beta',
    status_tested: 'протестированная база',
    status_supported: 'поддерживаемая связка',
    latest_kicker: 'Последнее обновление API',
    latest_title: 'Public API 1.9 — Creator & Localization Update',
    latest_body: 'API 1.9 улучшает именно сторону разработки аддонов: необязательная локализация с понятным английским fallback, данные аддонов, готовые Conditions и Effects, диагностика, creator-функции и оптимизация.',
    latest_api: 'Открыть API 1.9 →',
    latest_vision: 'Посмотреть направление фреймворка →',
    players_kicker: 'Игрокам',
    players_title: 'Political World как мод',
    players_politics_title: 'Политика, которая развивается',
    players_politics_body: 'Идеологии, партии, формы правления, выборы, кризисы, революции и смена руководства развиваются вместе с миром.',
    players_world_title: 'Политика между государствами',
    players_world_body: 'Международные блоки, интеграция с vanilla Alliance, физические саммиты правителей и политические последствия войн.',
    players_map_body: 'Отдельные режимы карты для партий, идеологий и политического напряжения без тяжёлой симуляции каждого жителя каждый кадр.',
    creators_kicker: 'Разработчикам',
    creators_title: 'Начни с малого, а потом расширяй',
    creator_start_title: 'Сделай первый аддон',
    creator_start_body: 'Возьми шаблон и быстрый старт. Для первого проекта достаточно одного события или одной идеологии.',
    creator_start_link: 'Первый аддон →',
    creator_api_title: 'Работай через Public API',
    creator_api_body: 'Используй PoliticalWorldAPI вместо зависимости от внутренних Main, ScenarioBridge и других деталей реализации.',
    creator_request_title: 'Проси недостающие возможности',
    creator_request_body: 'Если API не умеет что-то полезное, лучше запросить новую возможность, чем делать приватный обходной путь.',
    creator_request_link: 'Открыть Discussions →',
    community_kicker: 'Сообщество',
    community_title: 'Место, где могут расти аддоны',
    community_catalog: 'Каталог проверенных аддонов',
    community_empty_title: 'Проверенных аддонов сообщества пока нет',
    community_empty_body: 'И это нормально. Каталог специально модерируется: аддон появляется в нём после проверки на поддерживаемой версии Political World.',
    community_submit: 'Отправить аддон на проверку',
    community_view: 'Открыть каталог аддонов',
    community_testing_body: 'Проверка совместимости перед тем, как аддон попадёт в публичный каталог.',
    community_requests_title: 'Идеи и запросы к API',
    community_requests_body: 'Лучше рассказать, чего не хватает фреймворку, чем потом обходить Public API.',
    community_help_title: 'Помощь с моддингом',
    community_help_body: 'Вопросы, эксперименты и маленькие проекты тоже приветствуются.',
    direction_kicker: 'Направление разработки',
    direction_title: 'Мод не заброшен — сейчас важнее фундамент',
    direction_body: 'Какое-то время крупных игровых обновлений может быть меньше, потому что основной упор идёт на Public API. Цель — дать другим авторам возможность делать свои системы и аддоны, не дожидаясь, пока каждая идея появится в основном моде.',
    direction_link: 'Прочитать полное видение фреймворка →',
    roadmap_kicker: 'Планы',
    roadmap_title: 'От политического API к общей платформе',
    roadmap_done: 'Текущая стабильная база',
    roadmap_next: 'Следующий этап',
    roadmap_future: 'Позже',
    roadmap_19: 'Creator-функции, fallback-локализация, данные аддонов, Conditions / Effects, диагностика и оптимизация.',
    roadmap_110: 'Универсальные данные и теги для объектов мира, безопасная работа с Actor / City, свои события, расширяемые Conditions / Effects, generic content и более удобная связь между аддонами.',
    roadmap_ui_title: 'UI и интеграция экосистемы',
    roadmap_ui: 'Разделы инспектора, контекстные действия, интеграция creator tools, lifecycle-помощники и новые способы связи аддонов между собой.',
    directory_kicker: 'Навигация',
    directory_title: 'Всё важное в одном месте',
    directory_project: 'Проект',
    directory_docs: 'Документация',
    directory_build: 'Разработка',
    directory_community: 'Сообщество',
    directory_requests: 'Помощь / Идеи / Запросы к API',
    footer_tagline: 'Мод про политику • Public API • платформа для аддонов'
  }
};

function openMenu() {
  menu.classList.add('open');
  backdrop.classList.add('show');
  document.body.classList.add('menu-open');
  menuToggle.setAttribute('aria-expanded', 'true');
  menu.setAttribute('aria-hidden', 'false');
}

function closeMenu() {
  menu.classList.remove('open');
  backdrop.classList.remove('show');
  document.body.classList.remove('menu-open');
  menuToggle.setAttribute('aria-expanded', 'false');
  menu.setAttribute('aria-hidden', 'true');
}

menuToggle?.addEventListener('click', () => menu.classList.contains('open') ? closeMenu() : openMenu());
menuClose?.addEventListener('click', closeMenu);
backdrop?.addEventListener('click', closeMenu);
menu?.querySelectorAll('a').forEach(a => a.addEventListener('click', closeMenu));
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeMenu(); });

function setLanguage(lang) {
  document.documentElement.lang = lang;
  document.documentElement.dataset.lang = lang;
  localStorage.setItem('pw-lang', lang);

  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.dataset.i18n;
    const value = translations[lang]?.[key];
    if (value) el.textContent = value;
  });

  document.querySelectorAll('[data-lang-link]').forEach(el => {
    const href = el.getAttribute('href');
    if (!href) return;
    if (lang === 'ru') el.setAttribute('href', href.replace(/^en\//, 'ru/'));
    else el.setAttribute('href', href.replace(/^ru\//, 'en/'));
    el.dataset.langLink = lang;
  });

  langToggle.textContent = lang === 'en' ? 'RU' : 'EN';
  langToggle.setAttribute('aria-label', lang === 'en' ? 'Переключить на русский' : 'Switch to English');
}

langToggle?.addEventListener('click', () => {
  const current = document.documentElement.dataset.lang || 'en';
  setLanguage(current === 'en' ? 'ru' : 'en');
});

const preferred = localStorage.getItem('pw-lang');
const browserLang = navigator.language?.toLowerCase().startsWith('ru') ? 'ru' : 'en';
setLanguage(preferred || browserLang);
