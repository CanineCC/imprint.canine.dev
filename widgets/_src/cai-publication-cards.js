// <cai-publication-cards items='[{"series":"CAI Founding Papers","title":"From Score to Evidence",
//                                 "note":"What recomputing a score establishes…",
//                                 "byline":"Jimmy Borch","date":"17 September 2026",
//                                 "length":"38 pages","href":"/whitepaper-cai-evidence/",
//                                 "tone":"paper","go":"Read the paper"}]'
//                        kicker="Papers" heading="…" lede="…">
//
// A shelf of publications as covers. The publications index is a finite corpus that keeps
// growing — four founding papers and eight explainers before the cut-off — and a list that
// gives each item a paragraph turns into a page nobody reaches the bottom of. A cover is the
// densest honest summary of a document: you recognise it, and one line tells you whether it
// answers your question.
//
// EVERYTHING IS ON THE COVER. The first cut of this island put the description and the byline
// under the face, and the result was a tall empty panel with the facts orphaned beneath it: the
// cover looked like it carried no information, because it did not. Series, title, description,
// byline, length and signing date all sit inside the face now, and nothing renders outside it.
//
// THE WHOLE CARD IS THE LINK. A title link beside a dead card leaves most of the target inert,
// and a reader who aims at the card and hits nothing learns the page is not for them. The <a>
// IS the cover, so the target is the whole thing and the focus ring is drawn on what a mouse
// hits.
//
// THE FACE IS TYPE, NOT AN IMAGE. A real cover would mean rendering each PDF's first page and
// keeping that rendering in step with every reissue, which is a second artefact to sign and get
// wrong. The face is the series, a rule, and the title set as large as it will go: it reads as a
// cover at a glance, it costs nothing to keep true, and a title change cannot leave a stale
// picture behind.
//
// THE TRACK IS CAPPED, WHICH IS THE WHOLE REASON THE SHELF LOOKS LIKE A SHELF. The first cut
// sized the face from an aspect ratio alone, so the cards grew with their column: three papers
// came out 372px wide and two articles 567px, with faces of 496px and 756px, and one page
// carried covers of two different sizes. A track that can only be between 17rem and 20rem makes
// every cover on the site the same size whether its row holds two items or three, and
// `justify-content: start` leaves the remainder as a margin rather than inflating the cards
// into it.
//
// TONE SEPARATES THE SHELVES, not decoration. `tone` is "paper" or "article", and the two are
// the same size and the same markup — one grid, one track, one set of spans — but the face is
// set differently, so a reader can tell the shelves apart before reading a word:
//
//   paper    a title page. Series and rule at the top, title under them, the fine print and the
//            strapline together on the baseline. Weight at the top, the way a bound document
//            announces itself.
//   article  a cover line. The series sits at the top with air under it, and the title falls to
//            meet its own fine print at the foot. Weight at the bottom, the way a magazine
//            cover carries its headline.
//
// The difference is CSS over identical markup, so neither shelf can drift into a different card
// and a tone that is misspelt simply renders the paper layout rather than nothing.
//
// EVERY FIELD BUT TITLE AND HREF IS OPTIONAL and absent-safe: a card with no byline, date or
// length renders without the fine-print line rather than with an empty one, because an item is
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
.mk-pubs { display: grid; gap: 20px; justify-content: start;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 20rem)); }

a.mk-pub { display: block; text-decoration: none; color: inherit; border-radius: var(--r-md);
  transition: transform 120ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-2px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 3px; }

/* The face IS the card. The spine is a solid bar down the binding edge: it is what makes a
   rectangle of type read as a document rather than a panel, and it takes the tone colour so the
   shelf a card belongs to is legible before the words are. */
.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: flex; flex-direction: column;
  padding: 22px 20px 20px 26px; border: 1px solid var(--border); border-radius: var(--r-md);
  background: var(--pub-wash); overflow: hidden;
  transition: border-color 120ms ease; }
.mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 5px;
  background: var(--pub-spine); }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face { border-color: var(--pub-spine); }

.mk-pub-series { font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em;
  text-transform: uppercase; color: var(--muted); }
.mk-pub-rule { height: 1px; background: var(--border-strong); margin: 10px 0 12px; }
/* The title is the cover. It may be big and wrap as far as it needs; the face grows no taller,
   because the aspect ratio is fixed, so a long title simply fills more of it. */
.mk-pub-title { font-size: clamp(1.05rem, 0.9rem + 0.5vw, 1.35rem); line-height: 1.25;
  font-weight: 600; letter-spacing: -0.01em; color: var(--heading); }
.mk-pub-note { margin-top: 10px; font-size: var(--fs-sm); line-height: 1.5; color: var(--ink-soft); }

/* The fine print and the strapline travel together on the baseline of the cover, where a real
   cover carries its imprint. */
.mk-pub-foot { margin-top: auto; padding-top: 14px; }
.mk-pub-meta { display: block; font-size: var(--fs-xs); line-height: 1.5; color: var(--muted); }
.mk-pub-go { display: block; margin-top: 6px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); transition: color 120ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }

:host { --pub-wash: var(--surface); --pub-spine: var(--accent); }
.mk-pub.is-paper { --pub-wash: var(--accent-wash); --pub-spine: var(--accent); }
.mk-pub.is-article { --pub-wash: var(--surface-2); --pub-spine: var(--muted); }

/* The article face, same box and same spans, set from the baseline up: no rule under the series,
   and the title carries the auto margin instead of the foot, so title and fine print fall to the
   bottom of the cover together. */
.mk-pub.is-article .mk-pub-rule { display: none; }
.mk-pub.is-article .mk-pub-title { margin-top: auto; }
.mk-pub.is-article .mk-pub-foot { margin-top: 14px; }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;

// The fine print, assembled from whichever of the three parts the item carries. Joined with the
// same middot the site's own metadata lines use, and omitted entirely when the item carries none
// of them: an empty line on a cover reads as a rendering fault.
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
            cards += `<span class="mk-pub-series">${escapeHtml(item.series)}</span>`;
            cards += `<span class="mk-pub-rule"></span>`;
          }
          cards += `<span class="mk-pub-title">${escapeHtml(item.title)}</span>`;
          if (item.note) {
            cards += `<span class="mk-pub-note">${escapeHtml(item.note)}</span>`;
          }
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
