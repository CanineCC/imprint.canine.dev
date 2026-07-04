// <x-theme-toggle variant="sun-moon" size="medium" speed="smooth" label="">
// A dependency-free island: a light/dark switch a visitor clicks. It drives the SAME
// mechanism as the built-in header toggle and the first-paint <head> script —
// document.documentElement.dataset.theme + the 'imprint-theme' localStorage key — so a
// choice made here applies everywhere, survives reload, and every toggle on the page
// stays in visual sync (a MutationObserver on <html data-theme> reflects the current
// state no matter which control changed it). Inherits the site's --ip-* design tokens
// because CSS custom properties pierce shadow roots by inheritance.

const STORAGE_KEY = 'imprint-theme';

// The four dials, each defensively clamped to a known value so an unexpected attribute
// (or none) always renders something sensible rather than breaking.
const VARIANTS = new Set(['sun-moon', 'switch', 'circle']);
const SIZES = { small: '0.85rem', medium: '1rem', large: '1.35rem' };
const SPEEDS = { instant: '0ms', smooth: '200ms', playful: '450ms' };

const template = `
  <style>
    :host { display: inline-block; font-family: var(--ip-font-body, system-ui, sans-serif); }
    .toggle {
      --dur: 200ms;
      display: inline-flex; align-items: center; gap: .5em;
      font: inherit; font-size: var(--sz, 1rem); line-height: 1;
      padding: .4em .7em; border-radius: 999px; cursor: pointer;
      color: var(--ip-text, currentColor);
      background: var(--ip-surface-2, color-mix(in srgb, currentColor 8%, transparent));
      border: 1px solid var(--ip-border, color-mix(in srgb, currentColor 20%, transparent));
      transition: background var(--dur) ease, color var(--dur) ease, box-shadow var(--dur) ease;
    }
    .toggle:hover { background: var(--ip-surface-3, color-mix(in srgb, currentColor 14%, transparent)); }
    .toggle:focus-visible { outline: 2px solid var(--ip-primary, currentColor); outline-offset: 2px; }
    .label { font-size: .9em; }

    /* sun-moon + circle share a glyph that swaps/rotates on theme change. */
    .glyph { display: inline-grid; place-items: center; width: 1.4em; height: 1.4em; position: relative; }
    .glyph svg { width: 100%; height: 100%; grid-area: 1 / 1;
      transition: opacity var(--dur) ease, transform var(--dur) ease; }
    .sun { opacity: 1; transform: rotate(0) scale(1); }
    .moon { opacity: 0; transform: rotate(-90deg) scale(.6); }
    :host([data-on='dark']) .sun { opacity: 0; transform: rotate(90deg) scale(.6); }
    :host([data-on='dark']) .moon { opacity: 1; transform: rotate(0) scale(1); }

    /* circle variant: one glyph that rotates 180° between modes. */
    :host([data-variant='circle']) .moon { display: none; }
    :host([data-variant='circle']) .sun { transition: transform var(--dur) ease; }
    :host([data-variant='circle'][data-on='dark']) .sun { opacity: 1; transform: rotate(180deg); }

    /* switch variant: a sliding pill instead of an icon. */
    .switch { display: none; width: 2.4em; height: 1.3em; border-radius: 999px; position: relative;
      background: var(--ip-border, color-mix(in srgb, currentColor 25%, transparent)); transition: background var(--dur) ease; }
    .switch::after { content: ''; position: absolute; top: 50%; left: .18em; width: 1em; height: 1em;
      border-radius: 50%; transform: translateY(-50%); background: var(--ip-surface, #fff);
      transition: transform var(--dur) ease; box-shadow: 0 1px 3px rgba(0,0,0,.3); }
    :host([data-variant='switch']) .glyph { display: none; }
    :host([data-variant='switch']) .switch { display: inline-block; }
    :host([data-variant='switch'][data-on='dark']) .switch { background: var(--ip-primary, currentColor); }
    :host([data-variant='switch'][data-on='dark']) .switch::after { transform: translate(1.1em, -50%); }

    @media (prefers-reduced-motion: reduce) { .toggle, .glyph svg, .switch, .switch::after { transition-duration: 0ms; } }
  </style>
  <button class="toggle" type="button" role="switch">
    <span class="glyph" aria-hidden="true">
      <svg class="sun" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
        <circle cx="12" cy="12" r="4"></circle>
        <path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M19.1 4.9l-1.4 1.4M6.3 17.7l-1.4 1.4"></path>
      </svg>
      <svg class="moon" viewBox="0 0 24 24" fill="currentColor" stroke="none">
        <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"></path>
      </svg>
    </span>
    <span class="switch" aria-hidden="true"></span>
    <span class="label"></span>
  </button>`;

function currentTheme() {
  return document.documentElement.dataset.theme
    || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
}

customElements.define('x-theme-toggle', class extends HTMLElement {
  #observer;

  connectedCallback() {
    if (!this.shadowRoot) {
      this.attachShadow({ mode: 'open' }).innerHTML = template;
      this.shadowRoot.querySelector('.toggle').addEventListener('click', () => this.#flip());
    }
    this.#render();

    // Reflect changes made by ANY control (this widget, another instance, the header
    // button, the first-paint script) so every toggle on the page shows one truth.
    this.#observer = new MutationObserver(() => this.#reflect());
    this.#observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
  }

  disconnectedCallback() { this.#observer?.disconnect(); }

  #flip() {
    const next = currentTheme() === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;      // the MutationObserver calls #reflect
    try { localStorage.setItem(STORAGE_KEY, next); } catch { /* best effort */ }
  }

  #render() {
    const variant = VARIANTS.has(this.getAttribute('variant')) ? this.getAttribute('variant') : 'sun-moon';
    const size = SIZES[this.getAttribute('size')] ?? SIZES.medium;
    const speed = SPEEDS[this.getAttribute('speed')] ?? SPEEDS.smooth;
    const label = this.getAttribute('label') ?? '';

    this.dataset.variant = variant;
    const button = this.shadowRoot.querySelector('.toggle');
    button.style.setProperty('--sz', size);
    button.style.setProperty('--dur', speed);
    const labelEl = this.shadowRoot.querySelector('.label');
    labelEl.textContent = label;          // textContent, never innerHTML — no injection
    labelEl.hidden = label === '';
    this.#reflect();
  }

  #reflect() {
    const theme = currentTheme();
    this.dataset.on = theme;
    const button = this.shadowRoot.querySelector('.toggle');
    button.setAttribute('aria-checked', String(theme === 'dark'));
    const label = this.getAttribute('label');
    button.setAttribute('aria-label', label && label.length > 0
      ? label
      : theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
  }

  static get observedAttributes() { return ['variant', 'size', 'speed', 'label']; }
  attributeChangedCallback() { if (this.shadowRoot) { this.#render(); } }
});
