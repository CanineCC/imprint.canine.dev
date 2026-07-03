// <x-countdown until="2027-01-01T00:00" label="Launch" done-text="It's time!">
// A dependency-free island. Inherits the site's design tokens (--ip-*) because CSS
// custom properties pierce shadow roots by inheritance.

const template = `
  <style>
    :host { display: block; font-family: var(--ip-font-body, system-ui, sans-serif); }
    .wrap { display: flex; flex-direction: column; align-items: center; gap: .5rem; }
    .units { display: flex; gap: clamp(.5rem, 2cqi, 1.25rem); flex-wrap: wrap; justify-content: center; }
    .unit { display: flex; flex-direction: column; align-items: center; min-width: 3.5rem; }
    .value {
      font-variant-numeric: tabular-nums; font-weight: 700;
      font-size: var(--ip-text-3xl, 2.5rem); line-height: 1.1;
      color: var(--ip-primary, currentColor);
    }
    .name { font-size: var(--ip-text-sm, .875rem); color: var(--ip-text-muted, currentColor); text-transform: uppercase; letter-spacing: .06em; }
    .label { font-size: var(--ip-text-base, 1rem); color: var(--ip-text, currentColor); }
    .done { font-size: var(--ip-text-2xl, 2rem); font-weight: 700; color: var(--ip-accent, currentColor); }
  </style>
  <div class="wrap" role="timer">
    <div class="units" hidden>
      <span class="unit"><span class="value" data-u="d">0</span><span class="name">days</span></span>
      <span class="unit"><span class="value" data-u="h">0</span><span class="name">hours</span></span>
      <span class="unit"><span class="value" data-u="m">0</span><span class="name">min</span></span>
      <span class="unit"><span class="value" data-u="s">0</span><span class="name">sec</span></span>
    </div>
    <span class="done" hidden></span>
    <span class="label" hidden></span>
  </div>`;

customElements.define('x-countdown', class extends HTMLElement {
  #timer;

  connectedCallback() {
    if (!this.shadowRoot) {
      this.attachShadow({ mode: 'open' }).innerHTML = template;
    }
    const label = this.getAttribute('label') ?? '';
    const labelEl = this.shadowRoot.querySelector('.label');
    labelEl.textContent = label;
    labelEl.hidden = label === '';
    this.#tick();
    this.#timer = setInterval(() => this.#tick(), 1000);
  }

  disconnectedCallback() { clearInterval(this.#timer); }

  #tick() {
    const target = new Date(this.getAttribute('until') ?? '').getTime();
    const units = this.shadowRoot.querySelector('.units');
    const done = this.shadowRoot.querySelector('.done');
    if (Number.isNaN(target)) {
      units.hidden = true;
      done.hidden = false;
      done.textContent = 'Set a valid date';
      return;
    }
    let remaining = Math.max(0, Math.floor((target - Date.now()) / 1000));
    if (remaining === 0) {
      units.hidden = true;
      done.hidden = false;
      done.textContent = this.getAttribute('done-text') || "It's time!";
      clearInterval(this.#timer);
      return;
    }
    units.hidden = false;
    done.hidden = true;
    const parts = { d: Math.floor(remaining / 86400), h: Math.floor(remaining / 3600) % 24, m: Math.floor(remaining / 60) % 60, s: remaining % 60 };
    for (const [unit, value] of Object.entries(parts)) {
      this.shadowRoot.querySelector(`[data-u="${unit}"]`).textContent = String(value).padStart(2, '0');
    }
    const caption = this.getAttribute('label');
    this.setAttribute('aria-label',
      `${caption ? caption + ': ' : ''}${parts.d} days, ${parts.h} hours, ${parts.m} minutes and ${parts.s} seconds remaining`);
  }
});
