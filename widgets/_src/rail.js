// The section rail, once.
//
// A sheet of sections reads as ONE page only when every section is introduced the same way: a
// narrow left column carrying the section's label — "§1 POPULATION" — and its (i), with the
// section's content taking the whole of the rest. The alternative is what §4 and §5 of the
// corpus sheet did before this module: a big display <h2> over content centred in a measure of
// its own, which is a second look-and-feel on the same page.
//
// It is a module rather than a copy in each island for the reason hint.js gives for the (i):
// two copies mean two look-and-feels, and they diverge the first time one of them is touched.
// These two must be pixel-consistent — on the corpus sheet they sit directly above and below
// cai-figure-band and cai-share-bars.
//
// THE KICKER TYPE IS cai-figure-band's `.fb-cap`, VALUE FOR VALUE: 10.5px, .16em tracking,
// uppercase, 700, --muted. It is copied rather than imported because that island keeps its own
// stylesheet; the rail.test.mjs suite renders both and compares the COMPUTED values, so a drift
// here fails a test instead of only looking slightly wrong.
//
// A CALLER MUST ALSO INCLUDE `HINT_CSS`. The rail styles the (i)'s ANCHORING and nothing else —
// the dot, the tip and their theming belong to hint.js, and both current callers already carry
// it for their own tips.
//
// THE KICKER'S TIP HAS THE WHOLE RAIL AS ITS SUBJECT. hint.js explains why an absolutely
// positioned tip needs one: even hidden, it counts toward the document's scroll width, and a
// 320px tip anchored to a dot inside a 112px column is 200px of page-widening nobody sees until
// a phone scrolls sideways. Spanning the rail makes the tip at most as wide as the island.
//
// ★ THOSE THREE RULES ARE SCOPED TO `.rail-side`, NOT TO `.rail`. Scoped to the rail they also
// match every (i) in the CONTENT column — a findings figure's, a language row's, a link card's —
// and silently replace the subject each of those islands chose for its own tips. It is not
// hypothetical: with `.rail .info-hint` a card's (i) went from anchoring to its own right edge
// to anchoring to the island, and nothing failed except the look of it. The kicker's (i) is the
// only one the rail owns.

import { escapeHtml, renderInline } from "./tokens.js";
import { hintHtml } from "./hint.js";

// 112px is the measure of the label, not a fraction of the page: it holds "§1 POPULATION" on
// one line at the kicker's own size and does not grow when the column does, because a section
// label that reflowed with the viewport would stop lining up with the section above it.
export const RAIL_CSS = `
.rail { display: grid; grid-template-columns: 112px minmax(0, 1fr); gap: 0 24px;
  align-items: start; position: relative; }
.rail-side { display: flex; align-items: flex-start; padding-top: 3px; min-width: 0; }
.rail-kicker { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
  color: var(--muted); font-weight: 700; overflow-wrap: anywhere; }
.rail-body { min-width: 0; }
/* The KICKER's (i) — and only that one — takes the rail as its subject, so its tip can never
   reach past the island. Every other (i) inside keeps the subject its own island gave it. */
.rail-side .info-hint { position: static; }
.rail-side .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
/* A tip opens across the content beside it, which would otherwise paint over it. */
.rail:hover, .rail:focus-within { z-index: 2; }
/* The label takes its own line under 560px — the same width at which cai-share-bars reflows,
   so two adjacent sections never disagree about when a page has become narrow. A 112px column
   plus a 24px gutter is a third of a 400px screen spent on two words. */
@media (max-width: 560px) {
  .rail { grid-template-columns: minmax(0, 1fr); gap: 10px; }
  .rail-side { padding-top: 0; }
}
`;

/**
 * The rail: a kicker (and optionally the shared (i)) beside a column of content.
 *
 * @param {object} o
 * @param {string} o.kicker   the section label, e.g. "§1 Population"
 * @param {string} [o.tip]    the (i)'s body; absent renders no dot at all (hint.js)
 * @param {string} o.content  the section's own HTML, which takes the whole right column
 */
export function railHtml({ kicker, tip, content }) {
  return (
    `<div class="rail">` +
    `<div class="rail-side">` +
    `<span class="rail-kicker">${escapeHtml(kicker)}</span>` +
    hintHtml(tip, { label: kicker }) +
    `</div>` +
    `<div class="rail-body">${content}</div>` +
    `</div>`
  );
}

/**
 * The heading and lede an island would normally put in its section head, WITHOUT the kicker —
 * which the rail is now carrying.
 *
 * A section in the rail has no display heading unless the page asks for one: the kicker names
 * the section, and an <h2> saying the same words twice at 2.1rem is the very thing the rail
 * replaced. So no heading attribute means no <h2> — not an empty one, and not the kicker
 * promoted into one.
 */
export function headBelowRailHtml(host) {
  const heading = host.getAttribute("heading");
  const lede = host.getAttribute("lede");
  if (!heading && !lede) return "";
  let h = '<div class="mk-section-head">';
  if (heading) h += `<h2>${renderInline(heading)}</h2>`;
  if (lede) h += `<p>${renderInline(lede)}</p>`;
  h += "</div>";
  return h;
}
