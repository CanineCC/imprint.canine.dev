// <cai-share-bars layout="wide|table" kicker="…" tip="…" heading="…" lede="…"
//                 label-heading="Language" bar-heading="Affected of measurable"
//                 columns='["Surveys","Median","Measur.","No policy"]'
//                 rows='[{"label":"csharp","href":"…","tip":"…",
//                         "cells":["420","49.7","102","89%"],"cellTones":["","fair","",""],
//                         "parts":[{"weight":58,"tone":"accent"},{"weight":44,"tone":"track"}],
//                         "note":"56.9% · 58/102"},
//                        {"label":"vbnet","cells":["31","44.2","—","87%"],
//                         "unmeasured":"nothing resolvable"}]'>
//
// A population split into named parts and drawn to scale — once, wide, for a corpus ("1,511
// resolved · 42.9%" against "2,014 unmeasured · 57.1%"), or as a table with a bar per row for a
// cut of it ("affected of measurable", by language). One object at two densities, for the same
// reason cai-figure-band is: they are the same claim about the same population, and two islands
// would mean two places to fix the rule below.
//
// ★★ THE RULE THIS ISLAND EXISTS TO ENFORCE ────────────────────────────────────────────────
// A ROW WHOSE POPULATION WAS NOT MEASURED DRAWS NO BAR AND SAYS SO.
//
// A 0% bar is a claim: it says "we looked, and none of them are affected". A row with nothing
// resolvable supports no such claim — nobody looked. Drawing an empty track there is the same
// error as counting an unscanned project as a clean one, and it is worse than a blank because
// a track is drawn to scale and therefore reads as evidence. So `unmeasured` replaces the bar
// with its own sentence in muted ink and draws NOTHING: no track, no zero-width fill, not even
// a rounded 10px sliver of surface-2 that a reader could mistake for "almost none".
// ───────────────────────────────────────────────────────────────────────────────────────────
//
// WEIGHT IS GEOMETRY, COUNT IS THE NUMBER. `weight` decides how wide a part is drawn and
// nothing else; the figure a reader sees is the pre-formatted `count` string beside it. Keeping
// them apart is what stops the widget from rounding, re-deriving or re-scaling a published
// figure: it cannot show "42.9%" as anything other than the characters it was handed.
//
// NARROW REFLOW (≤560px): the CELLS drop onto their own line under the label, each prefixed
// with its column heading, and the bar takes a third line. The alternative — an
// overflow-x:auto wrapper — keeps the shape but hides columns behind a scrollbar in a page a
// reader is scrolling vertically, and a hidden column of a table like this is a hidden
// denominator. Reflowing costs vertical space, which the page has.
//
// WITH A KICKER IT IS A SECTION OF THE SHEET, and takes the section rail (rail.js): the label
// and its `tip` in a 112px left column, the bars — and the lede that says what a row's
// population is — taking the whole of the rest. Both layouts, because §1 and §3 of the corpus
// sheet are one wide bar and one table and are the same kind of thing on the page. Without a
// kicker the section head sits above the bars exactly as it always has.
//
// The `tip` is the SECTION's (i) and is a different thing from a row's: a row's (i) explains
// that row's population and has the row as its subject; this one explains the cut and has the
// rail as its. Both can be present, and on §3 both are.
//
// DATA ONLY: no api-base, no fetch. Every row arrives as a prop from the page.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";
import { SCORECARD_CSS } from "./scorecard.js";
import { HINT_CSS, hintHtml } from "./hint.js";
import { RAIL_CSS, railHtml, headBelowRailHtml } from "./rail.js";

const TONES = new Set(["exemplary", "healthy", "fair", "poor", "critical"]);

// A part's paint. "accent" is the inked share, "track" the remainder; a CAI band key inks it
// with that band instead (the by-language bars carry the band of their own share). Anything
// else falls back to the remainder, which is the safe direction: an unrecognised tone must not
// read as "affected".
function partClass(tone) {
  const t = String(tone || "").trim().toLowerCase();
  if (t === "accent") return "is-accent";
  if (TONES.has(t)) return `is-band fill-${t}`;
  return "is-track";
}

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + HINT_CSS + RAIL_CSS + `
.sb-mono { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.sb-cap { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase; color: var(--muted); font-weight: 700; }
/* The sentence that stands where a bar would be. Muted, never band-coloured: it is the absence
   of a measurement, and a colour from the band vocabulary would make it look like one. */
.sb-unmeasured { color: var(--muted); font-size: var(--fs-xs); line-height: 1.4; }
.sb-note { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 11.5px; color: var(--muted); }
.sb-label-row { display: flex; align-items: flex-start; min-width: 0; }
.sb-label { font-size: var(--fs-sm); font-weight: 600; color: var(--ink); overflow-wrap: anywhere; }
a.sb-label { color: var(--ink); }
a.sb-label:hover { color: var(--accent); text-decoration: none; }

/* ── wide: one population, full width ─────────────────────────────────────── */
.sb-wide-row { display: flex; flex-direction: column; gap: 9px; position: relative; }
.sb-wide-row + .sb-wide-row { margin-top: 18px; }
.sb-wide-bar { display: flex; height: 28px; gap: 2px; border-radius: 3px; overflow: hidden; }
.sb-wide-part { display: flex; align-items: center; min-width: 0; padding: 0 11px; }
.sb-wide-part.is-accent { background: var(--accent); }
.sb-wide-part.is-band { /* fill-* supplies the background */ }
.sb-wide-part.is-track { background: var(--surface-2); }
.sb-wide-part > span { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 11.5px; font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.sb-wide-part.is-accent > span, .sb-wide-part.is-band > span { color: var(--on-accent); }
.sb-wide-part.is-track > span { color: var(--muted); }
/* A part too narrow to hold its own label states it underneath instead, with a swatch so the
   reader can tell which slice it belongs to. Truncating it inside the bar would leave "1,5…".
   ★ The 18% share test is necessary but NOT sufficient, and 400px is where that shows: the
   population bar's 42.9% part clears 18% comfortably and still could not hold "1,511 resolved ·
   42.9%" in 151px — it rendered "1,511 resolved · 4…", which is a truncated FIGURE, the one
   thing a sheet of figures must never show. A share is a fraction of the bar; whether a string
   fits is a fraction of the viewport, and no render-time percentage can know it. So below 560px
   every part states itself in the legend and none of them state themselves inside. Each label is
   emitted once and placed by CSS, not emitted twice — display:none takes it out of the
   accessibility tree too, so a screen reader hears one label per part at either width. */
.sb-legend { display: flex; flex-wrap: wrap; gap: 6px 16px; }
.sb-legend-item.is-inside { display: none; }
@media (max-width: 560px) {
  .sb-wide-part > span { display: none; }
  .sb-legend-item.is-inside { display: inline-flex; }
}
.sb-legend-item { display: inline-flex; align-items: center; gap: 6px;
  font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-size: 11.5px; color: var(--muted); }
.sb-swatch { width: 9px; height: 9px; border-radius: 2px; flex: none; display: block; }
.sb-swatch.is-accent { background: var(--accent); }
.sb-swatch.is-track { background: var(--surface-2); border: 1px solid var(--border); }

/* ── table: a bar per row ─────────────────────────────────────────────────── */
.sb-head, .sb-row { display: grid; grid-template-columns: var(--sb-grid); gap: 12px; align-items: center; }
.sb-head { padding-bottom: 6px; border-bottom: 1px solid var(--border-strong); align-items: end; }
.sb-row { padding: 7px 0; border-bottom: 1px solid var(--border); position: relative; }
/* A row is the subject of its own (i): the tip spans the row rather than hanging off the dot,
   so it can never reach past the island — and a table of seventeen rows never widens the page.
   See hint.js. The z-index lifts the open tip above the rows it opens across. */
.sb-wide-row .info-hint, .sb-row .info-hint { position: static; }
.sb-wide-row .info-hint-tip, .sb-row .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
.sb-wide-row:hover, .sb-wide-row:focus-within, .sb-row:hover, .sb-row:focus-within { z-index: 2; }
.sb-cell { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-xs); text-align: right; color: var(--muted); }
.sb-head .sb-cell { color: var(--muted); }
.sb-bar-cell { display: flex; align-items: center; gap: 9px; min-width: 0; }
.sb-track { flex: 1; display: flex; height: 10px; background: var(--surface-2);
  border-radius: 4px; overflow: hidden; min-width: 40px; }
.sb-part { display: block; height: 100%; border-radius: 4px; }
.sb-part.is-accent { background: var(--accent); }
.sb-part.is-track { background: transparent; }

@media (max-width: 560px) {
  /* The cells take their own line under the label, each carrying its heading, and the bar takes
     a third. The header row has nothing left to head, so it goes. */
  .sb-head { display: none; }
  .sb-row { grid-template-columns: repeat(auto-fit, minmax(74px, 1fr)); gap: 8px 12px; padding: 11px 0; }
  .sb-label-row { grid-column: 1 / -1; }
  .sb-bar-cell { grid-column: 1 / -1; }
  .sb-cell { text-align: left; }
  .sb-cell[data-label]::before { content: attr(data-label) " "; display: block;
    font-family: var(--font-ui); font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
    font-weight: 700; color: var(--muted); }
}
`;

// Emit the parts of one bar. Returns "" when there is nothing to draw — the caller has already
// decided whether a bar belongs here at all.
function partsHtml(parts, { wide }) {
  const list = (parts || []).filter((p) => p && Number.isFinite(Number(p.weight)) && Number(p.weight) > 0);
  if (list.length === 0) return "";
  let h = "";
  for (const p of list) {
    const cls = partClass(p.tone);
    // flex-grow carries the proportion, so the 2px gaps come out of the parts rather than
    // pushing the bar past 100% — which a width:% part with a gap would do.
    const grow = Number(p.weight);
    if (wide) {
      const text = [p.count, p.label].filter((s) => s != null && String(s) !== "").join(" ");
      h += `<span class="sb-wide-part ${cls}" style="flex:${grow} 1 0">`;
      h += text ? `<span>${escapeHtml(text)}</span>` : "";
      h += `</span>`;
    } else {
      h += `<span class="sb-part ${cls}" style="flex:${grow} 1 0"></span>`;
    }
  }
  return h;
}

customElements.define(
  "cai-share-bars",
  class extends CaiIsland {
    render(root) {
      const layout = (this.getAttribute("layout") || "table").trim().toLowerCase() === "wide"
        ? "wide"
        : "table";
      const columns = (this.json("columns", []) || []).map((c) => (c == null ? "" : String(c)));
      const rows = (this.json("rows", []) || []).filter((r) => r && r.label != null && String(r.label) !== "");
      const kicker = (this.getAttribute("kicker") || "").trim();

      // In the rail the label leaves the section head, which keeps only what it was given
      // besides it. `bars` is the section's content, built below and placed at the end.
      const head = kicker ? headBelowRailHtml(this) : sectionHeadHtml(this);
      let html = "";

      if (rows.length === 0) {
        // No rows is not a population of zero. An empty frame would say the second thing.
        root.innerHTML = `<style>${CSS}</style>` + this.frame(kicker, head, "");
        return;
      }

      // The label, its optional link and its optional (i) — identical in both layouts.
      const labelHtml = (r, right) => {
        const label = String(r.label);
        let h = `<div class="sb-label-row">`;
        h += r.href
          ? `<a class="sb-label" href="${escapeHtml(String(r.href))}">${escapeHtml(label)}</a>`
          : `<span class="sb-label">${escapeHtml(label)}</span>`;
        h += hintHtml(r.tip, { right, label });
        h += `</div>`;
        return h;
      };

      if (layout === "wide") {
        for (const r of rows) {
          html += `<div class="sb-wide-row">`;
          html += labelHtml(r, false);
          if (r.unmeasured) {
            html += `<p class="sb-unmeasured">${escapeHtml(String(r.unmeasured))}</p>`;
          } else {
            const parts = (r.parts || []).filter(
              (p) => p && Number.isFinite(Number(p.weight)) && Number(p.weight) > 0
            );
            const total = parts.reduce((a, p) => a + Number(p.weight), 0);
            if (parts.length > 0) {
              // A part under ~18% of the bar cannot hold "1,511 resolved · 42.9%" at 11.5px, so
              // its text moves to the legend under the bar rather than being clipped.
              const NARROW = 0.18;
              const inside = parts.map((p) => Number(p.weight) / total >= NARROW);
              html += `<div class="sb-wide-bar">`;
              html += partsHtml(
                parts.map((p, i) =>
                  inside[i] ? p : { weight: p.weight, tone: p.tone }
                ),
                { wide: true }
              );
              html += `</div>`;
              // Every labelled part gets a legend entry. The ones that also state themselves
              // inside the bar carry is-inside, which hides the legend copy above 560px — one
              // label, placed by the stylesheet at whichever width can actually hold it.
              const legend = parts
                .map((p, i) => ({
                  text: [p.count, p.label].filter((s) => s != null && String(s) !== "").join(" "),
                  cls: partClass(p.tone),
                  inside: inside[i],
                }))
                .filter((e) => e.text !== "");
              if (legend.length > 0) {
                html += `<div class="sb-legend">`;
                for (const e of legend) {
                  html += `<span class="sb-legend-item${e.inside ? " is-inside" : ""}"><i class="sb-swatch ${e.cls}"></i>${escapeHtml(e.text)}</span>`;
                }
                html += `</div>`;
              }
            }
            if (r.note) html += `<span class="sb-note">${escapeHtml(String(r.note))}</span>`;
          }
          html += `</div>`;
        }
        root.innerHTML = `<style>${CSS}</style>` + this.frame(kicker, head, html);
        return;
      }

      // ── table ──
      // The grid rides a custom property so the ≤560px media query can override it; an inline
      // grid-template-columns on every row would outrank the stylesheet and never reflow.
      const n = Math.max(0, Math.min(12, Math.trunc(columns.length)));
      const grid = `minmax(90px,1.1fr) repeat(${n}, minmax(52px,auto)) minmax(120px,2fr)`;
      const labelHeading = this.getAttribute("label-heading") || "";
      const barHeading = this.getAttribute("bar-heading") || "";

      html += `<div class="sb-table" style="--sb-grid:${grid}">`;
      html += `<div class="sb-head">`;
      html += `<span class="sb-cap">${escapeHtml(labelHeading)}</span>`;
      for (const c of columns) html += `<span class="sb-cap sb-cell">${escapeHtml(c)}</span>`;
      html += `<span class="sb-cap">${escapeHtml(barHeading)}</span>`;
      html += `</div>`;

      for (const r of rows) {
        html += `<div class="sb-row">`;
        html += labelHtml(r, false);
        const cells = (r.cells || []).map((c) => (c == null ? "" : String(c)));
        const tones = r.cellTones || [];
        for (let i = 0; i < n; i++) {
          const tone = String(tones[i] || "").trim().toLowerCase();
          const ink = TONES.has(tone) ? ` ink-${tone}` : "";
          const heading = columns[i] || "";
          html += `<span class="sb-cell${ink}" data-label="${escapeHtml(heading)}">${escapeHtml(cells[i] ?? "")}</span>`;
        }
        html += `<div class="sb-bar-cell">`;
        if (r.unmeasured) {
          // No track. See the rule at the top of this file.
          html += `<span class="sb-unmeasured">${escapeHtml(String(r.unmeasured))}</span>`;
        } else {
          const parts = partsHtml(r.parts, { wide: false });
          if (parts) html += `<span class="sb-track">${parts}</span>`;
          if (r.note) html += `<span class="sb-note">${escapeHtml(String(r.note))}</span>`;
        }
        html += `</div>`;
        html += `</div>`;
      }
      html += `</div>`;

      root.innerHTML = `<style>${CSS}</style>` + this.frame(kicker, head, html);
    }

    /**
     * The section around the bars: the rail when the page named the section, the plain section
     * head when it did not.
     *
     * Both layouts and the no-rows case go through here, so a board cannot end up in the rail at
     * one density and above it at another — which is the whole reason §1 and §3 are one island.
     */
    frame(kicker, head, bars) {
      return kicker
        ? railHtml({ kicker, tip: this.getAttribute("tip"), content: head + bars })
        : head + bars;
    }
  }
);
