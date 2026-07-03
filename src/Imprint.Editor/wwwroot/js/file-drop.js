// Imprint editor — drag-file-anywhere-onto-panel upload plumbing (docs/editor-ux.md §9).
// Deliberately tiny: it only turns "files dropped on the panel" into "the panel's
// hidden <input type=file> changed", so Blazor's InputFile owns the actual streaming.
// Idempotent per element; full teardown via dispose (circuit reconnects must not leak).

/** @type {WeakMap<Element, () => void>} */
const teardowns = new WeakMap();

/**
 * Wires drop targeting on a panel element and forwards drops to the file input inside.
 * @param {HTMLElement} panelEl
 */
export function init(panelEl) {
  if (!panelEl || teardowns.has(panelEl)) {
    return;
  }

  let depth = 0;
  const hasFiles = (event) => [...(event.dataTransfer?.types ?? [])].includes('Files');
  const highlight = (on) => panelEl.classList.toggle('ed-drop-target', on);

  const onEnter = (event) => {
    if (!hasFiles(event)) {
      return;
    }
    event.preventDefault();
    depth += 1;
    highlight(true);
  };

  const onOver = (event) => {
    if (hasFiles(event)) {
      event.preventDefault();
    }
  };

  const onLeave = () => {
    depth = Math.max(0, depth - 1);
    if (depth === 0) {
      highlight(false);
    }
  };

  const onDrop = (event) => {
    if (!hasFiles(event)) {
      return;
    }
    event.preventDefault();
    depth = 0;
    highlight(false);
    const input = panelEl.querySelector('input[type="file"]');
    if (input && event.dataTransfer.files.length > 0) {
      input.files = event.dataTransfer.files;
      input.dispatchEvent(new Event('change', { bubbles: true }));
    }
  };

  panelEl.addEventListener('dragenter', onEnter);
  panelEl.addEventListener('dragover', onOver);
  panelEl.addEventListener('dragleave', onLeave);
  panelEl.addEventListener('drop', onDrop);

  teardowns.set(panelEl, () => {
    panelEl.removeEventListener('dragenter', onEnter);
    panelEl.removeEventListener('dragover', onOver);
    panelEl.removeEventListener('dragleave', onLeave);
    panelEl.removeEventListener('drop', onDrop);
    teardowns.delete(panelEl);
  });
}

/**
 * Undoes init for the element.
 * @param {HTMLElement} panelEl
 */
export function dispose(panelEl) {
  teardowns.get(panelEl)?.();
}
