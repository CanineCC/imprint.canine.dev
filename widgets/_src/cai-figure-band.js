// <cai-figure-band layout="head|lead" kicker="…" dateline="…" footnote="…" brand="…"
//                  figures='[{"lead":"1,161","label":"carry a known-vulnerable component",
//                             "support":"of 1,511 measurable · 76.8%","tone":"critical",
//                             "tip":"A component with an advisory published against it…"}]'>
//
// A row of figures. Each figure is one value and the words that finish its sentence — "1,161"
// means nothing, "1,161 carry a known-vulnerable component, of 1,511 measurable" is a finding —
// plus an optional (i) for the definition that would otherwise have to sit in a paragraph
// nobody reads.
//
// TWO DENSITIES OF THE SAME OBJECT, not two widgets. The corpus sheet's masthead and its §2
// findings band are the same thing at different sizes: a masthead states four figures in
// passing above a rule, a findings band gives four figures the page. Splitting them into two
// islands would mean two places to fix the day a figure needs a tone, or a footnote, or an (i)
// that does not overflow — which is exactly how the second look-and-feel gets in.
//
//   head  a masthead: kicker + dateline on the left, the figures as a quiet row on the right,
//         the whole block ruled off with 2px of accent. Its (i)s are hint-right — they sit at
//         the page's right edge, where a tip anchored left would hang off it.
//   lead  a findings band: the kicker becomes the section label, the figures become an
//         auto-fitting grid of 36px values, each over its label and its basis line.
//
// TONE INKS THE VALUE, NOT THE ROW. A figure with `tone` gets its lead in that band's text
// colour; a figure without one stays in the body ink. Absent is a real state — "65 publish a
// bill of materials" is a fact before anyone decides it is a bad one — so no tone means no
// colour rather than a default band.
//
// DATA ONLY: there is no api-base and no liveLoad. Every figure arrives as a prop from the page
// that renders it, so this island cannot pair one reading's number with another reading's date.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  escapeHtml,
  renderInline,
} from "./tokens.js";
import { SCORECARD_CSS } from "./scorecard.js";
import { HINT_CSS, hintHtml } from "./hint.js";

// The five band keys, as the ink-* classes SCORECARD_CSS defines. A `tone` outside this set is
// not a colour we have, so it is dropped rather than interpolated into a var() name — the
// figure renders in the body ink and nothing breaks.
const TONES = new Set(["exemplary", "healthy", "fair", "poor", "critical"]);

const CSS = TOKENS_CSS + BASE_CSS + SCORECARD_CSS + HINT_CSS + `
.fb-cap { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase; color: var(--muted); font-weight: 700; }
.fb-mono { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.fb-support { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-2xs); color: var(--muted); }
.fb-foot { margin: 0.9rem 0 0; font-size: var(--fs-xs); color: var(--muted); line-height: 1.6; }

/* ── head: the masthead ─────────────────────────────────────────────────────
   At 400px the figures WRAP onto their own line under the dateline. They must never be the
   reason a page scrolls sideways, and space-between with no wrap is exactly how that happens. */
.fb-head { display: flex; justify-content: space-between; align-items: flex-end; gap: 20px 28px;
  flex-wrap: wrap; padding-bottom: 20px; border-bottom: 2px solid var(--accent); }
.fb-head-id { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.fb-head-kicker { color: var(--accent); }
.fb-dateline { font-size: 30px; font-weight: 700; letter-spacing: -.02em; line-height: 1.1;
  color: var(--heading); overflow-wrap: anywhere; }
.fb-head-figs { display: flex; flex-wrap: wrap; gap: 14px 28px; position: relative; }
/* The masthead's (i)s are hint-right, and their subject is the FIGURES ROW: right-anchored to
   it, at most 320px, never wider than the row. At 400px the row wraps to the island's full
   width and the tip still cannot reach past either edge. See hint.js for the reasoning. */
.fb-head-figs .info-hint { position: static; }
.fb-head-figs .info-hint-tip { right: 0; left: auto; max-width: min(320px, 100%); }
.fb-head-fig { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.fb-head-lead { font-size: 18px; font-weight: 700; line-height: 1.2; overflow-wrap: anywhere; }

/* ── lead: the findings band ─────────────────────────────────────────────────
   auto-fit + minmax collapses to one column on its own at 400px; no media query needed. */
.fb-lead-kicker { display: block; margin-bottom: 14px; }
.fb-lead-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 24px; }
.fb-lead-fig { display: flex; flex-direction: column; gap: 5px; min-width: 0; position: relative; }
/* A findings figure's subject is its own grid cell. auto-fit means no figure knows whether it
   is in the last column, so no caller can be told to pass hint-right — spanning the cell is the
   only answer that holds at four columns, at two, and at one. */
.fb-lead-fig .info-hint { position: static; }
.fb-lead-fig .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
/* A tip opens BELOW its figure, and the next grid row would otherwise paint over it. */
.fb-lead-fig:hover, .fb-lead-fig:focus-within { z-index: 2; }
.fb-lead-lead { font-size: 36px; font-weight: 700; line-height: 1; overflow-wrap: anywhere; }
.fb-lead-label { font-size: var(--fs-sm); font-weight: 600; line-height: 1.3; }

/* The label row keeps the (i) on the baseline of the first line, not floating beside a
   two-line label. */
.fb-label-row { display: flex; align-items: flex-start; }
.fb-cap-row { display: flex; align-items: center; }
`;

customElements.define(
  "cai-figure-band",
  class extends CaiIsland {
    render(root) {
      const layout = (this.getAttribute("layout") || "lead").trim().toLowerCase() === "head"
        ? "head"
        : "lead";
      // A figure with no lead has no value to state, so it is not a figure. Dropping it beats
      // rendering an empty column that reads as a missing number.
      const figures = (this.json("figures", []) || []).filter(
        (f) => f && f.lead != null && String(f.lead) !== ""
      );
      const kicker = this.getAttribute("kicker");
      const dateline = this.getAttribute("dateline");
      const footnote = this.getAttribute("footnote");

      // The band ink for a figure's lead: an ink-* class, or nothing at all.
      const inkClass = (f) => {
        const tone = String(f.tone || "").trim().toLowerCase();
        return TONES.has(tone) ? ` ink-${tone}` : "";
      };

      let html = `<style>${CSS}</style>`;

      if (layout === "head") {
        html += `<div class="fb-head">`;
        if (kicker || dateline) {
          html += `<div class="fb-head-id">`;
          if (kicker) html += `<span class="fb-cap fb-head-kicker">${escapeHtml(kicker)}</span>`;
          if (dateline) html += `<span class="fb-dateline">${escapeHtml(dateline)}</span>`;
          html += `</div>`;
        }
        if (figures.length > 0) {
          html += `<div class="fb-head-figs">`;
          for (const f of figures) {
            html += `<div class="fb-head-fig">`;
            const label = f.label == null ? "" : String(f.label);
            // The caption row exists even for a blank label, so an (i) still has a home.
            html += `<div class="fb-cap-row">`;
            html += `<span class="fb-cap">${escapeHtml(label)}</span>`;
            html += hintHtml(f.tip, { right: true, label: label || "More information" });
            html += `</div>`;
            html += `<span class="fb-mono fb-head-lead${inkClass(f)}">${escapeHtml(String(f.lead))}</span>`;
            if (f.support) html += `<span class="fb-support">${escapeHtml(String(f.support))}</span>`;
            html += `</div>`;
          }
          html += `</div>`;
        }
        html += `</div>`;
      } else {
        if (kicker) html += `<span class="fb-cap fb-lead-kicker">${escapeHtml(kicker)}</span>`;
        if (figures.length > 0) {
          html += `<div class="fb-lead-grid">`;
          for (const f of figures) {
            html += `<div class="fb-lead-fig">`;
            html += `<span class="fb-mono fb-lead-lead${inkClass(f)}">${escapeHtml(String(f.lead))}</span>`;
            const label = f.label == null ? "" : String(f.label);
            const hint = hintHtml(f.tip, { label: label || "More information" });
            if (label || hint) {
              html += `<div class="fb-label-row">`;
              html += `<span class="fb-lead-label">${escapeHtml(label)}</span>`;
              html += hint;
              html += `</div>`;
            }
            if (f.support) html += `<span class="fb-support">${escapeHtml(String(f.support))}</span>`;
            html += `</div>`;
          }
          html += `</div>`;
        }
      }

      if (footnote) html += `<p class="fb-foot">${renderInline(footnote)}</p>`;

      root.innerHTML = html;
    }
  }
);
