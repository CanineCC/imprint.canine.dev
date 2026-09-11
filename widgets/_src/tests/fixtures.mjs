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

// ── the numeric columns of a table ──────────────────────────────────────────────────────────
// §3's Median column is the one place on the sheet where a FIGURE and a WORD share a cell, and
// it is the case that decides whether a column of numbers reads as a column. The four rows of
// the scraped fixture above happen to carry only two band words of two lengths, which is not
// enough to expose what the owner saw: the rows below are chosen so that every way the digits
// can be pushed around is present at once.
//
//   "49.6 Weak"        the SHORTEST band word in the vocabulary (cai.js: 4 characters)
//   "57 Adequate"      a median that landed on a whole number, so the string is a character
//                      short — the second of the two causes, and invisible in data that happens
//                      to have a decimal on every row
//   "91.4 Exemplary"   the LONGEST band word (9 characters)
//   "24.8 Critical"    a third length, on the row that also draws no bar
//
// The other columns vary in digit count on purpose (420/300/57/9/31), because a column of
// integers has a right edge to hold too and three of them held a different one per row.
const ALIGN_ROWS = [
  {
    label: "C#", href: "/state-of-the-corpus/language/csharp/",
    cells: ["420", "49.6 Weak", "102", "375"], cellTones: ["", "poor", "", ""],
    parts: [{ weight: 58, count: "58", label: "affected", tone: "accent" },
            { weight: 44, count: "44", label: "not affected", tone: "track" }],
    note: "58 of 102",
    tip: "56.9% of surveys here whose dependencies a scanner could resolve (58 of 102)",
  },
  {
    label: "Java", href: "/state-of-the-corpus/language/java/",
    cells: ["300", "52.7 Adequate", "71", "261"], cellTones: ["", "fair", "", ""],
    parts: [{ weight: 47, count: "47", label: "affected", tone: "accent" },
            { weight: 24, count: "24", label: "not affected", tone: "track" }],
    note: "47 of 71",
  },
  {
    label: "Go", href: "/state-of-the-corpus/language/go/",
    cells: ["57", "57 Adequate", "40", "48"], cellTones: ["", "fair", "", ""],
    parts: [{ weight: 12, count: "12", label: "affected", tone: "accent" },
            { weight: 28, count: "28", label: "not affected", tone: "track" }],
    note: "12 of 40",
  },
  {
    label: "Elixir", href: "/state-of-the-corpus/language/elixir/",
    cells: ["9", "91.4 Exemplary", "6", "4"], cellTones: ["", "exemplary", "", ""],
    parts: [{ weight: 1, count: "1", label: "affected", tone: "accent" },
            { weight: 5, count: "5", label: "not affected", tone: "track" }],
    note: "1 of 6",
  },
  {
    label: "VB.NET", href: "/state-of-the-corpus/language/vbnet/",
    cells: ["31", "24.8 Critical", "0", "27"], cellTones: ["", "critical", "", ""],
    unmeasured: "nothing a scanner could resolve",
  },
];

/** §3 with every band-word length in it, and a median that is a whole number. */
export const BARS_TABLE_BANDS = { ...BARS_TABLE, rows: JSON.stringify(ALIGN_ROWS) };

/**
 * §4 By country, the section this layout is about to carry — six numeric columns instead of
 * four, the same Median cell, and the longest label a place name is likely to hand it.
 *
 * It is here because the owner's answer to §3's alignment was "I'm sure what you just did for
 * countries have the same issue": a fix that only holds for four columns and short labels is
 * not a fix of the layout. The column names are the ones the section is specified with.
 */
export const BARS_TABLE_COUNTRY = {
  layout: "table",
  kicker: "§4 By country",
  "label-heading": "Country",
  "bar-heading": "Affected of measurable",
  columns: JSON.stringify(["Repositories", "Owners", "Surveys", "Median", "Measurable", "No policy"]),
  lede: "A country is read off the owner's published location, so nothing here rests on anybody declaring anything.",
  rows: JSON.stringify([
    {
      label: "United States", href: "/state-of-the-corpus/country/us/",
      cells: ["1,204", "812", "1,240", "51.2 Adequate", "402", "1,104"],
      cellTones: ["", "", "", "fair", "", ""],
      parts: [{ weight: 240, count: "240", label: "affected", tone: "accent" },
              { weight: 162, count: "162", label: "not affected", tone: "track" }],
      note: "240 of 402",
      tip: "59.7% of surveys here whose dependencies a scanner could resolve (240 of 402)",
    },
    {
      label: "Bosnia and Herzegovina", href: "/state-of-the-corpus/country/ba/",
      cells: ["7", "5", "7", "63 Adequate", "3", "6"],
      cellTones: ["", "", "", "fair", "", ""],
      parts: [{ weight: 1, count: "1", label: "affected", tone: "accent" },
              { weight: 2, count: "2", label: "not affected", tone: "track" }],
      note: "1 of 3",
    },
    {
      label: "Germany", href: "/state-of-the-corpus/country/de/",
      cells: ["318", "204", "330", "49.8 Weak", "96", "291"],
      cellTones: ["", "", "", "poor", "", ""],
      parts: [{ weight: 51, count: "51", label: "affected", tone: "accent" },
              { weight: 45, count: "45", label: "not affected", tone: "track" }],
      note: "51 of 96",
    },
    {
      label: "Iceland", href: "/state-of-the-corpus/country/is/",
      cells: ["4", "3", "4", "92.5 Exemplary", "0", "3"],
      cellTones: ["", "", "", "exemplary", "", ""],
      unmeasured: "nothing a scanner could resolve",
    },
  ]),
};

// ── §4 as the sheet is about to pass it ─────────────────────────────────────────────────────
// Three changes at once, all from the same owner review, and they interact — a fixture per
// change would have missed that the section has to read right with all three at the same time.
//
// 1. ONE POINT PER WEEK. "shows a marker pr ??? I have no idea what, they are very unevenly
//    distributed" — and they were: a mark sat wherever the median CHANGED, which is a position
//    no reader can decode. The resampling is the emitter's (it holds a date per reading and this
//    island receives two dates and some numbers), so what arrives here is already regular and
//    the island must be told not to reduce it further. The series below carries a FIVE-WEEK
//    PLATEAU on purpose: it is the shape collapseFlatRuns eats, and eating it is exactly how the
//    uneven spacing would come back after the emitter had done its half of the work.
// 2. NO `sub` ON EITHER END OF EITHER PAIR. The Median pair's two populations ARE the Codebases
//    pair, one line down — "most of this information is redundant with what comes right after".
//    A figure is still never separated from its population; the page states it, which is the
//    level the rule holds at.
// 3. NO `caption`. Its sentence moves behind the section's (i).
export const TREND_WEEKLY = {
  series: "[45.2,47.2,50.3,50.3,50.3,50.3,50.3,51.1,52.6,49.5]",
  sampled: "weekly",
  "first-date": "10 July 2026",
  "last-date": "11 September 2026",
  kicker: "§4 Population",
  tip: "The median of every measured codebase, **on the day it was read**. One point per week: the newest reading on or before that week's date.",
  figures: JSON.stringify([
    { label: "Median", from: { value: "45.2" }, to: { value: "49.5" } },
    { label: "Measured codebases", from: { value: "580" }, to: { value: "3,545" } },
  ]),
};

/** The same series with nothing said about it: the shape every other page still passes. */
export const TREND_WEEKLY_UNTOLD = (() => {
  const { sampled, ...rest } = TREND_WEEKLY;
  return rest;
})();
