// Theme override — inlined into <head> BEFORE the stylesheet so the explicit choice
// applies at first paint (no flash). Without a stored choice the site simply follows
// prefers-color-scheme via CSS light-dark(); this script only handles the override.
(() => {
  try {
    const stored = localStorage.getItem('imprint-theme');
    if (stored === 'light' || stored === 'dark') {
      document.documentElement.dataset.theme = stored;
    }
  } catch { /* storage disabled: follow the system, which is the default anyway */ }
  addEventListener('DOMContentLoaded', () => {
    const button = document.querySelector('[data-theme-toggle]');
    if (!button) {
      return;
    }
    button.addEventListener('click', () => {
      const current = document.documentElement.dataset.theme
        || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
      const next = current === 'dark' ? 'light' : 'dark';
      document.documentElement.dataset.theme = next;
      try { localStorage.setItem('imprint-theme', next); } catch { /* best effort */ }
    });
  });
})();
