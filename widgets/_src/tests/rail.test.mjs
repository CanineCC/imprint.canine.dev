// The section rail on cai-trend and cai-link-cards.
//
// The corpus sheet reads as one page only if its five sections are laid out by one system: a
// narrow left column carrying the section's label and its (i), and the section's content taking
// the whole of the rest. §4 and §5 were the two that predated it and rendered a big display
// heading over narrow centred content instead.
//
// Every assertion here is made against the BUILT bundle in widgets/, rendered by a real
// Chromium — geometry is the claim, and only a browser can answer it.
//
//   node --test widgets/_src/tests/
//
import { test, describe, before, after } from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { withIslands } from "./harness.mjs";
import {
  TREND_LEGACY,
  TREND_LEGACY_SOLO,
  LINKS_LEGACY,
  LINKS_FIVE,
  BAND_HEAD,
  BAND_LEAD,
  BAND_LEAD_NO_KICKER,
  BARS_WIDE,
  BARS_WIDE_NO_KICKER,
  BARS_TABLE,
  BARS_TABLE_NO_KICKER,
  BARS_TABLE_BANDS,
  BARS_TABLE_COUNTRY,
} from "./fixtures.mjs";

const SNAPSHOTS = path.join(path.dirname(fileURLToPath(import.meta.url)), "snapshots");
const snapshot = (name) => readFile(path.join(SNAPSHOTS, `${name}.html`), "utf8");

// The endpoints of the movement, as the kennel publisher emits them: a PAIR per quantity, not a
// cell per number. Four numbers in four boxes is the shape the sheet was redesigned away from —
// "my eye have nowhere to rest" — so the join happens here and the two ends stay one thing.
//
// The two entries differ in exactly the way that decides how many basis lines are drawn: the
// Median pair was taken over two different populations on two different days, the Codebases pair
// over the same words twice.
const FIGURES = JSON.stringify([
  {
    label: "Median",
    from: { value: "45.2", sub: "across 580 measured codebases, 16 July 2026" },
    to: { value: "49.5", sub: "across 3,542 measured codebases, 10 September 2026" },
  },
  {
    label: "Codebases",
    from: { value: "580", sub: "measured codebases" },
    to: { value: "3,542", sub: "measured codebases" },
  },
]);

const TIP = "The median of every measured codebase, **on the day it was read**.";

const TREND_RAIL = {
  ...TREND_LEGACY,
  heading: "",
  kicker: "§4 Population",
  tip: TIP,
  figures: FIGURES,
};

const LINKS_RAIL = { ...LINKS_FIVE, kicker: "§5 Where to look", tip: TIP };

describe("cai-trend: the section rail", () => {
  let r;
  before(async () => {
    r = await withIslands([{ tag: "cai-trend", attrs: TREND_RAIL }], { width: 1280 });
  });
  after(() => r?.close());

  test("the kicker sits in a 112px rail beside the content, not above it", async () => {
    const side = await r.box("cai-trend", ".rail-side");
    const body = await r.box("cai-trend", ".rail-body");
    assert.notEqual(side, null, "no rail: the kicker did not get a column of its own");
    assert.notEqual(body, null, "no rail: the content did not get a column of its own");
    assert.equal(Math.round(side.width), 112);
    // Beside, not above: the two columns share a top edge and the body starts to the right.
    assert.equal(Math.round(side.y), Math.round(body.y));
    assert.ok(body.x > side.x + side.width, `body starts at ${body.x}, rail ends at ${side.x + side.width}`);
  });

  test("the chart's row fills the content column instead of a centred 46rem card", async () => {
    const body = await r.box("cai-trend", ".rail-body");
    const row = await r.box("cai-trend", ".mk-trend-row");
    const chart = await r.box("cai-trend", ".mk-trend");
    assert.notEqual(chart, null);
    assert.equal(await r.computed("cai-trend", ".mk-trend", "maxWidth"), "none");
    // The row is the section's content; the chart is the row minus what the figures beside it
    // need, and it is still the larger half by some distance.
    assert.equal(Math.round(row.width), Math.round(body.width));
    assert.ok(chart.width > body.width * 0.55, `the chart got ${chart.width}px of ${body.width}px`);
  });

  test("a kicker with no heading emits no h2 at all", async () => {
    const markup = await r.shadowMarkup("cai-trend");
    assert.ok(!/<h2/.test(markup), "the rail layout still emitted a display heading");
  });

  test("the tip is the product's one (i), rendered with its markdown", async () => {
    const dot = await r.box("cai-trend", ".rail-side .info-hint");
    assert.notEqual(dot, null, "no (i) beside the kicker");
    const role = await r.page.evaluate(() =>
      document.querySelector("cai-trend").shadowRoot.querySelector(".info-hint").getAttribute("role"));
    assert.equal(role, "note");
    const tip = await r.page.evaluate(() =>
      document.querySelector("cai-trend").shadowRoot.querySelector(".info-hint-tip").innerHTML);
    assert.match(tip, /<strong>on the day it was read<\/strong>/);
  });

  test("the endpoints are stated as one pair each, beside the chart", async () => {
    const read = (sel) =>
      r.page.evaluate(
        (s) => [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(s)].map((e) => e.textContent),
        sel
      );

    assert.deepEqual(await read(".mk-trend-fig-label"), ["Median", "Codebases"]);
    // One string per quantity, joined by the arrow: the movement is the thing, not its ends.
    assert.deepEqual(await read(".mk-trend-fig-pair"), ["45.2 \u2192 49.5", "580 \u2192 3,542"]);

    // Two bases when the ends were taken over different populations, ONE when the words are the
    // same — printing "measured codebases" twice under 580 → 3,542 says something changed that
    // did not.
    const bases = await r.page.evaluate(() =>
      [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig")].map((f) =>
        [...f.querySelectorAll(".mk-trend-fig-sub")].map((e) => e.textContent)
      ));
    assert.deepEqual(bases, [
      ["across 580 measured codebases, 16 July 2026", "across 3,542 measured codebases, 10 September 2026"],
      ["measured codebases"],
    ]);

    // The pair carries the mono display face; the label is set in the eyebrow type the section
    // rail uses, so a stack beside the chart reads as a label and not as a second heading.
    assert.match(await r.computed("cai-trend", ".mk-trend-fig-pair", "fontFamily"), /JetBrains Mono/);
    for (const p of ["fontSize", "letterSpacing", "textTransform", "fontWeight", "color"]) {
      assert.equal(
        await r.computed("cai-trend", ".mk-trend-fig-label", p),
        await r.computed("cai-trend", ".rail-kicker", p),
        `the figure label disagrees with the section eyebrow on ${p}`
      );
    }

    // BESIDE the chart, sharing its top edge — not a row of boxes under the caption.
    const chart = await r.box("cai-trend", ".mk-trend");
    const figs = await r.box("cai-trend", ".mk-trend-figs");
    assert.ok(
      figs.x >= chart.x + chart.width - 1,
      `the figures start at ${figs.x}, inside a chart that ends at ${chart.x + chart.width}`
    );
    assert.ok(Math.abs(figs.y - chart.y) <= 2, `the figures start at y=${figs.y}, the chart at y=${chart.y}`);
    // And the chart is what grows: it takes the room the figures do not need.
    const body = await r.box("cai-trend", ".rail-body");
    assert.ok(
      chart.width > body.width * 0.55,
      `the chart got ${chart.width}px of a ${body.width}px column`
    );

    // One column of pairs, at this width and at every other: two of them side by side is a shape
    // that only holds while the sentences happen to fit, and flips back and forth around it.
    const boxes = await r.page.evaluate(() =>
      [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig")].map((e) => {
        const b = e.getBoundingClientRect();
        return { x: Math.round(b.x), y: Math.round(b.y) };
      }));
    assert.equal(new Set(boxes.map((b) => b.x)).size, 1, "the pairs are not in one column");
    assert.equal(new Set(boxes.map((b) => b.y)).size, boxes.length, "two pairs share a line");
  });

});

describe("cai-trend: the page it already renders on", () => {
  test("with no kicker the markup is exactly what it always was", async () => {
    for (const [name, attrs] of [
      ["cai-trend.legacy", TREND_LEGACY],
      ["cai-trend.legacy-solo", TREND_LEGACY_SOLO],
    ]) {
      const r = await withIslands([{ tag: "cai-trend", attrs }]);
      try {
        assert.equal(await r.shadowMarkup("cai-trend"), await snapshot(name));
      } finally {
        await r.close();
      }
    }
  });

  test("with no kicker the chart keeps its centred 46rem measure", async () => {
    const r = await withIslands([{ tag: "cai-trend", attrs: TREND_LEGACY }], { width: 1280 });
    try {
      assert.equal(await r.computed("cai-trend", ".mk-trend", "maxWidth"), "736px");
      const left = await r.computed("cai-trend", ".mk-trend", "marginLeft");
      const right = await r.computed("cai-trend", ".mk-trend", "marginRight");
      assert.equal(left, right, "the legacy chart is no longer centred");
      assert.notEqual(left, "0px", "the legacy chart is no longer centred");
      assert.equal(await r.box("cai-trend", ".rail-side"), null, "a rail appeared without a kicker");
    } finally {
      await r.close();
    }
  });
});

describe("cai-link-cards: the section rail", () => {
  let r;
  before(async () => {
    r = await withIslands([{ tag: "cai-link-cards", attrs: LINKS_RAIL }], { width: 1280 });
  });
  after(() => r?.close());

  test("the kicker sits in the same 112px rail", async () => {
    const side = await r.box("cai-link-cards", ".rail-side");
    const body = await r.box("cai-link-cards", ".rail-body");
    assert.notEqual(side, null, "no rail: the kicker did not get a column of its own");
    assert.equal(Math.round(side.width), 112);
    assert.equal(Math.round(side.y), Math.round(body.y));
  });

  test("the grid fills the content column, uncapped and unmargined", async () => {
    const body = await r.box("cai-link-cards", ".rail-body");
    const grid = await r.box("cai-link-cards", ".mk-links");
    assert.equal(await r.computed("cai-link-cards", ".mk-links", "maxWidth"), "none");
    assert.equal(await r.computed("cai-link-cards", ".mk-links", "marginLeft"), "0px");
    assert.equal(Math.round(grid.width), Math.round(body.width));
  });

  test("five cards land as three columns, never more", async () => {
    const tracks = await r.computed("cai-link-cards", ".mk-links", "gridTemplateColumns");
    assert.equal(tracks.split(" ").length, 3, `grid tracks: ${tracks}`);
  });

  test("a kicker with no heading emits no h2, and the tip is the shared (i)", async () => {
    const markup = await r.shadowMarkup("cai-link-cards");
    assert.ok(!/<h2/.test(markup), "the rail layout still emitted a display heading");
    assert.notEqual(await r.box("cai-link-cards", ".rail-side .info-hint"), null, "no (i) beside the kicker");
  });

});

describe("cai-link-cards: the pages it already renders on", () => {
  test("with no kicker the markup is exactly what it always was", async () => {
    const r = await withIslands([{ tag: "cai-link-cards", attrs: LINKS_LEGACY }]);
    try {
      assert.equal(await r.shadowMarkup("cai-link-cards"), await snapshot("cai-link-cards.legacy"));
    } finally {
      await r.close();
    }
  });

  test("with no kicker the grid keeps its centred 46rem cap", async () => {
    const r = await withIslands([{ tag: "cai-link-cards", attrs: LINKS_LEGACY }], { width: 1280 });
    try {
      assert.equal(await r.computed("cai-link-cards", ".mk-links", "maxWidth"), "736px");
      const left = await r.computed("cai-link-cards", ".mk-links", "marginLeft");
      assert.equal(left, await r.computed("cai-link-cards", ".mk-links", "marginRight"));
      assert.notEqual(left, "0px", "the legacy grid is no longer centred");
      assert.equal(await r.box("cai-link-cards", ".rail-side"), null, "a rail appeared without a kicker");
    } finally {
      await r.close();
    }
  });
});

// ── the four islands of the sheet ───────────────────────────────────────────────────────────
// §1 cai-share-bars (wide) · §2 cai-figure-band (lead) · §3 cai-share-bars (table)
// §4 cai-trend · §5 cai-link-cards. The masthead above them is cai-figure-band's OTHER layout,
// `head`, which is a masthead and not a section — it keeps its own shape.
const SHEET = [
  { tag: "cai-share-bars", attrs: BARS_WIDE },
  { tag: "cai-figure-band", attrs: BAND_LEAD },
  { tag: "cai-share-bars", attrs: { ...BARS_TABLE, tip: TIP } },
  { tag: "cai-trend", attrs: TREND_RAIL },
  { tag: "cai-link-cards", attrs: LINKS_RAIL },
];

describe("the sheet's five sections are one rail", () => {
  let r;
  before(async () => {
    r = await withIslands(SHEET, { width: 1280 });
  });
  after(() => r?.close());

  test("every kicker starts at the same left edge, and so does every content column", async () => {
    const rails = [];
    for (const [i, { tag }] of SHEET.entries()) {
      const index = SHEET.slice(0, i).filter((e) => e.tag === tag).length;
      const side = await r.box(tag, ".rail-side", index);
      const body = await r.box(tag, ".rail-body", index);
      assert.notEqual(side, null, `${tag} (#${index}) has no rail`);
      rails.push({ tag, side, body });
    }

    // Measured as boxes: what a reader's eye lines up, not what the stylesheet says.
    const kickerEdges = new Set(rails.map((x) => Math.round(x.side.x)));
    const contentEdges = new Set(rails.map((x) => Math.round(x.body.x)));
    const widths = new Set(rails.map((x) => Math.round(x.side.width)));
    assert.equal(
      kickerEdges.size,
      1,
      `the kickers start at ${[...kickerEdges].join(", ")} — ${rails.map((x) => x.tag).join(", ")}`
    );
    assert.equal(contentEdges.size, 1, `the content columns start at ${[...contentEdges].join(", ")}`);
    assert.deepEqual([...widths], [112]);
  });

  test("the (i) beside a kicker is reachable by keyboard in every one of them", async () => {
    for (const [i, { tag }] of SHEET.entries()) {
      const index = SHEET.slice(0, i).filter((e) => e.tag === tag).length;
      const state = await r.page.evaluate(
        ([t, n]) => {
          const hint = document.querySelectorAll(t)[n].shadowRoot.querySelector(".rail-side .info-hint");
          if (!hint) return null;
          const tip = hint.querySelector(".info-hint-tip");
          const before = getComputedStyle(tip).visibility;
          hint.focus();
          const after = getComputedStyle(tip).visibility;
          const focused = document.querySelectorAll(t)[n].shadowRoot.activeElement === hint;
          hint.blur();
          return {
            tabindex: hint.getAttribute("tabindex"),
            role: hint.getAttribute("role"),
            label: hint.getAttribute("aria-label"),
            before,
            after,
            focused,
          };
        },
        [tag, index]
      );
      // hint.js: an (i) with nothing behind it is a naked dot promising an explanation that
      // never arrives, so a section given no tip must render no affordance at all. §1 and §2 of
      // the sheet are that case today — their explanations hang off a row and off each figure.
      if (!SHEET[i].attrs.tip) {
        assert.equal(state, null, `${tag} (#${index}) drew a dot for a tip it was never given`);
        continue;
      }
      assert.notEqual(state, null, `${tag} (#${index}) has no (i) beside its kicker`);
      assert.equal(state.tabindex, "0", `${tag}: the (i) is not in the tab order`);
      assert.equal(state.role, "note", `${tag}: the (i) has no role`);
      assert.ok(state.label, `${tag}: the (i) has no accessible name`);
      assert.equal(state.focused, true, `${tag}: the (i) did not take focus`);
      assert.equal(state.before, "hidden", `${tag}: the tip is showing before it is asked for`);
      assert.equal(state.after, "visible", `${tag}: the tip stayed hidden when the (i) was focused`);
    }
  });

  test("the per-item tips the sheet already passes still work inside the rail", async () => {
    // §2's figure tips and §3's row tips are the (i)s that already ship. Their subject is their
    // own cell or row — NOT the rail — so the rail must not have quietly taken them over.
    const figureHint = await r.computed("cai-figure-band", ".fb-lead-fig .info-hint", "position");
    assert.equal(figureHint, "static", "a findings figure's (i) lost its cell as its subject");
    const rowHint = await r.computed("cai-share-bars", ".sb-row .info-hint", "position", 1);
    assert.equal(rowHint, "static", "a language row's (i) lost its row as its subject");
    // A card's (i) anchors to its own dot, flipped right — the rail must not make it static.
    const cardHint = await r.computed("cai-link-cards", ".mk-link .info-hint", "position");
    assert.equal(cardHint, "relative", "a card's (i) was taken over by the rail");

    // And they are still reached by keyboard, which is the requirement the owner asked for by
    // name: tabindex, role, and hidden -> visible on focus, with no script anywhere.
    for (const [tag, selector, index] of [
      ["cai-figure-band", ".fb-lead-fig .info-hint", 0],
      ["cai-share-bars", ".sb-row .info-hint", 1],
      ["cai-link-cards", ".mk-link .info-hint", 0],
    ]) {
      const state = await r.page.evaluate(
        ([t, sel, n]) => {
          const hint = document.querySelectorAll(t)[n].shadowRoot.querySelector(sel);
          const tip = hint.querySelector(".info-hint-tip");
          const before = getComputedStyle(tip).visibility;
          hint.focus();
          const after = getComputedStyle(tip).visibility;
          hint.blur();
          return { tabindex: hint.getAttribute("tabindex"), role: hint.getAttribute("role"), before, after };
        },
        [tag, selector, index]
      );
      assert.equal(state.tabindex, "0", `${selector}: not in the tab order`);
      assert.equal(state.role, "note", `${selector}: no role`);
      assert.equal(state.before, "hidden", `${selector}: showing before it is asked for`);
      assert.equal(state.after, "visible", `${selector}: stayed hidden when focused`);
    }
  });

  test("all five kickers are set in the masthead's kicker type, to the value", async () => {
    // The type source is cai-figure-band's `.fb-cap`, which its `head` layout still carries —
    // the masthead is the one place on the sheet the original eyebrow survives, so comparing
    // against it is a real check rather than the rail agreeing with itself. Each island bundles
    // its own copy of the rules, so this is also what catches one of the four going stale.
    const r2 = await withIslands([{ tag: "cai-figure-band", attrs: BAND_HEAD }, ...SHEET], { width: 1280 });
    try {
      const props = ["fontSize", "letterSpacing", "textTransform", "fontWeight"];
      const reference = {};
      for (const p of props) {
        reference[p] = await r2.computed("cai-figure-band", ".fb-cap", p);
      }
      for (const [i, { tag }] of SHEET.entries()) {
        const index = SHEET.slice(0, i).filter((e) => e.tag === tag).length
          + (tag === "cai-figure-band" ? 1 : 0); // the masthead is this tag's #0 on this page
        for (const p of props) {
          assert.equal(
            await r2.computed(tag, ".rail-kicker", p, index),
            reference[p],
            `${tag} (#${index}) disagrees with the masthead kicker on ${p}`
          );
        }
      }
    } finally {
      await r2.close();
    }
  });
});

describe("cai-figure-band: the rail", () => {
  test("lead + kicker puts the label in the rail and emits no h2", async () => {
    const r = await withIslands([{ tag: "cai-figure-band", attrs: BAND_LEAD }], { width: 1280 });
    try {
      const side = await r.box("cai-figure-band", ".rail-side");
      assert.notEqual(side, null, "no rail on the findings band");
      assert.equal(Math.round(side.width), 112);
      const kicker = await r.page.evaluate(() =>
        document.querySelector("cai-figure-band").shadowRoot.querySelector(".rail-kicker")?.textContent);
      assert.equal(kicker, "§2 Findings");
      const markup = await r.shadowMarkup("cai-figure-band");
      assert.ok(!/<h2/.test(markup), "the findings band emitted a display heading");
      // The footnote stays with the figures, in the content column.
      const body = await r.box("cai-figure-band", ".rail-body");
      const foot = await r.box("cai-figure-band", ".fb-foot");
      assert.ok(foot.x >= body.x - 1, `the footnote starts at ${foot.x}, the content column at ${body.x}`);
    } finally {
      await r.close();
    }
  });

  test("the masthead is not a section, and does not move", async () => {
    const r = await withIslands([{ tag: "cai-figure-band", attrs: BAND_HEAD }], { width: 1280 });
    try {
      assert.equal(await r.shadowMarkup("cai-figure-band"), await snapshot("cai-figure-band.masthead"));
      assert.equal(await r.box("cai-figure-band", ".rail-side"), null, "the masthead grew a rail");
    } finally {
      await r.close();
    }
  });

  test("lead without a kicker does not move either", async () => {
    const r = await withIslands([{ tag: "cai-figure-band", attrs: BAND_LEAD_NO_KICKER }]);
    try {
      assert.equal(
        await r.shadowMarkup("cai-figure-band"),
        await snapshot("cai-figure-band.lead-no-kicker")
      );
    } finally {
      await r.close();
    }
  });
});

describe("cai-share-bars: the rail", () => {
  test("both layouts put the label in the rail, and keep their own shape beside it", async () => {
    const r = await withIslands(
      [
        { tag: "cai-share-bars", attrs: BARS_WIDE },
        { tag: "cai-share-bars", attrs: BARS_TABLE },
      ],
      { width: 1280 }
    );
    try {
      for (const i of [0, 1]) {
        const side = await r.box("cai-share-bars", ".rail-side", i);
        assert.notEqual(side, null, `share bars #${i} has no rail`);
        assert.equal(Math.round(side.width), 112);
        const markup = await r.shadowMarkup("cai-share-bars", i);
        assert.ok(!/<h2/.test(markup), `share bars #${i} emitted a display heading`);
      }
      // §1's lede and §3's column headings are content, and belong in the content column.
      const body = await r.box("cai-share-bars", ".rail-body", 0);
      const lede = await r.box("cai-share-bars", ".mk-section-head p", 0);
      assert.notEqual(lede, null, "the lede went missing when the kicker moved into the rail");
      assert.ok(lede.x >= body.x - 1, `the lede starts at ${lede.x}, the content column at ${body.x}`);
      const head = await r.box("cai-share-bars", ".sb-head", 1);
      assert.notEqual(head, null, "the language table lost its column headings");
      assert.ok(head.x >= body.x - 1);
    } finally {
      await r.close();
    }
  });

  test("without a kicker neither layout moves", async () => {
    for (const [name, attrs] of [
      ["cai-share-bars.wide-no-kicker", BARS_WIDE_NO_KICKER],
      ["cai-share-bars.table-no-kicker", BARS_TABLE_NO_KICKER],
    ]) {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }]);
      try {
        assert.equal(await r.shadowMarkup("cai-share-bars"), await snapshot(name));
        assert.equal(await r.box("cai-share-bars", ".rail-side"), null, "a rail appeared without a kicker");
      } finally {
        await r.close();
      }
    }
  });
});

// ── §3 at the widths the rail leaves it ─────────────────────────────────────────────────────
// A table's headings are the first thing to give way when the column narrows, and they give way
// SILENTLY: a grid cell does not clip, so a heading too wide for its track simply draws over its
// neighbour and the page still looks like a page. At 680px the sheet rendered MEASURABLE and NO
// POLICY on top of each other, and nothing failed.
//
// The measurement, not the impression: a cell whose scrollWidth exceeds its clientWidth is
// drawing outside itself, and two cells that share a row and intersect are drawing over each
// other. Both are asserted at every width, in the table AND in the reflow it falls back to,
// because the reflow has cells with headings of their own and can do exactly the same thing.
//
// ★ IT TAKES THE TABLE AS AN ARGUMENT. §3 is four numeric columns and labels two characters
// long; §4 By country is six, and "Bosnia and Herzegovina". A width suite that only ever ran
// against §3 would have said the layout holds while the section about to reuse it did not, and
// the owner's answer to §3's alignment was "Im sure what you just did for countries have the
// same issue" — so the widths are asserted against both shapes of the same layout.
function describeTableAtEveryWidth(what, attrs, columnCount) {
describe(`cai-share-bars: ${what} holds up at every width`, () => {
  const measure = (r) =>
    r.page.evaluate(() => {
      const root = document.querySelector("cai-share-bars").shadowRoot;
      const box = (el) => {
        const b = el.getBoundingClientRect();
        return {
          text: (el.textContent.trim() || el.className).slice(0, 24),
          x: b.x, right: b.right, y: b.y, bottom: b.bottom,
          overflow: el.scrollWidth - el.clientWidth,
        };
      };
      const head = root.querySelector(".sb-head");
      const headShown = head && getComputedStyle(head).display !== "none";
      const rows = [...root.querySelectorAll(".sb-row")];
      return {
        headShown,
        headCells: headShown ? [...head.children].map(box) : [],
        rowCells: rows.map((r) => [...r.children].map(box)),
        // Every numeric column must still be somewhere: either the head row names them, or each
        // cell carries its own heading. A column that quietly went missing is the one outcome
        // worse than a cramped one.
        cellsPerRow: rows.map((r) => r.querySelectorAll(".sb-cell").length),
        labelled: rows.every((r) =>
          [...r.querySelectorAll(".sb-cell")].every((c) => c.getAttribute("data-label"))),
        pageOverflow: document.documentElement.scrollWidth > window.innerWidth + 1,
      };
    });

  // A cell is drawing over another when their boxes intersect on BOTH axes — cells simply
  // sitting on different lines of a reflowed row are not an overlap.
  const overlaps = (cells) => {
    const bad = [];
    for (let i = 0; i < cells.length; i++) {
      for (let j = i + 1; j < cells.length; j++) {
        const a = cells[i];
        const b = cells[j];
        if (a.right - b.x > 1 && b.right - a.x > 1 && a.bottom - b.y > 1 && b.bottom - a.y > 1) {
          bad.push(`"${a.text}" over "${b.text}"`);
        }
      }
    }
    return bad;
  };

  for (const width of [1280, 900, 760, 680, 600, 520, 400]) {
    test(`no heading draws over another at ${width}px, and no column goes missing`, async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width });
      try {
        const m = await measure(r);

        assert.equal(m.pageOverflow, false, `the page scrolls sideways at ${width}px`);

        const spilling = [...m.headCells, ...m.rowCells.flat()].filter((c) => c.overflow > 1);
        assert.deepEqual(
          spilling.map((c) => `${c.text} (+${c.overflow}px)`),
          [],
          `cells draw outside themselves at ${width}px`
        );

        assert.deepEqual(overlaps(m.headCells), [], `heading cells overlap at ${width}px`);
        for (const [i, cells] of m.rowCells.entries()) {
          assert.deepEqual(overlaps(cells), [], `row ${i} has cells over each other at ${width}px`);
        }

        // Every numeric column, at every width, named either by the head row or by each cell.
        assert.deepEqual(
          m.cellsPerRow,
          m.cellsPerRow.map(() => columnCount),
          `a column went missing at ${width}px`
        );
        assert.ok(
          m.headShown || m.labelled,
          `at ${width}px the head row is gone and the cells carry no headings of their own`
        );

        // NO HEADING IS BROKEN INSIDE A WORD, in the head row and in the reflow alike.
        //
        // Asserting only that nothing spills is not enough: `overflow-wrap: anywhere` satisfies
        // that by breaking MEASURABLE into MEASURAB / LE, which is how the first version of this
        // test went green against a table that had not been fixed at all. Asserting that a cell
        // holds the heading's whole PHRASE is too much the other way — "Affected of measurable"
        // over the bar wraps at its spaces at 760px and reads exactly as the design draws it.
        //
        // The line between those two is min-content: the longest single word. A cell narrower
        // than that is breaking a word; a cell narrower than the phrase is merely wrapping it.
        // Measured in the heading's own font, never by copying a track width into this file.
        const holds = async (selector, labelOf) =>
          r.page.evaluate(
            ([sel, useAttr]) => {
              const root = document.querySelector("cai-share-bars").shadowRoot;
              const probe = document.createElement("span");
              probe.style.position = "absolute";
              probe.style.visibility = "hidden";
              // min-content: the box shrinks to its longest unbreakable word.
              probe.style.display = "block";
              probe.style.width = "min-content";
              root.appendChild(probe);
              const bad = [];
              for (const cell of root.querySelectorAll(sel)) {
                const pseudo = useAttr ? "::before" : null;
                const st = getComputedStyle(cell, pseudo);
                probe.style.font = `${st.fontStyle} ${st.fontWeight} ${st.fontSize}/${st.lineHeight} ${st.fontFamily}`;
                probe.style.letterSpacing = st.letterSpacing;
                probe.style.textTransform = st.textTransform;
                const text = useAttr ? cell.getAttribute("data-label") : cell.textContent.trim();
                if (!text) continue;
                probe.textContent = text;
                const wanted = probe.getBoundingClientRect().width;
                if (wanted - cell.clientWidth > 1) {
                  bad.push(
                    `"${text}" needs ${Math.round(wanted)}px for its longest word, cell is ${Math.round(cell.clientWidth)}px`
                  );
                }
              }
              probe.remove();
              return bad;
            },
            [selector, labelOf]
          );

        if (m.headShown) {
          assert.deepEqual(
            await holds(".sb-head .sb-cap", false),
            [],
            `a heading is being broken mid-word, or painted over its neighbour, at ${width}px`
          );
        }

        // In the reflow every cell carries its own heading, so a cell must be at least as wide as
        // that heading WANTS to be — measured by laying the text out in the pseudo-element's own
        // font rather than by copying the track width out of the stylesheet into this file. A cell
        // narrower than its heading is the same defect the head row had, one size down.
        if (!m.headShown) {
          assert.deepEqual(
            await holds(".sb-cell[data-label]", true),
            [],
            `a reflowed cell breaks its own heading mid-word at ${width}px`
          );
        }
      } finally {
        await r.close();
      }
    });
  }

});
}

describeTableAtEveryWidth("the language table", { ...BARS_TABLE, tip: TIP }, 4);
describeTableAtEveryWidth("the by-country table", { ...BARS_TABLE_COUNTRY, tip: TIP }, 6);

describe("cai-share-bars: the wide bar", () => {
  test("§1's bar states its parts without a part drawing outside itself", async () => {
    for (const width of [1280, 680, 400]) {
      const r = await withIslands([{ tag: "cai-share-bars", attrs: BARS_WIDE }], { width });
      try {
        const spilling = await r.page.evaluate(() =>
          [...document.querySelector("cai-share-bars").shadowRoot.querySelectorAll(".sb-wide-part > span")]
            .filter((s) => getComputedStyle(s).display !== "none" && s.scrollWidth - s.clientWidth > 1)
            .map((s) => s.textContent.trim().slice(0, 24)));
        assert.deepEqual(spilling, [], `a bar part is cut off at ${width}px`);
      } finally {
        await r.close();
      }
    }
  });
});

describe("the rail at the widths a reader has", () => {
  for (const width of [1280, 680]) {
    test(`neither island pushes the page sideways at ${width}px`, async () => {
      const r = await withIslands(SHEET, { width });
      try {
        assert.equal(await r.overflows(), false, `the page scrolls sideways at ${width}px`);
        // The content column still has a readable measure — a rail that ate the page would
        // pass the overflow check and fail the reader.
        const body = await r.box("cai-trend", ".rail-body");
        assert.ok(body.width >= 440, `the content column is only ${body.width}px at ${width}px`);
        const cards = await r.computed("cai-link-cards", ".mk-links", "gridTemplateColumns");
        assert.ok(cards.split(" ").length <= 3, `${cards.split(" ").length} card columns at ${width}px`);
        // Above the stacking width the figures stay beside the chart, and the chart stays a
        // chart: a line squeezed under 240px is a decoration.
        const chart = await r.box("cai-trend", ".mk-trend");
        const figs = await r.box("cai-trend", ".mk-trend-figs");
        assert.notEqual(figs, null, `no figures rendered at ${width}px`);
        assert.ok(figs.x >= chart.x + chart.width - 1, `the figures are not beside the chart at ${width}px`);
        assert.ok(chart.width >= 240, `the chart is only ${chart.width}px wide at ${width}px`);
      } finally {
        await r.close();
      }
    });
  }

  test("at 400px the rail stacks and the figures take one column", async () => {
    const r = await withIslands([{ tag: "cai-trend", attrs: TREND_RAIL }], { width: 400 });
    try {
      assert.equal(await r.overflows(), false, "the page scrolls sideways at 400px");
      const side = await r.box("cai-trend", ".rail-side");
      const body = await r.box("cai-trend", ".rail-body");
      assert.ok(body.y > side.y, "the rail did not stack: the kicker is still beside the content");
      assert.equal(Math.round(side.width), Math.round(body.width));
      const chart = await r.box("cai-trend", ".mk-trend");
      const figs = await r.box("cai-trend", ".mk-trend-figs");
      assert.notEqual(figs, null, "no figures rendered at 400px");
      assert.ok(figs.y >= chart.y + chart.height - 1, "the figures are still beside the chart at 400px");
      const ys = await r.page.evaluate(() =>
        [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig")]
          .map((e) => Math.round(e.getBoundingClientRect().y)));
      assert.equal(new Set(ys).size, ys.length, "the figures are still side by side at 400px");
    } finally {
      await r.close();
    }
  });

  test("the rail is right in the light theme and in the dark one", async () => {
    for (const theme of ["light", "dark"]) {
      const r = await withIslands([{ tag: "cai-figure-band", attrs: BAND_HEAD }, ...SHEET],
                                  { width: 1280, theme });
      try {
        const reference = await r.computed("cai-figure-band", ".fb-cap", "color");
        for (const [i, { tag }] of SHEET.entries()) {
          const index = SHEET.slice(0, i).filter((e) => e.tag === tag).length
            + (tag === "cai-figure-band" ? 1 : 0);
          assert.equal(await r.computed(tag, ".rail-kicker", "color", index), reference,
            `the ${theme} kicker of ${tag} is not the muted ink the masthead uses`);
        }
        // A theme that never resolved would leave the island painting dark ink on a dark page.
        const themed = await r.page.evaluate(() =>
          document.querySelector("cai-trend").dataset.theme);
        assert.equal(themed, theme);
      } finally {
        await r.close();
      }
    }
  });
});

// ── a column of numbers is a column ─────────────────────────────────────────────────────────
// The owner, reading the live §3: "the elements in the table are all over the place, not aligned
// … there is no reason for the texts to float/jump around like they do." Measured, it was worse
// than one column: EVERY column held a different right edge on every row, and the head row held
// a third one. Each `.sb-row` was its own `display:grid` with content-sized tracks, so a row's
// own strings decided that row's columns and nothing lined up with anything.
//
// On top of that the Median cell carries "49.6 Weak" as ONE right-aligned string, so the length
// of the band word decides where the number starts, and a median that lands on a whole number
// renders "57" rather than "57.0" and loses a character on top of that.
//
// WHAT IS MEASURED HERE IS THE RENDERED TEXT, not a track width and not a stylesheet value: a
// Range is laid over the run of digits inside each cell, wherever the widget happens to have put
// it, and the x of its first character is compared row against row. A test that asserted
// `grid-template-columns` would pass on a stylesheet that says the right thing while the browser
// draws the wrong one — which is exactly the trap the first version of the reflow test fell into.
const geometry = (r, index = 0) =>
  r.page.evaluate(
    (i) => {
      const root = document.querySelectorAll("cai-share-bars")[i].shadowRoot;
      const round = (n) => Math.round(n * 100) / 100;

      // The figure a reader sees: the first run of digits in the cell, measured off the text
      // nodes themselves so that it does not matter whether the widget wraps it in a span.
      const textRange = (el, pick) => {
        const walk = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
        const nodes = [];
        let text = "";
        while (walk.nextNode()) {
          nodes.push([walk.currentNode, text.length]);
          text += walk.currentNode.textContent;
        }
        const span = pick(text);
        if (!span) return null;
        const at = (offset) => {
          for (const [n, base] of nodes) {
            if (offset <= base + n.textContent.length) return [n, offset - base];
          }
          return null;
        };
        const rect = (from, to) => {
          const rg = document.createRange();
          const [sn, so] = at(from);
          const [en, eo] = at(to);
          rg.setStart(sn, so);
          rg.setEnd(en, eo);
          return rg.getBoundingClientRect();
        };
        const whole = rect(span[0], span[1]);
        const first = rect(span[0], span[0] + 1);
        // The figure's own right edge — the axis plus whatever decimal tail this row has. It is
        // where the column's HEADING should end, and it is NOT where "57" ends: a whole number
        // is aligned on the axis with the rest, not dragged right to meet their last digit.
        const tail = /^[\d,]*(\.[\d]+)?/.exec(text.slice(span[0]))[0].length;
        return {
          text: text.slice(span[0], span[1]),
          firstX: round(first.x),
          right: round(whole.right),
          figureRight: round(rect(span[0], span[0] + tail).right),
        };
      };
      // The DECIMAL AXIS: the right edge of the integer part. It is the axis a column of figures
      // is read against, and the one thing a band word or a missing decimal must not move —
      // "57" belongs under the "49" of "49.6", not under its "9.6", and asserting a shared right
      // edge for the whole figure would demand the opposite of that.
      const digits = (el) =>
        textRange(el, (t) => {
          const m = /\d[\d,]*/.exec(t);
          return m ? [m.index, m.index + m[0].length] : null;
        });
      const words = (el) =>
        textRange(el, (t) => {
          const from = t.search(/\S/);
          return from < 0 ? null : [from, t.trimEnd().length];
        });

      const cellsOf = (row, read) =>
        [...row.querySelectorAll(".sb-cell")].map((c) => {
          const b = c.getBoundingClientRect();
          return {
            text: c.textContent.trim(),
            right: round(b.right),
            width: round(b.width),
            figure: read(c),
          };
        });

      const head = root.querySelector(".sb-head");
      const table = root.querySelector(".sb-table");
      const labelBox = (el) => (el ? round(el.getBoundingClientRect().width) : null);
      return {
        tableWidth: round(table.getBoundingClientRect().width),
        headShown: !!head && getComputedStyle(head).display !== "none",
        head: head ? cellsOf(head, words) : null,
        headLabelWidth: labelBox(head && head.firstElementChild),
        rows: [...root.querySelectorAll(".sb-row")].map((row) => ({
          label: row.querySelector(".sb-label").textContent,
          labelWidth: labelBox(row.querySelector(".sb-label-row")),
          barWidth: labelBox(row.querySelector(".sb-bar-cell")),
          cells: cellsOf(row, digits),
        })),
      };
    },
    index
  );

/** The largest disagreement in a set of measurements, in px. Zero is the claim. */
const spread = (values) => Math.max(...values) - Math.min(...values);
/** Sub-pixel: two identical strings laid out in identical boxes land on the same float. */
const SAME = 0.05;

const column = (g, i, pick) => g.rows.map((row) => pick(row.cells[i]));

// The two tables, and the widths at which each of them is still a table. §4 carries two more
// numeric columns than §3 and a heading — "Repositories" — that is 110px of unbreakable word, so
// its wide shape needs a wider column to live in and it reflows sooner. The bar is what gives
// way to make room for the columns, which is why the share it takes is not one number either.
const TABLES = [
  {
    what: "§3 By language",
    attrs: { ...BARS_TABLE_BANDS, tip: TIP },
    columns: 4, band: 1, widths: [1280, 900, 760], bar: [0.3, 0.36],
  },
  {
    what: "§4 By country",
    attrs: { ...BARS_TABLE_COUNTRY, tip: TIP },
    columns: 6, band: 3, widths: [1280], bar: [0.15, 0.26],
  },
];

for (const { what, attrs, columns, band, widths: shapeWidths, bar: barShare } of TABLES) {
  describe(`cai-share-bars: ${what} is aligned`, () => {
    for (const width of shapeWidths) {
      test(`every numeric column holds one right edge at ${width}px`, async () => {
        const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width });
        try {
          const g = await geometry(r);
          assert.equal(g.headShown, true, `the table shape is gone at ${width}px`);
          for (let i = 0; i < columns; i++) {
            // The cell box first: one grid, one track, one right edge — head row included.
            const edges = [g.head[i].right, ...column(g, i, (c) => c.right)];
            assert.ok(
              spread(edges) <= SAME,
              `column ${i} right edges at ${width}px: ${edges.join(", ")}`
            );
            // Then the FIGURES inside it, against their decimal axis. A band word sharing the
            // cell must not move that axis, and neither must a median that has no decimal.
            const axes = column(g, i, (c) => c.figure && c.figure.right).filter((x) => x != null);
            assert.ok(
              spread(axes) <= SAME,
              `column ${i} figures sit on axes ${axes.join(", ")} at ${width}px ` +
                `(${column(g, i, (c) => c.text).join(" | ")})`
            );
          }
        } finally {
          await r.close();
        }
      });
    }

    test("the band word does not move the digits it sits beside", async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width: 1280 });
      try {
        const g = await geometry(r);
        // The rows were chosen so this cannot pass by accident: the shortest band word in the
        // vocabulary, the longest, and a median that landed on a whole number.
        const texts = column(g, band, (c) => c.text);
        const shown = texts.map((t) => t.replace(/\s+/g, " "));
        assert.ok(
          shown.some((t) => /\bWeak\b/.test(t)) && shown.some((t) => /\bExemplary\b/.test(t)),
          `the fixture no longer holds both the shortest and the longest band word: ${shown.join(" | ")}`
        );
        assert.ok(
          shown.some((t) => /^\d+ /.test(t)),
          `the fixture no longer holds a whole-number median: ${shown.join(" | ")}`
        );
        const starts = column(g, band, (c) => c.figure.firstX);
        assert.ok(
          spread(starts) <= SAME,
          `the medians start at ${starts.join(", ")} — ${shown.join(" | ")}`
        );
      } finally {
        await r.close();
      }
    });

    test("the heading of a numeric column ends where its figures end", async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width: 1280 });
      try {
        const g = await geometry(r);
        for (let i = 0; i < columns; i++) {
          const figures = column(g, i, (c) => c.figure && c.figure.figureRight).filter((x) => x != null);
          assert.ok(
            Math.abs(g.head[i].figure.right - Math.max(...figures)) <= SAME,
            `"${g.head[i].text}" ends at ${g.head[i].figure.right}, its figures at ${figures.join(", ")}`
          );
        }
      } finally {
        await r.close();
      }
    });

    test("the label column sizes to its own content, floor 90px and ceiling 200px", async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width: 1280 });
      try {
        const g = await geometry(r);
        const widths = [g.headLabelWidth, ...g.rows.map((row) => row.labelWidth)];
        assert.ok(spread(widths) <= SAME, `the label column is ${widths.join(", ")} wide`);
        const w = widths[0];
        assert.ok(w >= 90, `the label column collapsed to ${w}px`);
        assert.ok(w <= 200, `the label column is ${w}px — it was 240px of mostly empty space`);
        // Sized to content, not to a fraction: the longest label still fits on one line.
        const longest = await r.page.evaluate(() => {
          const root = document.querySelector("cai-share-bars").shadowRoot;
          return Math.max(
            ...[...root.querySelectorAll(".sb-label")].map((el) => {
              const rg = document.createRange();
              rg.selectNodeContents(el);
              return rg.getClientRects().length;
            })
          );
        });
        assert.equal(longest, 1, "a label wrapped inside the column that is sized to hold it");
      } finally {
        await r.close();
      }
    });

    test("the bar column takes about a third of the table, not four ninths", async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width: 1280 });
      try {
        const g = await geometry(r);
        const bars = g.rows.map((row) => row.barWidth);
        assert.ok(spread(bars) <= SAME, `the bar column is ${bars.join(", ")} wide`);
        const share = bars[0] / g.tableWidth;
        // 437 of 984 before — 44.4%. §3 comes down to 32.6%, a 27% cut, which is the "reduced by
        // 25% in width and still work well" the owner asked for; §4 comes down further on its
        // own, because 2.5fr against a column's 1fr is a ratio and six columns claim more of it.
        assert.ok(
          share > barShare[0] && share < barShare[1],
          `the bar takes ${(share * 100).toFixed(1)}% of the table (${bars[0]} of ${g.tableWidth})`
        );
      } finally {
        await r.close();
      }
    });

    test("at 680px it is still the per-row reflow, and the figure keeps its band word", async () => {
      const r = await withIslands([{ tag: "cai-share-bars", attrs }], { width: 680 });
      try {
        assert.equal(await r.overflows(), false, "the page scrolls sideways at 680px");
        const g = await geometry(r);
        assert.equal(g.headShown, false, "the table did not reflow at 680px");
        // One line, one string: "49.6 Weak" must not have become two boxes with a gap in them.
        const cell = await r.page.evaluate(
          (i) => {
            const row = document.querySelector("cai-share-bars").shadowRoot.querySelectorAll(".sb-row")[0];
            const el = row.querySelectorAll(".sb-cell")[i];
            const rg = document.createRange();
            rg.selectNodeContents(el);
            return { text: el.textContent.replace(/\s+/g, " ").trim(), lines: rg.getClientRects().length };
          },
          band
        );
        assert.match(cell.text, /^\d[\d.]* [A-Z][a-z]+$/, `the reflowed median reads "${cell.text}"`);
      } finally {
        await r.close();
      }
    });
  });
}
