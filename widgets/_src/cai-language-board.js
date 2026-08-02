// <cai-language-board languages='[{"name":"C#","count":74,"median":51.3,"href":"/surveys/lang/csharp/"}]'
//                     ungrouped='{"count":32,"note":"…"}'
//                     kicker="…" heading="…" lede="…" empty-text="…">
//
// The way in for a reader who arrived without a language in mind: every measured language as a
// row, its median drawn as a band-inked bar against the same 0–100 scale the rest of the site
// uses, and a search over them.
//
// The chart IS the list. Sixteen medians in a column of text is a table nobody compares; the
// same sixteen as bars against one axis answers "which languages measure well here" at a
// glance, and each bar is the link to that language's field guide.
//
// The bars share ONE axis, always 0–100, and it is never scaled to the data. These are CAI
// scores: 60 means the same thing on every row, and a chart that stretched the range to make
// the differences look bigger would be inventing a spread the measurements do not have.
//
// The ungrouped row is rendered separately and never sorted away, because the counts have to
// reconcile: the headline counts every measured project, the language rows only the ones whose
// primary language the scan could name, and a reader who adds up the column and finds it short
// has been quietly misled.
//
// DATA ONLY: no api-base, no liveLoad, no fetch.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";
import { SCORECARD_CSS } from "./scorecard.js";
import { bandFor } from "./cai.js";

// The parked cutlines, drawn behind the bars so a median can be read as a band and not just a number.
const CUTS = [25, 50, 70, 90];

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-board { max-width: 52rem; margin: 0 auto; }
.mk-board-ctl { display: flex; flex-wrap: wrap; gap: 0.6rem; align-items: center; margin-bottom: 0.5rem; }
.mk-board-ctl input, .mk-board-ctl select { font: inherit; font-size: var(--fs-sm); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 7px 11px; }
.mk-board-ctl input { flex: 1 1 14rem; min-width: 0; }
.mk-board-ctl input:focus-visible, .mk-board-ctl select:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-board-count { font-size: var(--fs-xs); color: var(--muted); margin: 0 0 0.8rem; }

.mk-rows { display: grid; gap: 2px; }
a.mk-row, div.mk-row { display: grid; grid-template-columns: 9.5rem 1fr 3.2rem; gap: 0 0.9rem;
  align-items: center; padding: 9px 12px; border-radius: var(--r-sm); text-decoration: none; color: inherit; }
a.mk-row:hover, a.mk-row:focus-visible { background: var(--surface-2); text-decoration: none; }
.mk-row-name { font-weight: 650; font-size: var(--fs-sm); }
.mk-row-n { display: block; font-size: var(--fs-2xs); color: var(--muted); font-weight: 500; }
.mk-row-track { position: relative; height: 12px; border-radius: var(--r-full);
  background: var(--surface-2); overflow: hidden; }
.mk-row-fill { position: absolute; inset: 0 auto 0 0; border-radius: var(--r-full); }
/* The cutlines must read on BOTH sides of the fill. A ground-coloured tick disappears against the
   unfilled track in light mode, where track and ground are nearly the same value. */
.mk-row-cut { position: absolute; top: 0; bottom: 0; width: 1px;
  background: color-mix(in srgb, var(--muted) 60%, transparent); }
.mk-row-med { font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-weight: 700;
  font-size: var(--fs-sm); text-align: right; }
.mk-axis { display: grid; grid-template-columns: 9.5rem 1fr 3.2rem; gap: 0 0.9rem; padding: 0 12px;
  margin-top: 0.4rem; }
.mk-axis-scale { position: relative; height: 15px; grid-column: 2; }
.mk-axis-scale span { position: absolute; transform: translateX(-50%); font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); }
/* The end labels sit ON the ends of the axis; centring them would hang half of each outside it. */
.mk-axis-scale span:first-child { transform: none; }
.mk-axis-scale span:last-child { transform: translateX(-100%); }
.mk-board-note { margin: 1.1rem 0 0; padding-top: 0.9rem; border-top: 1px solid var(--border);
  font-size: var(--fs-xs); color: var(--muted); line-height: 1.6; }
.mk-board-empty { padding: 1.6rem 0; text-align: center; color: var(--muted); font-size: var(--fs-sm); }
@media (max-width: 34rem) {
  a.mk-row, div.mk-row, .mk-axis { grid-template-columns: 7rem 1fr 3rem; gap: 0 0.5rem; }
}
`;

const SORTS = [
  ["count", "Most projects first"],
  ["median-desc", "Highest median first"],
  ["median-asc", "Lowest median first"],
  ["name", "Name A–Z"],
];

customElements.define(
  "cai-language-board",
  class extends CaiIsland {
    render(root) {
      const rows = (this.json("languages", []) || []).filter((l) => l && l.name);
      const ungrouped = this.json("ungrouped", null);
      this._sort = this._sort || "count";
      this._q = this._q || "";

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (rows.length === 0) {
        root.innerHTML = html;
        return;
      }

      html += `<div class="mk-board">`;
      html += `<div class="mk-board-ctl">`;
      html += `<input type="search" class="mk-q" placeholder="Search languages"`
        + ` aria-label="Search languages" value="${escapeHtml(this._q)}">`;
      html += `<select class="mk-sort" aria-label="Sort">`;
      for (const [v, label] of SORTS) {
        html += `<option value="${v}"${v === this._sort ? " selected" : ""}>${escapeHtml(label)}</option>`;
      }
      html += `</select></div>`;
      html += `<p class="mk-board-count" role="status"></p>`;
      html += `<div class="mk-rows"></div>`;

      // One shared axis, stated once. The bars are all on it and none of them is scaled to fit.
      html += `<div class="mk-axis"><div class="mk-axis-scale">`;
      for (const c of [0, ...CUTS, 100]) {
        html += `<span style="left:${c}%">${c}</span>`;
      }
      html += `</div></div>`;

      if (ungrouped && Number(ungrouped.count) > 0) {
        html += `<p class="mk-board-note">${escapeHtml(
          `${ungrouped.count} measured project${Number(ungrouped.count) === 1 ? " has" : "s have"} `
          + `no primary language the scan could name, so ${Number(ungrouped.count) === 1 ? "it is" : "they are"} `
          + `not grouped above. They are counted in the total.`)}</p>`;
      }

      html += `</div>`;
      root.innerHTML = html;

      const q = root.querySelector(".mk-q");
      const sort = root.querySelector(".mk-sort");
      q?.addEventListener("input", () => { this._q = q.value; this.paint(root, rows); });
      sort?.addEventListener("change", () => { this._sort = sort.value; this.paint(root, rows); });
      this.paint(root, rows);
    }

    paint(root, rows) {
      const host = root.querySelector(".mk-rows");
      const count = root.querySelector(".mk-board-count");
      if (!host || !count) { return; }

      const q = this._q.trim().toLowerCase();
      const shown = rows
        .filter((l) => !q || String(l.name).toLowerCase().includes(q))
        .sort({
          count: (a, b) => Number(b.count) - Number(a.count),
          "median-desc": (a, b) => Number(b.median) - Number(a.median),
          "median-asc": (a, b) => Number(a.median) - Number(b.median),
          name: (a, b) => String(a.name).localeCompare(String(b.name)),
        }[this._sort] || (() => 0));

      count.textContent = shown.length === rows.length
        ? `${rows.length} language${rows.length === 1 ? "" : "s"} with a field guide`
        : `${shown.length} of ${rows.length} languages`;

      if (shown.length === 0) {
        host.innerHTML = `<p class="mk-board-empty">${escapeHtml(
          this.getAttribute("empty-text") || "No language here matches that.")}</p>`;
        return;
      }

      let html = "";
      for (const l of shown) {
        const median = Number(l.median);
        const band = bandFor(median);
        const n = Number(l.count) || 0;
        const tag = l.href ? "a" : "div";
        const href = l.href ? ` href="${escapeHtml(l.href)}"` : "";

        html += `<${tag} class="mk-row"${href}>`;
        html += `<span class="mk-row-name">${escapeHtml(l.name)}`
          + `<span class="mk-row-n">${n} project${n === 1 ? "" : "s"}</span></span>`;
        html += `<span class="mk-row-track" role="img"`
          + ` aria-label="${escapeHtml(`median ${median.toFixed(1)}, ${band.label}`)}">`;
        html += `<span class="mk-row-fill fill-${band.key}" style="width:${Math.max(1, Math.min(100, median))}%"></span>`;
        for (const c of CUTS) {
          html += `<span class="mk-row-cut" style="left:${c}%"></span>`;
        }
        html += `</span>`;
        html += `<span class="mk-row-med ink-${band.key}">${median.toFixed(1)}</span>`;
        html += `</${tag}>`;
      }
      host.innerHTML = html;
    }
  }
);
