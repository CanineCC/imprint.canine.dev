// <assure-progress-card app="your-project" band="Strong" band-key="healthy" score="80" series="61,64,66,70,73,77,80" trend-note="up 19 over 7 scans" lenses='[...]' done="280" total="312" items='[...]' facts='[...]'>
// Assure's example result in the same shape as Watchdog's published-survey card: the project, its
// band and the Example tag, the score out of 100 on the banded ladder, the trend as a line and a
// sentence, the score of each area, then what is Assure's own: how much the builder's AI has fixed,
// the recent fixes, and the facts of the last and next scan.
// Rendered into the element's LIGHT DOM with the site's own survey-card classes (.ip-survey,
// .ip-cai-ladder, .ip-lens-table …), so it reads exactly like the Watchdog card. The bands are
// redrawn in Assure's accent by setting the --band-* tokens on this element only; the band is said
// in words by the chip, so no state rests on colour alone. Every prop is set as text, never as markup.

const STYLE_ID = 'assure-progress-card-style';
const STYLE = `
  assure-progress-card {
    display: block;
    --band-critical: light-dark(#f9dcea, #4a1a33);
    --band-poor: light-dark(#f2b7d3, #7a2a55);
    --band-fair: light-dark(#e88bb9, #b0457e);
    --band-healthy: light-dark(#dc4f97, #e868a8);
    --band-exemplary: light-dark(#b8106a, #ff4fa3);
    --band-critical-text: light-dark(#b8106a, #ff4fa3);
    --band-poor-text: light-dark(#b8106a, #ff4fa3);
    --band-fair-text: light-dark(#b8106a, #ff4fa3);
    --band-healthy-text: light-dark(#b8106a, #ff4fa3);
    --band-exemplary-text: light-dark(#b8106a, #ff4fa3);
  }
  assure-progress-card .ip-survey-detail { max-width: none; gap: 0.55rem; }
  assure-progress-card .assure-tag {
    display: inline-block; margin-top: 0.2rem; font-family: var(--font-mono); font-size: var(--fs-2xs);
    font-weight: 500; letter-spacing: 0.08em; text-transform: uppercase; color: var(--muted);
    border: 1px solid var(--hairline); border-radius: var(--r-full); padding: 0.05rem 0.5rem;
  }
  assure-progress-card .assure-block { margin: 0.5rem 0 0; padding-top: 0.75rem; border-top: 1px solid var(--hairline); }
  assure-progress-card .assure-work-head { margin: 0 0 0.4rem; font-size: var(--fs-sm); color: var(--muted); }
  assure-progress-card .assure-work-head strong { color: var(--heading); font-variant-numeric: tabular-nums; }
  assure-progress-card .assure-wins { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.4rem; font-size: var(--fs-sm); }
  assure-progress-card .assure-win { display: flex; align-items: flex-start; gap: 0.55rem; color: var(--heading); line-height: 1.4; }
  assure-progress-card .assure-win::before {
    content: ""; flex: none; width: 0.7rem; height: 0.7rem; margin-top: 0.25rem; border-radius: 50%;
    border: 2px solid var(--heading); box-sizing: border-box;
  }
  assure-progress-card .assure-win.done::before { background: var(--accent); border-color: var(--accent); }
  assure-progress-card .assure-win.open { color: var(--muted); }
`;

const BAND_KEYS = ['critical', 'poor', 'fair', 'healthy', 'exemplary'];

const number = (value) => {
  const n = Number.parseFloat(value ?? '');
  return Number.isNaN(n) || n < 0 ? 0 : n;
};

const parseList = (value) => {
  try {
    const items = JSON.parse(value ?? '[]');
    return Array.isArray(items) ? items.filter((i) => i && typeof i === 'object') : [];
  } catch {
    return [];
  }
};

const el = (tag, className, text) => {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
};

const sparkline = (series) => {
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg');
  svg.setAttribute('viewBox', '0 0 100 32');
  svg.setAttribute('preserveAspectRatio', 'none');
  svg.setAttribute('aria-hidden', 'true');
  svg.setAttribute('focusable', 'false');
  const min = Math.min(...series);
  const span = Math.max(...series) - min || 1;
  const points = series.map((v, i) => {
    const x = series.length === 1 ? 100 : (i / (series.length - 1)) * 100;
    const y = 30 - ((v - min) / span) * 28;
    return `${Math.round(x * 100) / 100},${Math.round(y * 100) / 100}`;
  });
  const line = document.createElementNS(ns, 'polyline');
  line.setAttribute('points', points.join(' '));
  line.setAttribute('fill', 'none');
  line.setAttribute('stroke', 'currentColor');
  line.setAttribute('stroke-width', '2');
  line.setAttribute('stroke-linecap', 'round');
  line.setAttribute('stroke-linejoin', 'round');
  line.setAttribute('vector-effect', 'non-scaling-stroke');
  svg.append(line);
  return svg;
};

const table = (className, caption, rows) => {
  const t = el('table', `ip-price-table ${className}`);
  t.append(el('caption', 'sr-only', caption));
  const body = el('tbody');
  rows.forEach((row) => body.append(row));
  t.append(body);
  return t;
};

customElements.define('assure-progress-card', class extends HTMLElement {
  static observedAttributes = ['app', 'example-label', 'band', 'band-key', 'score', 'floors', 'series', 'trend-note',
    'lenses', 'done', 'total', 'done-text', 'items', 'facts'];

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
    const text = (name) => this.getAttribute(name) ?? '';
    const app = text('app') || 'your-project';
    const label = text('example-label') || 'Example';
    const bandKey = BAND_KEYS.includes(text('band-key')) ? text('band-key') : 'healthy';
    const score = number(text('score'));
    const shown = String(Math.round(score));

    const card = el('div', 'ip-stack ip-survey ip-survey-detail');
    card.setAttribute('role', 'group');
    card.setAttribute('aria-label', `${label}: a result for ${app}`);

    // The project, the Example tag and the band.
    const top = el('div', 'ip-survey-top');
    const id = el('h3', 'ip-survey-id');
    id.append(el('span', 'ip-survey-repo', app), el('span', 'assure-tag', label));
    top.append(id);
    if (text('band')) top.append(el('span', `ip-chip ip-chip-${bandKey}`, text('band')));
    card.append(top);

    // The score out of 100 on the banded ladder.
    const cai = el('p', 'ip-cai');
    cai.append(el('span', 'ip-cai-kicker', 'CAI'), el('span', `ip-cai-score ink-${bandKey}`, shown), el('span', 'ip-cai-unit', '/ 100'));
    card.append(cai);
    const floors = (text('floors') || '0,25,50,70,90').split(',').map(number);
    if (floors.length === BAND_KEYS.length) {
      const ladder = el('div', 'ip-cai-ladder');
      ladder.setAttribute('role', 'img');
      ladder.setAttribute('aria-label', `${shown} out of 100`);
      floors.forEach((floor, i) => {
        const rung = el('span', `ip-cai-rung ip-cai-rung-${BAND_KEYS[i]}`);
        rung.style.flex = String((i + 1 < floors.length ? floors[i + 1] : 100) - floor);
        ladder.append(rung);
      });
      const pin = el('span', 'ip-cai-pin');
      pin.style.left = `${Math.min(score, 100)}%`;
      ladder.append(pin);
      card.append(ladder);
    }

    // The trend as a line and a sentence.
    const series = text('series').split(',').map((v) => v.trim()).filter(Boolean).map(number);
    if (series.length > 1) {
      const spark = el('div', `ip-cai-spark ink-${bandKey}`);
      spark.append(sparkline(series));
      const trend = el('p', 'ip-trend');
      const arrow = el('span', 'ip-trend-arrow', '→');
      arrow.setAttribute('aria-hidden', 'true');
      trend.append(
        el('span', 'ip-trend-from', String(Math.round(series[0]))),
        arrow,
        el('span', `ip-trend-to ink-${bandKey}`, shown),
        el('span', 'ip-trend-note', text('trend-note')),
      );
      card.append(spark, trend);
    }

    // The score of each area.
    const lenses = parseList(text('lenses')).filter((l) => typeof l.label === 'string');
    if (lenses.length) {
      card.append(table('ip-lens-table', `${app}: score by area`, lenses.map((lens) => {
        const key = BAND_KEYS.includes(lens.band) ? lens.band : bandKey;
        const value = number(lens.value);
        const row = el('tr');
        const th = el('th', '', lens.label);
        th.scope = 'row';
        const barCell = el('td', 'ip-lens-barcell');
        const bar = el('span', 'ip-lens-bar');
        const fill = el('span', `ip-lens-fill fill-${key}`);
        fill.style.width = `${Math.min(value, 100)}%`;
        bar.append(fill);
        barCell.append(bar);
        row.append(th, barCell, el('td', `ip-lens-value ink-${key}`, String(Math.round(value))));
        return row;
      })));
    }

    // What the builder's AI has fixed, with the recent fixes under it.
    const total = number(text('total'));
    const done = Math.min(number(text('done')), total);
    const items = parseList(text('items')).filter((i) => typeof i.text === 'string');
    if (total > 0 || items.length) {
      const work = el('div', 'assure-block');
      if (total > 0) {
        const head = el('p', 'assure-work-head');
        head.append(el('strong', '', `${done} of ${total}`), ` ${text('done-text') || 'fixed by your AI'}`);
        const bar = el('span', 'ip-lens-bar');
        bar.setAttribute('role', 'progressbar');
        bar.setAttribute('aria-valuemin', '0');
        bar.setAttribute('aria-valuemax', String(total));
        bar.setAttribute('aria-valuenow', String(done));
        bar.setAttribute('aria-label', `${done} of ${total} ${text('done-text') || 'fixed by your AI'}`);
        const fill = el('span', 'ip-lens-fill');
        fill.style.width = `${(done / total) * 100}%`;
        bar.append(fill);
        work.append(head, bar);
      }
      if (items.length) {
        const list = el('ul', 'assure-wins');
        if (total > 0) list.style.marginTop = '0.75rem';
        items.forEach((item) => {
          const li = el('li', item.done ? 'assure-win done' : 'assure-win open', item.text);
          if (item.done) li.append(el('span', 'sr-only', ', done'));
          list.append(li);
        });
        work.append(list);
      }
      card.append(work);
    }

    // The facts of the last and the next scan.
    const facts = parseList(text('facts')).filter((f) => typeof f.label === 'string' && typeof f.value === 'string');
    if (facts.length) {
      card.append(table('ip-fact-table', `${app}: about the scans`, facts.map((fact) => {
        const row = el('tr');
        const th = el('th', '', fact.label);
        th.scope = 'row';
        row.append(th, el('td', '', fact.value));
        return row;
      })));
    }

    this.replaceChildren(card);
  }
});
