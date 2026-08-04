// <cai-language-board languages='[{"name":"C#","count":369,"median":49.7,"low":41.2,"high":58.0,
//                                  "dist":[0,2,9,40,120,90,60,30,12,6],"depth":22,"depthOf":180,
//                                  "href":"/surveys/lang/csharp/"}]'
//                     ungrouped='{"count":63}' depth-note="…"
//                     kicker="…" heading="…" lede="…" empty-text="…">
//
// The way in for a reader who arrived without a language in mind: every measured language as a
// row, the SPREAD of its projects' scores drawn against the same 0–100 scale the rest of the site
// uses, how deep the survey of them reached, and a search over them.
//
// The chart IS the list. Sixteen medians in a column of text is a table nobody compares, and a
// single median per language cannot tell a field clustered around 50 from one split between
// abandoned and excellent — those are different fields and they were drawing as the same bar. The
// distribution shows the shape; the median and the middle half sit beside it as the numbers.
//
// The bars share ONE axis, always 0–100, and it is never scaled to the data. These are CAI
// scores: 60 means the same thing on every row, and a chart that stretched the range to make the
// differences look bigger would be inventing a spread the measurements do not have. The bin
// HEIGHTS are scaled per row, because the question a row answers is "what shape is this field",
// not "is this language bigger than that one" — the project count answers that, in text.
//
// DEPTH is how many of the index's dimensions actually resolved on a project. It is a statement
// about how much of the survey completed, never about how good the code is, and the note under
// the board says so — a number in a column beside a score will otherwise be read as a grade.
// It is missing for projects last surveyed before per-dimension outcomes were recorded, so the
// cell reads "—" rather than 0: nothing resolved and nothing was written down are different
// claims, and only one of them is about the project.
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

// The parked cutlines, drawn behind the distribution so a score can be read as a band and not
// just a number.
const CUTS = [25, 50, 70, 90];

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-board { max-width: 56rem; margin: 0 auto; }
.mk-board-ctl { display: flex; flex-wrap: wrap; gap: 0.6rem; align-items: center; margin-bottom: 0.5rem; }
.mk-board-ctl input, .mk-board-ctl select { font: inherit; font-size: var(--fs-sm); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 7px 11px; }
.mk-board-ctl input { flex: 1 1 14rem; min-width: 0; }
.mk-board-ctl input:focus-visible, .mk-board-ctl select:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-board-count { font-size: var(--fs-xs); color: var(--muted); margin: 0 0 0.8rem; }

/* One grid, declared once and shared by the column heads, every row and the axis — so a heading
   can never drift off the column it names. */
.mk-rows { display: grid; gap: 2px; }
a.mk-row, div.mk-row, .mk-head, .mk-axis {
  display: grid; grid-template-columns: 9.5rem 1fr 4.4rem 3.4rem; gap: 0 0.9rem; align-items: center; }
a.mk-row, div.mk-row { padding: 9px 12px; border-radius: var(--r-sm); text-decoration: none; color: inherit; }
a.mk-row:hover, a.mk-row:focus-visible { background: var(--surface-2); text-decoration: none; }
.mk-head, .mk-axis { padding: 0 12px; }
.mk-head { margin-bottom: 0.35rem; font-size: var(--fs-2xs); color: var(--muted);
  text-transform: uppercase; letter-spacing: 0.06em; }
.mk-head span:nth-child(3), .mk-head span:nth-child(4) { text-align: right; }
.mk-row-name { font-weight: 650; font-size: var(--fs-sm); }
.mk-row-n { display: block; font-size: var(--fs-2xs); color: var(--muted); font-weight: 500; }

/* The distribution. Bars grow from the baseline, each bin fixed to its ten points of the axis. */
.mk-dist { position: relative; height: 26px; background: var(--surface-2);
  border-radius: var(--r-sm); overflow: hidden; }
/* The middle half, shaded behind the bars. Faint on purpose — it is context for the bars, not a
   sixth series — but not so faint it cannot be found: at 7% it was invisible in both themes. */
.mk-dist-iqr { position: absolute; top: 0; bottom: 0;
  background: color-mix(in srgb, var(--ink) 12%, transparent); }
.mk-dist-bin { position: absolute; bottom: 0; border-radius: 1px 1px 0 0; }
/* The cutlines must read on BOTH sides of a bar. A ground-coloured tick disappears against the
   empty track in light mode, where track and ground are nearly the same value. */
.mk-dist-cut { position: absolute; top: 0; bottom: 0; width: 1px;
  background: color-mix(in srgb, var(--muted) 55%, transparent); }
.mk-dist-med { position: absolute; top: 0; bottom: 0; width: 2px; transform: translateX(-1px);
  background: var(--ink); opacity: 0.75; }
.mk-row-med { font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-weight: 700;
  font-size: var(--fs-sm); text-align: right; }
.mk-row-iqr { display: block; font-family: var(--font-mono); font-size: var(--fs-2xs);
  color: var(--muted); font-weight: 500; }
.mk-row-depth { font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-weight: 650;
  font-size: var(--fs-sm); text-align: right; }
.mk-row-depth.is-none { color: var(--muted); font-weight: 500; }
.mk-row-depth span { display: block; font-family: var(--font-body, inherit); font-size: var(--fs-2xs);
  color: var(--muted); font-weight: 500; }

.mk-axis { margin-top: 0.4rem; }
.mk-axis-scale { position: relative; height: 15px; grid-column: 2; }
.mk-axis-scale span { position: absolute; transform: translateX(-50%); font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); }
/* The end labels sit ON the ends of the axis; centring them would hang half of each outside it. */
.mk-axis-scale span:first-child { transform: none; }
.mk-axis-scale span:last-child { transform: translateX(-100%); }
.mk-board-note { margin: 1.1rem 0 0; padding-top: 0.9rem; border-top: 1px solid var(--border);
  font-size: var(--fs-xs); color: var(--muted); line-height: 1.6; }
.mk-board-note + .mk-board-note { margin-top: 0.5rem; padding-top: 0; border-top: 0; }
.mk-board-empty { padding: 1.6rem 0; text-align: center; color: var(--muted); font-size: var(--fs-sm); }
@media (max-width: 40rem) {
  a.mk-row, div.mk-row, .mk-head, .mk-axis { grid-template-columns: 7rem 1fr 3.6rem 2.8rem; gap: 0 0.5rem; }
}
`;

const SORTS = [
  ["count", "Most projects first"],
  ["median-desc", "Highest median first"],
  ["median-asc", "Lowest median first"],
  ["depth", "Deepest survey first"],
  ["name", "Name A–Z"],
];

/** A number, or null when the value is absent or not a number — never a silent 0. */
function num(value) {
  const n = Number(value);
  return value === null || value === undefined || value === "" || Number.isNaN(n) ? null : n;
}

/** A percentage position on the fixed 0–100 axis. */
function pos(score) {
  return Math.max(0, Math.min(100, Number(score)));
}

customElements.define(
  "cai-language-board",
  class extends CaiIsland {
    render(root) {
      const rows = (this.json("languages", []) || []).filter((l) => l && l.name);
      const ungrouped = this.json("ungrouped", null);
      const depthNote = this.getAttribute("depth-note");
      // The depth column is drawn only where there is depth to put in it. A column of dashes
      // teaches a reader nothing and costs the distribution the width.
      this._hasDepth = rows.some((l) => num(l.depth) !== null);
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
        if (v === "depth" && !this._hasDepth) { continue; }
        html += `<option value="${v}"${v === this._sort ? " selected" : ""}>${escapeHtml(label)}</option>`;
      }
      html += `</select></div>`;
      html += `<p class="mk-board-count" role="status"></p>`;

      // The columns are named, because a bare number in a column beside a score is read as a
      // second score. "Dimensions" is the honest short name for what the last column holds.
      html += `<div class="mk-head" aria-hidden="true"><span>Language</span>`
        + `<span>Distribution of CAI scores</span><span>Median</span>`
        + `<span>${this._hasDepth ? "Depth" : ""}</span></div>`;
      html += `<div class="mk-rows"></div>`;

      // One shared axis, stated once. Every distribution is on it and none of them is scaled to fit.
      html += `<div class="mk-axis"><div class="mk-axis-scale">`;
      for (const c of [0, ...CUTS, 100]) {
        html += `<span style="left:${c}%">${c}</span>`;
      }
      html += `</div></div>`;

      if (depthNote) {
        html += `<p class="mk-board-note">${escapeHtml(depthNote)}</p>`;
      }

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
          // A language with no recorded depth sorts last rather than as a zero — it is unmeasured,
          // not shallow.
          depth: (a, b) => (num(b.depth) ?? -1) - (num(a.depth) ?? -1),
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
        html += this.distribution(l, median, band);
        html += `<span class="mk-row-med ink-${band.key}">${median.toFixed(1)}`
          + this.spread(l)
          + `</span>`;
        if (this._hasDepth) {
          html += this.depth(l);
        } else {
          html += `<span></span>`;
        }

        html += `</${tag}>`;
      }
      host.innerHTML = html;
    }

    /** The field's shape: ten fixed ten-point bins, the middle half shaded, the median marked. */
    distribution(l, median, band) {
      const bins = Array.isArray(l.dist) ? l.dist.map((b) => Number(b) || 0) : null;
      const low = num(l.low);
      const high = num(l.high);

      // What a screen reader is told is the same claim the picture makes, in words.
      let label = `median ${median.toFixed(1)}, ${band.label}`;
      if (low !== null && high !== null) {
        label += `; half of them between ${low.toFixed(1)} and ${high.toFixed(1)}`;
      }

      let html = `<span class="mk-dist" role="img" aria-label="${escapeHtml(label)}">`;

      // A board published before the distribution existed still renders: one band-inked bar to the
      // median, exactly as it always did.
      if (!bins || bins.length === 0 || bins.every((b) => b === 0)) {
        html += `<span class="mk-dist-bin fill-${band.key}" style="left:0;width:${pos(median)}%;height:100%"></span>`;
      } else {
        if (low !== null && high !== null && high > low) {
          html += `<span class="mk-dist-iqr" style="left:${pos(low)}%;width:${pos(high) - pos(low)}%"></span>`;
        }

        const width = 100 / bins.length;
        const tallest = Math.max(...bins);
        bins.forEach((value, i) => {
          if (value <= 0) { return; }
          // A minimum of 8% so a bin holding a single project is visible rather than a hairline —
          // one project IS the tail of the distribution, and a tail rounded to nothing is a lie
          // about the range.
          const height = Math.max(8, Math.round((value / tallest) * 100));
          const key = bandFor((i + 0.5) * width).key;
          html += `<span class="mk-dist-bin fill-${key}"`
            + ` style="left:${i * width}%;width:${width}%;height:${height}%"></span>`;
        });
      }

      for (const c of CUTS) {
        html += `<span class="mk-dist-cut" style="left:${c}%"></span>`;
      }

      html += `<span class="mk-dist-med" style="left:${pos(median)}%"></span>`;
      return html + `</span>`;
    }

    /** The middle half, under the median — the number that stops the median reading as a verdict. */
    spread(l) {
      const low = num(l.low);
      const high = num(l.high);
      return low === null || high === null
        ? ""
        : `<span class="mk-row-iqr">${low.toFixed(0)}–${high.toFixed(0)}</span>`;
    }

    /** Survey depth: dimensions resolved, or an em dash where none has been recorded yet. */
    depth(l) {
      const depth = num(l.depth);
      if (depth === null) {
        return `<span class="mk-row-depth is-none" title="No project in this language has been surveyed`
          + ` since per-dimension outcomes were recorded.">—</span>`;
      }

      const of = num(l.depthOf);
      const label = of === null
        ? `median depth ${depth} dimensions resolved`
        : `median depth ${depth} dimensions resolved, recorded for ${of} project${of === 1 ? "" : "s"}`;
      return `<span class="mk-row-depth" aria-label="${escapeHtml(label)}">${depth}`
        + `<span>dims</span></span>`;
    }
  }
);
