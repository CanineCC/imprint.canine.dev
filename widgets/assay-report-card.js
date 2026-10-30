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
// mode="inside" draws the inside of a report beside the value it gives (the home page): the decision, the route
// the evidence took, the score drawn against the reader's bar with the area that still fails it, one risk
// opened, the unknown beside the first questions, and the verification line.

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
  /* The inside of a report (mode="inside"): the score drawn against the reader's bar, the one area that fails it,
     one risk opened, the unknown beside the questions, each part opened by the 2px ink rule. */
  assay-report-card[data-inside] { max-width: 36rem; container-type: inline-size; }
  assay-report-card[data-inside] .arc-scale-ends + .arc-score { margin-top: 0.35rem; }
  assay-report-card[data-inside] .arc-decision { font-size: 20px; }
  assay-report-card .arc-evidence { display: grid; grid-template-columns: 8.5rem minmax(0, 1fr); gap: 0.75rem; align-items: baseline; margin: 0; font-size: 15px; line-height: 1.5; color: var(--arc-ink-soft); }
  assay-report-card .arc-scale { position: relative; height: 3.4rem; margin: 0.4rem 0 0; }
  assay-report-card .arc-scale-track { position: absolute; left: 0; right: 0; top: 1.55rem; height: 6px; border-radius: 3px; background: var(--arc-line); }
  assay-report-card .arc-scale-fill {
    position: absolute; left: 0; top: 1.55rem; height: 6px; border-radius: 3px;
    background: linear-gradient(90deg, light-dark(#d2a776, #ead2b4) 0%, light-dark(#8a5d33, #c99a6a) 70%, light-dark(#684626, #9a6e44) 100%);
  }
  assay-report-card .arc-scale-bar { position: absolute; top: 0.95rem; width: 2px; height: 1.6rem; margin-left: -1px; background: var(--arc-ink); }
  assay-report-card .arc-scale-dot {
    position: absolute; top: calc(1.55rem - 5px); width: 16px; height: 16px; margin-left: -8px; border-radius: 50%;
    background: var(--arc-accent); border: 3px solid var(--arc-card); box-shadow: 0 0 0 1px var(--arc-accent);
  }
  assay-report-card .arc-scale-score { position: absolute; top: 0; transform: translateX(-50%); font-size: 15px; font-weight: 800; line-height: 1; color: var(--arc-accent); }
  assay-report-card .arc-scale-barlabel { position: absolute; top: 2.7rem; transform: translateX(-50%); font-size: 12px; line-height: 1; color: var(--arc-ink-soft); white-space: nowrap; }
  assay-report-card .arc-scale-ends { display: flex; justify-content: space-between; margin-top: 0.35rem; font-family: var(--arc-mono); font-size: 11px; color: var(--arc-muted); }
  assay-report-card .arc-section { display: flex; flex-direction: column; gap: 0.55rem; padding-top: 0.85rem; border-top: 2px solid var(--arc-ink); }
  assay-report-card .arc-section .arc-risk-title { font-size: 16px; }
  assay-report-card .arc-pair { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 1.25rem; }
  assay-report-card .arc-pair > div { display: flex; flex-direction: column; gap: 0.4rem; font-size: 14px; line-height: 1.5; }
  assay-report-card .arc-pair .arc-list { font-size: 14px; }
  assay-report-card .arc-more { margin: 0; font-size: 13px; color: var(--arc-muted); }
  assay-report-card .arc-unknown { margin: 0; color: var(--arc-ink); }
  assay-report-card[data-inside] .arc-identity { padding-top: 0.75rem; border-top: 1px solid var(--arc-line); }
  /* The inside card sits in a column, not across the page, so it stacks by its own width. */
  @container (max-width: 30rem) {
    assay-report-card[data-inside] .arc-card { padding: 1.25rem 1.1rem; }
    assay-report-card[data-inside] .arc-layer, assay-report-card[data-inside] .arc-evidence { grid-template-columns: minmax(0, 1fr); gap: 0.25rem; }
    assay-report-card[data-inside] .arc-pair { grid-template-columns: minmax(0, 1fr); }
  }
  @media (max-width: 520px) {
    assay-report-card .arc-card { padding: 1.25rem 1.1rem; }
    assay-report-card .arc-decision { font-size: 20px; }
    assay-report-card .arc-layer, assay-report-card .arc-pairs, assay-report-card .arc-evidence { grid-template-columns: minmax(0, 1fr); gap: 0.25rem; }
    assay-report-card .arc-pairs dd { margin-bottom: 0.4rem; }
    assay-report-card .arc-pair { grid-template-columns: minmax(0, 1fr); }
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
  static observedAttributes = [
    'kind', 'example-label', 'decision', 'score-line', 'rows', 'summary', 'identity', 'evidence', 'risks', 'unknowns', 'questions', 'record', 'note',
    'mode', 'evidence-line', 'score', 'bar', 'scale-caption', 'risk-label', 'questions-label', 'more-questions',
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
    const text = (name) => (this.getAttribute(name) ?? '').trim();
    const inside = text('mode') === 'inside';
    this.toggleAttribute('data-inside', inside);
    if (inside) {
      this.#renderInside(text);
      return;
    }
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

  // The inside of a report, beside the value it gives (the home page's "Inside the report"): the decision, the
  // route the evidence took, the score drawn against the reader's bar with the area that still fails it, one risk
  // opened, the unknown beside the first questions, and the line that says it was verified.
  #renderInside(text) {
    this.removeAttribute('data-full');
    const label = text('example-label') || 'Example';
    const card = el('article', 'arc-card');
    card.setAttribute('aria-label', `${label}: ${text('kind') || 'an Assay report'}`);

    const head = el('div', 'arc-head');
    head.append(el('span', 'arc-label', text('kind') || 'Report'), el('span', 'arc-tag', label));
    card.append(head);

    const decision = el('div', 'arc-block');
    decision.append(el('span', 'arc-label', 'Decision'), el('p', 'arc-decision', text('decision')));
    card.append(decision);

    if (text('evidence-line')) {
      const line = el('p', 'arc-evidence');
      line.append(el('span', 'arc-label', 'Evidence used'), el('span', '', text('evidence-line')));
      card.append(line);
    }

    // The score against the bar, on the 0 to 100 scale the score is measured on.
    const clamp = (n) => Math.max(0, Math.min(100, n));
    const score = Number.parseFloat(text('score'));
    const bar = Number.parseFloat(text('bar'));
    const rows = parse(text('rows'), []).filter((r) => r && typeof r.area === 'string');
    if (Number.isFinite(score) && Number.isFinite(bar)) {
      const block = el('div', 'arc-block');
      block.append(el('span', 'arc-label', 'Against your bar'));
      const scale = el('div', 'arc-scale');
      scale.setAttribute('role', 'img');
      scale.setAttribute('aria-label', `Score ${score} against your bar of ${bar}, on a scale from 0 to 100`);
      const at = (node, n) => { node.style.left = `${clamp(n)}%`; return node; };
      const fill = el('div', 'arc-scale-fill');
      fill.style.width = `${clamp(score)}%`;
      scale.append(
        el('div', 'arc-scale-track'), fill, at(el('div', 'arc-scale-bar'), bar),
        at(el('span', 'arc-scale-barlabel', `your bar ${bar}`), bar), at(el('div', 'arc-scale-dot'), score),
        at(el('span', 'arc-scale-score', String(score)), score));
      scale.querySelectorAll('span').forEach((s) => s.setAttribute('aria-hidden', 'true'));
      const ends = el('div', 'arc-scale-ends');
      ends.setAttribute('aria-hidden', 'true');
      ends.append(el('span', '', '0'), el('span', '', '100'));
      block.append(scale, ends);
      if (text('scale-caption')) block.append(el('p', 'arc-score', text('scale-caption')));
      if (rows.length) {
        const table = el('table', 'arc-rows');
        table.append(el('caption', 'arc-sr', 'Areas that fall short of the bar'));
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
        block.append(table);
      }
      card.append(block);
    }

    // One risk, opened: what was measured, then what it means.
    const risk = parse(text('risks'), []).find((r) => r && typeof r.title === 'string');
    if (risk) {
      const section = el('section', 'arc-section');
      section.append(el('span', 'arc-label', text('risk-label') || 'Material risk'), el('h3', 'arc-risk-title', risk.title));
      [['measured', 'Measured'], ['interpretation', 'Interpretation']].forEach(([key, name]) => {
        if (typeof risk[key] !== 'string') return;
        const line = el('p', `arc-layer arc-${key}`);
        line.append(el('span', 'arc-layer-label', name), el('span', 'arc-layer-text', risk[key]));
        section.append(line);
      });
      card.append(section);
    }

    // The unknown beside the first questions to put to the seller.
    const unknowns = strings(text('unknowns'));
    const questions = strings(text('questions'));
    if (unknowns.length || questions.length) {
      const section = el('section', 'arc-section arc-pair');
      if (unknowns.length) {
        const col = el('div');
        col.append(el('span', 'arc-label', unknowns.length > 1 ? 'Unknowns' : 'Unknown'));
        unknowns.forEach((u) => col.append(el('p', 'arc-unknown', u)));
        section.append(col);
      }
      if (questions.length) {
        const col = el('div');
        const list = el('ol', 'arc-list');
        questions.forEach((q) => list.append(el('li', '', q)));
        col.append(el('span', 'arc-label', text('questions-label') || 'Questions to ask'), list);
        if (text('more-questions')) col.append(el('p', 'arc-more', text('more-questions')));
        section.append(col);
      }
      card.append(section);
    }

    if (text('identity')) card.append(el('p', 'arc-identity', text('identity')));
    this.replaceChildren(card);
  }
});
