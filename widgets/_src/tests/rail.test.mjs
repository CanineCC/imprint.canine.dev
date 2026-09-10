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
import { TREND_LEGACY, TREND_LEGACY_SOLO, LINKS_LEGACY, LINKS_FIVE } from "./fixtures.mjs";

const SNAPSHOTS = path.join(path.dirname(fileURLToPath(import.meta.url)), "snapshots");
const snapshot = (name) => readFile(path.join(SNAPSHOTS, `${name}.html`), "utf8");

// The figures the sheet states a movement's endpoints with.
const FIGURES = JSON.stringify([
  {
    value: "45.2",
    label: "the median at the first reading, 19 July 2026",
    sub: "across 580 measured codebases",
  },
  {
    value: "49.5",
    label: "the median at the latest reading, 10 September 2026",
    sub: "across 3,542 measured codebases",
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

  test("the chart fills the content column instead of a centred 46rem card", async () => {
    const body = await r.box("cai-trend", ".rail-body");
    const chart = await r.box("cai-trend", ".mk-trend");
    assert.notEqual(chart, null);
    assert.equal(await r.computed("cai-trend", ".mk-trend", "maxWidth"), "none");
    assert.equal(Math.round(chart.width), Math.round(body.width));
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

  test("the figures state the movement's endpoints inside the content column", async () => {
    const values = await r.page.evaluate(() =>
      [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig-value")]
        .map((e) => e.textContent));
    assert.deepEqual(values, ["45.2", "49.5"]);
    const labels = await r.page.evaluate(() =>
      [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig-label")]
        .map((e) => e.textContent));
    assert.equal(labels[0], "the median at the first reading, 19 July 2026");
    const subs = await r.page.evaluate(() =>
      [...document.querySelector("cai-trend").shadowRoot.querySelectorAll(".mk-trend-fig-sub")]
        .map((e) => e.textContent));
    assert.equal(subs[1], "across 3,542 measured codebases");

    // In the content column, not the rail, and after the caption.
    const body = await r.box("cai-trend", ".rail-body");
    const figs = await r.box("cai-trend", ".mk-trend-figs");
    assert.ok(figs.x >= body.x - 1, `figures start at ${figs.x}, the content column at ${body.x}`);
    const caption = await r.box("cai-trend", ".mk-trend-sum:last-of-type");
    assert.ok(figs.y > caption.y, "the figures are not beneath the caption");

    // The value carries the mono display face; its label does not.
    const mono = await r.computed("cai-trend", ".mk-trend-fig-value", "fontFamily");
    assert.match(mono, /JetBrains Mono/);
  });

  test("the kicker is set in cai-figure-band's kicker type, to the value", async () => {
    const both = await withIslands(
      [
        { tag: "cai-trend", attrs: TREND_RAIL },
        { tag: "cai-figure-band", attrs: { layout: "lead", kicker: "§2 Findings", figures: "[]" } },
      ],
      { width: 1280 }
    );
    try {
      const props = ["fontSize", "letterSpacing", "textTransform", "fontWeight", "color"];
      for (const p of props) {
        assert.equal(
          await both.computed("cai-trend", ".rail-kicker", p),
          await both.computed("cai-figure-band", ".fb-cap", p),
          `the two kickers disagree on ${p}`
        );
      }
    } finally {
      await both.close();
    }
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

  test("the kicker is set in cai-figure-band's kicker type, to the value", async () => {
    const both = await withIslands(
      [
        { tag: "cai-link-cards", attrs: LINKS_RAIL },
        { tag: "cai-figure-band", attrs: { layout: "lead", kicker: "§2 Findings", figures: "[]" } },
      ],
      { width: 1280 }
    );
    try {
      for (const p of ["fontSize", "letterSpacing", "textTransform", "fontWeight", "color"]) {
        assert.equal(
          await both.computed("cai-link-cards", ".rail-kicker", p),
          await both.computed("cai-figure-band", ".fb-cap", p),
          `the two kickers disagree on ${p}`
        );
      }
    } finally {
      await both.close();
    }
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

describe("the rail at the widths a reader has", () => {
  for (const width of [1280, 680]) {
    test(`neither island pushes the page sideways at ${width}px`, async () => {
      const r = await withIslands(
        [
          { tag: "cai-trend", attrs: TREND_RAIL },
          { tag: "cai-link-cards", attrs: LINKS_RAIL },
        ],
        { width }
      );
      try {
        assert.equal(await r.overflows(), false, `the page scrolls sideways at ${width}px`);
        // The content column still has a readable measure — a rail that ate the page would
        // pass the overflow check and fail the reader.
        const body = await r.box("cai-trend", ".rail-body");
        assert.ok(body.width >= 440, `the content column is only ${body.width}px at ${width}px`);
        const cards = await r.computed("cai-link-cards", ".mk-links", "gridTemplateColumns");
        assert.ok(cards.split(" ").length <= 3, `${cards.split(" ").length} card columns at ${width}px`);
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
      const r = await withIslands([{ tag: "cai-trend", attrs: TREND_RAIL },
                                   { tag: "cai-figure-band", attrs: { layout: "lead", kicker: "§2 Findings", figures: "[]" } }],
                                  { width: 1280, theme });
      try {
        const kicker = await r.computed("cai-trend", ".rail-kicker", "color");
        assert.equal(kicker, await r.computed("cai-figure-band", ".fb-cap", "color"),
          `the ${theme} kicker is not the muted ink cai-figure-band uses`);
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
