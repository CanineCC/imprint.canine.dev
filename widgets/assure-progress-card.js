// <assure-progress-card app="your-app" count="312" done="280" since="since 9 October" items='[...]' score="80" score-change="up 6 since your last scan">
// Assure's example result: how many things the builder's AI can improve, how many are done, the
// items as chips (struck through when done) and the score with its change. Always carries its
// "Example" tag inside the card, so the figures can never be read as a real customer's result.
// A dependency-free island; inherits the site's design tokens (--ip-*) through the shadow root.
// Every prop is set as text (textContent), never as markup.

const template = `
  <style>
    :host { display: block; container-type: inline-size; font-family: var(--ip-font-body, system-ui, sans-serif); color: var(--ip-text, #121318); }
    .card {
      background: var(--ip-surface, #fff);
      border: 1px solid var(--ip-border, #e4e5ec);
      border-radius: 28px;
      padding: clamp(1.25rem, 6cqi, 2rem);
      box-shadow: 0 1px 2px rgb(18 19 24 / .04), 0 16px 40px rgb(18 19 24 / .08);
    }
    .top { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .app { font-family: var(--ip-font-mono, ui-monospace, monospace); font-size: .95rem; overflow-wrap: anywhere; }
    .tag {
      flex: none; font-family: var(--ip-font-mono, ui-monospace, monospace); font-size: .72rem;
      letter-spacing: .12em; text-transform: uppercase; color: var(--ip-text-muted, #5b5e6b);
      border: 1px solid var(--ip-border, #e4e5ec); border-radius: 999px; padding: .25rem .65rem;
    }
    .count {
      margin: 1.25rem 0 .25rem; font-family: var(--ip-font-heading, inherit); font-weight: 800;
      font-size: clamp(3.5rem, 20cqi, 6.5rem); line-height: .95; letter-spacing: -.04em;
      font-variant-numeric: tabular-nums;
    }
    .count-text { margin: 0; font-size: 1.15rem; }
    .progress-head { display: flex; justify-content: space-between; gap: 1rem; margin-top: 1.75rem; font-size: .95rem; }
    .done-text { font-weight: 600; }
    .since { color: var(--ip-text-muted, #5b5e6b); text-align: end; }
    .track { height: 12px; margin-top: .5rem; border-radius: 999px; background: var(--ip-surface-alt, #eeeff4); overflow: hidden; }
    .fill { height: 100%; border-radius: 999px; background: var(--ip-primary, #d4157a); }
    .chips { list-style: none; display: flex; flex-wrap: wrap; gap: .5rem; margin: 1.25rem 0 0; padding: 0; }
    .chip {
      display: inline-flex; align-items: center; gap: .45rem; padding: .4rem .8rem; border-radius: 999px;
      font-size: .9rem; background: var(--ip-primary-wash, #fce4f0); color: var(--ip-text, #121318);
    }
    .chip::before { content: ""; flex: none; width: .55rem; height: .55rem; border-radius: 50%; border: 1.5px solid currentColor; box-sizing: border-box; }
    .chip.done { background: var(--ip-surface-alt, #f2f2f6); color: var(--ip-text-muted, #5b5e6b); text-decoration: line-through; }
    .chip.done::before { background: var(--ip-primary, #d4157a); border-color: var(--ip-primary, #d4157a); }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
    .score { margin: 1.25rem 0 0; font-size: .95rem; color: var(--ip-text-muted, #5b5e6b); }
    .score b { color: var(--ip-text, #121318); font-weight: 700; }
  </style>
  <div class="card" role="group">
    <div class="top"><span class="app"></span><span class="tag"></span></div>
    <p class="count"></p>
    <p class="count-text"></p>
    <div class="progress-head"><span class="done-text"></span><span class="since"></span></div>
    <div class="track" role="progressbar" aria-valuemin="0"><div class="fill"></div></div>
    <ul class="chips"></ul>
    <p class="score" hidden>Score <b></b><span class="change"></span></p>
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
  static observedAttributes = ['app', 'example-label', 'count', 'count-text', 'done', 'done-label', 'since', 'items', 'score', 'score-change'];

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
    const app = this.getAttribute('app') || 'your-app';
    const label = this.getAttribute('example-label') || 'Example';
    const count = number(this.getAttribute('count'));
    const done = Math.min(number(this.getAttribute('done')), count);
    const doneText = `${done} ${this.getAttribute('done-label') || 'done'}`;

    $('.card').setAttribute('aria-label', `${label}: a result for ${app}`);
    $('.app').textContent = app;
    $('.tag').textContent = label;
    $('.count').textContent = String(count);
    $('.count-text').textContent = this.getAttribute('count-text') || 'things your AI can improve';
    $('.done-text').textContent = doneText;
    $('.since').textContent = this.getAttribute('since') ?? '';

    const track = $('.track');
    track.setAttribute('aria-valuemax', String(count));
    track.setAttribute('aria-valuenow', String(done));
    track.setAttribute('aria-label', `${doneText} of ${count}`);
    $('.fill').style.width = count === 0 ? '0%' : `${(done / count) * 100}%`;

    const chips = $('.chips');
    chips.replaceChildren(...parseItems(this.getAttribute('items')).map((item) => {
      const li = document.createElement('li');
      li.className = item.done ? 'chip done' : 'chip';
      li.textContent = item.text;
      if (item.done) {
        const state = document.createElement('span');
        state.className = 'sr';
        state.textContent = ', done';
        li.append(state);
      }
      return li;
    }));
    chips.hidden = chips.childElementCount === 0;

    const score = this.getAttribute('score') ?? '';
    const change = this.getAttribute('score-change') ?? '';
    $('.score').hidden = score === '';
    $('.score b').textContent = score;
    $('.score .change').textContent = change ? `, ${change}` : '';
  }
});
