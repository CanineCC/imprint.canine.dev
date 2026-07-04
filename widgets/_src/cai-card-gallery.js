// <cai-card-gallery api-base="…" count="6" cards='[{…},{…}]' kicker="…" heading="…"
//                   lede="…" footnote="…" brand="watchdog|assay|cai">
//
// The public-record grid of published CAI score cards — "real reports, fully open, not a
// logo wall" (port of the CardGallery renderer, packages/ui/src/blocks.tsx). Gallery
// cards are quiet peers (a hairline border, no seal, no shadow), unlike the accent hero.
//
// LIVE by default: when `api-base` is set it fetches {api}/api/oss and renders the
// curated set — newest-published first, then best score (the same quiet-peer ordering
// the public gallery uses), capped at `count`. Each card links to its real report.
// Without `api-base`, or if the fetch fails, it renders the labelled SAMPLE `cards`.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
} from "./tokens.js";
import { scoreCardBodyHtml, parseCard, SCORECARD_CSS } from "./scorecard.js";
import { cardFromGallery, reportUrl, galleryOrder, fetchJson } from "./live.js";

const DEFAULT_COUNT = 6;

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers — the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;

customElements.define(
  "cai-card-gallery",
  class extends CaiIsland {
    #count() {
      const raw = this.getAttribute("count");
      const n = Number(raw);
      return raw != null && raw !== "" && Number.isFinite(n) && n > 0 ? Math.floor(n) : DEFAULT_COUNT;
    }

    // Fetch the corpus, order it as the public gallery does, take `count`, map each to a
    // card model with its real report link, cache, re-render. Empty/failed ⇒ sample stays.
    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const cards = await fetchJson(api, "/api/oss", null);
      if (!Array.isArray(cards) || cards.length === 0) return;
      const chosen = galleryOrder(cards).slice(0, this.#count());
      const mapped = chosen
        .map((c) => {
          const m = cardFromGallery(c);
          if (m) m.href = reportUrl(c);
          return m;
        })
        .filter(Boolean);
      if (mapped.length === 0) return;
      this._live = mapped;
      this.render(this.shadowRoot);
    }

    render(root) {
      // Prefer the LIVE cards once they arrive; else the seeded sample array.
      const cards = this._live
        ? this._live
        : (this.json("cards", []) || []).map((c) => parseCard(c)).filter(Boolean);
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
