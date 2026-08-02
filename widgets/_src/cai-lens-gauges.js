// <cai-lens-gauges lenses='[{"label":"Code health","value":95.8,"note":"…"}]'
//                  kicker="…" heading="…" lede="…" footnote="…" brand="…">
//
// The ten lenses as gauges: each one's score inked with its band, a bar to read at a
// glance, and the dimension that GATES it — which is the one thing the report says and a
// bare figure cannot. "Architecture 77.0 — gated by AX5" tells a reader what to fix;
// "Architecture 77.0" tells them only that it could be better.
//
// DATA ONLY, deliberately: there is no api-base and no liveLoad. Every number arrives as a
// prop from the page that renders it, so this island cannot show one repository's figures
// under another repository's name. That is not a theoretical concern — the live-fetching
// score card did exactly that across ~1,000 published survey pages, because a widget that
// can fetch will fetch whatever the API hands back, and what it hands back is the hero.
//
// A lens with a null value renders its row with an em dash rather than an empty bar: "we
// looked and it does not apply" is a measurement, and dropping the row would read as a gap.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
  renderInline,
} from "./tokens.js";
import { SCORECARD_CSS } from "./scorecard.js";
import { bandFor } from "./cai.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-gauges { max-width: 46rem; margin: 0 auto; display: grid; gap: 2px;
  border: 1px solid var(--border); border-radius: 8px; overflow: hidden; background: var(--border); }
.mk-gauge { background: var(--surface); padding: 13px 16px;
  display: grid; grid-template-columns: 1fr auto; gap: 4px 16px; align-items: baseline; }
.mk-gauge-name { font-weight: 650; font-size: var(--fs-sm); }
.mk-gauge-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-sm); }
.mk-gauge .cai-lens-bar { grid-column: 1 / -1; height: 6px; }
.mk-gauge-note { grid-column: 1 / -1; font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }
.mk-gauges-foot { max-width: 46rem; margin: 0.9rem auto 0; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; }
`;

customElements.define(
  "cai-lens-gauges",
  class extends CaiIsland {
    render(root) {
      const lenses = (this.json("lenses", []) || []).filter((l) => l && l.label);
      const footnote = this.getAttribute("footnote");

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (lenses.length === 0) {
        // Nothing measured is not the same as a score of zero, and an empty frame says neither.
        root.innerHTML = html;
        return;
      }

      html += `<div class="mk-gauges">`;
      for (const lens of lenses) {
        const raw = lens.value == null ? null : Number(lens.value);
        const value = raw != null && Number.isFinite(raw) ? raw : null;
        const band = value == null ? null : bandFor(value);

        html += `<div class="mk-gauge">`;
        html += `<span class="mk-gauge-name">${escapeHtml(lens.label)}</span>`;
        html += value == null
          ? `<span class="mk-gauge-num cai-muted">—</span>`
          : `<span class="mk-gauge-num ink-${band.key}">${value.toFixed(1)}</span>`;
        html += `<span class="cai-lens-bar">`;
        if (value != null) {
          html += `<span class="cai-lens-fill fill-${band.key}" style="width:${Math.max(2, Math.round(value))}%"></span>`;
        }
        html += `</span>`;
        if (lens.note) {
          html += `<span class="mk-gauge-note">${renderInline(String(lens.note))}</span>`;
        }
        html += `</div>`;
      }
      html += `</div>`;

      if (footnote) {
        html += `<p class="mk-gauges-foot">${renderInline(footnote)}</p>`;
      }
      root.innerHTML = html;
    }
  }
);
