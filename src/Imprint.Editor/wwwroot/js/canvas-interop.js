// Canvas interop — the direct-manipulation layer of the editor (docs/editor-ux.md).
//
// Division of labor with Blazor:
//   C# owns all STATE (selection, drag plan, edit session) and all DECISIONS
//   (what is selectable, which drops are valid — the DragPlan is computed from the
//   same Placement rules the aggregate enforces).
//   This module owns GEOMETRY and MOTION: hit-testing, 60fps drag tracking against
//   precomputed slots, overlay drawing (outlines, indicator, ghost, gap pills), and
//   contenteditable choreography. It never mutates the canvas DOM.
//
// Everything here is idempotent and disposable: Blazor circuits reconnect, and a
// leaked listener would double-fire across a reconnect.

/** @type {ReturnType<typeof createState> | null} */
let state = null;

const NODE_SELECTOR = '[data-node-id]';
const GAP_THRESHOLD = 10;      // px from a sibling boundary that reveals the + pill
const LIFT_DISTANCE = 6;       // px of pointer travel that turns a press into a drag
const LONG_PRESS_MS = 350;     // touch lift delay
const EDGE_SCROLL_ZONE = 48;   // px from scroller edge that auto-scrolls during drag
const EDGE_SCROLL_MAX = 14;    // px per frame at the very edge
const INDICATOR_HYSTERESIS = 4;

export function init(canvasEl, overlayEl, scrollerEl, dotnetRef) {
  dispose();
  state = createState(canvasEl, overlayEl, scrollerEl, dotnetRef);
  buildOverlay();
  attachListeners();
  observe();
}

export function dispose() {
  if (!state) {
    return;
  }
  state.aborter.abort();
  state.mutations?.disconnect();
  state.resizes?.disconnect();
  cancelAnimationFrame(state.rafId);
  clearTimeout(state.longPressTimer);
  state.overlay.replaceChildren();
  state = null;
}

function createState(canvas, overlay, scroller, dotnet) {
  return {
    canvas, overlay, scroller, dotnet,
    aborter: new AbortController(),
    ui: {},                 // overlay elements, filled by buildOverlay
    selectionId: null,
    hoverId: null,
    drag: null,             // { nodeId, plan, geoms, candidate, pointerId }
    press: null,            // { nodeId, x, y, pointerId, viaHandle }
    edit: null,             // { nodeId, el, mode, original, debounce }
    gap: null,              // { parentId, index, x, y, orientation }
    longPressTimer: 0,
    rafId: 0,
    mutations: null,
    resizes: null,
  };
}

// ------------------------------------------------------------------ overlay DOM

function buildOverlay() {
  const make = (tag, className) => {
    const el = document.createElement(tag);
    el.className = className;
    el.hidden = true;
    state.overlay.append(el);
    return el;
  };

  state.ui.hover = make('div', 'ed-ov ed-ov-hover');
  state.ui.hoverChip = document.createElement('span');
  state.ui.hoverChip.className = 'ed-ov-chip';
  state.ui.hover.append(state.ui.hoverChip);

  state.ui.selection = make('div', 'ed-ov ed-ov-selection');
  state.ui.handle = document.createElement('button');
  state.ui.handle.type = 'button';
  state.ui.handle.className = 'ed-ov-handle';
  state.ui.handle.setAttribute('aria-label', 'Drag to move');
  state.ui.handle.innerHTML = '<svg viewBox="0 0 8 14" width="8" height="14" aria-hidden="true"><circle cx="2" cy="2" r="1.3"/><circle cx="6" cy="2" r="1.3"/><circle cx="2" cy="7" r="1.3"/><circle cx="6" cy="7" r="1.3"/><circle cx="2" cy="12" r="1.3"/><circle cx="6" cy="12" r="1.3"/></svg>';
  state.ui.selection.append(state.ui.handle);

  state.ui.indicator = make('div', 'ed-ov ed-ov-indicator');
  state.ui.into = make('div', 'ed-ov ed-ov-into');
  state.ui.ghost = make('div', 'ed-ov-ghost');
  state.ui.target = make('div', 'ed-ov ed-ov-target');

  state.ui.gapPill = make('button', 'ed-ov-gap');
  state.ui.gapPill.type = 'button';
  state.ui.gapPill.setAttribute('aria-label', 'Insert here');
  state.ui.gapPill.textContent = '+';
}

// ------------------------------------------------------------------- listeners

function attachListeners() {
  const opts = { signal: state.aborter.signal };
  const canvas = state.canvas;

  canvas.addEventListener('pointerdown', onPointerDown, opts);
  window.addEventListener('pointermove', onPointerMove, { ...opts, passive: false });
  window.addEventListener('pointerup', onPointerUp, opts);
  window.addEventListener('pointercancel', onPointerCancel, opts);
  canvas.addEventListener('pointerover', onPointerOver, opts);
  canvas.addEventListener('pointerleave', () => setHover(null), opts);
  canvas.addEventListener('dblclick', onDoubleClick, opts);
  window.addEventListener('keydown', onKeyDown, { ...opts, capture: true });
  state.scroller.addEventListener('scroll', scheduleSync, { ...opts, passive: true });
  window.addEventListener('resize', scheduleSync, opts);

  state.ui.handle.addEventListener('pointerdown', event => {
    if (state.selectionId) {
      event.preventDefault();
      event.stopPropagation();
      state.press = { nodeId: state.selectionId, x: event.clientX, y: event.clientY, pointerId: event.pointerId, viaHandle: true };
    }
  }, opts);

  state.ui.gapPill.addEventListener('click', event => {
    event.stopPropagation();
    if (state.gap) {
      const { parentId, index } = state.gap;
      const rect = state.ui.gapPill.getBoundingClientRect();
      invoke('ReportGapClick', parentId, index, rect.left + rect.width / 2, rect.bottom);
    }
  }, opts);
}

function invoke(method, ...args) {
  return state?.dotnet.invokeMethodAsync(method, ...args);
}

// ----------------------------------------------------------------- hit testing

function nodeElFromPoint(x, y) {
  // elementFromPoint sees the overlay only where it takes pointer events, and those
  // handlers stopPropagation — so anything arriving here is really the canvas.
  const el = document.elementFromPoint(x, y);
  return el?.closest(NODE_SELECTOR) ?? null;
}

function elOf(nodeId) {
  return state.canvas.querySelector(`[data-node-id="${nodeId}"]`);
}

/** Selection routes to the whole block instance: inner nodes are not addressable. */
function effectiveNode(el) {
  const inner = el?.closest('[data-block-inner]');
  return inner ? inner.closest('[data-block-instance]') ?? el : el;
}

// -------------------------------------------------------------- pointer events

function onPointerDown(event) {
  if (state.edit) {
    return; // typing — the contenteditable owns the pointer
  }
  const el = effectiveNode(event.target.closest?.(NODE_SELECTOR));
  const nodeId = el?.getAttribute('data-node-id') ?? null;

  if (event.pointerType === 'touch' && nodeId) {
    // Long-press lifts; early movement cancels into a natural scroll.
    state.press = { nodeId, x: event.clientX, y: event.clientY, pointerId: event.pointerId, viaHandle: false };
    state.longPressTimer = setTimeout(() => {
      if (state.press?.nodeId === nodeId) {
        startDrag(state.press);
      }
    }, LONG_PRESS_MS);
    return;
  }

  // Mouse/pen: report the click; dragging only ever starts from the handle.
  invoke('ReportClick', nodeId);
}

function onPointerOver(event) {
  if (state.drag || state.edit) {
    return;
  }
  const el = effectiveNode(event.target.closest?.(NODE_SELECTOR));
  setHover(el?.getAttribute('data-node-id') ?? null);
}

function onPointerMove(event) {
  if (state.press && !state.drag) {
    const travel = Math.hypot(event.clientX - state.press.x, event.clientY - state.press.y);
    if (state.press.viaHandle && travel > LIFT_DISTANCE) {
      startDrag(state.press);
    } else if (!state.press.viaHandle && travel > 8) {
      clearTimeout(state.longPressTimer); // moved too early: it's a scroll, not a lift
      state.press = null;
    }
  }

  if (state.drag) {
    event.preventDefault(); // no text selection / native scrolling while dragging
    trackDrag(event.clientX, event.clientY);
    return;
  }

  if (!state.edit) {
    trackGap(event.clientX, event.clientY);
  }
}

function onPointerUp(event) {
  clearTimeout(state.longPressTimer);
  if (state.drag) {
    const candidate = state.drag.candidate;
    endDragVisuals();
    if (candidate) {
      invoke('CompleteDrag', candidate.slotId);
    } else {
      invoke('CancelDrag');
    }
    state.drag = null;
  } else if (state.press?.viaHandle === false && event.pointerType === 'touch') {
    // A touch tap (long-press never fired): treat as a click.
    invoke('ReportClick', state.press.nodeId);
  }
  state.press = null;
}

function onPointerCancel() {
  clearTimeout(state.longPressTimer);
  state.press = null;
  if (state.drag) {
    endDragVisuals();
    state.drag = null;
    invoke('CancelDrag');
  }
}

function onDoubleClick(event) {
  if (state.edit) {
    return;
  }
  const el = effectiveNode(event.target.closest?.(NODE_SELECTOR));
  if (el) {
    invoke('ReportDoubleClick', el.getAttribute('data-node-id'));
  }
}

// --------------------------------------------------------------------- keyboard

const OWNED_KEYS = new Set(['Escape', 'Delete', 'Backspace', 'Enter', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', '/']);

function onKeyDown(event) {
  if (state.edit) {
    if (event.key === 'Escape') {
      event.preventDefault();
      finishEdit(false);
    }
    if (event.key === 'Enter' && state.edit?.mode === 'plain') {
      event.preventDefault();
      finishEdit(true);
    }
    return; // everything else belongs to the text
  }

  if (state.drag && event.key === 'Escape') {
    event.preventDefault();
    endDragVisuals();
    state.drag = null;
    state.press = null;
    invoke('CancelDrag');
    return;
  }

  // Only intercept when the canvas is the active surface — panels keep their keys.
  if (!state.canvas.contains(document.activeElement) && document.activeElement !== document.body) {
    return;
  }
  const owned = OWNED_KEYS.has(event.key)
    || ((event.ctrlKey || event.metaKey) && ['d', 'z', 'y'].includes(event.key.toLowerCase()));
  if (owned) {
    event.preventDefault();
    invoke('ReportKey', event.key, event.ctrlKey || event.metaKey, event.altKey, event.shiftKey);
  }
}

// ----------------------------------------------------------------- drag & drop

async function startDrag(press) {
  state.press = null;
  const plan = await invoke('BeginDrag', press.nodeId);
  if (!plan || !plan.slots?.length) {
    return; // nowhere to go (e.g. the only section on the page)
  }
  state.drag = { nodeId: press.nodeId, plan, candidate: null, geoms: null };
  state.ui.ghost.textContent = plan.dragLabel ?? 'Move';
  state.ui.ghost.hidden = false;
  state.canvas.classList.add('ed-dragging');
  setHover(null);
  hideGapPill();
  autoScrollLoop();
}

function slotGeometries() {
  // Anchor rects move under auto-scroll, so geometry is recomputed per frame from
  // live elements; with slot counts in the dozens this is comfortably cheap.
  const geoms = [];
  for (const slot of state.drag.plan.slots) {
    const anchor = elOf(slot.anchorId);
    if (!anchor) {
      continue;
    }
    const rect = anchor.getBoundingClientRect();
    if (slot.edge === 'into') {
      geoms.push({ slot, kind: 'into', rect, x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 });
    } else if (slot.orientation === 'h') {
      const x = slot.edge === 'before' ? rect.left : rect.right;
      geoms.push({ slot, kind: 'line-v', x, y1: rect.top, y2: rect.bottom, y: (rect.top + rect.bottom) / 2 });
    } else {
      const y = slot.edge === 'before' ? rect.top : rect.bottom;
      geoms.push({ slot, kind: 'line-h', y, x1: rect.left, x2: rect.right, x: (rect.left + rect.right) / 2 });
    }
  }
  return geoms;
}

function distanceTo(geom, x, y) {
  if (geom.kind === 'into') {
    const dx = Math.max(geom.rect.left - x, 0, x - geom.rect.right);
    const dy = Math.max(geom.rect.top - y, 0, y - geom.rect.bottom);
    return Math.hypot(dx, dy) + 2; // slight bias toward explicit edges
  }
  if (geom.kind === 'line-v') {
    const dy = Math.max(geom.y1 - y, 0, y - geom.y2);
    return Math.hypot(x - geom.x, dy);
  }
  const dx = Math.max(geom.x1 - x, 0, x - geom.x2);
  return Math.hypot(y - geom.y, dx);
}

function trackDrag(x, y) {
  const ghost = state.ui.ghost;
  ghost.style.transform = `translate(${x + 14}px, ${y + 10}px)`;
  state.lastPointer = { x, y };

  const geoms = slotGeometries();
  let best = null;
  let bestDist = Infinity;
  for (const geom of geoms) {
    const dist = distanceTo(geom, x, y);
    if (dist < bestDist) {
      best = geom;
      bestDist = dist;
    }
  }

  const current = state.drag.candidate;
  if (current && best && current.slotId !== best.slot.slotId) {
    const currentGeom = geoms.find(g => g.slot.slotId === current.slotId);
    if (currentGeom && distanceTo(currentGeom, x, y) - INDICATOR_HYSTERESIS <= bestDist) {
      return; // sticky: don't flicker between near-equal slots
    }
  }

  state.drag.candidate = best?.slot ?? null;
  drawDropVisuals(best);
}

function drawDropVisuals(geom) {
  const { indicator, into, target } = state.ui;
  const base = state.overlay.getBoundingClientRect();
  indicator.hidden = into.hidden = target.hidden = true;

  if (!geom) {
    return;
  }
  if (geom.kind === 'into') {
    place(into, geom.rect, base, 4);
    into.hidden = false;
  } else if (geom.kind === 'line-h') {
    into.hidden = true;
    indicator.className = 'ed-ov ed-ov-indicator ed-h';
    Object.assign(indicator.style, {
      left: `${geom.x1 - base.left}px`, top: `${geom.y - base.top - 1}px`,
      width: `${geom.x2 - geom.x1}px`, height: '2px',
    });
    indicator.hidden = false;
  } else {
    indicator.className = 'ed-ov ed-ov-indicator ed-v';
    Object.assign(indicator.style, {
      left: `${geom.x - base.left - 1}px`, top: `${geom.y1 - base.top}px`,
      width: '2px', height: `${geom.y2 - geom.y1}px`,
    });
    indicator.hidden = false;
  }

  const parentEl = elOf(geom.slot.parentId) ?? (geom.slot.parentId === '' ? state.canvas : null);
  if (parentEl) {
    place(target, parentEl.getBoundingClientRect(), base, 0);
    target.hidden = false;
  }
}

function endDragVisuals() {
  state.ui.ghost.hidden = true;
  state.ui.indicator.hidden = true;
  state.ui.into.hidden = true;
  state.ui.target.hidden = true;
  state.canvas.classList.remove('ed-dragging');
}

function autoScrollLoop() {
  if (!state?.drag) {
    return;
  }
  const pointer = state.lastPointer;
  if (pointer) {
    const rect = state.scroller.getBoundingClientRect();
    let speed = 0;
    if (pointer.y < rect.top + EDGE_SCROLL_ZONE) {
      speed = -EDGE_SCROLL_MAX * (1 - (pointer.y - rect.top) / EDGE_SCROLL_ZONE);
    } else if (pointer.y > rect.bottom - EDGE_SCROLL_ZONE) {
      speed = EDGE_SCROLL_MAX * (1 - (rect.bottom - pointer.y) / EDGE_SCROLL_ZONE);
    }
    if (speed !== 0) {
      state.scroller.scrollTop += speed;
      trackDrag(pointer.x, pointer.y); // rects moved under the pointer
    }
  }
  state.rafId = requestAnimationFrame(autoScrollLoop);
}

// ---------------------------------------------------------------- gap affordance

function trackGap(x, y) {
  const el = nodeElFromPoint(x, y);
  const container = containerFor(el);
  if (!container) {
    hideGapPill();
    return;
  }

  const children = [...container.el.children].filter(child => child.matches?.(NODE_SELECTOR));
  const boundaries = [];
  if (children.length === 0) {
    const rect = container.el.getBoundingClientRect();
    boundaries.push({ index: 0, x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 });
  } else {
    const horizontal = isHorizontalFlow(children);
    children.forEach((child, i) => {
      const rect = child.getBoundingClientRect();
      boundaries.push(horizontal
        ? { index: i, x: rect.left, y: rect.top + rect.height / 2 }
        : { index: i, x: rect.left + rect.width / 2, y: rect.top });
      if (i === children.length - 1) {
        boundaries.push(horizontal
          ? { index: i + 1, x: rect.right, y: rect.top + rect.height / 2 }
          : { index: i + 1, x: rect.left + rect.width / 2, y: rect.bottom });
      }
    });
  }

  let best = null;
  let bestDist = Infinity;
  for (const boundary of boundaries) {
    const dist = Math.hypot(boundary.x - x, boundary.y - y);
    if (dist < bestDist) {
      best = boundary;
      bestDist = dist;
    }
  }

  if (best && bestDist <= Math.max(GAP_THRESHOLD * 4, 28)) {
    showGapPill(container.parentId, best);
  } else {
    hideGapPill();
  }
}

/** The container whose child list a hovered point inserts into. */
function containerFor(el) {
  if (!el) {
    return null; // outside all nodes: no pill (root gaps appear when hovering sections)
  }
  const type = el.getAttribute('data-node-type');
  if (['stack', 'grid'].includes(type)) {
    return { el, parentId: el.getAttribute('data-node-id') };
  }
  if (type === 'section') {
    // Hovering a section near its top/bottom edge means "between sections" (root),
    // otherwise inside the section itself.
    const rect = el.getBoundingClientRect();
    const nearEdge = Math.min(Math.abs(state.lastY - rect.top), Math.abs(state.lastY - rect.bottom)) < GAP_THRESHOLD * 2;
    return nearEdge
      ? { el: el.parentElement, parentId: '' }
      : { el, parentId: el.getAttribute('data-node-id') };
  }
  const parent = el.parentElement?.closest(NODE_SELECTOR);
  return parent ? containerFor(parent) : { el: el.parentElement ?? state.canvas, parentId: '' };
}

function isHorizontalFlow(children) {
  if (children.length < 2) {
    return false;
  }
  const a = children[0].getBoundingClientRect();
  const b = children[1].getBoundingClientRect();
  return b.left >= a.right - 1;
}

function showGapPill(parentId, boundary) {
  state.gap = { parentId, index: boundary.index };
  const base = state.overlay.getBoundingClientRect();
  const pill = state.ui.gapPill;
  pill.style.left = `${boundary.x - base.left}px`;
  pill.style.top = `${boundary.y - base.top}px`;
  pill.hidden = false;
}

function hideGapPill() {
  state.gap = null;
  state.ui.gapPill.hidden = true;
}

// ----------------------------------------------------------------- inline edit

export function enterInlineEdit(nodeId, mode) {
  finishEdit(true);
  const root = elOf(nodeId);
  if (!root) {
    return;
  }
  // The text surface is the node root itself (heading/prose) — views keep text
  // as the root element's direct content by contract.
  const el = root;
  state.edit = { nodeId, el, mode, original: el.innerHTML, debounce: 0 };
  el.setAttribute('contenteditable', mode === 'plain' ? 'plaintext-only' : 'true');
  el.classList.add('ed-editing');
  el.focus();
  placeCaretAtEnd(el);

  const opts = { signal: state.aborter.signal };
  el.addEventListener('input', onEditInput, opts);
  el.addEventListener('blur', onEditBlur, opts);
  if (mode === 'rich') {
    window.imprintRichText?.showToolbarFor(el);
  }
  setSelection(nodeId); // keep the ring on the edited node
}

function onEditInput() {
  clearTimeout(state.edit.debounce);
  state.edit.debounce = setTimeout(() => {
    if (state.edit) {
      invoke('CommitText', state.edit.nodeId, currentEditValue());
    }
  }, 800);
}

function onEditBlur() {
  // Clicking the floating toolbar blurs the text; that must not end the session.
  setTimeout(() => {
    if (state?.edit && !state.edit.el.contains(document.activeElement)
        && !document.activeElement?.closest('.ed-richbar')) {
      finishEdit(true);
    }
  }, 0);
}

function finishEdit(commit) {
  const edit = state?.edit;
  if (!edit) {
    return;
  }
  clearTimeout(edit.debounce);
  state.edit = null;
  const value = commit ? currentEditValueFor(edit) : '';
  if (!commit) {
    edit.el.innerHTML = edit.original;
  }
  edit.el.removeAttribute('contenteditable');
  edit.el.classList.remove('ed-editing');
  edit.el.removeEventListener('input', onEditInput);
  edit.el.removeEventListener('blur', onEditBlur);
  window.imprintRichText?.hideToolbar();
  invoke('EndInlineEdit', edit.nodeId, commit, value);
}

function currentEditValue() {
  return currentEditValueFor(state.edit);
}

function currentEditValueFor(edit) {
  if (edit.mode === 'plain') {
    return edit.el.innerText.replace(/\s+/g, ' ').trim();
  }
  return window.imprintRichText.normalize(edit.el);
}

function placeCaretAtEnd(el) {
  const range = document.createRange();
  range.selectNodeContents(el);
  range.collapse(false);
  const selection = getSelection();
  selection.removeAllRanges();
  selection.addRange(range);
}

// ------------------------------------------------------------ selection overlay

export function setSelection(nodeId) {
  if (!state) {
    return;
  }
  state.selectionId = nodeId;
  scheduleSync();
}

function setHover(nodeId) {
  if (state.hoverId === nodeId) {
    return;
  }
  state.hoverId = nodeId;
  scheduleSync();
}

let syncQueued = false;
function scheduleSync() {
  if (syncQueued || !state) {
    return;
  }
  syncQueued = true;
  requestAnimationFrame(() => {
    syncQueued = false;
    sync();
  });
}

function sync() {
  if (!state) {
    return;
  }
  const base = state.overlay.getBoundingClientRect();

  const hoverEl = state.hoverId && state.hoverId !== state.selectionId ? elOf(state.hoverId) : null;
  if (hoverEl) {
    place(state.ui.hover, hoverEl.getBoundingClientRect(), base, 0);
    state.ui.hoverChip.textContent = prettyType(hoverEl.getAttribute('data-node-type'));
    state.ui.hover.hidden = false;
  } else {
    state.ui.hover.hidden = true;
  }

  const selEl = state.selectionId ? elOf(state.selectionId) : null;
  if (selEl) {
    const rect = selEl.getBoundingClientRect();
    place(state.ui.selection, rect, base, 0);
    state.ui.selection.classList.toggle('ed-is-editing', !!state.edit);
    state.ui.selection.hidden = false;
    invoke('ReportSelectionRect', rect.left - base.left, rect.top - base.top, rect.width, rect.height);
  } else {
    state.ui.selection.hidden = true;
  }
}

function place(el, rect, base, inset) {
  Object.assign(el.style, {
    left: `${rect.left - base.left + inset}px`,
    top: `${rect.top - base.top + inset}px`,
    width: `${rect.width - inset * 2}px`,
    height: `${rect.height - inset * 2}px`,
  });
}

function prettyType(type) {
  const names = {
    section: 'Section', stack: 'Stack', columns: 'Columns', grid: 'Grid',
    heading: 'Heading', richtext: 'Text', button: 'Button', image: 'Image',
    video: 'Video', svg: 'Graphic', divider: 'Divider', spacer: 'Spacer',
    widget: 'Widget', 'block-instance': 'Block',
  };
  return names[type] ?? type ?? '';
}

// ------------------------------------------------------------------- observers

function observe() {
  state.mutations = new MutationObserver(() => {
    // Blazor re-rendered the canvas: overlay geometry is stale; the edited node, if
    // any, survives because the canvas pauses rendering during inline edits.
    scheduleSync();
  });
  state.mutations.observe(state.canvas, { childList: true, subtree: true, characterData: true });

  state.resizes = new ResizeObserver(scheduleSync);
  state.resizes.observe(state.canvas);

  // Track the last pointer position for gap/section-edge decisions.
  window.addEventListener('pointermove', event => {
    if (state) {
      state.lastY = event.clientY;
    }
  }, { signal: state.aborter.signal, passive: true });
}
