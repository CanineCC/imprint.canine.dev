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
  },
]);
