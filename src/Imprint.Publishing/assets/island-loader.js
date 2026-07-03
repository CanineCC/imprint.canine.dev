// The island loader — the only platform JavaScript a published page can carry, and
// only when the page actually contains widgets. Inlined into <head> at publish time.
// Hydrates each island's ES module when it approaches the viewport; eager islands
// load immediately. One import per bundle no matter how many instances.
(() => {
  const loaded = new Set();
  const load = el => {
    const src = el.getAttribute('data-island');
    if (src && !loaded.has(src)) {
      loaded.add(src);
      import(src);
    }
  };
  const lazy = [];
  for (const el of document.querySelectorAll('[data-island]')) {
    el.hasAttribute('data-island-eager') ? load(el) : lazy.push(el);
  }
  if (lazy.length) {
    const io = new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          io.unobserve(entry.target);
          load(entry.target);
        }
      }
    }, { rootMargin: '200px' });
    lazy.forEach(el => io.observe(el));
  }
})();
