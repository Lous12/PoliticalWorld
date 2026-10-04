const menuToggle = document.getElementById('menuToggle');
const menuClose = document.getElementById('menuClose');
const menu = document.getElementById('mobileMenu');
const backdrop = document.getElementById('menuBackdrop');
const langToggle = document.getElementById('langToggle');

const translations = {
  en: {
    brand_sub: 'Politics • API • Open development',
    nav_status: 'Status',
    nav_players: 'Players',
    nav_creators: 'Creators',
    nav_community: 'Community',
    nav_development: 'Development',
    nav_support: 'Support',
    menu_all: 'Everything important in one place',
    menu_project: 'Project',
    menu_build: 'Build',
    menu_contribute: 'Contribute',
    menu_support: 'Support',
    menu_get_started: 'Getting Started',
    menu_examples: 'Examples',
    menu_templates: 'Templates',
    menu_addons: 'Community Addons',
    hero_eyebrow: 'WorldBox politics mod • Public API • Open development',
    hero_title: 'Political World 1.11',
    hero_lead: 'A politics mod for WorldBox with ideologies, parties, governments, crises, international blocs and a public API for addons.',
    hero_steam: 'Open on Steam',
    hero_github: 'Open GitHub',
    status_kicker: 'Current status',
    status_title: 'What the repository targets now',
    status_maintenance: 'Maintenance / open development',
    status_release: 'current release source',
    status_api: 'current public API',
    status_supported: 'supported setup',
    players_kicker: 'For players',
    players_title: 'Politics that lives with the world',
    players_politics_title: 'Domestic politics',
    players_politics_body: 'Ideologies, parties, governments, elections, stability, crises, coups and revolutions.',
    players_world_title: 'International politics',
    players_world_body: 'International blocs, vanilla alliance integration, summits, diplomacy and political consequences of war.',
    players_map_body: 'Political map modes and dedicated kingdom/city Politics pages without turning the mod into a constant full-world scan.',
    creators_kicker: 'For creators',
    creators_title: 'Build on the public API, not private internals',
    creator_start_title: 'Start an addon',
    creator_start_body: 'Use the addon template and examples. Start with one visible feature and grow from there.',
    creator_start_link: 'Getting Started →',
    creator_api_title: 'Use API 1.19',
    creator_api_body: 'Events, addon data, world access, UI hooks, warfare helpers, content registration, diagnostics and more are exposed publicly.',
    creator_ai_title: 'Using an AI coding assistant?',
    creator_ai_body: 'Give it AI_START_HERE.md and AGENTS.md first so it does not invent WorldBox APIs or bypass PoliticalWorldAPI with reflection.',
    community_kicker: 'Community development',
    community_title: 'Fork it, patch it, build on it',
    community_main_title: 'Political World is open for community work',
    community_main_body: 'Forks, focused pull requests, addon experiments, documentation fixes and AI-assisted contributions are welcome. Official releases still come from Lous12.',
    community_fork: 'Browse forks',
    community_pr: 'Pull Requests',
    community_addons_title: 'Community Addons',
    community_addons_body: 'Independent addons can stay separate from core and use the same public API.',
    community_issues_title: 'Issues',
    community_issues_body: 'Good bug reports include versions, reproduction steps, Player.log and a save when it matters.',
    community_discord_body: 'Talk about PW, addons, ideas, bugs and experiments with the community.',
    community_catalog: 'Community Addons catalog',
    development_kicker: 'Development status',
    development_title: 'Active feature development is on a break',
    development_body: 'Political World is not being pushed through another giant feature cycle right now. The focus is maintenance, critical fixes, repository cleanup, documentation and making the project easier for other programmers and addon authors to understand.',
    development_chip_1: 'Critical fixes',
    development_chip_2: 'Repository cleanup',
    development_chip_3: 'Community contributions',
    support_kicker: 'Support',
    support_title: 'Support Political World',
    support_da_body: 'The easiest regular way to support the project. Completely optional.',
    support_da_button: 'Open DonationAlerts',
    support_network_title: 'Check the network before sending.',
    support_network_body: 'USDT on TRC20/TRON and USDT on TON use different networks. Send only through the network shown on the card.',
    support_note: 'Donations never unlock features and are never required to use Political World, the API, source code or documentation.',
    directory_kicker: 'Directory',
    directory_title: 'Everything important in one place',
    directory_project: 'Project',
    directory_docs: 'Documentation',
    directory_build: 'Build',
    directory_contribute: 'Contribute',
    footer_tagline: 'Politics mod • Public API • Open development',
    copy_address: 'Copy address',
    copied: 'Copied!'
  },
  ru: {
    brand_sub: 'Политика • API • открытая разработка',
    nav_status: 'Статус',
    nav_players: 'Игрокам',
    nav_creators: 'Авторам',
    nav_community: 'Сообщество',
    nav_development: 'Разработка',
    nav_support: 'Поддержать',
    menu_all: 'Всё важное в одном месте',
    menu_project: 'Проект',
    menu_build: 'Разработка',
    menu_contribute: 'Участие в разработке',
    menu_support: 'Поддержать',
    menu_get_started: 'Первый аддон',
    menu_examples: 'Примеры',
    menu_templates: 'Шаблоны',
    menu_addons: 'Аддоны сообщества',
    hero_eyebrow: 'Мод для WorldBox • Public API • открытая разработка',
    hero_title: 'Political World 1.11',
    hero_lead: 'Мод про политику для WorldBox: идеологии, партии, формы правления, кризисы, международные блоки и публичный API для аддонов.',
    hero_steam: 'Открыть в Steam',
    hero_github: 'Открыть GitHub',
    status_kicker: 'Текущий статус',
    status_title: 'На что сейчас рассчитан репозиторий',
    status_maintenance: 'Поддержка / открытая разработка',
    status_release: 'актуальные исходники релиза',
    status_api: 'актуальный Public API',
    status_supported: 'поддерживаемая сборка',
    players_kicker: 'Игрокам',
    players_title: 'Политика, которая живёт вместе с миром',
    players_politics_title: 'Внутренняя политика',
    players_politics_body: 'Идеологии, партии, формы правления, выборы, стабильность, кризисы, перевороты и революции.',
    players_world_title: 'Международная политика',
    players_world_body: 'Международные блоки, интеграция с vanilla Alliance, саммиты, дипломатия и политические последствия войн.',
    players_map_body: 'Политические режимы карты и отдельные страницы Politics для государств и поселений без постоянного полного сканирования мира.',
    creators_kicker: 'Авторам',
    creators_title: 'Стройте на Public API, а не на внутренних классах',
    creator_start_title: 'Сделать первый аддон',
    creator_start_body: 'Возьмите шаблон и примеры. Начните с одной видимой функции и расширяйте проект постепенно.',
    creator_start_link: 'Первый аддон →',
    creator_api_title: 'Использовать API 1.19',
    creator_api_body: 'События, данные аддона, доступ к миру, UI hooks, warfare helpers, регистрация контента, диагностика и другое доступны публично.',
    creator_ai_title: 'Используете ИИ для кода?',
    creator_ai_body: 'Сначала дайте ему AI_START_HERE.md и AGENTS.md, чтобы он не выдумывал API WorldBox и не лез reflection’ом мимо PoliticalWorldAPI.',
    community_kicker: 'Разработка сообщества',
    community_title: 'Форкайте, патчите, стройте сверху',
    community_main_title: 'Political World открыт для работы сообщества',
    community_main_body: 'Форки, небольшие PR, эксперименты с аддонами, исправления документации и AI-assisted contributions приветствуются. Официальные релизы по-прежнему выпускает Lous12.',
    community_fork: 'Посмотреть форки',
    community_pr: 'Pull Requests',
    community_addons_title: 'Аддоны сообщества',
    community_addons_body: 'Независимые аддоны могут оставаться отдельно от core и использовать тот же Public API.',
    community_issues_title: 'Issues',
    community_issues_body: 'Хороший баг-репорт содержит версии, шаги воспроизведения, Player.log и сейв, если он нужен.',
    community_discord_body: 'Обсуждение PW, аддонов, идей, багов и экспериментов вместе с сообществом.',
    community_catalog: 'Каталог аддонов сообщества',
    development_kicker: 'Статус разработки',
    development_title: 'Активная разработка больших функций сейчас на паузе',
    development_body: 'Political World сейчас не гонится за очередным огромным циклом новых механик. Фокус — поддержка, критические фиксы, уборка репозитория, документация и то, чтобы другим программистам и авторам аддонов было проще разобраться в проекте.',
    development_chip_1: 'Критические фиксы',
    development_chip_2: 'Уборка репозитория',
    development_chip_3: 'Вклад сообщества',
    support_kicker: 'Поддержка',
    support_title: 'Поддержать Political World',
    support_da_body: 'Самый простой обычный способ поддержать проект. Полностью добровольно.',
    support_da_button: 'Открыть DonationAlerts',
    support_network_title: 'Перед отправкой проверьте сеть.',
    support_network_body: 'USDT в TRC20/TRON и USDT в TON используют разные сети. Отправляйте только через сеть, указанную на карточке.',
    support_note: 'Донаты не открывают функции и никогда не нужны для использования Political World, API, исходников или документации.',
    directory_kicker: 'Навигация',
    directory_title: 'Всё важное в одном месте',
    directory_project: 'Проект',
    directory_docs: 'Документация',
    directory_build: 'Разработка',
    directory_contribute: 'Участие',
    footer_tagline: 'Мод про политику • Public API • открытая разработка',
    copy_address: 'Скопировать адрес',
    copied: 'Скопировано!'
  }
};

function openMenu() {
  if (!menu) return;
  menu.classList.add('open');
  backdrop?.classList.add('show');
  document.body.classList.add('menu-open');
  menuToggle?.setAttribute('aria-expanded', 'true');
  menu.setAttribute('aria-hidden', 'false');
}

function closeMenu() {
  if (!menu) return;
  menu.classList.remove('open');
  backdrop?.classList.remove('show');
  document.body.classList.remove('menu-open');
  menuToggle?.setAttribute('aria-expanded', 'false');
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
    const value = translations[lang]?.[el.dataset.i18n];
    if (value) el.textContent = value;
  });

  document.querySelectorAll('[data-lang-link]').forEach(el => {
    const marker = el.dataset.langLink || '';
    if (!marker) return;
    const localized = lang === 'ru' ? marker.replace(/^en\//, 'ru/') : marker.replace(/^ru\//, 'en/');
    if (localized.startsWith('docs/')) {
      el.href = 'https://github.com/Lous12/PoliticalWorld/blob/main/' + localized;
    } else {
      el.href = localized;
    }
  });

  document.querySelectorAll('.copy-button').forEach(button => {
    button.textContent = translations[lang]?.copy_address || 'Copy address';
  });

  if (langToggle) {
    langToggle.textContent = lang === 'en' ? 'RU' : 'EN';
    langToggle.setAttribute('aria-label', lang === 'en' ? 'Переключить на русский' : 'Switch to English');
  }
}

async function copyWallet(button) {
  const targetId = button.dataset.copyTarget;
  const target = targetId ? document.getElementById(targetId) : null;
  const value = target?.textContent?.trim();
  if (!value) return;

  try {
    await navigator.clipboard.writeText(value);
  } catch (_) {
    const textarea = document.createElement('textarea');
    textarea.value = value;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    textarea.remove();
  }

  const lang = document.documentElement.dataset.lang || 'en';
  button.textContent = translations[lang]?.copied || 'Copied!';
  window.setTimeout(() => {
    button.textContent = translations[lang]?.copy_address || 'Copy address';
  }, 1400);
}

document.querySelectorAll('.copy-button').forEach(button => {
  button.addEventListener('click', () => copyWallet(button));
});

const preferred = localStorage.getItem('pw-lang');
const browserLang = navigator.language?.toLowerCase().startsWith('ru') ? 'ru' : 'en';
setLanguage(preferred || browserLang);
