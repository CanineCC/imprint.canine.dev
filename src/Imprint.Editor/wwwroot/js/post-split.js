// Imprint editor — the draggable divider between a post's markdown and its preview.
//
// The split is a CSS custom property on the panes container, so the whole layout stays in CSS and
// this file only decides a number. It is stored per site-and-post-editor rather than per post:
// an author who widened the markdown wants it wide for the next post too.
//
// Pointer events, not mouse events: one code path covers mouse, pen and touch, and setPointerCapture
// keeps the drag alive when the cursor outruns the divider — which it does immediately, because a
// 6px target is easy to leave behind at speed.

const KEY = 'imprint-post-split';
const MIN = 15;   // percent — below this a pane is a sliver nobody can read or aim at
const MAX = 85;

/** @type {WeakMap<Element, () => void>} */
const teardowns = new WeakMap();

const clamp = (value) => Math.min(MAX, Math.max(MIN, value));

function apply(panes, percent) {
  panes.style.setProperty('--post-split', `${percent}%`);
  const handle = panes.querySelector('[data-split-handle]');
  if (handle) {
    handle.setAttribute('aria-valuenow', String(Math.round(percent)));
  }
}

/**
 * Wires the divider inside `panes`. Restores the stored split first, so the pane is the size the
 * author left it at rather than flashing to half and jumping.
 * @param {HTMLElement} panes
 */
export function init(panes) {
  if (!panes || teardowns.has(panes)) {
    return;
  }

  const handle = panes.querySelector('[data-split-handle]');
  if (!handle) {
    return;
  }

  let stored = NaN;
  try {
    stored = Number.parseFloat(localStorage.getItem(KEY) ?? '');
  } catch {
    /* storage disabled — the default split is a fine answer */
  }
  apply(panes, Number.isFinite(stored) ? clamp(stored) : 50);

  const remember = (percent) => {
    try {
      localStorage.setItem(KEY, String(Math.round(percent)));
    } catch {
      /* best effort */
    }
  };

  const percentAt = (clientX) => {
    const box = panes.getBoundingClientRect();
    return clamp(((clientX - box.left) / box.width) * 100);
  };

  const onPointerMove = (event) => {
    event.preventDefault();
    apply(panes, percentAt(event.clientX));
  };

  const onPointerUp = (event) => {
    handle.releasePointerCapture?.(event.pointerId);
    handle.removeEventListener('pointermove', onPointerMove);
    handle.removeEventListener('pointerup', onPointerUp);
    panes.classList.remove('is-splitting');
    remember(percentAt(event.clientX));
  };

  const onPointerDown = (event) => {
    if (event.button !== 0 && event.pointerType === 'mouse') {
      return;
    }
    event.preventDefault();          // or the drag selects the markdown behind it
    handle.setPointerCapture?.(event.pointerId);
    handle.addEventListener('pointermove', onPointerMove);
    handle.addEventListener('pointerup', onPointerUp);
    panes.classList.add('is-splitting');
  };

  // A separator that only responds to a drag is unusable without a pointing device, and this one
  // is a real control: arrows nudge, Home/End go to the extremes, Enter recentres.
  const onKeyDown = (event) => {
    const current = Number.parseFloat(panes.style.getPropertyValue('--post-split')) || 50;
    const step = event.shiftKey ? 10 : 2;
    let next = current;
    switch (event.key) {
      case 'ArrowLeft': next = current - step; break;
      case 'ArrowRight': next = current + step; break;
      case 'Home': next = MIN; break;
      case 'End': next = MAX; break;
      case 'Enter': next = 50; break;
      default: return;
    }
    event.preventDefault();
    apply(panes, clamp(next));
    remember(clamp(next));
  };

  handle.addEventListener('pointerdown', onPointerDown);
  handle.addEventListener('keydown', onKeyDown);
  // Says the divider is wired, not merely present. The element is rendered by Blazor and this
  // module is imported after the first render, so between those two moments the handle looks
  // draggable and is not — a gap a person never notices and a test hits every time the machine
  // is busy.
  panes.setAttribute('data-split-ready', '');
  // A double-click on a divider means "even them up" in every tool that has one.
  const onDoubleClick = () => { apply(panes, 50); remember(50); };
  handle.addEventListener('dblclick', onDoubleClick);

  teardowns.set(panes, () => {
    panes.removeAttribute('data-split-ready');
    handle.removeEventListener('pointerdown', onPointerDown);
    handle.removeEventListener('keydown', onKeyDown);
    handle.removeEventListener('dblclick', onDoubleClick);
    handle.removeEventListener('pointermove', onPointerMove);
    handle.removeEventListener('pointerup', onPointerUp);
    teardowns.delete(panes);
  });
}

/**
 * Undoes init for the element — a reconnecting circuit must not leave listeners on a detached node.
 * @param {HTMLElement} panes
 */
export function dispose(panes) {
  teardowns.get(panes)?.();
}
