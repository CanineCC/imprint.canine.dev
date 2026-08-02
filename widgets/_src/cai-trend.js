// <cai-trend series="[54.2,57.1,61]" first-date="4 March 2026" last-date="29 July 2026"
//            kicker="…" heading="…" lede="…" caption="…">
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
import { bandFor } from "./cai.js";

// The band cutlines. A snapped axis picks the pair of these that brackets the data.
const CUTS = [0, 25, 50, 70, 90, 100];

const W = 720;
const H = 240;
const PAD = { top: 26, right: 18, bottom: 34, left: 40 };

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
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
      const firstDate = this.getAttribute("first-date");
      const lastDate = this.getAttribute("last-date");
      const caption = this.getAttribute("caption");

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (series.length === 0) {
        root.innerHTML = html;
        return;
      }

      if (series.length === 1) {
        // One measurement is a fact, not a trend. State it and stop.
        const only = series[0];
        const band = bandFor(only);
        html += `<div class="mk-trend"><p class="mk-trend-solo">`;
        html += `<span class="mk-trend-solo-num ink-${band.key}">${fmt(only)}</span>`;
        if (lastDate || firstDate) {
          html += `<span class="mk-trend-solo-date">measured ${escapeHtml(lastDate || firstDate)}</span>`;
        }
        html += `</p></div>`;
        if (caption) { html += `<p class="mk-trend-sum">${renderInline(caption)}</p>`; }
        root.innerHTML = html;
        return;
      }

      const { min, max } = snapDomain(series);
      const plotW = W - PAD.left - PAD.right;
      const plotH = H - PAD.top - PAD.bottom;
      const x = (i) => PAD.left + (plotW * i) / (series.length - 1);
      const y = (v) => PAD.top + plotH * (1 - (v - min) / (max - min));

      const last = series[series.length - 1];
      const lastBand = bandFor(last);
      const points = series.map((v, i) => `${x(i).toFixed(1)},${y(v).toFixed(1)}`);

      html += `<div class="mk-trend"><div class="mk-trend-plot">`;
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

      series.forEach((v, i) => {
        const isLast = i === series.length - 1;
        const cx = x(i).toFixed(1);
        const cy = y(v).toFixed(1);
        // A hit target far bigger than the mark, so a 5px dot is not a 5px target.
        html += `<circle class="mk-trend-hit" cx="${cx}" cy="${cy}" r="18" tabindex="0"`
          + ` data-i="${i}" data-v="${fmt(v)}"></circle>`;
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

      root.innerHTML = html;
      this.wireTips(root, series);
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
        tip.hidden = false;
        tip.innerHTML = `<b>${escapeHtml(hit.getAttribute("data-v") || "")}</b> · scan ${i + 1} of ${series.length}`;
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
