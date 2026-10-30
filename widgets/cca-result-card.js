// <cca-result-card repo="acme/payments-api" call="GET /v1/repositories/{id}/scores" band="Strong" score="78" series="64,…,78" lenses='[…]' facts='[…]'>
// CCA's example result: what a pipeline, an agent harness or a product gets back from one call, drawn in
// the shape the family's result cards share (Assert's score card, Assure's progress card) so the three
// read as one measurement. The repository and the call that returns the result, the Example tag, the CAI
// score out of 100 with its band on the ladder, the score scan by scan, the five lenses, then the facts
// the result carries (commit, rubric version, findings with file and line, lines measured, signed
// evidence, a reused check). It is the RESULT, not a screen: CCA has no screen of its own, so nothing
// here may look like a view, a button or a schedule.
// Dressed in CCA's kit: a silver card with a 1 px edge and no shadow, 4 px corners, the ladder in five
// greys with a titanium metal marker, the lens bars in titanium, labels and figures in the mono face.
// CCA has one accent and never uses colour for a warning, so the band is said in words on the chip.
// Rendered into the element's LIGHT DOM with its own classes; every prop is set as text, never as markup.

const STYLE_ID = 'cca-result-card-style';
const STYLE = `
  cca-result-card {
    display: block; width: 100%; max-width: 28rem;
    --crc-ink: light-dark(#16181d, #e8eaf0);
    --crc-ink-soft: light-dark(#3a3f4a, #c3c8d2);
    --crc-muted: light-dark(#5c6370, #9aa3b2);
    --crc-accent: light-dark(#7f5b6f, #d0b2c1);
    --crc-line: light-dark(#e3e6eb, #2c323c);
    --crc-outline: light-dark(#c9cdd4, #3a404c);
    --crc-wash: light-dark(#f8eff3, #3a2430);
    --crc-on-wash: light-dark(#503042, #ead7e0);
    --crc-silver-hi: light-dark(#ffffff, #232a31);
    --crc-silver-lo: light-dark(#f4f5f8, #1a1f25);
    --crc-silver-edge: light-dark(#c9ccd4, #3a434d);
    --crc-fill-lo: light-dark(#5a394c, #a37f92);
    --crc-fill: light-dark(#7f5b6f, #d0b2c1);
    --crc-fill-hi: light-dark(#9f7b8e, #ead8e2);
    --crc-rung-1: light-dark(#d5dae0, #2b323a);
    --crc-rung-2: light-dark(#bfc6ce, #3a434d);
    --crc-rung-3: light-dark(#a5adb7, #555f6a);
    --crc-rung-4: light-dark(#8b95a0, #7a8591);
    --crc-rung-5: light-dark(#5f6974, #a3afbb);
    --crc-mono: var(--font-mono, ui-monospace), Menlo, Consolas, monospace;
    --crc-display: var(--font-display, system-ui), sans-serif;
    font-family: var(--font-ui, system-ui), sans-serif;
    text-align: left;
    color-scheme: light;
  }
  @media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) cca-result-card { color-scheme: dark; } }
  :root[data-theme="dark"] cca-result-card { color-scheme: dark; }
  cca-result-card .crc-card {
    background: linear-gradient(180deg, var(--crc-silver-hi), var(--crc-silver-lo));
    border: 1px solid var(--crc-silver-edge); border-radius: 4px; padding: 1.5rem 1.6rem; color: var(--crc-ink);
  }
  cca-result-card .crc-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
  cca-result-card .crc-repo { margin: 0; font-size: 17px; font-weight: 700; line-height: 1.3; color: var(--crc-ink); }
  cca-result-card .crc-call { display: block; margin-top: 0.2rem; font-family: var(--crc-mono); font-size: 11px; color: var(--crc-muted); overflow-wrap: anywhere; }
  cca-result-card .crc-tag {
    flex: none; font-family: var(--crc-mono); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--crc-muted);
    border: 1px solid var(--crc-outline); border-radius: 4px; padding: 0.2rem 0.45rem;
  }
  cca-result-card .crc-label { font-family: var(--crc-mono); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--crc-muted); }
  cca-result-card .crc-score { display: flex; align-items: baseline; gap: 0.5rem; margin: 1.1rem 0 0.9rem; }
  cca-result-card .crc-number {
    font-family: var(--crc-display); font-weight: 800; font-size: 60px; line-height: 0.9; letter-spacing: -0.03em; color: var(--crc-ink);
    font-variant-numeric: tabular-nums;
  }
  cca-result-card .crc-unit { font-size: 17px; color: var(--crc-muted); }
  cca-result-card .crc-chip {
    margin-left: auto; align-self: center; background: var(--crc-wash); color: var(--crc-on-wash);
    font-size: 13px; font-weight: 700; border-radius: 4px; padding: 0.25rem 0.65rem;
  }
  cca-result-card .crc-ladder { position: relative; display: flex; gap: 2px; height: 8px; }
  cca-result-card .crc-rung { display: block; min-width: 0; border-radius: 2px; }
  cca-result-card .crc-pin {
    position: absolute; top: -3px; width: 14px; height: 14px; box-sizing: border-box; transform: translateX(-50%) rotate(45deg);
    background: linear-gradient(135deg, #cdaebe, #7f5b6f 50%, #492a3c); border: 2px solid var(--crc-silver-hi);
  }
  cca-result-card .crc-spark { margin-top: 1rem; color: var(--crc-accent); }
  cca-result-card .crc-spark svg { display: block; width: 100%; height: 40px; }
  cca-result-card .crc-trend { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; margin: 0.5rem 0 1rem; }
  cca-result-card .crc-arc { font-size: 20px; font-weight: 700; color: var(--crc-ink-soft); font-variant-numeric: tabular-nums; }
  cca-result-card .crc-arc strong { color: var(--crc-accent); }
  cca-result-card .crc-note { font-size: 14px; color: var(--crc-muted); text-align: right; }
  cca-result-card table { width: 100%; border-collapse: collapse; }
  cca-result-card .crc-lenses th, cca-result-card .crc-lenses td { padding: 0.28rem 0; font-size: 15px; font-weight: 400; }
  cca-result-card .crc-lenses th { width: 1%; white-space: nowrap; padding-right: 0.9rem; text-align: left; color: var(--crc-ink-soft); }
  cca-result-card .crc-track { display: block; height: 6px; background: var(--crc-line); border-radius: 3px; }
  cca-result-card .crc-fill { display: block; height: 100%; border-radius: 3px; background: linear-gradient(90deg, var(--crc-fill-lo), var(--crc-fill) 60%, var(--crc-fill-hi)); }
  cca-result-card .crc-lenses td.crc-value { width: 1%; padding-left: 0.9rem; text-align: right; font-family: var(--crc-mono); font-size: 13px; color: var(--crc-ink); }
  cca-result-card .crc-facts { margin: 1.1rem 0 0; border-top: 1px solid var(--crc-line); font-size: 15px; }
  cca-result-card .crc-facts div { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: baseline; gap: 0 1rem; padding: 0.55rem 0; }
  cca-result-card .crc-facts div + div { border-top: 1px solid var(--crc-line); }
  cca-result-card .crc-facts dt { color: var(--crc-muted); }
  cca-result-card .crc-facts dd { margin: 0 0 0 auto; text-align: right; font-weight: 500; color: var(--crc-ink); }
  cca-result-card .crc-facts dd.crc-mono { font-family: var(--crc-mono); font-size: 13px; font-weight: 400; }
  cca-result-card .crc-sr { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
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

customElements.define('cca-result-card', class extends HTMLElement {
  static observedAttributes = ['repo', 'call', 'example-label', 'band', 'score', 'floors', 'series', 'trend-note', 'lenses', 'facts'];

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
    const repo = text('repo') || 'your-repository';
    const label = text('example-label') || 'Example';
    const score = number(text('score'));
    const shown = String(Math.round(score));

    const card = el('div', 'crc-card');
    card.setAttribute('role', 'group');
    card.setAttribute('aria-label', `${label}: the result CCA returns for ${repo}`);

    // The repository, the call that returns the result, and the Example tag.
    const head = el('div', 'crc-head');
    const id = el('div');
    id.append(el('p', 'crc-repo', repo));
    if (text('call')) id.append(el('span', 'crc-call', text('call')));
    head.append(id, el('span', 'crc-tag', label));
    card.append(head);

    // The score out of 100, the band in words, and the ladder with the marker.
    const row = el('p', 'crc-score');
    row.append(el('span', 'crc-label', 'CAI'), el('span', 'crc-number', shown), el('span', 'crc-unit', '/ 100'));
    if (text('band')) row.append(el('span', 'crc-chip', text('band')));
    card.append(row);
    const floors = (text('floors') || '0,25,50,70,90').split(',').map(number);
    if (floors.length === 5) {
      const ladder = el('div', 'crc-ladder');
      ladder.setAttribute('role', 'img');
      ladder.setAttribute('aria-label', `${shown} out of 100${text('band') ? `, ${text('band')}` : ''}`);
      floors.forEach((floor, i) => {
        const rung = el('span', 'crc-rung');
        rung.style.flex = String((i + 1 < floors.length ? floors[i + 1] : 100) - floor);
        rung.style.background = `var(--crc-rung-${i + 1})`;
        ladder.append(rung);
      });
      const pin = el('span', 'crc-pin');
      pin.style.left = `${Math.min(score, 100)}%`;
      ladder.append(pin);
      card.append(ladder);
    }

    // The score scan by scan, as a line and a sentence.
    const series = text('series').split(',').map((v) => v.trim()).filter(Boolean).map(number);
    if (series.length > 1) {
      const spark = el('div', 'crc-spark');
      spark.append(sparkline(series));
      const trend = el('p', 'crc-trend');
      const arc = el('span', 'crc-arc', `${Math.round(series[0])} → `);
      arc.append(el('strong', '', shown));
      trend.append(arc, el('span', 'crc-note', text('trend-note')));
      card.append(spark, trend);
    }

    // The five lenses.
    const lenses = parseList(text('lenses')).filter((l) => typeof l.label === 'string');
    if (lenses.length) {
      const t = el('table', 'crc-lenses');
      t.append(el('caption', 'crc-sr', `${repo}: score by lens`));
      const body = el('tbody');
      lenses.forEach((lens) => {
        const value = number(lens.value);
        const tr = el('tr');
        const th = el('th', '', lens.label);
        th.scope = 'row';
        const barCell = el('td');
        const bar = el('span', 'crc-track');
        const fill = el('span', 'crc-fill');
        fill.style.width = `${Math.min(value, 100)}%`;
        bar.append(fill);
        barCell.append(bar);
        tr.append(th, barCell, el('td', 'crc-value', String(Math.round(value))));
        body.append(tr);
      });
      t.append(body);
      card.append(t);
    }

    // The facts the result carries; a fact marked mono (a commit, a version) is set in the mono face.
    const facts = parseList(text('facts')).filter((f) => typeof f.label === 'string' && typeof f.value === 'string');
    if (facts.length) {
      const list = el('dl', 'crc-facts');
      list.setAttribute('aria-label', `${repo}: what the result carries`);
      facts.forEach((fact) => {
        const div = el('div');
        div.append(el('dt', '', fact.label), el('dd', fact.mono ? 'crc-mono' : '', fact.value));
        list.append(div);
      });
      card.append(list);
    }

    this.replaceChildren(card);
  }
});
