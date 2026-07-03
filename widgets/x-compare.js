// <x-compare before="…/a.webp" after="…/b.webp" start="50" before-label="Before" after-label="After">
// Pointer- and keyboard-operable image comparison slider. Dependency-free island.

const template = `
  <style>
    :host { display: block; font-family: var(--ip-font-body, system-ui, sans-serif); }
    .frame { position: relative; overflow: hidden; border-radius: var(--ip-radius, 8px); aspect-ratio: inherit; touch-action: none; }
    img { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; display: block; }
    .after-wrap { position: absolute; inset: 0; overflow: hidden; }
    .divider {
      position: absolute; top: 0; bottom: 0; width: 2px; background: var(--ip-background, #fff);
      box-shadow: 0 0 0 1px rgb(0 0 0 / .2); transform: translateX(-1px);
    }
    .handle {
      position: absolute; top: 50%; left: 50%; translate: -50% -50%;
      width: 2.25rem; height: 2.25rem; border-radius: 50%;
      background: var(--ip-background, #fff); color: var(--ip-text, #111);
      display: grid; place-items: center; box-shadow: 0 1px 6px rgb(0 0 0 / .3);
      font-size: .9rem; user-select: none;
    }
    .tag {
      position: absolute; bottom: .5rem; padding: .15rem .5rem; border-radius: 999px;
      background: rgb(0 0 0 / .55); color: #fff; font-size: var(--ip-text-sm, .8rem);
    }
    .tag.before { left: .5rem; } .tag.after { right: .5rem; }
    input[type=range] { position: absolute; inset: 0; opacity: 0; width: 100%; height: 100%; cursor: ew-resize; margin: 0; }
    input[type=range]:focus-visible ~ .divider .handle { outline: 2px solid var(--ip-primary, #36c); outline-offset: 2px; }
  </style>
  <div class="frame" part="frame">
    <img class="before" alt="" />
    <div class="after-wrap"><img class="after" alt="" /></div>
    <input type="range" min="0" max="100" step="1" />
    <div class="divider"><span class="handle" aria-hidden="true">⇔</span></div>
    <span class="tag before"></span>
    <span class="tag after"></span>
  </div>`;

customElements.define('x-compare', class extends HTMLElement {
  connectedCallback() {
    if (this.shadowRoot) {
      return;
    }
    const root = this.attachShadow({ mode: 'open' });
    root.innerHTML = template;

    const beforeLabel = this.getAttribute('before-label') || 'Before';
    const afterLabel = this.getAttribute('after-label') || 'After';
    root.querySelector('img.before').src = this.getAttribute('before') ?? '';
    root.querySelector('img.before').alt = beforeLabel;
    root.querySelector('img.after').src = this.getAttribute('after') ?? '';
    root.querySelector('img.after').alt = afterLabel;
    root.querySelector('.tag.before').textContent = beforeLabel;
    root.querySelector('.tag.after').textContent = afterLabel;

    const range = root.querySelector('input');
    range.setAttribute('aria-label', `Reveal ${afterLabel}`);
    range.value = this.#clamp(Number(this.getAttribute('start') ?? 50));

    const apply = () => {
      const pct = this.#clamp(Number(range.value));
      root.querySelector('.after-wrap').style.clipPath = `inset(0 0 0 ${pct}%)`;
      root.querySelector('.divider').style.left = `${pct}%`;
    };
    range.addEventListener('input', apply);

    // Direct pointer drags feel better than the invisible range alone on touch.
    const frame = root.querySelector('.frame');
    frame.addEventListener('pointerdown', event => {
      if (event.target === range) {
        return;
      }
      frame.setPointerCapture(event.pointerId);
      const track = move => {
        const rect = frame.getBoundingClientRect();
        range.value = this.#clamp(((move.clientX - rect.left) / rect.width) * 100);
        apply();
      };
      track(event);
      frame.addEventListener('pointermove', track);
      frame.addEventListener('pointerup', () => frame.removeEventListener('pointermove', track), { once: true });
    });

    apply();
  }

  #clamp(value) { return Number.isNaN(value) ? 50 : Math.min(100, Math.max(0, value)); }
});
