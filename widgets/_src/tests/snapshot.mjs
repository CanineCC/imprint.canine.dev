// Re-record the backwards-compatibility snapshots from the BUILT bundles in widgets/.
//
//   node widgets/_src/tests/snapshot.mjs
//
// Run this ONLY to record a change you intend. The snapshots exist so that a change to the
// kicker path cannot quietly move the no-kicker path, and re-recording them to make a test pass
// is the one thing that defeats them.
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { withIslands } from "./harness.mjs";
import { TREND_LEGACY, TREND_LEGACY_SOLO, LINKS_LEGACY } from "./fixtures.mjs";

const DIR = path.join(path.dirname(fileURLToPath(import.meta.url)), "snapshots");

const SUBJECTS = [
  ["cai-trend.legacy", "cai-trend", TREND_LEGACY],
  ["cai-trend.legacy-solo", "cai-trend", TREND_LEGACY_SOLO],
  ["cai-link-cards.legacy", "cai-link-cards", LINKS_LEGACY],
];

await mkdir(DIR, { recursive: true });
for (const [name, tag, attrs] of SUBJECTS) {
  const r = await withIslands([{ tag, attrs }]);
  await writeFile(path.join(DIR, `${name}.html`), await r.shadowMarkup(tag));
  await r.close();
  console.log(`recorded ${name}.html`);
}
