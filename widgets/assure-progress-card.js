// <assure-progress-card app="your-app" score="80" score-text="Quality score up 6 in two weeks" done="280" total="312" done-text="improvements made by your AI" done-note="while you kept building" items='[...]' next="Assure scans again on 6 November, on its own.">
// Assure's example result, told as what the builder gains: the quality score and how far it rose, how much
// their AI improved, the wins in plain words and when Assure looks again. Always carries its "Example" tag
// inside the card, so the figures can never be read as a real customer's result.
// A dependency-free island; inherits the site's design tokens (--ip-*) through the shadow root. The track,
// the divider and the card's edge use Assure's own light and dark values, which the site has no token for.
// Every prop is set as text (textContent), never as markup.

const template = `
  <style>
    :host { display: block; container-type: inline-size; font-family: var(--ip-font-body, system-ui, sans-serif); color: var(--ip-text, #121318); }
    [hidden] { display: none !important; }
    .card {
      background: var(--ip-surface, #fff);
      border: 1px solid light-dark(transparent, #2e3038);
      border-radius: 28px;
      padding: clamp(1.25rem, 6cqi, 2rem);
      box-shadow: 0 1px 2px light-dark(rgb(18 19 24 / .06), transparent), 0 12px 32px light-dark(rgb(18 19 24 / .08), transparent);
    }
    .top { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .app { font-family: var(--ip-font-mono, ui-monospace, monospace); font-size: .95rem; overflow-wrap: anywhere; }
    .tag {
      flex: none; font-family: var(--ip-font-mono, ui-monospace, monospace); font-size: .75rem;
      letter-spacing: .1em; text-transform: uppercase; color: var(--ip-text-muted, #5b5e6b);
      border: 1px solid light-dark(#d9dae2, #3a3d48); border-radius: 999px; padding: .2rem .65rem;
    }
    .score {
      margin: 1.25rem 0 0; font-family: var(--ip-font-heading, inherit); font-weight: 800;
      font-size: clamp(4rem, 26cqi, 7rem); line-height: .9; letter-spacing: -.04em;
      font-variant-numeric: tabular-nums;
    }
    .score-text { margin: .6rem 0 0; font-size: 1.15rem; font-weight: 600; }
    .progress { margin-top: 1.75rem; }
    .done-text { margin: 0; font-weight: 600; }
    .done-note { margin: .15rem 0 0; font-size: .95rem; color: var(--ip-text-muted, #5b5e6b); }
    .track { height: 12px; margin-top: .75rem; border-radius: 999px; background: light-dark(#eeeff4, #2e3038); overflow: hidden; }
    .fill { height: 100%; border-radius: 999px; background: var(--ip-primary, #d4157a); }
    .wins { list-style: none; display: grid; gap: .7rem; margin: 1.5rem 0 0; padding: 0; }
    .win { display: flex; align-items: flex-start; gap: .7rem; font-size: 1rem; line-height: 1.4; }
    .win::before {
      content: ""; flex: none; width: .8rem; height: .8rem; margin-top: .28rem; border-radius: 50%;
      border: 2px solid var(--ip-text, #121318); box-sizing: border-box;
    }
    .win.done::before { background: var(--ip-primary, #d4157a); border-color: var(--ip-primary, #d4157a); }
    .win.open { color: var(--ip-text-muted, #5b5e6b); }
    .next {
      margin: 1.5rem 0 0; padding-top: 1.25rem; border-top: 1px solid light-dark(#e4e5ec, #2e3038);
      font-size: .95rem; color: var(--ip-text-muted, #5b5e6b);
    }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
  </style>
  <div class="card" role="group">
    <div class="top"><span class="app"></span><span class="tag"></span></div>
    <p class="score"></p>
    <p class="score-text"></p>
    <div class="progress">
      <p class="done-text"></p>
      <p class="done-note"></p>
      <div class="track" role="progressbar" aria-valuemin="0"><div class="fill"></div></div>
    </div>
    <ul class="wins"></ul>
    <p class="next"></p>
  </div>`;

const number = (value) => {
  const n = Number.parseInt(value ?? '', 10);
  return Number.isNaN(n) || n < 0 ? 0 : n;
};

const parseItems = (value) => {
  try {
    const items = JSON.parse(value ?? '[]');
    return Array.isArray(items) ? items.filter((i) => i && typeof i.text === 'string') : [];
  } catch {
    return [];
  }
};

customElements.define('assure-progress-card', class extends HTMLElement {
  static observedAttributes = ['app', 'example-label', 'score', 'score-text', 'done', 'total', 'done-text', 'done-note', 'items', 'next'];

  connectedCallback() {
    if (!this.shadowRoot) {
      this.attachShadow({ mode: 'open' }).innerHTML = template;
    }
    this.#render();
  }

  attributeChangedCallback() {
    if (this.shadowRoot) this.#render();
  }

  #render() {
    const root = this.shadowRoot;
    const $ = (selector) => root.querySelector(selector);
    const text = (name) => this.getAttribute(name) ?? '';
    const show = (selector, value) => {
      const el = $(selector);
      el.textContent = value;
      el.hidden = value === '';
    };
    const app = text('app') || 'your-app';
    const label = text('example-label') || 'Example';
    const total = number(text('total'));
    const done = Math.min(number(text('done')), total);
    const doneText = text('done-text') || 'improvements made by your AI';

    $('.card').setAttribute('aria-label', `${label}: a result for ${app}`);
    $('.app').textContent = app;
    $('.tag').textContent = label;
    show('.score', text('score'));
    show('.score-text', text('score-text'));
    $('.done-text').textContent = `${done} ${doneText}`;
    show('.done-note', text('done-note'));

    const track = $('.track');
    track.setAttribute('aria-valuemax', String(total));
    track.setAttribute('aria-valuenow', String(done));
    track.setAttribute('aria-label', `${done} of ${total} ${doneText}`);
    $('.fill').style.width = total === 0 ? '0%' : `${(done / total) * 100}%`;

    const wins = $('.wins');
    wins.replaceChildren(...parseItems(text('items')).map((item) => {
      const li = document.createElement('li');
      li.className = item.done ? 'win done' : 'win open';
      li.textContent = item.text;
      if (item.done) {
        const state = document.createElement('span');
        state.className = 'sr';
        state.textContent = ', done';
        li.append(state);
      }
      return li;
    }));
    wins.hidden = wins.childElementCount === 0;

    show('.next', text('next'));
  }
});
