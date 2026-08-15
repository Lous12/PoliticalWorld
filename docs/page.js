const menuToggle = document.getElementById('menuToggle');
const menuClose = document.getElementById('menuClose');
const menu = document.getElementById('mobileMenu');
const backdrop = document.getElementById('menuBackdrop');

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
