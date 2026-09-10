// <cai-trend series="[54.2,57.1,61]" first-date="4 March 2026" last-date="29 July 2026"
//            kicker="§4 Series" tip="…" heading="…" lede="…" caption="…"
//            figures='[{"label":"Median",
//                       "from":{"value":"45.2","sub":"across 580 measured codebases, 16 July 2026"},
//                       "to":{"value":"49.5","sub":"across 3,542 measured codebases, 10 Sep 2026"}}]'>
//
// How one repository's score moved across its own scans. A line, not a table — a reader
// asks "is it getting better?", and a column of numbers makes them do the differencing
// themselves.
//
// THE Y-AXIS IS SNAPPED TO THE CAI CUTLINES (0/25/50/70/90/100), never autoscaled to the
// data. Free autoscale is the standard way a chart lies: three scans of 61.0, 61.2 and 61.4
// become a triumphant climb because the axis quietly spans 0.4 points. Snapping to the bands
// the score is DEFINED against means the slope you see is the slope that matters — and the
// cutlines the line sits between are drawn, so "it rose but stayed Fair" is legible.
//
// One measurement renders as the number and its date, with no chart: a line through a single
// point is a decoration, and drawing one would imply a trajectory nobody measured.
//
// TWO LAYOUTS, chosen by whether the page passes a `kicker`:
//
//   without a kicker  the survey pages and the corpus archive: a section head above a chart
//                     centred in a 46rem measure. Unchanged, byte for byte — those pages pass
//                     series, dates, heading and caption and nothing else, and a chart that
//                     moved because a different page needed a rail would be this widget
//                     breaking three thousand pages to fix one.
//   with a kicker     the corpus sheet: the section rail (rail.js) — the label and its (i) in
//                     a 112px column, the chart taking the whole of the rest.
//
// `figures` states the endpoints of the movement the line draws, as text: a line says "it rose",
// and only the two numbers with the POPULATIONS they were taken over say what rose and among
// how many. The chart cannot derive them — a median across 580 codebases and a median across
// 3,542 are two different measurements, and the series carries no populations at all.
//
// ★★ A PAIR IS ONE THING, NOT TWO CELLS. The sheet this island lives on was redrawn because a
// reader met four numbers in four boxes and said their eye had nowhere to rest. "45.2" and
// "49.5" in two cells rebuild exactly that; "45.2 → 49.5" under the word Median is one fact with
// a direction, which is what a trend section is for. So an entry is a quantity with two ends —
// {label, from:{value,sub}, to:{value,sub}} — and the join is drawn here rather than left to a
// reader's eye.
//
// THE BASIS IS PRINTED ONCE WHEN BOTH ENDS SHARE IT. Two identical lines under 580 → 3,542 say
// the two ends were taken over different things and happen to read alike, which is the opposite
// of true. Two DIFFERENT bases are printed in full, because a median over 580 codebases and a
// median over 3,542 are the fact a reader most needs and the one a single line would hide.
//
// The emitter owns the words — "across 3,542 measured codebases, 10 September 2026" is composed
// upstream, never assembled here out of parts this island would have to keep in step with it.
//
// DATA ONLY, deliberately: no api-base, no liveLoad. Every number arrives as a prop from the
// page that renders it, so this island cannot plot one repository's history under another
// repository's name.

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
import { HINT_CSS } from "./hint.js";
import { RAIL_CSS, railHtml, headBelowRailHtml } from "./rail.js";
import { bandFor } from "./cai.js";

// The band cutlines. A snapped axis picks the pair of these that brackets the data.
const CUTS = [0, 25, 50, 70, 90, 100];

// Two scores are the same score when they round to the same tenth — the precision every surface
// here publishes at. Comparing floats exactly would leave 82.5 and 82.50000000000001 as a "change".
const SAME = 0.05;

/**
 * A series reduced to the marks worth drawing: a run of identical scores keeps its FIRST and LAST
 * and drops everything between.
 *
 * A repository scanned nightly sits at one score for weeks, and drawing a dot per scan turned a
 * flat stretch into a solid bar of overlapping circles — a hundred marks saying what two marks and
 * the line between them already say. Each kept mark carries its own index, so the line's geometry
 * is UNCHANGED: dropping interior points of a flat run cannot move the path, because they were
 * collinear with its ends. The time axis keeps its true spacing, and a plateau still looks as long
 * as it was — which a chart that merely deleted the duplicates would get wrong.
 *
 * `run` travels with each mark so the tooltip can say what the missing dots said: how many scans
 * held that score.
 */
export function collapseFlatRuns(series) {
  const marks = [];
  let i = 0;
  while (i < series.length) {
    let end = i;
    while (end + 1 < series.length && Math.abs(series[end + 1] - series[i]) < SAME) {
      end++;
    }

    const run = end - i + 1;
    marks.push({ i, v: series[i], run });
    // A run of one or two is already its own first-and-last; only a third point can be interior.
    if (end > i) {
      marks.push({ i: end, v: series[end], run });
    }

    i = end + 1;
  }

  return marks;
}

const W = 720;
const H = 240;
const PAD = { top: 26, right: 18, bottom: 34, left: 40 };

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + HINT_CSS + RAIL_CSS + `
.mk-trend { max-width: 46rem; margin: 0 auto; }
.mk-trend-plot { position: relative; }
.mk-trend svg { display: block; width: 100%; height: auto; overflow: visible; }
.mk-trend-grid { stroke: var(--border); stroke-width: 1; }
.mk-trend-cut { fill: var(--muted); font-family: var(--font-mono); font-size: 11px; }
.mk-trend-line { fill: none; stroke: var(--accent); stroke-width: 2;
  stroke-linejoin: round; stroke-linecap: round; }
.mk-trend-area { fill: var(--accent); opacity: 0.10; }
.mk-trend-dot { fill: var(--accent); stroke: var(--bg); stroke-width: 2; }
.mk-trend-end { stroke: var(--bg); stroke-width: 2.5; }
/* The shared .fill-* classes set the background property, which an SVG circle ignores. */
.mk-trend-end.fill-exemplary { fill: var(--band-exemplary); }
.mk-trend-end.fill-healthy { fill: var(--band-healthy); }
.mk-trend-end.fill-fair { fill: var(--band-fair); }
.mk-trend-end.fill-poor { fill: var(--band-poor); }
.mk-trend-end.fill-critical { fill: var(--band-critical); }
.mk-trend-endlabel { font-family: var(--font-mono); font-size: 15px; font-weight: 700; }
.mk-trend-date { fill: var(--muted); font-size: 12px; }
.mk-trend-hit { fill: transparent; cursor: default; }
.mk-trend-hit:hover + .mk-trend-dot, .mk-trend-hit:focus + .mk-trend-dot { stroke: var(--accent-strong); }
.mk-trend-tip { position: absolute; transform: translate(-50%, -100%);
  background: var(--surface-2); border: 1px solid var(--border-strong); border-radius: var(--r-sm);
  box-shadow: var(--shadow-overlay); padding: 6px 10px; pointer-events: none; white-space: nowrap;
  font-size: var(--fs-xs); color: var(--ink); opacity: 0; transition: opacity 90ms ease; }
.mk-trend-tip.on { opacity: 1; }
.mk-trend-tip b { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.mk-trend-solo { display: flex; align-items: baseline; justify-content: center; gap: 0.6rem;
  padding: 1.6rem 0 0.4rem; }
.mk-trend-solo-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-4xl); line-height: 1; }
.mk-trend-solo-date { font-size: var(--fs-sm); color: var(--muted); }
.mk-trend-sum { margin: 0.9rem auto 0; max-width: 46rem; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; text-align: center; }

/* ── the endpoints, BESIDE the chart ─────────────────────────────────────────
   The chart takes what the figures do not need (flex: 1), which is the design's shape: a line
   long enough to read a slope off, and the two quantities stated beside it. Each stack is capped
   at 220px so a basis sentence wraps instead of pushing the line down to a stub. */
.mk-trend-row { display: flex; gap: 32px; align-items: flex-start; }
/* The line never gives up more than two fifths of the row: shrink:0 on a 60% basis means the
   FIGURES yield when the column narrows, wrapping into one stack, rather than both giving way
   proportionally until the chart is a 117px stub — which is what a plain flex:1 produced at
   680px, measured. */
.mk-trend-row > .mk-trend { flex: 1 0 60%; min-width: 0; }
/* The pairs are a COLUMN beside the line, at every width, and that is a decision rather than
   what the wrapping happened to do. The design puts its two stacks in a row because it prints no
   basis; ours each carry a sentence naming the population, and two of those side by side in the
   third of the column the chart leaves over wrap into ragged blocks — and then unwrap into a row
   again at some other width, so the section would be a different shape on a laptop and on a
   desktop. One column reads the same everywhere. */
.mk-trend-figs { display: flex; flex-direction: column; gap: 16px; flex: 0 1 auto; min-width: 0; }
.mk-trend-fig { display: flex; flex-direction: column; gap: 2px; flex: none;
  min-width: 0; max-width: 260px; }
/* The same eyebrow the section rail uses: a label over a figure, never a second heading. */
.mk-trend-fig-label { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
  color: var(--muted); font-weight: 700; }
.mk-trend-fig-pair { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 18px; font-weight: 700; line-height: 1.2; color: var(--ink); overflow-wrap: anywhere; }
.mk-trend-fig-sub { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-2xs); color: var(--muted); line-height: 1.45; }
/* Under the rail's own stacking width the figures go beneath the line, one column: 220px of
   stack beside a 130px chart is neither a chart nor a figure. */
@media (max-width: 560px) {
  .mk-trend-row { flex-direction: column; gap: 16px; }
  .mk-trend-figs { flex-direction: column; gap: 14px; }
  .mk-trend-fig { max-width: none; }
}

/* ── inside the rail ────────────────────────────────────────────────────────
   The chart's 46rem measure and its auto margins are a CARD centred on a page that has no other
   column. In the rail the content column IS the measure, and a 736px card inside a 950px column
   is a second, narrower page drawn inside the first. These are descendant selectors rather than
   a modifier class on purpose: the no-kicker markup then cannot change at all, which is what the
   snapshot tests assert. */
.rail-body .mk-trend { max-width: none; margin-left: 0; margin-right: 0; }
.rail-body .mk-trend-sum { max-width: none; margin-left: 0; margin-right: 0; text-align: left; }
@media (prefers-reduced-motion: reduce) { .mk-trend-tip { transition: none; } }
`;

/** The pair of cutlines that brackets every value, so the axis is the vocabulary, not the data. */
function snapDomain(values) {
  const lo = Math.min(...values);
  const hi = Math.max(...values);
  let min = 0;
  let max = 100;
  for (const c of CUTS) { if (c <= lo) { min = c; } }
  for (let i = CUTS.length - 1; i >= 0; i--) { if (CUTS[i] >= hi) { max = CUTS[i]; } }
  // A run sitting entirely ON a cutline collapses the axis to zero height and turns every
  // plotted y into NaN. Widen downwards first so the ceiling stays a real cutline.
  if (max - min < 10) { min = Math.max(0, Math.min(min, max - 25)); }
  if (max - min < 10) { max = Math.min(100, min + 25); }
  return { min, max };
}

function fmt(n) {
  return (Math.round(n * 10) / 10).toFixed(1);
}

customElements.define(
  "cai-trend",
  class extends CaiIsland {
    render(root) {
      const series = (this.json("series", []) || [])
        .map(Number)
        .filter((n) => Number.isFinite(n));
      const kicker = (this.getAttribute("kicker") || "").trim();

      // The chart and the endpoints stand side by side when both exist. The row only appears
      // when there are figures, so a page that passes none renders the markup it always did.
      const figures = this.figuresHtml();
      const plot = this.plotHtml(series);
      const body = figures ? `<div class="mk-trend-row">${plot}${figures}</div>` : plot;

      let html = `<style>${CSS}</style>`;
      html += kicker
        ? railHtml({
            kicker,
            tip: this.getAttribute("tip"),
            content: headBelowRailHtml(this) + body,
          })
        : sectionHeadHtml(this) + body;

      root.innerHTML = html;
      // Only a line has points to answer for; a solo reading and an empty series have none.
      if (series.length > 1) {
        this.wireTips(root, series);
      }
    }

    /**
     * The movement's endpoints: one stack per quantity, each a label over "from → to" over the
     * population each end was measured over.
     *
     * An entry needs both ends to be a movement, so one missing either is dropped rather than
     * drawn as an arrow into nothing — the same rule cai-figure-band applies to a figure with no
     * value, and for the same reason: a half-drawn figure reads as a number that went missing.
     */
    figuresHtml() {
      const figures = (this.json("figures", []) || []).filter(
        (f) =>
          f &&
          f.from != null && f.from.value != null && String(f.from.value) !== "" &&
          f.to != null && f.to.value != null && String(f.to.value) !== ""
      );
      if (figures.length === 0) { return ""; }

      let h = `<div class="mk-trend-figs">`;
      for (const f of figures) {
        h += `<div class="mk-trend-fig">`;
        if (f.label != null && String(f.label) !== "") {
          h += `<span class="mk-trend-fig-label">${escapeHtml(String(f.label))}</span>`;
        }
        h += `<span class="mk-trend-fig-pair">`
          + `${escapeHtml(String(f.from.value))} \u2192 ${escapeHtml(String(f.to.value))}`
          + `</span>`;
        // One basis when both ends share it, two when they differ. Repeating "measured
        // codebases" under 580 → 3,542 would say the ends were taken over different things.
        const from = f.from.sub == null ? "" : String(f.from.sub);
        const to = f.to.sub == null ? "" : String(f.to.sub);
        const bases = from === to ? [from] : [from, to];
        for (const b of bases.filter((x) => x !== "")) {
          h += `<span class="mk-trend-fig-sub">${escapeHtml(b)}</span>`;
        }
        h += `</div>`;
      }
      return h + `</div>`;
    }

    /** The chart itself: nothing, a single stated reading, or the line. */
    plotHtml(series) {
      const firstDate = this.getAttribute("first-date");
      const lastDate = this.getAttribute("last-date");
      const caption = this.getAttribute("caption");

      if (series.length === 0) {
        return "";
      }

      if (series.length === 1) {
        // One measurement is a fact, not a trend. State it and stop.
        const only = series[0];
        const band = bandFor(only);
        let solo = `<div class="mk-trend"><p class="mk-trend-solo">`;
        solo += `<span class="mk-trend-solo-num ink-${band.key}">${fmt(only)}</span>`;
        if (lastDate || firstDate) {
          solo += `<span class="mk-trend-solo-date">measured ${escapeHtml(lastDate || firstDate)}</span>`;
        }
        solo += `</p></div>`;
        if (caption) { solo += `<p class="mk-trend-sum">${renderInline(caption)}</p>`; }
        return solo;
      }

      const { min, max } = snapDomain(series);
      const plotW = W - PAD.left - PAD.right;
      const plotH = H - PAD.top - PAD.bottom;
      const x = (i) => PAD.left + (plotW * i) / (series.length - 1);
      const y = (v) => PAD.top + plotH * (1 - (v - min) / (max - min));

      const last = series[series.length - 1];
      const lastBand = bandFor(last);
      // The marks, and the path through them. Both come from the collapsed set: the interior of a
      // flat run is collinear with its ends, so the drawn line is identical and the markup is not.
      const marks = collapseFlatRuns(series);
      const points = marks.map((m) => `${x(m.i).toFixed(1)},${y(m.v).toFixed(1)}`);

      let html = `<div class="mk-trend"><div class="mk-trend-plot">`;
      html += `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="${escapeHtml(
        `${series.length} measurements, from ${fmt(series[0])} to ${fmt(last)}.`)}">`;

      // The cutlines inside the domain, drawn and labelled — the axis IS the band vocabulary.
      for (const c of CUTS) {
        if (c < min || c > max) { continue; }
        const yc = y(c).toFixed(1);
        html += `<line class="mk-trend-grid" x1="${PAD.left}" y1="${yc}" x2="${W - PAD.right}" y2="${yc}"></line>`;
        html += `<text class="mk-trend-cut" x="${PAD.left - 8}" y="${yc}" text-anchor="end" dominant-baseline="middle">${c}</text>`;
      }

      html += `<path class="mk-trend-area" d="M${x(0).toFixed(1)},${y(min).toFixed(1)} L${points.join(" L")} L${x(series.length - 1).toFixed(1)},${y(min).toFixed(1)} Z"></path>`;
      html += `<polyline class="mk-trend-line" points="${points.join(" ")}" vector-effect="non-scaling-stroke"></polyline>`;

      // Only the first and last dates are labelled: the scans between them are a count, and
      // stamping every one of them turns an axis into a wall of text nobody reads.
      if (firstDate) {
        html += `<text class="mk-trend-date" x="${PAD.left}" y="${H - 10}" text-anchor="start">${escapeHtml(firstDate)}</text>`;
      }
      if (lastDate) {
        html += `<text class="mk-trend-date" x="${W - PAD.right}" y="${H - 10}" text-anchor="end">${escapeHtml(lastDate)}</text>`;
      }

      marks.forEach((m) => {
        const isLast = m.i === series.length - 1;
        const cx = x(m.i).toFixed(1);
        const cy = y(m.v).toFixed(1);
        // A hit target far bigger than the mark, so a 5px dot is not a 5px target. `data-run` is
        // what the dropped dots knew: a mark capping a flat stretch answers for the whole stretch.
        html += `<circle class="mk-trend-hit" cx="${cx}" cy="${cy}" r="18" tabindex="0"`
          + ` data-i="${m.i}" data-v="${fmt(m.v)}" data-run="${m.run}"></circle>`;
        html += isLast
          ? `<circle class="mk-trend-end fill-${lastBand.key}" cx="${cx}" cy="${cy}" r="5.5"></circle>`
          : `<circle class="mk-trend-dot" cx="${cx}" cy="${cy}" r="4"></circle>`;
      });

      // One direct label, on the point a reader came for. Not a number on every dot.
      html += `<text class="mk-trend-endlabel ink-${lastBand.key}" x="${x(series.length - 1).toFixed(1)}" y="${(y(last) - 14).toFixed(1)}" text-anchor="end">${fmt(last)}</text>`;
      html += `</svg>`;
      html += `<div class="mk-trend-tip" hidden></div>`;
      html += `</div>`;

      // The same figures as text, for a reader who never runs the script and for one who
      // would rather read than measure a slope with their eye.
      const moved = last - series[0];
      const direction = Math.abs(moved) < 0.05
        ? "unchanged"
        : `${moved > 0 ? "up" : "down"} ${fmt(Math.abs(moved))}`;
      html += `<p class="mk-trend-sum">${escapeHtml(
        `${series.length} measurements${firstDate ? `, from ${firstDate}` : ""}${lastDate ? ` to ${lastDate}` : ""}: `
        + `${fmt(series[0])} to ${fmt(last)} — ${direction}.`)}</p>`;
      if (caption) { html += `<p class="mk-trend-sum">${renderInline(caption)}</p>`; }
      html += `</div>`;

      return html;
    }

    /** A tooltip per point. The chart is HTML, so it may as well answer a pointer. */
    wireTips(root, series) {
      const tip = root.querySelector(".mk-trend-tip");
      const plot = root.querySelector(".mk-trend-plot");
      if (!tip || !plot) { return; }

      const show = (hit) => {
        const i = Number(hit.getAttribute("data-i"));
        const rect = hit.getBoundingClientRect();
        const box = plot.getBoundingClientRect();
        // A mark that caps a flat stretch answers for the whole stretch, because the dots inside it
        // were dropped: say how many scans held the score instead of naming one of them and leaving
        // a reader to wonder what happened to the other thirty.
        const run = Number(hit.getAttribute("data-run")) || 1;
        const where = run >= 3
          ? `unchanged across ${run} scans`
          : `scan ${i + 1} of ${series.length}`;
        tip.hidden = false;
        tip.innerHTML = `<b>${escapeHtml(hit.getAttribute("data-v") || "")}</b> · ${escapeHtml(where)}`;
        tip.style.left = `${rect.left + rect.width / 2 - box.left}px`;
        tip.style.top = `${rect.top - box.top - 6}px`;
        tip.classList.add("on");
      };
      const hide = () => { tip.classList.remove("on"); };

      for (const hit of root.querySelectorAll(".mk-trend-hit")) {
        hit.addEventListener("pointerenter", () => show(hit));
        hit.addEventListener("focus", () => show(hit));
        hit.addEventListener("pointerleave", hide);
        hit.addEventListener("blur", hide);
      }
    }
  }
);
