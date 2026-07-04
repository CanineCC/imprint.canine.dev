// <cai-composition-bar segments='[{"label":"Brilliant","pct":32,"band":"exemplary"},…]'
//                      kicker="…" heading="…" lede="…" caption="…"
//                      brand="watchdog|assay|cai">
//
// Port of the Composition renderer (packages/ui/src/blocks.tsx): the brilliant /
// fine / slop split as a pure-SVG proportion bar, band colours from the CAI
// vocabulary. Segment widths are proportional to pct over the segment total.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";

const BAND_FILL = {
  exemplary: "var(--band-exemplary)",
  healthy: "var(--band-healthy)",
  fair: "var(--band-fair)",
  poor: "var(--band-poor)",
  critical: "var(--band-critical)",
};
const BAND_OPACITY = {
  exemplary: 0.85,
  healthy: 0.85,
  fair: 0.7,
  poor: 0.8,
  critical: 0.8,
};

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-compbar { max-width: 46rem; margin: 0 auto; }
.mk-compbar svg { width: 100%; height: auto; display: block; }
.mk-compbar-label { font-family: var(--font-ui); font-size: 16px; fill: var(--surface); }
.mk-compbar-pct { font-family: var(--font-mono); font-size: 13px; fill: var(--muted); }
.mk-compbar-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.4rem; text-align: center; }
`;

customElements.define(
  "cai-composition-bar",
  class extends CaiIsland {
    render(root) {
      const segs = (this.json("segments", []) || []).filter(
        (s) => s && Number(s.pct) > 0
      );
      const total =
        segs.reduce((a, s) => a + (Number(s.pct) || 0), 0) || 100;
      const W = 760;
      const BAR_H = 72;
      const caption = this.getAttribute("caption");

      let x = 0;
      const rects = segs.map((s) => {
        const w = (Number(s.pct) / total) * W;
        const r = { seg: s, x, w };
        x += w;
        return r;
      });
      const aria =
        "Code composition: " +
        segs
          .map((s) => `${s.pct}% ${String(s.label || "").toLowerCase()}`)
          .join(", ");

      let svg = `<svg viewBox="0 0 ${W} 116" role="img" aria-label="${escapeHtml(aria)}">`;
      for (const { seg, x, w } of rects) {
        const fill = BAND_FILL[seg.band] || "var(--band-fair)";
        const opacity = BAND_OPACITY[seg.band] ?? 0.8;
        const weight = seg.band === "fair" ? 500 : 700;
        svg += `<g>`;
        svg += `<rect x="${x}" y="0" width="${w}" height="${BAR_H}" fill="${fill}" opacity="${opacity}"></rect>`;
        svg += `<text class="mk-compbar-label" x="${x + w / 2}" y="${BAR_H / 2 + 5}" text-anchor="middle" font-weight="${weight}">${escapeHtml(seg.label)}</text>`;
        svg += `<text class="mk-compbar-pct" x="${x + 2}" y="${BAR_H + 30}">${escapeHtml(String(seg.pct))}%</text>`;
        svg += `</g>`;
      }
      svg += `</svg>`;

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<figure class="mk-compbar">${svg}`;
      if (caption)
        html += `<figcaption class="mk-compbar-cap">${renderInline(caption)}</figcaption>`;
      html += `</figure>`;
      root.innerHTML = html;
    }
  }
);
