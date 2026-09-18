// <cai-publication-cards items='[{"series":"CAI Founding Papers","title":"From Score to Evidence",
//                                 "note":"What recomputing a score establishes…",
//                                 "byline":"Jimmy Borch","date":"17 September 2026",
//                                 "length":"38 pages","href":"/whitepaper-cai-evidence/",
//                                 "tone":"paper","go":"Read the paper"}]'
//                        kicker="Papers" heading="…" lede="…" brand="cai">
//
// A shelf of publications as covers. The publications index is a finite corpus that keeps
// growing — four founding papers and eight explainers before the cut-off — and a list that
// gives each item a paragraph turns into a page nobody reaches the bottom of. A cover is the
// densest honest summary of a document: you recognise it, and one line tells you whether it
// answers your question.
//
// TWO COVERS, NOT ONE COVER AND A DISCOUNT. The shelves had been told apart by taking things
// away from the article — the same face with its band unfilled and its spine removed — which
// read as an unfinished paper rather than a different kind of document. They are now two
// layouts over identical markup, each borrowing from the thing it actually is:
//
//   paper    a TITLE PAGE. A solid masthead across the head carrying the series, a double rule
//            under it in the old title-page manner, the title, the description, and the imprint
//            on the baseline. Weight at the top, centred measure, bound: a spine runs down the
//            binding edge.
//   article  a JOURNAL PAGE. The series is set vertically up the fore-edge in a ruled column of
//            its own, the way a bound periodical carries its name where a reader will see it on
//            a shelf. That frees the whole face for the title, which opens the page and is
//            followed by a short accent rule and then the description.
//
// The grid does the work: the same band/body/foot spans lay out as head/body/foot on a paper and
// as edge/body/foot on an article. Identical markup, so neither shelf can drift into a different
// card, and a tone that is misspelt renders the paper layout rather than nothing.
//
// EVERYTHING IS ON THE COVER. An earlier cut hung the description and the byline underneath the
// face and the cover looked like it carried nothing, because it did not.
//
// THE TITLE ALWAYS SITS AT THE HEAD. A cut that pushed the article title to the baseline read as
// a fault rather than a choice: you look at a cover to find out what it is, and the answer was
// in the wrong place.
//
// THE TRACK IS CAPPED, which is the whole reason the shelf looks like a shelf. Sizing the face
// from an aspect ratio alone let the cards grow with their column: three papers came out 372px
// wide and two articles 567px, giving covers of two sizes on one page. A track that can only be
// between 17rem and 20rem makes every cover identical whether its row holds two items or three,
// and `justify-content: start` leaves the remainder as a margin rather than inflating the cards
// into it.
//
// PASS brand="cai" ON A CAI PAGE. The island carries its own copy of the token table, so without
// it a cover takes the family's steel accent rather than the green the rest of
// codeassuranceindex.info is built from.
//
// EVERY FIELD BUT TITLE AND HREF IS OPTIONAL and absent-safe: a card with no byline, date or
// length renders without the imprint line rather than with an empty one, because an item is
// listed the day it is signed and not every publication carries the same metadata.
//
// DATA ONLY: no api-base, no fetch. Every row arrives as a prop from the page, because the index
// is edited by a person as each item is signed, not generated from a feed.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
/* auto-fill with a CAPPED track: a cover is never narrower than 17rem and never wider than
   20rem, so a row of two and a row of three carry the same card. The leftover width is a margin
   at the end of the row, not extra card. */
.mk-pubs { display: grid; gap: 22px; justify-content: start;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 20rem)); }

a.mk-pub { display: block; text-decoration: none; color: inherit;
  transition: transform 140ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-3px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 4px; }

.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: grid; overflow: hidden;
  border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 140ms ease, box-shadow 140ms ease; }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face {
  border-color: var(--accent); box-shadow: var(--shadow-overlay); }

.mk-pub-title { display: block; font-weight: 600; letter-spacing: -0.012em; color: var(--heading); }
.mk-pub-note { display: block; font-size: var(--fs-sm); line-height: 1.55; color: var(--ink-soft); }
.mk-pub-meta { display: block; font-size: var(--fs-xs); line-height: 1.5; color: var(--muted); }
.mk-pub-go { display: block; margin-top: 7px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); letter-spacing: 0.02em; color: var(--accent-ink);
  transition: color 140ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }
.mk-pub-series { font-size: var(--fs-2xs); font-weight: 700; letter-spacing: 0.1em;
  text-transform: uppercase; }

/* ── the paper: a title page ────────────────────────────────────────────────
   Head, body, foot. The wash lifts from the masthead down, so the weight sits where a bound
   document announces itself, and the double rule under the band is the title-page convention
   that says "this is the front of something", not a decoration. */
.mk-pub.is-paper .mk-pub-face { grid-template-rows: auto 1fr auto;
  background: linear-gradient(180deg, var(--accent-wash) 0%, var(--surface) 76%); }
.mk-pub.is-paper .mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0;
  width: 5px; background: var(--accent); }
.mk-pub.is-paper .mk-pub-band { padding: 11px 18px 11px 22px; background: var(--accent); }
.mk-pub.is-paper .mk-pub-series { color: var(--on-accent); }
.mk-pub.is-paper .mk-pub-body { padding: 0 18px 0 22px; }
.mk-pub.is-paper .mk-pub-body::before { content: ""; display: block; height: 3px;
  border-top: 1px solid var(--border-strong); border-bottom: 1px solid var(--border-strong);
  margin: 16px 0 15px; }
.mk-pub.is-paper .mk-pub-title { font-size: clamp(1.1rem, 0.92rem + 0.55vw, 1.4rem); line-height: 1.24; }
.mk-pub.is-paper .mk-pub-note { margin-top: 11px; }
.mk-pub.is-paper .mk-pub-foot { padding: 12px 18px 15px 22px; border-top: 1px solid var(--hairline); }

/* ── the article: a journal page ────────────────────────────────────────────
   The series runs up the fore-edge in a ruled column of its own, which is where a bound
   periodical carries its name so a reader can find it side-on. That leaves the whole face for
   the title, and the short accent rule under the title is the paragraph opener a journal uses
   instead of a masthead. */
.mk-pub.is-article .mk-pub-face { grid-template-columns: 40px 1fr; grid-template-rows: 1fr auto;
  background: linear-gradient(200deg, var(--surface-2) 0%, var(--surface) 62%); }
.mk-pub.is-article .mk-pub-band { grid-row: 1 / span 2; display: flex; align-items: flex-end;
  justify-content: center; padding: 0 0 18px; border-right: 1px solid var(--accent); }
.mk-pub.is-article .mk-pub-series { writing-mode: vertical-rl; transform: rotate(180deg);
  color: var(--accent-ink); white-space: nowrap; }
.mk-pub.is-article .mk-pub-body { padding: 22px 20px 0 18px; }
.mk-pub.is-article .mk-pub-title { font-size: clamp(1.15rem, 0.95rem + 0.6vw, 1.5rem); line-height: 1.2; }
.mk-pub.is-article .mk-pub-title::after { content: ""; display: block; width: 34px; height: 2px;
  background: var(--accent); margin: 14px 0 0; }
.mk-pub.is-article .mk-pub-note { margin-top: 13px; }
.mk-pub.is-article .mk-pub-foot { padding: 12px 20px 15px 18px; }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;

// The imprint line, assembled from whichever of the three parts the item carries. Joined with
// the same middot the site's own metadata lines use, and omitted entirely when the item carries
// none of them: an empty line on a cover reads as a rendering fault.
function metaLine(item) {
  const parts = [item.byline, item.length, item.date]
    .map((p) => (p == null ? "" : String(p).trim()))
    .filter((p) => p !== "");
  return parts.length === 0 ? "" : parts.join(" · ");
}

customElements.define(
  "cai-publication-cards",
  class extends CaiIsland {
    render(root) {
      const items = (this.json("items", []) || []).filter((i) => i && i.href && i.title);

      let cards = "";
      if (items.length > 0) {
        cards = `<div class="mk-pubs">`;
        for (const item of items) {
          const tone = String(item.tone || "").toLowerCase() === "article" ? "article" : "paper";
          const meta = metaLine(item);
          cards += `<a class="mk-pub is-${tone}" href="${escapeHtml(item.href)}">`;
          cards += `<span class="mk-pub-face">`;
          if (item.series) {
            cards += `<span class="mk-pub-band"><span class="mk-pub-series">${escapeHtml(item.series)}</span></span>`;
          }
          cards += `<span class="mk-pub-body">`;
          cards += `<span class="mk-pub-title">${escapeHtml(item.title)}</span>`;
          if (item.note) {
            cards += `<span class="mk-pub-note">${escapeHtml(item.note)}</span>`;
          }
          cards += `</span>`;
          cards += `<span class="mk-pub-foot">`;
          if (meta !== "") {
            cards += `<span class="mk-pub-meta">${escapeHtml(meta)}</span>`;
          }
          if (item.go) {
            cards += `<span class="mk-pub-go">${escapeHtml(String(item.go))} →</span>`;
          }
          cards += `</span></span></a>`;
        }
        cards += `</div>`;
      }

      root.innerHTML = `<style>${CSS}</style>` + sectionHeadHtml(this) + cards;
    }
  }
);
