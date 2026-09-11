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
// A COLUMN OF NUMBERS IS A COLUMN, WHICH IS NOT WHAT A TABLE OF INDEPENDENT ROWS GIVES YOU.
// The table is one grid and every row a subgrid of it, so a column's right edge is decided once
// over all of them; and a numeric cell is laid out as an integer part, a decimal tail and a band
// word in boxes of their own, so that neither "Exemplary" nor a median that came out a whole
// number can move the digits beside it. Both rules are stated where they are enforced, in the
// table section of the stylesheet below, with the measurements that made them necessary.
//
// NARROW REFLOW (ON THE ISLAND'S OWN WIDTH, not the viewport's — 560px for a table of four
// numeric columns or fewer, 800px for a wider one, see tableReflowCss): the CELLS drop onto
// their own line under the label, each prefixed with its column heading, and the bar takes a
// third line. The alternative — an overflow-x:auto wrapper — keeps the shape but hides columns
// behind a scrollbar in a page a reader is scrolling vertically, and a hidden column of a table
// like this is a hidden denominator. Reflowing costs vertical space, which the page has.
//
// ★ THE WIDTH THAT DECIDES IS THE TABLE'S, AND IT STOPPED BEING THE VIEWPORT'S THE DAY THIS
// ISLAND MOVED INTO THE SECTION RAIL. A rail takes 112px and a gutter out of the column, so at a
// 680px viewport the table is 512px wide while a viewport media query still calls the page wide
// and keeps the six-column shape. What that looked like: MEASURABLE drew straight over NO POLICY
// — a grid cell does not clip, so a heading too wide for its track just paints over its
// neighbour and the page still looks like a page. At 600px it was worse: the grid's own minimum
// track widths exceeded the column and the whole PAGE scrolled sideways.
//
// So the queries below are @container, on two NAMED containers: `table` for the table's shape,
// `island` (declared once in rail.js, for all four sheet islands) for the wide bar's legend.
// Each asks about the box it is actually laid out in, which is the question a media query could
// only ever approximate — and now cannot get wrong when a caller puts this island in a narrower
// column than the page. Named, because an unnamed query binds to the nearest container: the
// names are what stop a container added later, anywhere in between, from silently answering a
// question it was never asked.
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

/**
 * One numeric cell, cut into the parts a reader lines up separately.
 *
 *   "52.7 Adequate" → { int: "52", dec: ".7", band: "Adequate" }
 *   "57 Adequate"   → { int: "57", dec: "",   band: "Adequate" }
 *   "1,204"         → { int: "1,204", dec: "", band: "" }
 *
 * The band word is only taken off a cell the PAGE marked as a band, through `cellTones` — the
 * signal the emitter already sends, and the reason this needs no change on the other side of the
 * wire. Without it "58 of 102" would be cut into a figure and a word, and this widget does not
 * get to decide that a cell it was handed is two things.
 *
 * Nothing is reformatted. A figure is split where its own characters already split it and the
 * pieces are printed back exactly as they arrived: "57" stays "57" and is aligned as one, rather
 * than being padded to "57.0", which would be this widget inventing a digit of precision.
 */
function splitCell(text, toned) {
  const s = String(text ?? "");
  const m = toned ? /^(\S+)[ \u00a0]+(\S.*)$/.exec(s) : null;
  const figure = m ? m[1] : s;
  const band = m ? m[2] : "";
  const dot = figure.indexOf(".");
  return dot < 0
    ? { int: figure, dec: "", band }
    : { int: figure.slice(0, dot), dec: figure.slice(dot), band };
}

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + HINT_CSS + RAIL_CSS + `
/* The two containers the queries at the foot of this sheet ask about. The island — the whole
   island — is declared once in rail.js for all four sheet islands and is not re-declared here;
   the table container is this island's own, because a table's shape is decided by its width and
   nothing else. Both are NAMED, and the names are the point: an unnamed @container binds to
   whichever container happens to be nearest when the rule runs, so putting a container anywhere
   inside .rail-body would have silently re-pointed the wide bar's legend at a box 136px
   narrower than the one its 560px threshold was measured against. */
.sb-table { container-type: inline-size; container-name: table; }
.sb-mono { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.sb-cap { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase; color: var(--muted); font-weight: 700; }
/* The sentence that stands where a bar would be. Muted, never band-coloured: it is the absence
   of a measurement, and a colour from the band vocabulary would make it look like one. */
.sb-unmeasured { color: var(--muted); font-size: var(--fs-xs); line-height: 1.4; }
.sb-note { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 11.5px; color: var(--muted); }
/* The 90px is the label column's FLOOR, and it is expressed here rather than in the track list
   because fit-content() takes no minimum of its own: a track's floor is whatever its items say
   they need. A column of "Go" and "C#" is a column all the same. */
.sb-label-row { display: flex; align-items: flex-start; min-width: 90px; }
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
/* 560px OF THE ISLAND, which is the box this threshold was measured against — not the content
   column beside the rail. The number came from a string ("1,511 resolved · 42.9%" in a 151px
   part at a 400px screen), and re-pointing it at a column 136px narrower without re-measuring
   that string would tighten a rule whose derivation is a fact about type, not about layout.
   The box is arguably the bar's own; the threshold for that box is not 560, and finding it means
   measuring the live page's part labels rather than this fixture's shorter ones. */
@container island (max-width: 560px) {
  .sb-wide-part > span { display: none; }
  .sb-legend-item.is-inside { display: inline-flex; }
}
.sb-legend-item { display: inline-flex; align-items: center; gap: 6px;
  font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-size: 11.5px; color: var(--muted); }
.sb-swatch { width: 9px; height: 9px; border-radius: 2px; flex: none; display: block; }
.sb-swatch.is-accent { background: var(--accent); }
.sb-swatch.is-track { background: var(--surface-2); border: 1px solid var(--border); }

/* ── table: a bar per row ─────────────────────────────────────────────────── */
/* THE GRID IS AN ELEMENT INSIDE THE CONTAINER, NOT THE CONTAINER ITSELF. A @container rule
   styles the DESCENDANTS of the container it asks about and never the container, so the reflow
   rules below, written against .sb-table, would have been dead text — and dead in the silent
   direction: the head row would still have gone away at 560px while the grid it belonged to
   stayed a grid. So .sb-table is the box that is measured and .sb-grid is the one that lays out.
   ★★ ONE GRID FOR THE WHOLE TABLE, AND EVERY ROW A SUBGRID OF IT.
   A row that carries a grid-template-columns of its own IS its own grid, and a content-sized
   track in its own grid is sized by THAT ROW's strings: which is why "420" and "300" ended a
   column at 529px and 519px, why the head row ended it at a third place again, and why the owner
   read the section as "all over the place, not aligned". Nothing about the track list said so —
   every row obeyed it perfectly and resolved it to a different number. Subgrid is what makes
   seventeen rows one table: the tracks are sized once, over every row's content at once, and a
   row is still a box of its own with the border, the padding, the hover and the (i) anchoring it
   has always had. (display: contents would have shared the tracks too, and taken all four of
   those away with the box.) */
.sb-grid { display: grid; grid-template-columns: var(--sb-grid); column-gap: 12px; }
.sb-head, .sb-row { display: grid; grid-column: 1 / -1; grid-template-columns: subgrid;
  gap: 12px; align-items: center; }
/* The last defence, for a column heading longer than any threshold anticipated: a heading with
   nowhere to go wraps inside its own cell. Ugly beats painted over the cell beside it, and both
   beat the third option nobody should take — clipping a word off a denominator's name.
   ★ break-word, NOT anywhere, and the difference is not cosmetic: anywhere also tells the layout
   that the cell's MINIMUM WIDTH IS ONE CHARACTER, so a track asking for min-content is handed
   7px and MEASURABLE is broken mid-word to fit it. break-word breaks the same word in the same
   place when the box is genuinely too small, while still telling the grid that the word is what
   the column needs. The tracks below ask for min-content precisely so they are told. */
.sb-head .sb-cap { overflow-wrap: break-word; }
.sb-head { padding-bottom: 6px; border-bottom: 1px solid var(--border-strong); align-items: end; }
.sb-row { padding: 7px 0; border-bottom: 1px solid var(--border); position: relative; }
/* A row is the subject of its own (i): the tip spans the row rather than hanging off the dot,
   so it can never reach past the island — and a table of seventeen rows never widens the page.
   See hint.js. The z-index lifts the open tip above the rows it opens across. */
.sb-wide-row .info-hint, .sb-row .info-hint { position: static; }
.sb-wide-row .info-hint-tip, .sb-row .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
.sb-wide-row:hover, .sb-wide-row:focus-within, .sb-row:hover, .sb-row:focus-within { z-index: 2; }
/* A NUMERIC CELL IS THREE THINGS, AND ONLY THE FIRST OF THEM MAY DECIDE WHERE THE OTHERS SIT.
   Right-aligning "49.6 Weak" as one string hands the position of the digits to the length of the
   band word: "49.6 Weak" started at 541px, "91.4 Exemplary" at 529px, and a reader scanning the
   Median column for a number found it in a different place on every line. So the band word gets
   a box of its own, as wide as the longest word in ITS OWN COLUMN, and the digits are laid out
   against the axis that box leaves — which cannot move, because no row's word is wider than the
   widest one. The same trick one character down: the decimal tail gets a box too, so a median
   that landed on a whole number ("57", not "57.0") does not pull its digits a character right.
   Widths are in ch units of the cell's own mono face, so they are exact rather than estimated, and
   they come from the DATA at render time rather than from a constant somebody has to maintain.
   The integer part is what is left, right-aligned against the decimal axis: 49|.6, 57|, 24|.8. */
.sb-cell { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-xs); text-align: right; color: var(--muted);
  display: flex; justify-content: flex-end; align-items: baseline;
  /* min-width: 0 so that in the reflow a cell holding "91.4 Exemplary" wraps onto a second line
     inside its 96px track rather than widening the track and taking a column off the row. */
  min-width: 0; }
.sb-int { white-space: nowrap; }
.sb-dec, .sb-band { flex: none; text-align: left; white-space: nowrap; }
/* The gap between a figure and its band word is this padding up here and a real space down in
   the reflow — and the widget emits BOTH, one space of markup and one character of padding. In a
   flex row a whitespace-only child is not rendered at all, so what a reader sees is the padding;
   when the cell stops being a flex row that same space becomes the only place the line is
   allowed to break, and "91.4 Exemplary" needs one in a 96px cell at 400px.
   Border-box (tokens.js sets it globally), so the width the widget writes on this span is the
   OUTER one: the widest word in the column plus that character, which is exactly what the head
   cell reserves beside its heading. */
.sb-band { padding-left: 1ch; }
/* The head cell's matching reserve, so a column's HEADING ends where its figures end rather than
   over the band words. It does NOT give way when the table is tight: a reserve that shrinks
   under pressure buys a few pixels by putting the heading somewhere other than over its own
   numbers, which is the defect this file is about, one row higher up. It is part of what the
   column needs, and when the column cannot have what it needs the table reflows instead. */
.sb-reserve { flex: none; height: 0; }
.sb-head .sb-cell { color: var(--muted); }
.sb-bar-cell { display: flex; align-items: center; gap: 9px; min-width: 0; }
.sb-track { flex: 1; display: flex; height: 10px; background: var(--surface-2);
  border-radius: 4px; overflow: hidden; min-width: 40px; }
.sb-part { display: block; height: 100%; border-radius: 4px; }
.sb-part.is-accent { background: var(--accent); }
.sb-part.is-track { background: transparent; }

`;

/**
 * THE WIDTH AT WHICH THE TABLE STOPS BEING A TABLE, and why it is not one number.
 *
 * The shape below is the table's minimum: a label column, one track per numeric column no
 * narrower than the longest word in its heading, the reserve a band word needs, and a bar. Add a
 * column and that minimum goes up by the width of a heading — §3's four columns need about 570px
 * of table and §4's six need about 760px, both measured in rail.test.mjs at the widths the rail
 * actually leaves them. One threshold cannot serve both: at 560px §4 would have spent three
 * viewport sizes drawing REPOSITORIES over OWNERS, which is the exact defect the reflow exists
 * to prevent and the one that gets reported as "the table looks fine to me" because a grid cell
 * does not clip.
 *
 * So the threshold is chosen per table, from the count of columns it was given, and the rules
 * are written once here rather than copied per breakpoint.
 */
const tableReflowCss = (max) => `
@container table (max-width: ${max}px) {
  /* The cells take their own line under the label, each carrying its heading, and the bar takes
     a third. The header row has nothing left to head, so it goes — and with it the one grid,
     because a row here is no longer a slice of the table's columns but a little grid of its own.
     The figure's boxes go with it: reserving the widest band word in a 96px cell would leave the
     number nowhere to sit, and down here nothing is beside anything to line up with. */
  .sb-grid { display: block; }
  .sb-head { display: none; }
  .sb-cell { display: block; text-align: left; }
  .sb-cell .sb-dec, .sb-cell .sb-band { width: auto; }
  .sb-cell .sb-band { padding-left: 0; }
  /* 96px, not 74px: a cell's heading is now the widest thing in it — "MEASURABLE" needs 87px at
     this size and tracking — and a 74px track made the reflow do in miniature exactly what the
     six-column shape was doing at 680px. Measured, not guessed. */
  .sb-row { grid-template-columns: repeat(auto-fit, minmax(96px, 1fr)); gap: 8px 12px; padding: 11px 0; }
  .sb-label-row { grid-column: 1 / -1; }
  .sb-bar-cell { grid-column: 1 / -1; }
  .sb-cell[data-label]::before { content: attr(data-label) " "; display: block;
    font-family: var(--font-ui); font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
    font-weight: 700; color: var(--muted); overflow-wrap: anywhere; }
}
`;

/** Four columns or fewer is §3's shape; five or more is §4's, and needs the room to say so. */
const reflowWidth = (columnCount) => (columnCount >= 5 ? 800 : 560);

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
      // The grid rides a custom property so the ≤560px container query can override it; an
      // inline grid-template-columns on the table would outrank the stylesheet and never reflow.
      //
      // THE THREE TRACKS, AND WHY EACH IS THE SHAPE IT IS.
      //
      // The label was `minmax(90px, 1.1fr)`, which is not a measure of anything: it made the
      // column 240px wide to hold "Dart", "Go" and "C#", because a fraction of the table is
      // decided by the table and not by what is in the cell. `fit-content(200px)` asks the
      // content instead — the widest label, or the heading, whichever needs more — with 90px of
      // floor under it (the min-width on .sb-label-row) so a table of two-letter labels still
      // reads as a column, and 200px of ceiling over it so "Bosnia and Herzegovina" fits on one
      // line and a longer name wraps inside its own cell rather than taking the table with it.
      //
      // The numeric columns were `minmax(52px, auto)`, an `auto` maximum that let each row's own
      // strings size that row's tracks. `min-content` as the MINIMUM is what keeps a heading from
      // being broken mid-word when the column is tight (MEASURAB / LE is the failure that shape
      // has), and `1fr` as the maximum is where the room freed below now goes: the four columns
      // share it equally instead of it all falling into the bar.
      //
      // The bar was `2fr` against the label's `1.1fr`, which took 437px of a 984px table — "could
      // be reduced by 25% in width and still work well". `2.5fr` against a column's `1fr` puts it
      // at 321px of §3's table, a 27% cut, and it is a RATIO rather than a percentage on purpose:
      // §4 has six columns to find room for and the bar gives way to them by itself, down to
      // 224px, without anybody choosing a second number for a second table. A bar is a proportion
      // drawn to scale — it says the same thing 100px narrower — and a column of figures is not.
      const n = Math.max(0, Math.min(12, Math.trunc(columns.length)));
      const grid = `fit-content(200px) repeat(${n}, minmax(min-content,1fr)) minmax(96px,2.5fr)`;
      const labelHeading = this.getAttribute("label-heading") || "";
      const barHeading = this.getAttribute("bar-heading") || "";

      // Every numeric cell, split into the parts that must not move each other, and the width
      // each part is given in its own column: the widest decimal tail and the widest band word
      // anywhere in that column. Both are counted in characters and spent as `ch` of the cell's
      // mono face, which is exact — a proportional face would make this a guess.
      const cellParts = rows.map((r) => {
        const cells = (r.cells || []).map((c) => (c == null ? "" : String(c)));
        const tones = r.cellTones || [];
        return Array.from({ length: n }, (_, i) =>
          splitCell(cells[i] ?? "", TONES.has(String(tones[i] || "").trim().toLowerCase()))
        );
      });
      const widest = (i, part) => cellParts.reduce((w, row) => Math.max(w, row[i][part].length), 0);
      const decCh = Array.from({ length: n }, (_, i) => widest(i, "dec"));
      const bandCh = Array.from({ length: n }, (_, i) => widest(i, "band"));

      html += `<div class="sb-table"><div class="sb-grid" style="--sb-grid:${grid}">`;
      html += `<div class="sb-head">`;
      html += `<span class="sb-cap">${escapeHtml(labelHeading)}</span>`;
      for (let i = 0; i < n; i++) {
        // The heading ends where the figures end: the reserve stands in for the band word.
        const reserve = bandCh[i]
          ? `<i class="sb-reserve" style="width:${bandCh[i] + 1}ch" aria-hidden="true"></i>`
          : "";
        html += `<span class="sb-cap sb-cell">${escapeHtml(columns[i] || "")}${reserve}</span>`;
      }
      html += `<span class="sb-cap">${escapeHtml(barHeading)}</span>`;
      html += `</div>`;

      for (const [k, r] of rows.entries()) {
        html += `<div class="sb-row">`;
        html += labelHtml(r, false);
        const tones = r.cellTones || [];
        for (let i = 0; i < n; i++) {
          const tone = String(tones[i] || "").trim().toLowerCase();
          const ink = TONES.has(tone) ? ` ink-${tone}` : "";
          const heading = columns[i] || "";
          const part = cellParts[k][i];
          // The empty spans are not noise: a row whose median has no decimal, or no band word,
          // must still hold that column's reserve open, or its digits step right into the space
          // the other rows are keeping — which is the whole defect, one row at a time.
          html += `<span class="sb-cell${ink}" data-label="${escapeHtml(heading)}">`;
          html += `<span class="sb-int">${escapeHtml(part.int)}</span>`;
          if (decCh[i]) html += `<span class="sb-dec" style="width:${decCh[i]}ch">${escapeHtml(part.dec)}</span>`;
          // The literal space is the reflow's only break opportunity — see .sb-band in the CSS.
          if (bandCh[i]) html += ` <span class="sb-band" style="width:${bandCh[i] + 1}ch">${escapeHtml(part.band)}</span>`;
          html += `</span>`;
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
      html += `</div></div>`;

      root.innerHTML =
        `<style>${CSS}${tableReflowCss(reflowWidth(n))}</style>` + this.frame(kicker, head, html);
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
