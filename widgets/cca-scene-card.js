// <cca-scene-card scene="review|fleet|product|reuse" heading="…" items='[…]' note="…">
// Small example scenes for the cards on CCA's home page, each showing what the reader gets rather than how it works:
//   review  — the queue a reviewer sees: pull requests that arrive already checked (items: {title, note}).
//   fleet   — every repository on the same 0 to 100 scale (items: {label, value, band}).
//   product — the score shown inside the customer's own product, under their name (brand, score, band, caption).
//   reuse   — a day of hourly checks: the few that scanned changed code, the rest reused at no charge (checks, scans).
// Dressed in CCA's kit: a silver panel with a 1 px edge, 4 px corners, titanium for the accent and the usage fill,
// the band said in words. Always tagged as an example inside the card. Rendered into the LIGHT DOM; every prop is
// set as text, never as markup.

const STYLE_ID = 'cca-scene-card-style';
const STYLE = `
  cca-scene-card {
    display: block; width: 100%;
    --csc-ink: light-dark(#16181d, #e8eaf0);
    --csc-ink-soft: light-dark(#3a3f4a, #c3c8d2);
    --csc-muted: light-dark(#5c6370, #9aa3b2);
    --csc-accent: light-dark(#7f5b6f, #d0b2c1);
    --csc-line: light-dark(#e3e6eb, #2c323c);
    --csc-outline: light-dark(#c9cdd4, #3a404c);
    --csc-wash: light-dark(#f8eff3, #3a2430);
    --csc-on-wash: light-dark(#503042, #ead7e0);
    --csc-panel: light-dark(#ffffff, #0f1115);
    --csc-fill-hi: light-dark(#9f7b8e, #ead8e2);
    --csc-fill: light-dark(#7f5b6f, #d0b2c1);
    --csc-fill-lo: light-dark(#5a394c, #a37f92);
    --csc-mono: var(--font-mono, ui-monospace), Menlo, Consolas, monospace;
    --csc-display: var(--font-display, system-ui), sans-serif;
    font-family: var(--font-ui, system-ui), sans-serif;
    text-align: left;
  }
  cca-scene-card .csc-card { background: var(--csc-panel); border: 1px solid var(--csc-line); border-radius: 4px; padding: 0.9rem 1rem; color: var(--csc-ink); }
  cca-scene-card .csc-head { display: flex; justify-content: space-between; align-items: baseline; gap: 0.75rem; margin-bottom: 0.6rem; }
  cca-scene-card .csc-heading { margin: 0; font-family: var(--csc-mono); font-size: 11px; font-weight: 400; letter-spacing: 0.08em; text-transform: uppercase; color: var(--csc-muted); }
  cca-scene-card .csc-tag {
    flex: none; font-family: var(--csc-mono); font-size: 10px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--csc-muted);
    border: 1px solid var(--csc-outline); border-radius: 4px; padding: 0.1rem 0.35rem;
  }
  cca-scene-card ul { list-style: none; margin: 0; padding: 0; }
  cca-scene-card .csc-row { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; padding: 0.45rem 0; font-size: 14px; }
  cca-scene-card .csc-row + .csc-row { border-top: 1px solid var(--csc-line); }
  cca-scene-card .csc-row-title { min-width: 0; color: var(--csc-ink-soft); }
  cca-scene-card .csc-pill { flex: none; background: var(--csc-wash); color: var(--csc-on-wash); font-size: 12px; font-weight: 700; border-radius: 4px; padding: 0.15rem 0.45rem; white-space: nowrap; }
  cca-scene-card .csc-bar-row { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: baseline; gap: 0.3rem 0.6rem; padding: 0.4rem 0; font-size: 14px; }
  cca-scene-card .csc-bar-row .csc-track { grid-column: 1 / -1; }
  cca-scene-card .csc-bar-label { font-family: var(--csc-mono); font-size: 12px; color: var(--csc-ink-soft); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  cca-scene-card .csc-track { display: block; height: 6px; background: var(--csc-line); border-radius: 3px; }
  cca-scene-card .csc-fill { display: block; height: 100%; border-radius: 3px; background: linear-gradient(90deg, var(--csc-fill-lo), var(--csc-fill) 60%, var(--csc-fill-hi)); }
  cca-scene-card .csc-bar-value { font-variant-numeric: tabular-nums; font-size: 13px; color: var(--csc-ink); white-space: nowrap; }
  cca-scene-card .csc-bar-value span { color: var(--csc-muted); }
  cca-scene-card .csc-app { border: 1px solid var(--csc-line); border-radius: 4px; overflow: hidden; }
  cca-scene-card .csc-app-bar { display: flex; justify-content: space-between; align-items: center; padding: 0.45rem 0.7rem; background: light-dark(#eceef2, #1f242e); font-size: 13px; font-weight: 700; color: var(--csc-ink-soft); }
  cca-scene-card .csc-app-bar span { font-weight: 400; color: var(--csc-muted); font-size: 12px; }
  cca-scene-card .csc-app-body { padding: 0.7rem; display: flex; align-items: baseline; flex-wrap: wrap; gap: 0.3rem 0.6rem; }
  cca-scene-card .csc-app-label { width: 100%; font-size: 13px; color: var(--csc-muted); }
  cca-scene-card .csc-big { font-family: var(--csc-display); font-weight: 800; font-size: 30px; line-height: 1; letter-spacing: -0.02em; font-variant-numeric: tabular-nums; }
  cca-scene-card .csc-caption { width: 100%; font-size: 12px; color: var(--csc-muted); }
  cca-scene-card .csc-cells { display: grid; grid-template-columns: repeat(12, 1fr); gap: 3px; }
  cca-scene-card .csc-cell { display: block; aspect-ratio: 1; border-radius: 2px; border: 1px solid var(--csc-outline); }
  cca-scene-card .csc-cell.is-scan { border-color: transparent; background: linear-gradient(135deg, var(--csc-fill-hi), var(--csc-fill) 50%, var(--csc-fill-lo)); }
  cca-scene-card .csc-legend { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 0.25rem 1rem; margin-top: 0.6rem; font-size: 13px; color: var(--csc-muted); }
  cca-scene-card .csc-legend strong { color: var(--csc-ink); font-weight: 700; }
  cca-scene-card .csc-note { margin: 0.6rem 0 0; font-size: 13px; color: var(--csc-muted); }
`;

const parseList = (value) => {
  try {
    const items = JSON.parse(value ?? '[]');
    return Array.isArray(items) ? items.filter((i) => i && typeof i === 'object') : [];
  } catch {
    return [];
  }
};

const number = (value) => {
  const n = Number.parseFloat(value ?? '');
  return Number.isNaN(n) || n < 0 ? 0 : n;
};

const el = (tag, className, text) => {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
};

const scenes = {
  // Pull requests that reach the reviewer already checked.
  review(card, items) {
    const list = el('ul');
    items.filter((i) => typeof i.title === 'string').forEach((i) => {
      const row = el('li', 'csc-row');
      row.append(el('span', 'csc-row-title', i.title));
      if (typeof i.note === 'string' && i.note) row.append(el('span', 'csc-pill', i.note));
      list.append(row);
    });
    card.append(list);
  },

  // Every repository on the same 0 to 100 scale.
  fleet(card, items) {
    const list = el('ul');
    items.filter((i) => typeof i.label === 'string').forEach((i) => {
      const value = Math.min(number(i.value), 100);
      const row = el('li', 'csc-bar-row');
      const track = el('span', 'csc-track');
      const fill = el('span', 'csc-fill');
      fill.style.width = `${value}%`;
      track.append(fill);
      track.setAttribute('aria-hidden', 'true');
      const figure = el('span', 'csc-bar-value', String(Math.round(value)));
      if (typeof i.band === 'string' && i.band) figure.append(el('span', '', ` ${i.band}`));
      row.append(el('span', 'csc-bar-label', i.label), figure, track);
      list.append(row);
    });
    card.append(list);
  },

  // The score inside the customer's own product, under their own name.
  product(card, _items, text) {
    const app = el('div', 'csc-app');
    const bar = el('div', 'csc-app-bar', text('brand', 'Your product'));
    if (text('section')) bar.append(el('span', '', text('section')));
    const body = el('div', 'csc-app-body');
    if (text('label')) body.append(el('span', 'csc-app-label', text('label')));
    body.append(el('span', 'csc-big', text('score', '78')));
    if (text('band')) body.append(el('span', 'csc-pill', text('band')));
    if (text('caption')) body.append(el('span', 'csc-caption', text('caption')));
    app.append(bar, body);
    card.append(app);
  },

  // A day of hourly checks: a few scanned changed code, the rest returned the earlier result at no charge.
  reuse(card, _items, text) {
    const checks = Math.max(1, Math.min(48, Math.round(number(text('checks', '24')))));
    const scans = Math.min(checks, Math.round(number(text('scans', '3'))));
    const at = new Set(Array.from({ length: scans }, (_, i) => Math.floor(((i + 0.5) * checks) / scans)));
    const cells = el('div', 'csc-cells');
    cells.setAttribute('role', 'img');
    cells.setAttribute('aria-label', `${checks} checks: ${scans} new scans charged, ${checks - scans} reused at no charge`);
    for (let i = 0; i < checks; i += 1) cells.append(el('span', at.has(i) ? 'csc-cell is-scan' : 'csc-cell'));
    const legend = el('p', 'csc-legend');
    const charged = el('span');
    charged.append(el('strong', '', String(scans)), document.createTextNode(` ${text('scans-label', 'new scans, charged')}`));
    const free = el('span');
    free.append(el('strong', '', String(checks - scans)), document.createTextNode(` ${text('reused-label', 'reused, no charge')}`));
    legend.append(charged, free);
    card.append(cells, legend);
  },
};

customElements.define('cca-scene-card', class extends HTMLElement {
  static observedAttributes = [
    'scene', 'heading', 'example-label', 'items', 'note', 'brand', 'section', 'label', 'score', 'band', 'caption',
    'checks', 'scans', 'scans-label', 'reused-label',
  ];

  connectedCallback() {
    if (!document.getElementById(STYLE_ID)) {
      const style = el('style');
      style.id = STYLE_ID;
      style.textContent = STYLE;
      document.head.append(style);
    }
    this.#render();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.#render();
  }

  #render() {
    const text = (name, fallback = '') => this.getAttribute(name) || fallback;
    const draw = scenes[text('scene', 'review')] ?? scenes.review;
    const label = text('example-label', 'Example');

    const card = el('div', 'csc-card');
    card.setAttribute('role', 'group');
    card.setAttribute('aria-label', `${label}: ${text('heading', 'an example')}`);
    const head = el('div', 'csc-head');
    head.append(el('p', 'csc-heading', text('heading')), el('span', 'csc-tag', label));
    card.append(head);
    draw(card, parseList(text('items')), text);
    if (text('note')) card.append(el('p', 'csc-note', text('note')));

    this.replaceChildren(card);
  }
});
