// The resource center index's tabs and product filter (<rc-filter>, placed beside the server-rendered
// <rc-hub> in the same section): a progressive enhancement over markup the publisher already rendered
// (every article listed, grouped by kind). Nothing here fetches, and the choice is never remembered across
// visits: a remembered filter once made a reader see 12 of 29 and believe the rest were only reachable by
// search (owner decision 2026-10-25). A ?product= in the address (the header's product links) starts it on
// that product, which is the address saying so, not a memory. The featured cards are copies of cards in the
// lists, so they are neither filtered nor counted; the bento steps aside while a filter is on.
class RcFilter extends HTMLElement {
  connectedCallback() {
    const hub = this.closest('section')?.querySelector('.rc-hub');
    if (!hub || hub.classList.contains('rc-live')) return;
    hub.classList.add('rc-live');

    const tabs = [...hub.querySelectorAll('.rc-tab')];
    const chips = [...hub.querySelectorAll('.rc-chip')];
    const groups = [...hub.querySelectorAll('.rc-group')];
    const cards = [...hub.querySelectorAll('.rc-group .rc-card')];
    const bar = hub.querySelector('.rc-filterbar');
    const empty = hub.querySelector('.rc-empty');
    const total = cards.length;
    let kind = 'all';
    const asked = new URLSearchParams(location.search).get('product');
    let product = chips.some((c) => c.dataset.product === asked) ? asked : 'all';

    const fits = (card, k, p) =>
      (k === 'all' || card.dataset.kind === k) &&
      (p === 'all' || (card.dataset.products || '').split(' ').includes(p));

    const apply = () => {
      let shown = 0;
      for (const card of cards) {
        const ok = fits(card, kind, product);
        card.hidden = !ok;
        if (ok) shown++;
      }
      for (const group of groups) {
        const visible = group.querySelectorAll('.rc-card:not([hidden])').length;
        group.hidden = visible === 0;
        const count = group.querySelector('.rc-count');
        if (count) count.textContent = String(visible);
      }
      // A tab's count is what it would show under the current product filter.
      for (const tab of tabs) {
        const n = tab.querySelector('.rc-n');
        if (n) n.textContent = String(cards.filter((c) => fits(c, tab.dataset.kind, product)).length);
        tab.setAttribute('aria-selected', String(tab.dataset.kind === kind));
        tab.tabIndex = tab.dataset.kind === kind ? 0 : -1;
      }
      for (const chip of chips) chip.setAttribute('aria-pressed', String(chip.dataset.product === product));

      const filtered = kind !== 'all' || product !== 'all';
      hub.classList.toggle('rc-filtered', filtered);
      if (bar) {
        bar.hidden = !filtered;
        if (filtered) {
          const parts = [];
          if (kind !== 'all') parts.push(tabs.find((t) => t.dataset.kind === kind)?.firstChild?.textContent?.trim() || kind);
          if (product !== 'all') parts.push(chips.find((c) => c.dataset.product === product)?.textContent?.trim() || product);
          bar.replaceChildren();
          const text = document.createElement('span');
          text.textContent = `Showing ${shown} of ${total}` + (parts.length ? ` · ${parts.join(' · ')}` : '');
          const clear = document.createElement('button');
          clear.type = 'button';
          clear.className = 'rc-clear';
          clear.textContent = `Show all ${total}`;
          clear.addEventListener('click', () => { kind = 'all'; product = 'all'; apply(); });
          bar.append(text, clear);
        }
      }
      if (empty) empty.hidden = shown > 0;
    };

    for (const tab of tabs) {
      tab.addEventListener('click', () => { kind = tab.dataset.kind; apply(); });
      tab.addEventListener('keydown', (e) => {
        if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
        e.preventDefault();
        const i = tabs.indexOf(tab) + (e.key === 'ArrowRight' ? 1 : -1);
        const next = tabs[(i + tabs.length) % tabs.length];
        next.focus();
        kind = next.dataset.kind;
        apply();
      });
    }
    for (const chip of chips) chip.addEventListener('click', () => { product = chip.dataset.product; apply(); });
    apply();
  }
}

if (!customElements.get('rc-filter')) customElements.define('rc-filter', RcFilter);
