# Known issues

_None open._

## Resolved

### Inline-edit commits intermittently lost (fixed)

**Symptom.** In full `Imprint.E2E` batch runs, a rich-text edit committed by *blur*
(clicking away) sometimes didn't persist: the live canvas showed the edit, no error
surfaced, but no `page.text-changed` event reached the store. Reload showed the
original text.

**Root cause.** The contenteditable blur handler in `canvas-interop.js` deferred its
commit through `setTimeout(0)` and then probed `document.activeElement` to decide
whether focus had really left the editor. That macrotask could be starved — when the
same click drove a concurrent Blazor Server round-trip (e.g. the breadcrumb's
`Session.Select(null)`), the timer callback never ran, so `finishEdit` never fired and
the commit was silently dropped. It reproduced under test-suite load but was a genuine
latent bug: any real user whose blur coincided with a server round-trip could lose an
edit.

**Fix.** Commit synchronously from the blur event's `relatedTarget` (the element
gaining focus) instead of a deferred `activeElement` probe — no timer, no race. Focus
moving into the rich-text toolbar or link popover is recognized as part of the same
session and does not commit; everything else does. The full E2E suite is deterministically
green afterward (7/7 across repeated runs) and ~3× faster (the dropped-commit path had
been burning 10s selector timeouts).
