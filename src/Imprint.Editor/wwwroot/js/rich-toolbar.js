// Rich text editing support: the floating mark toolbar and — more importantly — the
// normalizer that rebuilds contenteditable output as the canonical inline subset
// (docs/domain-model.md §2.2). The server's CanonicalHtml validator REJECTS anything
// non-canonical; this normalizer is UX, the validator is the guarantee, and the two
// must agree — the E2E suite feeds adversarial paste content through both.

const ENTITY = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
const MAX_INLINE_DEPTH = 6;

function escapeText(text) {
  return text.replace(/\u00a0/g, ' ').replace(/[&<>"']/g, ch => ENTITY[ch]);
}

function isAllowedHref(href) {
  const value = href.trim();
  if (/^https?:\/\/./i.test(value)) {
    return !value.includes(' ');
  }
  if (/^mailto:./i.test(value)) {
    return true;
  }
  return /^page:[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$/i.test(value);
}

/** Serializes the inline content of one block-level container. */
function serializeInline(container, allowAnchor, depth, marks) {
  let out = '';
  for (const node of container.childNodes) {
    if (node.nodeType === Node.TEXT_NODE) {
      out += escapeText(node.data.replace(/\s+/g, ' '));
      continue;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) {
      continue;
    }
    const tag = node.tagName.toLowerCase();
    if (tag === 'br') {
      out += '<br>';
      continue;
    }
    if ((tag === 'strong' || tag === 'b') && depth < MAX_INLINE_DEPTH && !marks.has('strong')) {
      marks.add('strong');
      const inner = serializeInline(node, allowAnchor, depth + 1, marks);
      marks.delete('strong');
      out += inner === '' ? '' : `<strong>${inner}</strong>`;
      continue;
    }
    if ((tag === 'em' || tag === 'i') && depth < MAX_INLINE_DEPTH && !marks.has('em')) {
      marks.add('em');
      const inner = serializeInline(node, allowAnchor, depth + 1, marks);
      marks.delete('em');
      out += inner === '' ? '' : `<em>${inner}</em>`;
      continue;
    }
    if (tag === 'a' && allowAnchor && depth < MAX_INLINE_DEPTH) {
      const href = node.getAttribute('href') ?? '';
      const inner = serializeInline(node, false, depth + 1, marks);
      if (isAllowedHref(href) && inner !== '') {
        out += `<a href="${escapeText(href.trim())}">${inner}</a>`;
      } else {
        out += inner; // broken/disallowed link: keep the words, lose the link
      }
      continue;
    }
    // Every other element — spans, fonts, styling soup from paste — unwraps.
    out += serializeInline(node, allowAnchor, depth, marks);
  }
  return out;
}

function isBlank(inlineHtml) {
  return inlineHtml.replace(/<br>/g, '').trim() === '';
}

function trimBlock(inlineHtml) {
  // Leading/trailing breaks and spaces around a block add nothing but diff noise.
  return inlineHtml.replace(/^(?:\s|<br>)+/, '').replace(/(?:\s|<br>)+$/, '');
}

/** Rebuilds a contenteditable DOM as canonical subset markup. Pure; unit-tested via E2E. */
export function normalize(root) {
  const blocks = [];
  let pendingInline = '';

  const flushPending = () => {
    const trimmed = trimBlock(pendingInline);
    if (!isBlank(trimmed)) {
      blocks.push(`<p>${trimmed}</p>`);
    }
    pendingInline = '';
  };

  const walk = nodes => {
    for (const node of nodes) {
      if (node.nodeType === Node.TEXT_NODE) {
        pendingInline += escapeText(node.data.replace(/\s+/g, ' '));
        continue;
      }
      if (node.nodeType !== Node.ELEMENT_NODE) {
        continue;
      }
      const tag = node.tagName.toLowerCase();
      if (tag === 'ul' || tag === 'ol') {
        flushPending();
        const items = [...node.children]
          .filter(child => child.tagName.toLowerCase() === 'li')
          .map(li => trimBlock(serializeInline(li, true, 0, new Set())))
          .filter(item => !isBlank(item));
        if (items.length > 0) {
          blocks.push(`<${tag}>${items.map(item => `<li>${item}</li>`).join('')}</${tag}>`);
        }
        continue;
      }
      if (['p', 'div', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'li'].includes(tag)) {
        flushPending();
        // Nested block structure (divs in divs from paste) flattens to paragraphs.
        if ([...node.children].some(child => /^(p|div|ul|ol|h[1-6]|blockquote|pre)$/i.test(child.tagName))) {
          walk(node.childNodes);
          flushPending();
        } else {
          const inline = trimBlock(serializeInline(node, true, 0, new Set()));
          if (!isBlank(inline)) {
            blocks.push(`<p>${inline}</p>`);
          }
        }
        continue;
      }
      if (tag === 'br') {
        pendingInline += '<br>';
        continue;
      }
      // Inline element at block level: accumulate into the pending paragraph.
      pendingInline += serializeInline({ childNodes: [node] }, true, 0, new Set());
    }
  };

  walk(root.childNodes);
  flushPending();
  return blocks.join('');
}

// --------------------------------------------------------------- the toolbar

let bar = null;
let attachedTo = null;
let linkPopover = null;

function buildBar() {
  bar = document.createElement('div');
  bar.className = 'ed-richbar';
  bar.hidden = true;
  const buttons = [
    ['strong', '<strong>B</strong>', 'Bold (Ctrl+B)'],
    ['em', '<em>I</em>', 'Italic (Ctrl+I)'],
    ['link', '&#128279;', 'Link (Ctrl+K)'],
    ['ul', '&bull;&ndash;', 'Bullet list'],
    ['ol', '1.', 'Numbered list'],
  ];
  for (const [command, html, label] of buttons) {
    const button = document.createElement('button');
    button.type = 'button';
    button.innerHTML = html;
    button.title = label;
    button.setAttribute('aria-label', label);
    // pointerdown, not click: click would land after the blur we're trying to avoid.
    button.addEventListener('pointerdown', event => {
      event.preventDefault();
      apply(command);
    });
    bar.append(button);
  }
  document.body.append(bar);
}

export function showToolbarFor(el) {
  if (!bar) {
    buildBar();
  }
  attachedTo = el;
  el.addEventListener('keydown', onEditKeydown);
  document.addEventListener('selectionchange', reposition);
  reposition();
}

export function hideToolbar() {
  attachedTo?.removeEventListener('keydown', onEditKeydown);
  document.removeEventListener('selectionchange', reposition);
  attachedTo = null;
  if (bar) {
    bar.hidden = true;
  }
  closeLinkPopover();
}

function onEditKeydown(event) {
  if (!(event.ctrlKey || event.metaKey)) {
    return;
  }
  const command = { b: 'strong', i: 'em', k: 'link' }[event.key.toLowerCase()];
  if (command) {
    event.preventDefault();
    apply(command);
  }
}

function currentRange() {
  const selection = getSelection();
  if (!selection || selection.rangeCount === 0) {
    return null;
  }
  const range = selection.getRangeAt(0);
  return attachedTo && attachedTo.contains(range.commonAncestorContainer) ? range : null;
}

function reposition() {
  const range = currentRange();
  if (!range || range.collapsed) {
    bar.hidden = true;
    return;
  }
  const rect = range.getBoundingClientRect();
  bar.hidden = false;
  bar.style.left = `${Math.max(8, rect.left + rect.width / 2 - bar.offsetWidth / 2)}px`;
  bar.style.top = `${Math.max(8, rect.top - bar.offsetHeight - 8)}px`;
}

function apply(command) {
  const range = currentRange();
  if (!range) {
    return;
  }
  if (command === 'strong' || command === 'em') {
    toggleMark(range, command);
  } else if (command === 'link') {
    openLinkPopover(range);
  } else {
    toggleList(range, command);
  }
  attachedTo.dispatchEvent(new Event('input', { bubbles: true })); // wake the autosave
}

function markAncestor(node, tagNames) {
  for (let current = node; current && current !== attachedTo; current = current.parentNode) {
    if (current.nodeType === Node.ELEMENT_NODE && tagNames.includes(current.tagName.toLowerCase())) {
      return current;
    }
  }
  return null;
}

function toggleMark(range, mark) {
  const tags = mark === 'strong' ? ['strong', 'b'] : ['em', 'i'];
  const existing = markAncestor(range.commonAncestorContainer, tags);
  if (existing) {
    existing.replaceWith(...existing.childNodes); // unwrap; normalize() heals the rest
    return;
  }
  const wrapper = document.createElement(mark);
  wrapper.append(range.extractContents());
  range.insertNode(wrapper);
  getSelection().selectAllChildren(wrapper);
}

function toggleList(range, kind) {
  const block = markAncestor(range.startContainer, ['p', 'div', 'li'])
    ?? (range.startContainer === attachedTo ? null : range.startContainer.parentElement);
  if (!block || block === attachedTo) {
    return;
  }
  if (block.tagName.toLowerCase() === 'li') {
    const list = block.parentElement;
    const paragraph = document.createElement('p');
    paragraph.append(...block.childNodes);
    if (list.children.length === 1) {
      list.replaceWith(paragraph);
    } else {
      list.parentNode.insertBefore(paragraph, list.nextSibling);
      block.remove();
    }
    getSelection().selectAllChildren(paragraph);
    return;
  }
  const list = document.createElement(kind);
  const item = document.createElement('li');
  item.append(...block.childNodes);
  list.append(item);
  block.replaceWith(list);
  getSelection().selectAllChildren(item);
}

function openLinkPopover(range) {
  closeLinkPopover();
  const existing = markAncestor(range.commonAncestorContainer, ['a']);

  linkPopover = document.createElement('div');
  linkPopover.className = 'ed-linkpop';
  linkPopover.innerHTML = `
    <input type="text" placeholder="https://… or page id" spellcheck="false" />
    <button type="button" data-act="ok">Apply</button>
    <button type="button" data-act="clear" ${existing ? '' : 'hidden'}>Remove</button>`;
  document.body.append(linkPopover);

  const rect = range.getBoundingClientRect();
  linkPopover.style.left = `${Math.max(8, rect.left)}px`;
  linkPopover.style.top = `${rect.bottom + 6}px`;

  const input = linkPopover.querySelector('input');
  input.value = existing?.getAttribute('href') ?? '';
  const saved = range.cloneRange();

  linkPopover.addEventListener('pointerdown', event => event.stopPropagation());
  linkPopover.querySelector('[data-act=ok]').addEventListener('click', () => {
    const href = input.value.trim();
    restore(saved);
    if (href && isAllowedHref(normalizeHref(href))) {
      if (existing) {
        existing.setAttribute('href', normalizeHref(href));
      } else {
        const anchor = document.createElement('a');
        anchor.setAttribute('href', normalizeHref(href));
        anchor.append(saved.extractContents());
        saved.insertNode(anchor);
      }
      attachedTo.dispatchEvent(new Event('input', { bubbles: true }));
    }
    closeLinkPopover();
  });
  linkPopover.querySelector('[data-act=clear]').addEventListener('click', () => {
    existing?.replaceWith(...existing.childNodes);
    attachedTo.dispatchEvent(new Event('input', { bubbles: true }));
    closeLinkPopover();
  });
  input.addEventListener('keydown', event => {
    if (event.key === 'Enter') {
      event.preventDefault();
      linkPopover.querySelector('[data-act=ok]').click();
    }
    if (event.key === 'Escape') {
      closeLinkPopover();
    }
  });
  input.focus();

  function restore(savedRange) {
    const selection = getSelection();
    selection.removeAllRanges();
    selection.addRange(savedRange);
  }
}

function normalizeHref(href) {
  // Bare domains become https; everything else passes through the allowlist as-is.
  return /^(https?:|mailto:|page:)/i.test(href) ? href : `https://${href}`;
}

function closeLinkPopover() {
  linkPopover?.remove();
  linkPopover = null;
}

// canvas-interop reaches these through the window (no module graph between them).
window.imprintRichText = { normalize, showToolbarFor, hideToolbar, isAllowedHref };
