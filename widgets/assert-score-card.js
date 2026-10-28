// <assert-score-card repo="checkout-service" sprint="Sprint 46" band="Strong" score="78" series="64,68,71,72,74,78" trend-note="up 14 since sprint 41" areas='[...]' facts='[...]'>
// Assert's example result in the shape of Watchdog's published-survey card: the repository and its
// sprint, the Example tag, the CAI score out of 100 with its band on the ladder, the trend as a line
// and a sentence, the score of each area, then the facts of the scan (open findings by sprint, the
// work with the team's coding agents, the rebuild cost).
// Dressed in Assert's kit (v0.2): a silver card with a 1 px edge and no shadow, the score large in the
// display face, the ladder in five greys with a warm steel metal marker, the area bars in warm steel, labels and
// area figures in the mono face (the facts stay in the text face so they fit on one line). Assert has one accent and never uses colour for a warning, so the band is
// said in words on the chip. Rendered into the element's LIGHT DOM with its own classes; every prop
// is set as text, never as markup.

const STYLE_ID = 'assert-score-card-style';
const STYLE = `
  assert-score-card {
    display: block; width: 100%; max-width: 27rem;
    --asc-ink: light-dark(#1c2126, #e4e9ed);
    --asc-ink-soft: light-dark(#3d4650, #c3cbd3);
    --asc-muted: light-dark(#616b76, #8694a1);
    --asc-accent: light-dark(#56557a, #b6b6d6);
    --asc-line: light-dark(#e2e6eb, #2b323a);
    --asc-outline: light-dark(#c3cad2, #3a434d);
    --asc-wash: light-dark(#efeff9, #262635);
    --asc-on-wash: light-dark(#444367, #cbcbe5);
    --asc-silver-hi: light-dark(#ffffff, #232a31);
    --asc-silver-lo: light-dark(#f4f5f8, #1a1f25);
    --asc-silver-edge: light-dark(#c9ccd4, #3a434d);
    --asc-chart-lo: light-dark(#2d2c4c, #8b8bae);
    --asc-chart: light-dark(#56557a, #b6b6d6);
    --asc-chart-hi: light-dark(#76769d, #dfdff1);
    --asc-rung-1: light-dark(#d5dae0, #2b323a);
    --asc-rung-2: light-dark(#bfc6ce, #3a434d);
    --asc-rung-3: light-dark(#a5adb7, #555f6a);
    --asc-rung-4: light-dark(#8b95a0, #7a8591);
    --asc-rung-5: light-dark(#5f6974, #a3afbb);
    --asc-mono: "Martian Mono", var(--font-mono, ui-monospace), Menlo, Consolas, monospace;
    --asc-display: "Schibsted Grotesk", var(--font-display, system-ui), sans-serif;
    font-family: "Schibsted Grotesk", var(--font-ui, system-ui), sans-serif;
    text-align: left;
    color-scheme: light;
  }
  /* The card follows the site's theme, not the graphite top it sits on (which is dark in both). */
  @media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) assert-score-card { color-scheme: dark; } }
  :root[data-theme="dark"] assert-score-card { color-scheme: dark; }
  assert-score-card .asc-card {
    background: linear-gradient(180deg, var(--asc-silver-hi), var(--asc-silver-lo));
    border: 1px solid var(--asc-silver-edge); border-radius: 6px; padding: 1.5rem 1.6rem; color: var(--asc-ink);
  }
  assert-score-card .asc-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
  assert-score-card .asc-repo { margin: 0; font-size: 17px; font-weight: 700; line-height: 1.3; color: var(--asc-ink); }
  assert-score-card .asc-label {
    font-family: var(--asc-mono); font-size: 11px; font-weight: 400; letter-spacing: 0.08em; text-transform: uppercase; color: var(--asc-muted);
  }
  assert-score-card .asc-tag {
    flex: none; font-family: var(--asc-mono); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--asc-muted);
    border: 1px solid var(--asc-outline); border-radius: 4px; padding: 0.2rem 0.45rem;
  }
  assert-score-card .asc-score { display: flex; align-items: baseline; gap: 0.5rem; margin: 1.1rem 0 0.9rem; }
  assert-score-card .asc-number {
    font-family: var(--asc-display); font-weight: 900; font-size: 60px; line-height: 0.9; letter-spacing: -0.03em; color: var(--asc-ink);
    font-variant-numeric: tabular-nums;
  }
  assert-score-card .asc-unit { font-size: 17px; color: var(--asc-muted); }
  assert-score-card .asc-chip {
    margin-left: auto; align-self: center; background: var(--asc-wash); color: var(--asc-on-wash);
    font-size: 13px; font-weight: 700; border-radius: 4px; padding: 0.25rem 0.65rem;
  }
  assert-score-card .asc-ladder { position: relative; display: flex; gap: 2px; height: 8px; }
  assert-score-card .asc-rung { display: block; min-width: 0; border-radius: 2px; }
  assert-score-card .asc-pin {
    position: absolute; top: -3px; width: 14px; height: 14px; box-sizing: border-box; transform: translateX(-50%) rotate(45deg);
    background: linear-gradient(135deg, #a7a7ca, #56557a 50%, #2d2c4c); border: 2px solid var(--asc-silver-hi);
  }
  assert-score-card .asc-spark { margin-top: 1rem; color: var(--asc-accent); }
  assert-score-card .asc-spark svg { display: block; width: 100%; height: 40px; }
  assert-score-card .asc-trend { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; margin: 0.5rem 0 1rem; }
  assert-score-card .asc-arc { font-size: 20px; font-weight: 700; color: var(--asc-ink-soft); font-variant-numeric: tabular-nums; }
  assert-score-card .asc-arc strong { color: var(--asc-accent); }
  assert-score-card .asc-note { font-size: 14px; color: var(--asc-muted); text-align: right; }
  assert-score-card table { width: 100%; border-collapse: collapse; }
  assert-score-card .asc-areas th, assert-score-card .asc-areas td { padding: 0.28rem 0; font-size: 15px; font-weight: 400; }
  assert-score-card .asc-areas th { width: 1%; white-space: nowrap; padding-right: 0.9rem; text-align: left; color: var(--asc-ink-soft); }
  assert-score-card .asc-track { display: block; height: 6px; background: var(--asc-line); border-radius: 3px; }
  assert-score-card .asc-fill {
    display: block; height: 100%; border-radius: 3px;
    background: linear-gradient(90deg, var(--asc-chart-lo), var(--asc-chart) 60%, var(--asc-chart-hi));
  }
  assert-score-card .asc-areas td.asc-value { width: 1%; padding-left: 0.9rem; text-align: right; font-family: var(--asc-mono); font-size: 13px; color: var(--asc-ink); }
  assert-score-card .asc-facts { margin: 1.1rem 0 0; border-top: 1px solid var(--asc-line); font-size: 15px; }
  assert-score-card .asc-facts div {
    display: flex; flex-wrap: wrap; justify-content: space-between; align-items: baseline; gap: 0 1rem; padding: 0.55rem 0;
  }
  assert-score-card .asc-facts div + div { border-top: 1px solid var(--asc-line); }
  assert-score-card .asc-facts dt { color: var(--asc-muted); }
  assert-score-card .asc-facts dd { margin: 0 0 0 auto; text-align: right; font-weight: 500; color: var(--asc-ink); }
  assert-score-card .asc-sr {
    position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0;
  }
`;

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
  const t = el('table', className);
  t.append(el('caption', 'asc-sr', caption));
  const body = el('tbody');
  rows.forEach((row) => body.append(row));
  t.append(body);
  return t;
};

customElements.define('assert-score-card', class extends HTMLElement {
  static observedAttributes = ['repo', 'sprint', 'example-label', 'band', 'score', 'floors', 'series', 'trend-note', 'areas', 'facts'];

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
    const repo = text('repo') || 'your-product';
    const label = text('example-label') || 'Example';
    const score = number(text('score'));
    const shown = String(Math.round(score));

    const card = el('div', 'asc-card');
    card.setAttribute('role', 'group');
    card.setAttribute('aria-label', `${label}: a result for ${repo}`);

    // The repository, its sprint and the Example tag.
    const head = el('div', 'asc-head');
    const id = el('div');
    id.append(el('h3', 'asc-repo', repo));
    if (text('sprint')) id.append(el('span', 'asc-label', text('sprint')));
    head.append(id, el('span', 'asc-tag', label));
    card.append(head);

    // The score out of 100, the band in words, and the ladder with the marker.
    const row = el('p', 'asc-score');
    row.append(el('span', 'asc-label', 'CAI'), el('span', 'asc-number', shown), el('span', 'asc-unit', '/ 100'));
    if (text('band')) row.append(el('span', 'asc-chip', text('band')));
    card.append(row);
    const floors = (text('floors') || '0,25,50,70,90').split(',').map(number);
    if (floors.length === 5) {
      const ladder = el('div', 'asc-ladder');
      ladder.setAttribute('role', 'img');
      ladder.setAttribute('aria-label', `${shown} out of 100${text('band') ? `, ${text('band')}` : ''}`);
      floors.forEach((floor, i) => {
        const rung = el('span', 'asc-rung');
        rung.style.flex = String((i + 1 < floors.length ? floors[i + 1] : 100) - floor);
        rung.style.background = `var(--asc-rung-${i + 1})`;
        ladder.append(rung);
      });
      const pin = el('span', 'asc-pin');
      pin.style.left = `${Math.min(score, 100)}%`;
      ladder.append(pin);
      card.append(ladder);
    }

    // The trend as a line and a sentence.
    const series = text('series').split(',').map((v) => v.trim()).filter(Boolean).map(number);
    if (series.length > 1) {
      const spark = el('div', 'asc-spark');
      spark.append(sparkline(series));
      const trend = el('p', 'asc-trend');
      const arc = el('span', 'asc-arc', `${Math.round(series[0])} → `);
      arc.append(el('strong', '', shown));
      trend.append(arc, el('span', 'asc-note', text('trend-note')));
      card.append(spark, trend);
    }

    // The score of each area.
    const areas = parseList(text('areas')).filter((a) => typeof a.label === 'string');
    if (areas.length) {
      card.append(table('asc-areas', `${repo}: score by area`, areas.map((area) => {
        const value = number(area.value);
        const tr = el('tr');
        const th = el('th', '', area.label);
        th.scope = 'row';
        const barCell = el('td');
        const bar = el('span', 'asc-track');
        const fill = el('span', 'asc-fill');
        fill.style.width = `${Math.min(value, 100)}%`;
        bar.append(fill);
        barCell.append(bar);
        tr.append(th, barCell, el('td', 'asc-value', String(Math.round(value))));
        return tr;
      })));
    }

    // The facts of the scan.
    const facts = parseList(text('facts')).filter((f) => typeof f.label === 'string' && typeof f.value === 'string');
    if (facts.length) {
      const list = el('dl', 'asc-facts');
      list.setAttribute('aria-label', `${repo}: about the scan`);
      facts.forEach((fact) => {
        const row = el('div');
        row.append(el('dt', '', fact.label), el('dd', '', fact.value));
        list.append(row);
      });
      card.append(list);
    }

    this.replaceChildren(card);
  }
});
