const toggle = document.getElementById('menuToggle');
const menu = document.getElementById('mobileMenu');
const backdrop = document.getElementById('menuBackdrop');

function closeMenu() {
  menu.classList.remove('open');
  backdrop.classList.remove('show');
  toggle.setAttribute('aria-expanded', 'false');
}

function openMenu() {
  menu.classList.add('open');
  backdrop.classList.add('show');
  toggle.setAttribute('aria-expanded', 'true');
}

toggle?.addEventListener('click', () => {
  if (menu.classList.contains('open')) closeMenu();
  else openMenu();
});

backdrop?.addEventListener('click', closeMenu);
menu?.querySelectorAll('a').forEach(a => a.addEventListener('click', closeMenu));
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') closeMenu();
});
