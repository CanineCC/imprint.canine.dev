// <cai-link-cards links='[{"icon":"github","label":"The project's source repository",
//                          "note":"github.com/MathewSachin/Captura","href":"https://…"}]'
//                 kicker="…" heading="…" lede="…">
//
// The places a reader goes next, as cards rather than a bullet list of link text. Four
// underlined sentences in a row all look equally like footnotes; four cards with a mark on
// each say "the source", "the report", "the surveyor", "check it yourself" at a glance.
//
// The icon is a claim about the destination, so each one has to be true:
//
//   github / gitlab  the forge's own mark, used for the one thing a forge mark is for —
//                    labelling a link to a repository on it.
//   html             the HTML5 mark. The full measurement report is served as HTML, so this
//                    says what opens; a PDF glyph would promise a download that does not exist.
//   doc              a plain document, for a destination we have no mark for.
//   watchdog / cai   the shared canine badge, masked and tinted with that product's accent —
//                    the same mark and the same tint the site headers use, so a card cannot
//                    drift into being a second, slightly different logo.
//
// DATA ONLY: no api-base, no fetch. Every row arrives as a prop from the page.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";

// The badge is the site's own file, tinted by mask rather than redrawn: one shape, and a
// card can never disagree with the header about what the mark is.
const BADGE = "/brand/canine-badge.svg";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
/* Two columns, so the usual four land as a 2x2 block rather than a row of three and an
   orphan. minmax keeps it one column when there is no room for two. */
.mk-links { max-width: 46rem; margin: 0 auto; display: grid; gap: 10px;
  grid-template-columns: repeat(auto-fit, minmax(19rem, 1fr)); }
a.mk-link { display: grid; grid-template-columns: auto 1fr; gap: 2px 12px; align-items: start;
  padding: 14px 16px; text-decoration: none; color: inherit; background: var(--surface);
  border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 120ms ease, background 120ms ease; }
a.mk-link:hover, a.mk-link:focus-visible { border-color: var(--accent); background: var(--surface-2);
  text-decoration: none; }
.mk-link-ico { grid-row: 1 / span 2; width: 26px; height: 26px; display: block; flex: none; }
.mk-link-ico svg { width: 100%; height: 100%; display: block; }
.mk-link-badge { width: 24px; height: 26px; display: block;
  -webkit-mask: var(--badge) no-repeat center / contain; mask: var(--badge) no-repeat center / contain; }
.mk-link-badge.is-watchdog { background: var(--wd-accent); }
.mk-link-badge.is-cai { background: var(--cai-accent); }
.mk-link-label { font-weight: 650; font-size: var(--fs-sm); line-height: 1.4; }
/* A long forge path breaks between segments rather than mid-word: "…/Captur a" reads as a
   typo, and these notes are addresses a reader may want to recognise. */
.mk-link-note { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5;
  word-break: break-word; overflow-wrap: break-word; }
:host { --wd-accent: #7faace; --cai-accent: #6fbfa4; }
:host([data-theme="light"]) { --wd-accent: #35618a; --cai-accent: #2e7d64; }
@media (prefers-reduced-motion: reduce) { a.mk-link { transition: none; } }
`;

// Third-party marks, all monochrome: they label a destination, they are not a brand statement of
// ours. Ours are the only ones that carry colour, which is the hierarchy a reader should see —
// and the shield, the octocat and the tanuki are each recognisable by shape alone.
const ICONS = {
  github: `<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"/></svg>`,
  gitlab: `<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="m15.73 6.49-.02-.06-2.17-5.66a.57.57 0 0 0-.22-.27.58.58 0 0 0-.88.27L10.98 5.2H5.03L3.57.77a.58.58 0 0 0-.88-.27.57.57 0 0 0-.22.27L.3 6.43l-.02.06a4.03 4.03 0 0 0 1.34 4.65l.01.01.02.01 3.3 2.47 1.63 1.23.99.75a.68.68 0 0 0 .82 0l.99-.75 1.64-1.23 3.32-2.48.01-.01a4.03 4.03 0 0 0 1.34-4.65Z"/></svg>`,
  // The HTML5 shield: the report opens as a page, and this says so before the click.
  html: `<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M1.5 0h21l-1.91 21.563L11.977 24l-8.564-2.438L1.5 0zm7.031 9.75l-.232-2.718 10.059.003.23-2.622L5.412 4.41l.698 8.01h9.126l-.326 3.426-2.91.804-2.955-.81-.188-2.11H6.248l.33 4.171L12 19.351l5.379-1.443.744-8.157H8.531z"/></svg>`,
  // A plain document, for a destination we have no mark for.
  doc: `<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9.2 1.4H4a1.6 1.6 0 0 0-1.6 1.6v10A1.6 1.6 0 0 0 4 14.6h8a1.6 1.6 0 0 0 1.6-1.6V5.8Z"/><path d="M9.2 1.4v4.4h4.4"/><path d="M5.6 8.4h4.8M5.6 11h4.8"/></svg>`,
};

customElements.define(
  "cai-link-cards",
  class extends CaiIsland {
    render(root) {
      const links = (this.json("links", []) || []).filter((l) => l && l.href && l.label);

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (links.length === 0) {
        root.innerHTML = html;
        return;
      }

      html += `<div class="mk-links">`;
      for (const l of links) {
        const icon = String(l.icon || "").toLowerCase();
        html += `<a class="mk-link" href="${escapeHtml(l.href)}" rel="noopener noreferrer">`;
        html += `<span class="mk-link-ico" aria-hidden="true">`;
        if (icon === "watchdog" || icon === "cai") {
          html += `<span class="mk-link-badge is-${icon}" style="--badge:url('${BADGE}')"></span>`;
        } else {
          html += ICONS[icon] || ICONS.doc;
        }
        html += `</span>`;
        html += `<span class="mk-link-label">${escapeHtml(l.label)}</span>`;
        html += `<span class="mk-link-note">${escapeHtml(l.note || "")}</span>`;
        html += `</a>`;
      }
      html += `</div>`;

      root.innerHTML = html;
    }
  }
);
