// The one way a widget writes a CAI score (ADR-0028 in kennel.canine.dev): one decimal, always, rounded half to even
// as the app does, and the Danish decimal comma on a Danish page (owner, 13 November 2026: "use comma in danish").
// A pure function, so no browser is needed.
//
//   node --test widgets/_src/tests/
//
import { test } from "node:test";
import assert from "node:assert/strict";
import { caiText, caiChange, caiMove } from "../cai.js";

test("a score is written with one decimal, always", () => {
  assert.equal(caiText(97.6), "97.6");
  assert.equal(caiText(98), "98.0");
  assert.equal(caiText(100), "100.0");
  assert.equal(caiText(86.81073415417612), "86.8");
});

test("a tie rounds to the even tenth, as the app does", () => {
  assert.equal(caiText(72.25), "72.2");
  assert.equal(caiText(72.75), "72.8");
});

test("never -0.0, and a value that is not a number is a dash", () => {
  assert.equal(caiText(-0.04), "0.0");
  assert.equal(caiText(Number.NaN), "—");
  assert.equal(caiText(undefined), "—");
});

test("a change is the difference of the two shown figures, and 0.0 has no direction", () => {
  assert.equal(caiChange(62, 97.6), "35.6");
  assert.equal(caiChange(57.46, 72.24), "14.7");
  assert.equal(caiMove(72.21, 72.24), "same");
  assert.equal(caiMove(99, 97.6), "down");
  assert.equal(caiMove(62, 97.6), "up");
});

test("a Danish page writes the decimal comma, one decimal still", () => {
  const saved = globalThis.document;
  globalThis.document = { documentElement: { lang: "da" } };
  try {
    assert.equal(caiText(72.4), "72,4");
    assert.equal(caiText(70), "70,0");
    assert.equal(caiChange(62, 97.6), "35,6");
  } finally {
    if (saved === undefined) {
      delete globalThis.document;
    } else {
      globalThis.document = saved;
    }
  }
});
