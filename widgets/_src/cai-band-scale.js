// <cai-band-scale api-base="…" score="62" kicker="…" heading="…"
//                 lede="…" caption="…" brand="watchdog|assay|cai">
//
// Port of the BandScale renderer (packages/ui/src/blocks.tsx): the canonical
// full-width CAI band scale — five bands worst→best, the parked cutlines
// 25/50/70/90, the display words, and an optional Score-pin marker.
//
// LIVE by default: when `api-base` is set it fetches {api}/api/public/showcase and pins the
// scale at the SERVER-CURATED representative score (showcase.bandScale.score) — a real
// public repo the server chose to best illustrate the band ladder. Without `api-base`, or
// if the fetch fails, it uses the seeded `score` attribute (or renders the bare scale, no
// pin). No client-side pick.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
} from "./tokens.js";
import { ladderHtml, SCORECARD_CSS } from "./scorecard.js";
import { CAI_BANDS } from "./cai.js";
import { fetchShowcase } from "./live.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-bandscale { max-width: 46rem; margin: 0 auto; }
.mk-bandscale-rail { position: relative; }
.mk-bandscale-rail.has-pin { padding-top: 44px; }
.mk-bandscale .cai-rail, .mk-bandscale .cai-segs { height: 16px; }
.mk-bandscale .cai-segs { border-radius: 8px; }
.mk-bandscale-cuts { position: relative; height: 18px; margin-top: 7px; }
.mk-bandscale-cuts span { position: absolute; transform: translateX(-50%); font-family: var(--font-mono); font-size: var(--fs-2xs); color: var(--muted); }
.mk-bandscale-cuts span:first-child { transform: none; }
.mk-bandscale-cuts span:last-child { transform: translateX(-100%); }
.mk-bandscale-words { display: flex; margin-top: 2px; }
.mk-bandscale-words span { flex: 1; text-align: center; font-size: var(--fs-xs); font-weight: 600; }
.mk-bandscale-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;

customElements.define(
  "cai-band-scale",
  class extends CaiIsland {
    // Pin the scale at the SERVER-CURATED representative score. Cache + re-render.
    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const showcase = await fetchShowcase(api);
      const bs = showcase && showcase.bandScale;
      const score = bs && bs.score;
      if (score == null || !Number.isFinite(Number(score))) return;
      this._liveScore = Number(score);
      this.render(this.shadowRoot);
    }

    render(root) {
      // Prefer the LIVE score once it arrives; else the seeded `score` attribute.
      const scoreRaw = this.getAttribute("score");
      const score = this._liveScore != null
        ? this._liveScore
        : (scoreRaw != null && scoreRaw !== "" && Number.isFinite(Number(scoreRaw)) ? Number(scoreRaw) : null);
      const hasPin = score != null;
      const caption = this.getAttribute("caption");

      let rail;
      if (hasPin) {
        rail = ladderHtml(score, { variant: "pin" });
      } else {
        const segs = CAI_BANDS.map((b) => `<i class="seg-${b.key}"></i>`).join("");
        rail =
          `<div class="cai-ladder compact"><div class="cai-rail" role="img" ` +
          `aria-label="The fixed worst-to-best CAI band scale">` +
          `<div class="cai-segs">${segs}</div></div></div>`;
      }
      const words = CAI_BANDS.map(
        (b) => `<span class="ink-${b.key}">${b.label}</span>`
      ).join("");

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<figure class="mk-bandscale">`;
      html += `<div class="mk-bandscale-rail${hasPin ? " has-pin" : ""}">`;
      html += rail;
      html +=
        `<div class="mk-bandscale-cuts" aria-hidden="true">` +
        `<span style="left:0%">0</span>` +
        `<span style="left:20%">25</span>` +
        `<span style="left:40%">50</span>` +
        `<span style="left:60%">70</span>` +
        `<span style="left:80%">90</span>` +
        `<span style="left:100%">100</span></div>`;
      html += `</div>`;
      html += `<div class="mk-bandscale-words">${words}</div>`;
      if (caption)
        html += `<figcaption class="mk-bandscale-cap">${renderInline(caption)}</figcaption>`;
      html += `</figure>`;
      root.innerHTML = html;
    }
  }
);
