// ADR-0028 on the websites, as a reader sees it: the score card, rendered by Chromium from the BUILT bundle in
// widgets/, writes every CAI score with one decimal, and a Danish page (<html lang="da">) writes the Danish comma.
// cai-text.test.mjs proves the formatter in the source; this proves the shipped bundle carries it, so a source edit
// that was never rebuilt fails here rather than on the live page.
//
//   node --test "widgets/_src/tests/*.test.mjs"
import { test, describe } from "node:test";
import assert from "node:assert/strict";
import { withIslands } from "./harness.mjs";

// 68.25 is a tie in binary as well as in decimal, so it rounds to the even tenth (68.2), as the app does.
const CARD = JSON.stringify({
  name: "checkout-service",
  owner: "acme",
  score: 72.44,
  series: [70, 71.3, 72.44],
  arcFirst: 70,
  arcBest: 72.44,
  lenses: [
    { label: "Code health", value: 68.25 },
    { label: "Security", value: 81 },
  ],
});

async function scoresOn(lang) {
  const r = await withIslands([{ tag: "cai-score-card", attrs: { card: CARD } }], { lang });
  try {
    return await r.page.evaluate(() => {
      const root = document.querySelector("cai-score-card").shadowRoot;
      const all = (s) => [...root.querySelectorAll(s)].map((e) => e.textContent.trim());
      return {
        score: all(".cai-score"),
        from: all(".cai-arc-from"),
        to: all(".cai-arc-to"),
        up: all(".cai-arc-up"),
        lenses: all(".cai-lens-num"),
        rail: root.querySelector(".cai-rail")?.getAttribute("aria-label") ?? null,
      };
    });
  } finally {
    await r.close();
  }
}

describe("cai-score-card: every CAI score has one decimal (ADR-0028)", () => {
  test("an English page writes a point", async () => {
    assert.deepEqual(await scoresOn(""), {
      score: ["72.4"],
      from: ["70.0"],
      to: ["72.4"],
      up: ["↑ +2.4"],
      lenses: ["68.2", "81.0"],
      rail: "72.4 of 100 on a fixed worst-to-best scale",
    });
  });

  test("a Danish page writes the Danish comma, one decimal still", async () => {
    assert.deepEqual(await scoresOn("da"), {
      score: ["72,4"],
      from: ["70,0"],
      to: ["72,4"],
      up: ["↑ +2,4"],
      lenses: ["68,2", "81,0"],
      rail: "72,4 of 100 on a fixed worst-to-best scale",
    });
  });
});
