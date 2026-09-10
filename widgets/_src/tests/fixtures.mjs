// The attribute sets the pages actually pass today, lifted from the live site.
//
// fixtures/sheet-islands.json is /state-of-the-corpus/ itself, scraped from the served HTML: the
// four islands' props exactly as the kennel publisher emits them, with the language table cut
// from seventeen rows to four (one of them `unmeasured`, which draws no bar) and the findings
// band from four figures to two. Everything a layout can trip over is still in it — a tip, a
// row link, a cell tone, a two-part bar, a lede, a footnote.
//
// The sheet is the ONLY page carrying cai-figure-band or cai-share-bars: the front page, the
// registry, /verify/, /spec/, /rubric/, /noise/, /badge/, /page-cli/, /dimensions/, the corpus
// archive and a survey page were each fetched and none of them mentions either tag. So their
// kicker path has one caller, and the no-kicker path below is a regression pin rather than a
// page anybody is being served.

import SHEET from "./fixtures/sheet-islands.json" with { type: "json" };

/** The masthead: layout=head, no kicker. It must not move — the rail is the `lead` layout. */
export const BAND_HEAD = SHEET["figure-band-head"];

/** §2 as the sheet passes it: layout=lead, a kicker, per-figure tips and a footnote. */
export const BAND_LEAD = SHEET["figure-band-lead"];

/** §1 as the sheet passes it: one population, wide, a kicker, a lede and a row tip. */
export const BARS_WIDE = SHEET["share-bars-wide"];

/** §3 as the sheet passes it: the language table, a kicker, a lede, per-row tips. */
export const BARS_TABLE = SHEET["share-bars-table"];

/** The same two islands with the kicker taken away — the path no page uses, pinned anyway. */
const without = (attrs, ...drop) =>
  Object.fromEntries(Object.entries(attrs).filter(([k]) => !drop.includes(k)));
export const BAND_LEAD_NO_KICKER = without(BAND_LEAD, "kicker");
export const BARS_WIDE_NO_KICKER = without(BARS_WIDE, "kicker");
export const BARS_TABLE_NO_KICKER = without(BARS_TABLE, "kicker");

// ── cai-trend and cai-link-cards ────────────────────────────────────────────────────────────
// The attribute sets the OTHER pages actually pass today, lifted from the live site:
// the survey pages and the corpus archive pass heading + caption to cai-trend and nothing but
// links to cai-link-cards — no page anywhere passes `kicker` to either. They are the
// backwards-compatibility subjects: whatever the rail work does, these must not move.

export const TREND_LEGACY = {
  series: "[45.199,47.188,50.268,50.268,50.268,51.133,52.98,49.609,49.494,49.52]",
  "first-date": "19 July 2026",
  "last-date": "10 September 2026",
  heading: "Score over time",
  caption: "Every reading of this repository, **kept exactly as it was taken**.",
};

/** One measurement is a fact, not a trend — a second legacy shape, and its own code path. */
export const TREND_LEGACY_SOLO = {
  series: "[61.4]",
  "last-date": "10 September 2026",
  heading: "Score over time",
  caption: "The only reading held for this repository.",
};

export const LINKS_LEGACY = {
  links: JSON.stringify([
    {
      icon: "github",
      label: "The project's source repository",
      note: "github.com/MathewSachin/Captura",
      href: "https://github.com/MathewSachin/Captura",
    },
    {
      icon: "html",
      label: "The full measurement report",
      note: "every dimension, with the evidence behind each",
      href: "/surveys/github/mathewsachin/captura/report/",
    },
    {
      icon: "cai",
      label: "How this is measured",
      note: "the rubric this reading was taken against",
      href: "/rubric/",
    },
    {
      icon: "doc",
      label: "Check it yourself",
      note: "the verifier, and what it checks",
      href: "/verify/",
    },
  ]),
};

/** The sheet's §5, which is where the orphan-fifth-card problem was seen. */
export const LINKS_FIVE = JSON.parse(JSON.stringify(LINKS_LEGACY));
LINKS_FIVE.links = JSON.stringify([
  ...JSON.parse(LINKS_LEGACY.links),
  {
    icon: "doc",
    label: "Every reading",
    note: "one dated reading per day, kept exactly as it was taken",
    href: "/state-of-the-corpus/archive/",
    figure: "54",
    go: "The archive",
    // A card tip, because §5's cards carry them on the live sheet — and because the rail must
    // not take a card's (i) over: its subject is the card's own right edge (hint-right).
    tip: "Readings are append-only and never rewritten, so a figure cited in July still resolves to what July said.",
  },
]);
