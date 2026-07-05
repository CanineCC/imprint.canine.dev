// Hydrates widget islands inside the editor canvas. The published site's island loader
// is a one-shot query at page load; the canvas is re-rendered live by Blazor, so this
// variant keeps watching for island elements arriving at any time. A module is imported
// once per URL — custom elements upgrade in place, so a re-created element needs no
// re-import. Widgets are shadow-DOM components; their internals never touch the light
// DOM Blazor diffs.
const loaded = new Set();

function load(el) {
  const src = el.getAttribute('data-island');
  if (src && !loaded.has(src)) {
    loaded.add(src);
    import(src);
  }
}

function hydrate(root) {
  if (root.querySelectorAll) {
    for (const el of root.querySelectorAll('[data-island]')) {
      load(el);
    }
  }
}

hydrate(document);
new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    for (const node of mutation.addedNodes) {
      if (node.nodeType === 1) {
        if (node.hasAttribute && node.hasAttribute('data-island')) {
          load(node);
        }

        hydrate(node);
      }
    }
  }
}).observe(document.body, { childList: true, subtree: true });
