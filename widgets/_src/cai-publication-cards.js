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
// THE WHOLE CARD IS THE LINK. A title link beside a description leaves most of the card dead,
// and a reader who aims at the card and hits nothing learns the page is not for them. The <a>
// wraps the face and the fine print, so the target is the whole thing, and the focus ring is
// drawn on the same element a mouse hits.
//
// THE FACE IS TYPE, NOT AN IMAGE. A real cover would mean rendering each PDF's first page and
// keeping that rendering in step with every reissue, which is a second artefact to sign and get
// wrong. The face is the series, a rule, and the title set as large as it will go: it reads as a
// cover at a glance, it costs nothing to keep true, and a title change cannot leave a stale
// picture behind. A 3:4 face is near enough to a page that the shelf reads as documents.
//
// TONE SEPARATES THE SHELVES, not decoration. `tone` is "paper" or "article", and the two are the
// same size and the same markup — one grid, one track, one set of spans — but the face is set
// differently, so a reader can tell the shelves apart before reading a word and a mixed page
// never needs a heading to explain itself:
//
//   paper    a title page. The series sits at the top under a rule, the title follows it, and the
//            strapline sits alone on the baseline. Weight at the top, the way a bound document
//            announces itself.
//   article  a cover line. The series sits at the top with no rule and a lot of air under it, and
//            the title and its strapline are pushed down to sit on the baseline together. Weight
//            at the bottom, the way a magazine cover carries its headline.
//
// The difference is CSS over identical markup, so neither shelf can drift into a different card
// and a tone that is misspelt simply renders the paper layout rather than nothing.
//
// EVERY FIELD BUT TITLE AND HREF IS OPTIONAL and absent-safe: a card with no byline, no date and
// no length renders without the fine-print row rather than with an empty one. That matters
// because an item is listed the day it is signed, and not every kind of publication carries the
// same metadata.
//
// AT MOST THREE COLUMNS. auto-fit alone keeps dividing a wide column into narrow tracks until
// the covers are a row of stamps; the max() in the track floor makes the smallest permissible
// track a third of the row, so a fourth column can never fit, and the same rule still collapses
// to one column on a phone.
//
// DATA ONLY: no api-base, no fetch. Every row arrives as a prop from the page, because the
// index is edited by a person as each item is signed, not generated from a feed.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
/* The track floor is max(18rem, (100% - two gaps) / 3): 18rem where the column is narrow, and a
   full third of the row once a third would be wider than that — which caps the shelf at three
   without hard-coding a count that would then never collapse. */
.mk-pubs { display: grid; gap: 18px;
  grid-template-columns: repeat(auto-fit, minmax(max(18rem, (100% - 36px) / 3), 1fr)); }

a.mk-pub { display: flex; flex-direction: column; gap: 12px; text-decoration: none; color: inherit;
  border-radius: var(--r-md); transition: transform 120ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-2px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 3px; }

/* The face. The spine is a solid bar down the binding edge: it is what makes a rectangle of type
   read as a document rather than a panel, and it takes the tone colour so the shelf a card
   belongs to is legible before the words are. */
.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: flex; flex-direction: column;
  padding: 22px 20px 20px 26px; border: 1px solid var(--border); border-radius: var(--r-md);
  background: var(--pub-wash); overflow: hidden;
  transition: border-color 120ms ease, background 120ms ease; }
.mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 5px;
  background: var(--pub-spine); }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face { border-color: var(--pub-spine); }

.mk-pub-series { font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em;
  text-transform: uppercase; color: var(--muted); }
.mk-pub-rule { height: 1px; background: var(--border-strong); margin: 12px 0 14px; }
/* The title is the cover. It is allowed to be big and to wrap as far as it needs; the face grows
   no taller because the aspect ratio is fixed, so a long title simply fills more of it. */
.mk-pub-title { font-size: clamp(1.15rem, 0.95rem + 0.8vw, 1.6rem); line-height: 1.22;
  font-weight: 600; letter-spacing: -0.01em; color: var(--heading); }
/* The go line sits on the baseline of the face, where the strapline sits on a real cover. */
.mk-pub-go { margin-top: auto; padding-top: 14px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); transition: color 120ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }

.mk-pub-note { font-size: var(--fs-sm); line-height: 1.5; color: var(--ink-soft); }
.mk-pub-meta { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }

:host { --pub-wash: var(--surface); --pub-spine: var(--accent); }
.mk-pub.is-paper { --pub-wash: var(--accent-wash); --pub-spine: var(--accent); }
.mk-pub.is-article { --pub-wash: var(--surface-2); --pub-spine: var(--muted); }

/* The article face, same box and same spans, set from the baseline up: no rule under the series,
   and the title carries the auto margin instead of the strapline so the pair falls to the foot of
   the cover together. */
.mk-pub.is-article .mk-pub-rule { display: none; }
.mk-pub.is-article .mk-pub-title { margin-top: auto; }
.mk-pub.is-article .mk-pub-go { margin-top: 0; padding-top: 10px; }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;

// The fine print under the face, assembled from whichever of the three parts the item carries.
// Joined with the same middot the site's own metadata lines use, and omitted entirely when the
// item carries none of them — an empty row under a cover reads as a rendering fault.
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
          if (item.go) {
            cards += `<span class="mk-pub-go">${escapeHtml(String(item.go))} →</span>`;
          }
          cards += `</span>`;
          if (item.note) {
            cards += `<span class="mk-pub-note">${escapeHtml(item.note)}</span>`;
          }
          if (meta !== "") {
            cards += `<span class="mk-pub-meta">${escapeHtml(meta)}</span>`;
          }
          cards += `</a>`;
        }
        cards += `</div>`;
      }

      root.innerHTML = `<style>${CSS}</style>` + sectionHeadHtml(this) + cards;
    }
  }
);
