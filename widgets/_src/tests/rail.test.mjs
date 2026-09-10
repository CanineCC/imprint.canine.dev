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
