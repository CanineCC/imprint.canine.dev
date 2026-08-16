// Imprint editor — caret position for the post source textarea.
//
// Deliberately tiny, and deliberately NOT a text mutator: it reports where the caret is
// and puts it back afterwards, while the splice itself happens in C# on the bound value.
// Writing .value from JS would leave Blazor's binding holding the old string, and the next
// render would silently throw the insert away.

/**
 * The caret offset in a textarea, or the end of its text when it has never been focused.
 * @param {HTMLTextAreaElement} el
 * @returns {number}
 */
export function caretOf(el) {
  if (!el) {
    return 0;
  }

  return typeof el.selectionStart === 'number' ? el.selectionStart : el.value.length;
}

/**
 * Restores focus and drops the caret at `pos` — after an insert that is the end of the
 * text just added, so typing continues where the author was rather than at the top.
 * @param {HTMLTextAreaElement} el
 * @param {number} pos
 */
export function focusAt(el, pos) {
  if (!el) {
    return;
  }

  el.focus();
  const clamped = Math.max(0, Math.min(pos, el.value.length));
  el.setSelectionRange(clamped, clamped);
}
