// The resource center's search palette (<rc-search>, placed by the publisher on every resource-center
// page). Opened by any [data-rc-search] control (the header's Search, the index's search field), by
// Cmd/Ctrl+K and by '/'. It searches the site's search.json, which the publisher writes from the same list
// the index shows, fetched once when the palette first opens; every word typed must match. Nothing is
// remembered. It also does the hand-off: the title of the card a reader clicks is named for the
// cross-document view transition, so it travels to become the article's title (off under reduced motion,
// in CSS).
const ROOT = new URL('../', import.meta.url).pathname; // the site root, also under a /preview/<site>/ prefix
const GROUP = { essay: 'Thought leadership', whitepaper: 'Whitepapers', guide: 'Guides', documentation: 'Documentation' };
const ORDER = ['essay', 'whitepaper', 'guide', 'documentation'];
const KIND = { essay: 'thought leadership essay', whitepaper: 'whitepaper paper', guide: 'guide', documentation: 'documentation docs' };
const PRODUCT = { canine: 'Canine Development', assert: 'Assert', assure: 'Assure', assay: 'Assay', cca: 'CCA', cai: 'Code Assurance Index' };

let index = null;
const load = () => (index ??= fetch(ROOT + 'search.json').then((r) => (r.ok ? r.json() : [])).catch(() => []));
const site = (href) => ROOT + String(href).replace(/^\//, '');
const accent = (products) => (products || []).find((p) => p !== 'canine' && PRODUCT[p]) || 'canine';
const typing = (el) => el && (el.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName));

let dialog = null;

function build() {
  dialog = document.createElement('dialog');
  dialog.className = 'rc-palette';
  dialog.setAttribute('aria-label', 'Search');
  dialog.innerHTML =
    '<div class="rc-in"><svg class="rc-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>' +
    '<input type="search" role="combobox" aria-expanded="true" aria-controls="rc-results" aria-autocomplete="list" aria-label="Search everything" placeholder="Search everything" autocomplete="off" spellcheck="false">' +
    '<span class="rc-kbd">esc</span></div>' +
    '<div class="rc-res" id="rc-results" role="listbox" aria-label="Results"></div>' +
    '<p class="rc-pfoot"><span><span class="rc-kbd">↑</span> <span class="rc-kbd">↓</span> to move</span><span><span class="rc-kbd">↵</span> to open</span><span class="rc-total"></span></p>';
  document.body.append(dialog);
  const input = dialog.querySelector('input');
  const results = dialog.querySelector('.rc-res');
  const total = dialog.querySelector('.rc-total');
  let options = [];
  let selected = 0;

  const select = (n) => {
    selected = Math.max(0, Math.min(options.length - 1, n));
    options.forEach((o, k) => o.setAttribute('aria-selected', String(k === selected)));
    const current = options[selected];
    if (current) {
      input.setAttribute('aria-activedescendant', current.id);
      current.scrollIntoView({ block: 'nearest' });
    } else input.removeAttribute('aria-activedescendant');
  };

  const mark = (title, words) => {
    const span = document.createElement('span');
    span.className = 'rc-tt';
    const word = words[0];
    const at = word ? title.toLowerCase().indexOf(word) : -1;
    if (at < 0) span.textContent = title;
    else {
      const m = document.createElement('mark');
      m.textContent = title.slice(at, at + word.length);
      span.append(title.slice(0, at), m, title.slice(at + word.length));
    }
    return span;
  };

  const render = async () => {
    const all = await load();
    const words = input.value.trim().toLowerCase().split(/\s+/).filter(Boolean);
    const hits = all.filter((a) => {
      const text = [a.title, a.dek, ...(a.products || []).map((p) => PRODUCT[p] || p), KIND[a.kind] || a.kind].join(' ').toLowerCase();
      return words.every((w) => text.includes(w));
    });
    results.replaceChildren();
    options = [];
    let n = 0;
    for (const kind of ORDER) {
      const group = hits.filter((h) => h.kind === kind).slice(0, words.length ? 8 : 4);
      if (!group.length) continue;
      const box = document.createElement('div');
      box.setAttribute('role', 'group');
      box.setAttribute('aria-label', GROUP[kind]);
      const head = document.createElement('div');
      head.className = 'rc-grp';
      head.setAttribute('aria-hidden', 'true');
      head.textContent = GROUP[kind];
      box.append(head);
      for (const a of group) {
        const option = document.createElement('a');
        option.className = 'rc-opt p-' + accent(a.products);
        option.id = 'rc-opt-' + n++;
        option.href = site(a.href);
        option.setAttribute('role', 'option');
        option.tabIndex = -1;
        const dot = document.createElement('span');
        dot.className = 'rc-pdot';
        dot.setAttribute('aria-hidden', 'true');
        const product = document.createElement('span');
        product.className = 'rc-eyebrow';
        product.textContent = PRODUCT[accent(a.products)];
        option.append(dot, mark(a.title, words), product);
        option.addEventListener('mousemove', () => select(options.indexOf(option)));
        box.append(option);
        options.push(option);
      }
      results.append(box);
    }
    if (!options.length) {
      const none = document.createElement('p');
      none.className = 'rc-grp rc-none';
      none.textContent = `No match for "${input.value.trim()}". Try a product, a kind, or a word from a title.`;
      results.append(none);
    }
    total.textContent = `Searches all ${all.length} articles`;
    select(0);
  };

  input.addEventListener('input', render);
  input.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      select(selected + 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      select(selected - 1);
    } else if (e.key === 'Enter' && options[selected]) {
      e.preventDefault();
      location.href = options[selected].href;
    }
  });
  // A click on the backdrop (the dialog itself, outside its content box) closes it.
  dialog.addEventListener('click', (e) => {
    if (e.target === dialog) dialog.close();
  });
  dialog.render = render;
}

function open() {
  if (!dialog) build();
  if (dialog.open) return;
  dialog.showModal();
  const input = dialog.querySelector('input');
  input.value = '';
  dialog.render();
  input.focus();
}

// The hand-off: the clicked card's title becomes the article's title (::view-transition-group(rc-title)).
function handoff(event) {
  const link = event.target.closest('.rc-card a, .rc-ctx-item, .rc-latest a');
  const title = link?.querySelector('h3, b');
  if (!title) return;
  document.querySelectorAll('[data-rc-vt]').forEach((el) => {
    el.style.viewTransitionName = '';
    delete el.dataset.rcVt;
  });
  // One element per page may carry a transition name: on an article the header's title has it, so
  // it steps aside for the clicked one.
  document.querySelectorAll('.rc-art-head h1').forEach((h) => {
    h.style.viewTransitionName = 'none';
    h.dataset.rcVt = '1';
  });
  title.style.viewTransitionName = 'rc-title';
  title.dataset.rcVt = '1';
}

class RcSearch extends HTMLElement {
  connectedCallback() {
    const root = document.documentElement;
    if (root.classList.contains('rc-search-ready')) return;
    root.classList.add('rc-search-ready');
    document.addEventListener('click', (e) => {
      const control = e.target.closest('[data-rc-search]');
      if (control) {
        e.preventDefault();
        open();
        return;
      }
      handoff(e);
    });
    document.addEventListener('keydown', (e) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        if (dialog?.open) dialog.close();
        else open();
      } else if (e.key === '/' && !dialog?.open && !typing(e.target)) {
        e.preventDefault();
        open();
      }
    });
    // Back from an article (the page restored from the cache): no title may keep the transition's name.
    addEventListener('pageshow', () => document.querySelectorAll('[data-rc-vt]').forEach((el) => (el.style.viewTransitionName = '')));
  }
}

if (!customElements.get('rc-search')) customElements.define('rc-search', RcSearch);
