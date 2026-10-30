// <cca-loop-card repo="acme/payments-api" task="Add a refund endpoint" findings='[{"title":"…","where":"…"}]' before="71" after="78" band="Strong">
// CCA's example card for the top of its home page: one agent task followed from the first push to the moment it is
// ready for a person. The first push and the problems CCA found, each with its file and line; the agent fixing them
// and pushing again; the second push with no open problems and the score before and after; and the outcome.
// It shows what CCA gives an engineer (an agent that finishes its own work), not how the API is called.
// Dressed in CCA's kit: a silver card with a 1 px edge and no shadow, 4 px corners, the titanium accent, Atkinson
// Hyperlegible Next for text and Mono for files and labels. CCA has one accent and never uses colour for a warning, so
// the band and the outcome are said in words. Rendered into the element's LIGHT DOM with its own classes; every prop
// is set as text, never as markup.

const STYLE_ID = 'cca-loop-card-style';
const STYLE = `
  cca-loop-card {
    display: block; width: 100%; max-width: 30rem;
    --clc-ink: light-dark(#16181d, #e8eaf0);
    --clc-ink-soft: light-dark(#3a3f4a, #c3c8d2);
    --clc-muted: light-dark(#5c6370, #9aa3b2);
    --clc-accent: light-dark(#7f5b6f, #d0b2c1);
    --clc-line: light-dark(#e3e6eb, #2c323c);
    --clc-outline: light-dark(#c9cdd4, #3a404c);
    --clc-wash: light-dark(#f8eff3, #3a2430);
    --clc-on-wash: light-dark(#503042, #ead7e0);
    --clc-silver-hi: light-dark(#ffffff, #232a31);
    --clc-silver-lo: light-dark(#f4f5f8, #1a1f25);
    --clc-silver-edge: light-dark(#c9ccd4, #3a434d);
    --clc-mono: var(--font-mono, ui-monospace), Menlo, Consolas, monospace;
    --clc-display: var(--font-display, system-ui), sans-serif;
    font-family: var(--font-ui, system-ui), sans-serif;
    text-align: left;
    color-scheme: light;
  }
  /* The card follows the site's theme, not the graphite top it sits on (which is dark in both). */
  @media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) cca-loop-card { color-scheme: dark; } }
  :root[data-theme="dark"] cca-loop-card { color-scheme: dark; }
  cca-loop-card .clc-card {
    background: linear-gradient(180deg, var(--clc-silver-hi), var(--clc-silver-lo));
    border: 1px solid var(--clc-silver-edge); border-radius: 4px; padding: 1.4rem 1.5rem; color: var(--clc-ink);
  }
  cca-loop-card .clc-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 1.1rem; }
  cca-loop-card .clc-repo { margin: 0; font-size: 17px; font-weight: 700; line-height: 1.3; color: var(--clc-ink); }
  cca-loop-card .clc-task { display: block; margin-top: 0.15rem; font-size: 15px; color: var(--clc-muted); }
  cca-loop-card .clc-tag {
    flex: none; font-family: var(--clc-mono); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--clc-muted);
    border: 1px solid var(--clc-outline); border-radius: 4px; padding: 0.2rem 0.45rem;
  }
  cca-loop-card ol.clc-steps { list-style: none; margin: 0; padding: 0; }
  cca-loop-card .clc-step { position: relative; padding: 0 0 1.05rem 2.4rem; }
  cca-loop-card .clc-step::before {
    content: ""; position: absolute; left: 0.8rem; top: 1.7rem; bottom: 0.15rem; width: 1px; background: var(--clc-line);
  }
  cca-loop-card .clc-step:last-child { padding-bottom: 0; }
  cca-loop-card .clc-step:last-child::before { display: none; }
  cca-loop-card .clc-num {
    position: absolute; left: 0; top: 0; width: 1.6rem; height: 1.6rem; display: grid; place-items: center; border-radius: 50%;
    font-family: var(--clc-mono); font-size: 12px; color: var(--clc-accent); border: 1px solid var(--clc-outline); background: var(--clc-silver-hi);
  }
  cca-loop-card .clc-when { display: block; font-family: var(--clc-mono); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--clc-muted); line-height: 1.6rem; }
  cca-loop-card .clc-what { margin: 0.1rem 0 0; font-size: 16px; font-weight: 700; line-height: 1.35; color: var(--clc-ink); }
  cca-loop-card ul.clc-findings { list-style: none; margin: 0.55rem 0 0; padding: 0; border: 1px solid var(--clc-line); border-radius: 4px; background: var(--clc-silver-hi); }
  cca-loop-card .clc-finding { padding: 0.5rem 0.7rem; }
  cca-loop-card .clc-finding + .clc-finding { border-top: 1px solid var(--clc-line); }
  cca-loop-card .clc-title { display: block; font-size: 14px; line-height: 1.4; color: var(--clc-ink-soft); }
  cca-loop-card .clc-where { display: block; margin-top: 0.1rem; font-family: var(--clc-mono); font-size: 12px; color: var(--clc-accent); overflow-wrap: anywhere; }
  cca-loop-card .clc-result { display: flex; flex-wrap: wrap; align-items: baseline; gap: 0.3rem 0.6rem; margin-top: 0.35rem; }
  cca-loop-card .clc-score { font-family: var(--clc-display); font-weight: 800; font-size: 34px; line-height: 1; letter-spacing: -0.02em; font-variant-numeric: tabular-nums; color: var(--clc-ink); }
  cca-loop-card .clc-from { font-size: 16px; color: var(--clc-muted); font-variant-numeric: tabular-nums; }
  cca-loop-card .clc-chip { background: var(--clc-wash); color: var(--clc-on-wash); font-size: 13px; font-weight: 700; border-radius: 4px; padding: 0.2rem 0.6rem; }
  cca-loop-card .clc-outcome {
    display: flex; flex-wrap: wrap; justify-content: space-between; align-items: baseline; gap: 0.3rem 1rem;
    margin-top: 1.1rem; padding-top: 0.8rem; border-top: 1px solid var(--clc-line); font-size: 15px;
  }
  cca-loop-card .clc-outcome strong { color: var(--clc-accent); font-weight: 700; }
  cca-loop-card .clc-outcome span { color: var(--clc-muted); }
`;

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

const step = (number, when, what) => {
  const li = el('li', 'clc-step');
  const num = el('span', 'clc-num', String(number));
  num.setAttribute('aria-hidden', 'true');
  li.append(num, el('span', 'clc-when', when), el('p', 'clc-what', what));
  return li;
};

customElements.define('cca-loop-card', class extends HTMLElement {
  static observedAttributes = [
    'repo', 'task', 'example-label', 'first-label', 'findings', 'fix-label', 'fix-note',
    'second-label', 'second-note', 'before', 'after', 'band', 'outcome', 'outcome-note',
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
    const repo = text('repo', 'your-repository');
    const label = text('example-label', 'Example');
    const findings = parseList(text('findings')).filter((f) => typeof f.title === 'string');

    const card = el('div', 'clc-card');
    card.setAttribute('role', 'group');
    card.setAttribute('aria-label', `${label}: one agent task in ${repo}`);

    // The repository, the task and the Example tag.
    const head = el('div', 'clc-head');
    const id = el('div');
    id.append(el('p', 'clc-repo', repo));
    if (text('task')) id.append(el('span', 'clc-task', text('task')));
    head.append(id, el('span', 'clc-tag', label));
    card.append(head);

    const steps = el('ol', 'clc-steps');

    // The first push and what CCA found, each with its file and line.
    const count = findings.length;
    const first = step(1, text('first-label', 'First push'), count === 1 ? '1 problem to fix' : `${count} problems to fix`);
    if (count) {
      const list = el('ul', 'clc-findings');
      findings.forEach((f) => {
        const li = el('li', 'clc-finding');
        li.append(el('span', 'clc-title', f.title));
        if (typeof f.where === 'string' && f.where) li.append(el('span', 'clc-where', f.where));
        list.append(li);
      });
      first.append(list);
    }
    steps.append(first);

    // The agent fixes them and pushes again.
    steps.append(step(2, text('fix-label', 'The agent'), text('fix-note', 'Fixed them and pushed again')));

    // The second push: nothing open, and the score before and after.
    const second = step(3, text('second-label', 'Second push'), text('second-note', 'No open problems'));
    if (text('after')) {
      const result = el('p', 'clc-result');
      result.append(el('span', 'clc-score', text('after')));
      if (text('before')) result.append(el('span', 'clc-from', `up from ${text('before')}`));
      if (text('band')) result.append(el('span', 'clc-chip', text('band')));
      second.append(result);
    }
    steps.append(second);
    card.append(steps);

    // The outcome, in words.
    if (text('outcome')) {
      const outcome = el('p', 'clc-outcome');
      outcome.append(el('strong', '', text('outcome')));
      if (text('outcome-note')) outcome.append(el('span', '', text('outcome-note')));
      card.append(outcome);
    }

    this.replaceChildren(card);
  }
});
