// <assay-report-card kind="Report · acquisition" decision="Whether to proceed ..." rows='[...]' summary='[...]' identity="score 74, measured against CAI · ...">
// Assay's example report, as the kit (v0.2) draws it beside the hero heading: what kind of report it is
// and the Example tag in one line, the decision in the reader's words, the result against the bar one
// area per row with the result said as its word on a small label, the count of risks, unknowns and
// questions, and the measurement details in the mono face. Given the optional parts (evidence, risks,
// unknowns, questions, record) it draws a whole report page in the report's own order, numbered as a
// report is. Assay has one accent and no green or red, so a result is carried by its word and the shape
// of its label, never by colour: Pass outlined, Fail inverted, Not applicable plain, No usable evidence
// dashed so it cannot be read as a fail. Rendered into the element's LIGHT DOM with its own classes;
// every prop is set as text, never as markup.

const STYLE_ID = 'assay-report-card-style';
const STYLE = `
  assay-report-card {
    display: block; width: 100%; max-width: 34rem;
    --arc-ink: light-dark(#22201c, #ece7dd);
    --arc-ink-soft: light-dark(#46413a, #ccc5b8);
    --arc-muted: light-dark(#6b6459, #9a9284);
    --arc-accent: light-dark(#8a5d33, #c99a6a);
    --arc-card: light-dark(#ffffff, #232120);
    --arc-bg: light-dark(#fbfaf7, #1b1a17);
    --arc-line: light-dark(#e2dcd1, #36322c);
    --arc-rule: light-dark(#cfc6b8, #4c463d);
    --arc-wash: light-dark(#f3eadf, #2e2115);
    --arc-on-wash: light-dark(#5a3a1e, #e4c9a8);
    --arc-serif: "Source Serif 4", var(--font-ui, Georgia), Georgia, serif;
    --arc-mono: "Source Code Pro", var(--font-mono, ui-monospace), Menlo, Consolas, monospace;
    font-family: var(--arc-serif);
    font-variant-numeric: tabular-nums lining-nums;
    text-align: left;
  }
  assay-report-card[data-full] { max-width: 48rem; }
  assay-report-card .arc-card {
    background: var(--arc-card); color: var(--arc-ink); border: 1px solid var(--arc-line); border-radius: 2px;
    padding: 1.75rem 1.875rem; display: flex; flex-direction: column; gap: 0.9rem;
  }
  assay-report-card .arc-head { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; }
  assay-report-card .arc-label {
    font-size: 12px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--arc-muted); line-height: 1.35;
  }
  assay-report-card .arc-tag {
    flex: none; font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase;
    color: var(--arc-on-wash); background: var(--arc-wash); border-radius: 2px; padding: 2px 8px; white-space: nowrap;
  }
  assay-report-card .arc-block { display: flex; flex-direction: column; gap: 0.3rem; margin: 0; }
  assay-report-card .arc-decision { margin: 0; font-size: 22px; line-height: 1.25; font-weight: 700; letter-spacing: -0.01em; color: var(--arc-ink); }
  assay-report-card table { width: 100%; border-collapse: collapse; }
  assay-report-card .arc-rows th, assay-report-card .arc-rows td {
    padding: 0.56rem 0; border-bottom: 1px solid var(--arc-line); font-size: 15px; font-weight: 400; line-height: 1.4; vertical-align: middle;
  }
  assay-report-card .arc-rows th { text-align: left; color: var(--arc-ink); padding-right: 0.75rem; }
  assay-report-card .arc-rows td { text-align: right; }
  assay-report-card .arc-result {
    display: inline-block; font-size: 12px; font-weight: 600; line-height: 1.4; padding: 2px 8px; border-radius: 2px;
    border: 1px solid transparent; white-space: nowrap; color: var(--arc-ink);
  }
  assay-report-card .arc-pass { border-color: var(--arc-rule); }
  assay-report-card .arc-fail { color: var(--arc-bg); background: var(--arc-ink); border-color: var(--arc-ink); }
  assay-report-card .arc-na { color: var(--arc-muted); font-weight: 400; padding-inline: 0; }
  assay-report-card .arc-none { border: 1px dashed var(--arc-ink); }
  assay-report-card .arc-score { margin: 0; font-size: 15px; color: var(--arc-ink-soft); }
  assay-report-card .arc-score strong { font-weight: 700; color: var(--arc-ink); }
  assay-report-card .arc-summary { display: flex; flex-wrap: wrap; gap: 0.25rem 1.4rem; margin: 0; padding: 0; list-style: none; font-size: 15px; color: var(--arc-ink-soft); }
  assay-report-card .arc-identity { margin: 0; font-family: var(--arc-mono); font-size: 12px; line-height: 1.6; color: var(--arc-muted); overflow-wrap: anywhere; }
  /* The whole page: numbered parts, each opened by a hairline, the number in the accent. */
  assay-report-card .arc-part { display: flex; flex-direction: column; gap: 0.6rem; padding-top: 1rem; border-top: 1px solid var(--arc-line); }
  assay-report-card .arc-part-title { margin: 0; display: flex; gap: 0.75rem; align-items: baseline; font-size: 19px; font-weight: 700; line-height: 1.3; letter-spacing: -0.01em; color: var(--arc-ink); }
  assay-report-card .arc-part-number { color: var(--arc-accent); font-size: 16px; font-weight: 600; }
  assay-report-card .arc-pairs { display: grid; grid-template-columns: minmax(0, 10rem) minmax(0, 1fr); gap: 0.35rem 1rem; margin: 0; font-size: 15px; }
  assay-report-card .arc-pairs dt { color: var(--arc-muted); }
  assay-report-card .arc-pairs dd { margin: 0; color: var(--arc-ink); }
  assay-report-card .arc-record { font-family: var(--arc-mono); font-size: 13px; line-height: 1.6; }
  assay-report-card .arc-record dd { overflow-wrap: anywhere; }
  assay-report-card .arc-risk { border: 1px solid var(--arc-line); border-radius: 2px; padding: 1rem 1.1rem; display: flex; flex-direction: column; gap: 0.55rem; }
  assay-report-card .arc-risk-title { margin: 0; font-size: 17px; font-weight: 700; line-height: 1.3; }
  assay-report-card .arc-layer { display: grid; grid-template-columns: 8.5rem minmax(0, 1fr); gap: 0.75rem; align-items: baseline; margin: 0; font-size: 15px; line-height: 1.5; }
  assay-report-card .arc-layer-label { justify-self: start; font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; border-radius: 2px; padding: 1px 6px; white-space: nowrap; }
  assay-report-card .arc-measured .arc-layer-label { color: var(--arc-ink); border: 1px solid var(--arc-rule); }
  assay-report-card .arc-interpretation .arc-layer-label { color: var(--arc-on-wash); background: var(--arc-wash); padding: 2px 7px; }
  assay-report-card .arc-interpretation .arc-layer-text { color: var(--arc-ink-soft); font-style: italic; }
  assay-report-card .arc-caption { margin: 0; font-size: 14px; color: var(--arc-muted); }
  assay-report-card .arc-list { margin: 0; padding-left: 1.2rem; display: grid; gap: 0.35rem; font-size: 15px; line-height: 1.5; color: var(--arc-ink); }
  assay-report-card .arc-note { margin: 0; font-size: 14px; line-height: 1.5; color: var(--arc-muted); }
  assay-report-card .arc-sr {
    position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0;
  }
  @media (max-width: 520px) {
    assay-report-card .arc-card { padding: 1.25rem 1.1rem; }
    assay-report-card .arc-decision { font-size: 20px; }
    assay-report-card .arc-layer, assay-report-card .arc-pairs { grid-template-columns: minmax(0, 1fr); gap: 0.25rem; }
    assay-report-card .arc-pairs dd { margin-bottom: 0.4rem; }
  }
`;

const RESULTS = {
  pass: ['Pass', 'arc-pass'],
  fail: ['Fail', 'arc-fail'],
  'not applicable': ['Not applicable', 'arc-na'],
  'no usable evidence': ['No usable evidence', 'arc-none'],
};

const parse = (value, fallback) => {
  try {
    const parsed = JSON.parse(value ?? '');
    return Array.isArray(parsed) ? parsed : fallback;
  } catch {
    return fallback;
  }
};

const el = (tag, className, text) => {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
};

const strings = (value) => parse(value, []).filter((s) => typeof s === 'string' && s.trim());
const pairs = (value, key, val) => parse(value, []).filter((p) => p && typeof p[key] === 'string' && typeof p[val] === 'string');

const pairList = (className, items, key, val) => {
  const list = el('dl', className);
  items.forEach((item) => list.append(el('dt', '', item[key]), el('dd', '', item[val])));
  return list;
};

customElements.define('assay-report-card', class extends HTMLElement {
  static observedAttributes = ['kind', 'example-label', 'decision', 'score-line', 'rows', 'summary', 'identity', 'evidence', 'risks', 'unknowns', 'questions', 'record', 'note'];

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
    const text = (name) => (this.getAttribute(name) ?? '').trim();
    const label = text('example-label') || 'Example';
    const evidence = pairs(text('evidence'), 'label', 'value');
    const risks = parse(text('risks'), []).filter((r) => r && typeof r.title === 'string');
    const unknowns = strings(text('unknowns'));
    const questions = strings(text('questions'));
    const record = pairs(text('record'), 'key', 'value');
    // A whole page when any part beyond the summary is given; the hero's card otherwise.
    const full = evidence.length + risks.length + unknowns.length + questions.length + record.length > 0;
    this.toggleAttribute('data-full', full);

    const card = el('article', 'arc-card');
    card.setAttribute('aria-label', `${label}: ${text('kind') || 'an Assay report'}`);

    const head = el('div', 'arc-head');
    head.append(el('span', 'arc-label', text('kind') || 'Report'), el('span', 'arc-tag', label));
    card.append(head);

    const numbered = (n, title) => {
      const part = el('section', 'arc-part');
      const h = el('h3', 'arc-part-title');
      h.append(el('span', 'arc-part-number', String(n)), el('span', '', title));
      part.append(h);
      card.append(part);
      return part;
    };

    // 1 The decision, in the reader's words.
    if (full) {
      numbered(1, 'The decision').append(el('p', 'arc-decision', text('decision')));
    } else {
      const block = el('div', 'arc-block');
      block.append(el('span', 'arc-label', 'Decision'), el('p', 'arc-decision', text('decision')));
      card.append(block);
    }

    // 2 Evidence used.
    if (evidence.length) numbered(2, 'Evidence used').append(pairList('arc-pairs', evidence, 'label', 'value'));

    // 3 Result against the bar: one area per row, the result as its word.
    const rows = parse(text('rows'), []).filter((r) => r && typeof r.area === 'string');
    if (rows.length || text('score-line')) {
      const holder = full ? numbered(3, 'Result against the bar') : el('div', 'arc-block');
      if (!full) holder.append(el('span', 'arc-label', 'Result against the bar'));
      if (text('score-line')) holder.append(el('p', 'arc-score', text('score-line')));
      if (rows.length) {
        const table = el('table', 'arc-rows');
        table.append(el('caption', 'arc-sr', 'Result against the bar, by area'));
        const body = el('tbody');
        rows.forEach((row) => {
          const [word, className] = RESULTS[String(row.result ?? '').toLowerCase()] ?? [String(row.result ?? ''), 'arc-na'];
          const tr = el('tr');
          const th = el('th', '', row.area);
          th.scope = 'row';
          const td = el('td');
          td.append(el('span', `arc-result ${className}`, word));
          tr.append(th, td);
          body.append(tr);
        });
        table.append(body);
        holder.append(table);
      }
      if (!full) card.append(holder);
    }

    // 4 Material risks: what was measured, then what it means, on separate lines.
    if (risks.length) {
      const part = numbered(4, 'Material risks');
      risks.forEach((risk) => {
        const box = el('div', 'arc-risk');
        box.append(el('h4', 'arc-risk-title', risk.title));
        [['measured', 'Measured'], ['interpretation', 'Interpretation']].forEach(([key, name]) => {
          if (typeof risk[key] !== 'string') return;
          const line = el('p', `arc-layer arc-${key}`);
          line.append(el('span', 'arc-layer-label', name), el('span', 'arc-layer-text', risk[key]));
          box.append(line);
        });
        if (typeof risk.evidence === 'string') box.append(el('p', 'arc-caption', risk.evidence));
        part.append(box);
      });
    }

    // 5 Unknowns, 6 Questions.
    if (unknowns.length) {
      const list = el('ul', 'arc-list');
      unknowns.forEach((u) => list.append(el('li', '', u)));
      numbered(5, 'Unknowns').append(list);
    }
    if (questions.length) {
      const list = el('ol', 'arc-list');
      questions.forEach((q) => list.append(el('li', '', q)));
      numbered(6, 'Questions for the seller or team').append(list);
    }

    // The count of risks, unknowns and questions (the hero's card).
    const summary = strings(text('summary'));
    if (summary.length) {
      const list = el('ul', 'arc-summary');
      summary.forEach((s) => list.append(el('li', '', s)));
      card.append(list);
    }

    // 7 Measurement identity and verification record; the one line in the mono face otherwise.
    if (record.length) {
      numbered(7, 'Measurement identity and verification record').append(pairList('arc-pairs arc-record', record, 'key', 'value'));
    } else if (text('identity')) {
      card.append(el('p', 'arc-identity', text('identity')));
    }
    if (text('note')) card.append(el('p', 'arc-note', text('note')));

    this.replaceChildren(card);
  }
});
