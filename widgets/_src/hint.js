// The product's ONE (i), ported for the shadow-DOM islands.
//
// Kennel.Ui/Components/InfoHint.razor says it in its own comment: "This is the ONE (i) in the
// product. Do not hand-roll another disclosure for the same job — a second idiom means two
// look-and-feels, two keyboard behaviours and two a11y contracts." Three islands need it
// (cai-figure-band, cai-share-bars, cai-link-cards), so it is ONE module they all import
// rather than three copies that will drift apart the first time one of them is touched.
//
// The rules below are the `.info-hint` block of Kennel.Ui/wwwroot/app.css, value for value,
// with exactly three deliberate differences — each one because a shadow root inside a
// syndicated page is not the product's own page:
//
//   1. TOKEN NAMES. app.css paints the tip `background: var(--panel)`; the island token
//      vocabulary (tokens.js / imprint-marketing.css) has no --panel — its raised-surface
//      role is `--surface`. app.css hard-codes `box-shadow: 0 8px 26px rgba(0,0,0,.18)`;
//      the island vocabulary carries that role as `--shadow-overlay`, which is already
//      theme-aware. Same two roles, the names this side of the boundary uses.
//   2. A NARROW-VIEWPORT CLAMP. app.css can afford a flat `max-width: 320px` because its
//      tips hang off a full-width page. An island is often much narrower than the viewport,
//      and a 320px tip inside it overflows the page at 400px — the one thing these pages
//      must never do. `min(320px, calc(100vw - 32px))` keeps the 320px reading measure
//      wherever there is room and gives up width, not the page's horizontal axis, when
//      there is not.
//   3. A FIRST-PARAGRAPH RESET. app.css's tips are Razor fragments whose first line is bare
//      text followed by <p> siblings, so `> p { margin: 4px 0 0 }` reads as a separator.
//      Here the tip arrives as ONE string and every chunk becomes a <p>, so the first one
//      would open with a 4px gap it does not need. `> p:first-child { margin-top: 0 }`.
//
// Everything else — the 15px circle, the Georgia italic "i", the accent on hover AND focus,
// the reveal on :hover/:focus/:focus-within, the hint-right flip, the reduced-motion opt-out
// — is app.css unchanged. Keyboard reach is the `tabindex="0"` plus :focus-within, with no
// script at all: a tip that needed JavaScript would be a second idiom by another name.

import { escapeHtml, renderInline } from "./tokens.js";

export const HINT_CSS = `
.info-hint { position: relative; display: inline-flex; align-items: center; vertical-align: middle; margin-left: 6px; cursor: help; }
.info-hint:focus { outline: none; }
.info-hint-dot { width: 15px; height: 15px; border-radius: 50%; border: 1px solid var(--border-strong); color: var(--muted);
  font: italic 700 10px/1 Georgia, "Times New Roman", serif; display: grid; place-items: center; }
.info-hint:hover .info-hint-dot, .info-hint:focus .info-hint-dot { border-color: var(--accent); color: var(--accent); }
.info-hint-tip { position: absolute; left: 0; top: calc(100% + 8px); z-index: 60; width: max-content;
  max-width: min(320px, calc(100vw - 32px));
  background: var(--surface); color: var(--muted); border: 1px solid var(--border-strong); border-radius: 8px;
  box-shadow: var(--shadow-overlay); padding: 10px 12px; font-size: var(--fs-sm); font-weight: 400; line-height: 1.5;
  white-space: normal; text-align: left; opacity: 0; visibility: hidden; transform: translateY(-3px);
  transition: opacity .12s ease, transform .12s ease; pointer-events: none; }
.info-hint-tip a { pointer-events: auto; }
/* Paragraphs inside a tip sit tight — the UA's 1em block margins read as gaps in a small pop. */
.info-hint-tip > p { margin: 4px 0 0; }
.info-hint-tip > p:first-child { margin-top: 0; }
.info-hint-tip > p:last-child { margin-bottom: 0; }
.info-hint:hover .info-hint-tip, .info-hint:focus .info-hint-tip, .info-hint:focus-within .info-hint-tip {
  opacity: 1; visibility: visible; transform: none; pointer-events: auto; }
/* Flip to the right edge when the dot sits at the end of a row or the last column of a grid —
   a tip anchored left:0 there hangs off the island, and in the last column off the page. */
.info-hint.hint-right .info-hint-tip { left: auto; right: 0; }

/* ── Anchoring the tip to its SUBJECT rather than its dot ────────────────────────────────────
   app.css carries a third variant, .hint-card, for the case neither left:0 nor right:0 can
   solve, and its comment states the reason: "the dot sits at the card's right edge, so any tip
   wider than the dot's offset overhangs the card's LEFT edge and, in the first column, the
   viewport. Spanning the card makes the tip exactly as wide as its subject, so it can never
   overflow at any column or width."

   An island hits that case constantly, because an absolutely positioned tip STILL COUNTS toward
   the document's scroll width even while it is hidden. A single 320px tip hanging off the last
   column of a four-up grid is enough to make the whole page scroll sideways at every viewport —
   measured, not assumed: it was the only thing over the line at both 1280 and 400.

   app.css pins .hint-card's insets to one particular card's padding, which does not generalise.
   Here the geometry belongs to the SUBJECT — a grid cell, a table row, a figures row — so each
   island scopes these three declarations to its own container instead, and this comment is the
   one place that says why they all look the same:

     <subject> { position: relative; }              the tip's containing block
     <subject> .info-hint { position: static; }     so left/right resolve against the subject
     <subject> .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }

   The max-width is the whole trick: 320px where the subject has room for it, the subject's own
   width where it does not, and never a pixel past either. A subject that is itself inside the
   island cannot then push the page.

   One consequence to hold onto: a positioned subject paints its tip inside its own stacking
   order, so a LATER sibling row would draw over a tip that opens across it. Each island lifts
   the subject on :hover/:focus-within for that reason — see the z-index bumps at the call sites. */
@media (prefers-reduced-motion: reduce) { .info-hint-tip { transition: none; } }
`;

/**
 * The (i) trigger and its tip, as HTML.
 *
 * Returns "" for a blank tip. That is the point of the guard rather than an optimisation:
 * an (i) with nothing behind it is a naked dot promising an explanation that never arrives,
 * so an absent `tip` must render no affordance at all.
 *
 * The tip is a plain string. A blank line (\n\n) starts a new paragraph, and each paragraph
 * runs through renderInline, so **bold**, `code` and [label](href) work — the mockup's tips
 * lean on bold for the sentence that carries the warning. Everything else is escaped.
 *
 * @param {string} tip           the explanation; blank renders nothing at all
 * @param {object} [opts]
 * @param {boolean} [opts.right] flip the tip to the right edge (hint-right)
 * @param {string} [opts.label]  the accessible name of the trigger
 */
export function hintHtml(tip, { right = false, label = "More information" } = {}) {
  const text = tip == null ? "" : String(tip).trim();
  if (text === "") return "";

  const paragraphs = text
    .split(/\n\s*\n/)
    .map((p) => p.trim())
    .filter((p) => p !== "")
    .map((p) => `<p>${renderInline(p)}</p>`)
    .join("");

  const cls = right ? "info-hint hint-right" : "info-hint";
  return (
    `<span class="${cls}" tabindex="0" role="note" aria-label="${escapeHtml(label || "More information")}">` +
    `<span class="info-hint-dot" aria-hidden="true">i</span>` +
    `<span class="info-hint-tip">${paragraphs}</span>` +
    `</span>`
  );
}
