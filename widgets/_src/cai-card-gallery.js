// <cai-card-gallery cards='[{…},{…}]' kicker="…" heading="…" lede="…"
//                   footnote="…" brand="watchdog|assay|cai">
//
// Port of the CardGallery renderer (packages/ui/src/blocks.tsx): the public-record
// grid of published CAI score cards — "real reports, fully open, not a logo wall".
// Gallery cards are quiet peers (a hairline border, no seal, no shadow) unlike the
// accent hero artifact. Cards are the SAMPLE/labelled data the CMS ships.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
} from "./tokens.js";
import { scoreCardBodyHtml, parseCard, SCORECARD_CSS } from "./scorecard.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers — the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;

customElements.define(
  "cai-card-gallery",
  class extends CaiIsland {
    render(root) {
      const cards = (this.json("cards", []) || [])
        .map((c) => parseCard(c))
        .filter(Boolean);
      const footnote = this.getAttribute("footnote");

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-cardgallery">`;
      for (const c of cards) {
        const tag = c.href ? "a" : "div";
        const hrefAttr = c.href ? ` href="${c.href.replace(/"/g, "&quot;")}"` : "";
        html += `<${tag} class="cai-card"${hrefAttr}>${scoreCardBodyHtml(c)}</${tag}>`;
      }
      html += `</div>`;
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;
    }
  }
);
