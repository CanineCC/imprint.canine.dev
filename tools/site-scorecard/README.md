# Site scorecard — "to be found", measured

We sell the position that a quality claim is worthless unless it is measured, reproducible
and independently checkable. This applies that standard to our own marketing sites.

`scorecard.py` fetches the published sites and scores them out of 100 on things that are
**facts about the bytes we serve** — never opinions about the prose. Two runs are directly
comparable, so "the optimisation run worked" becomes a diff instead of an argument.

```bash
python3 scorecard.py                      # all sites, human summary
python3 scorecard.py --site watchdog      # one site
python3 scorecard.py --json report.json   # full machine-readable report
python3 scorecard.py --min-score 93       # CI gate: exit 1 below the bar
python3 scorecard.py --full               # audit template classes page-by-page
```

Stdlib only — it has to run on a build agent with nothing provisioned.

## What it does not measure

Copy quality, brand voice, persuasiveness, whether a page is worth reading. Those are the
`docs/marketing` review's job and a human's call. **Nothing in this tool rewards making a
page worse to read** — that separation is the point. A page can score 100 here and still be
badly written, and the fix for that is never to loosen a check.

It also does not measure rankings or traffic. Those are outcomes, lagging and contaminated
by things we do not control. This measures the surface we *do* control.

## The rubric

Weights encode consequence, not effort. Categories are scored separately so a run says what
*kind* of thing is wrong.

| Category | What it asks | Heaviest checks |
|---|---|---|
| `identity` | Does the page state what it is, once, uniquely? | `title.present` (10), `title.not-generic` (8), `description.present` (8) |
| `machine` | Can a crawler or a model extract it without reading prose? | `jsonld.present` (12), `og.title` (6), `og.image` (5) |
| `structure` | Is it built like a document? | `h1.exactly-one` (6), `content.depth` (4) |
| `hygiene` | Do our own links work and land in one hop? | `links.no-broken` (10), `links.no-redirect-hops` (4) |
| `site` | Robots, sitemap, `llms.txt` — the whole-site contract | `llms.present` (10), `llms.links-resolve` (10), `sitemap.all-resolve` (8) |

Grades follow the usual bands: **A+ ≥ 97, A ≥ 93, A− ≥ 90, B+ ≥ 87**, down to F below 60.
The target is **A (93)**; `--min-score 93` is the gate.

## Template classes

`cai.canine.dev` publishes ~2,700 survey pages from one template. Every page shares the
template's virtues and defects, so auditing it whole is 2,700 fetches to learn one thing.
The tool samples it — **evenly spaced and deterministic, never random**, so two runs audit
the same pages and the scores stay comparable. `--full` overrides this.

## Keeping the instrument honest

A checker that reports defects that are not there is worse than no checker, because it
teaches you to ignore it. Three false positives were found and fixed on the first runs, and
each is now a load-bearing comment in the source:

- **HEAD is not trustworthy.** `unfold.canine.dev` answers 404 to HEAD and 200 to GET. Any
  HEAD failure is re-tried as a GET before a link is called broken.
- **Prose punctuation is not part of a URL.** `…see https://cai.canine.dev.` was being read
  as a link to `cai.canine.dev.` — trailing sentence marks are stripped before checking.
- **An orphan is judged by where a link lands, not where it points.** `/dpa` redirects to
  the listed `/page-dpa/`; counting it as an unlisted page turned one redirect defect into
  two findings and inflated the apparent problem.

A fourth correction went the other way, and the direction matters. `og.image` passed as soon
as the tag existed — but every derived variant is WebP, and LinkedIn skips a WebP `og:image`
and shows a no-image card. The pages scored 100 on the machine layer while sharing as bare
links on the channel that matters most for this audience. `og.image-scrapable` was added and
the sites' score fell before the fix raised it again.

**Tightening a check because reality is worse than the score claimed is honest. Loosening one
because reality is inconvenient is not.** Both pressures show up in the same week and feel
identical from the inside; the difference is whether the change makes the number track the
world more closely or less. When in doubt, the check that lowers the score is the safer bet.

If a check fires, verify the finding by hand before acting on it. If it turns out to be a
tool defect, fix the tool in the same change — an instrument that drifts is the one failure
mode that invalidates every number it has ever produced.

## Result — 2026-08-01, after the first optimisation run

| Site | Score | Grade | identity | machine | structure | hygiene | site |
|---|---|---|---|---|---|---|---|
| **overall** | **95.2** | **A** | | | | | |
| cai | 96.8 | A | 99.1 | 100.0 | 87.5 | 89.8 | 100.0 |
| www | 96.5 | A | 94.0 | 100.0 | 100.0 | 85.7 | 100.0 |
| assay | 93.7 | A | 91.4 | 100.0 | 100.0 | 71.4 | 100.0 |
| watchdog | 93.7 | A | 92.6 | 100.0 | 100.0 | 71.4 | 83.3 |

Measured against a **stricter** rubric than the baseline below: `og.image-scrapable` was added
mid-run after the cards shipped, because presence of an `og:image` turned out not to mean the
card renders (see "Keeping the instrument honest"). Watchdog scored 93.5 before that check and
90.7 immediately after it, on unchanged pages — the drop was the check starting to tell the
truth, and the fix that followed earned the grade back.

What is left, and why it is not a number problem:

- **`hygiene` 71.4 on assay and watchdog** — pinned by ONE link, the header CTA to
  `app.*.canine.dev/`, which 302s to `/ui` by deliberate product design. The check is binary
  per page, so a page with one unfixable hop scores the same as one with twelve fixable ones,
  and fixing the twelve shows no movement. That is a flaw in this rubric, not in the sites;
  making the check proportional is the open proposal.
- **`site` 83.3 on watchdog** — five dead URLs in an `llms.txt` served through an on-box nginx
  override, outside the publish root and outside this repository. See
  `docs/recovered/watchdog-llms.txt.2026-07-05`.
- **`structure` 87.5 on cai** — the ~2,700-page survey template is thin (about 256 words).

## Baseline — 2026-08-01, before any optimisation work

| Site | Score | Grade | identity | machine | structure | hygiene | site |
|---|---|---|---|---|---|---|---|
| **overall** | **64.2** | **D** | | | | | |
| assay | 63.7 | D | 91.4 | 9.5 | 100.0 | 71.4 | 100.0 |
| cai | 62.1 | D− | 99.1 | 9.5 | 81.1 | 62.9 | 90.0 |
| watchdog | 63.3 | D | 91.8 | 9.5 | 100.0 | 71.4 | 83.3 |
| www | 67.6 | D+ | 94.0 | 9.5 | 100.0 | 71.4 | 100.0 |

The shape of that table is the whole diagnosis: **`identity` is already in the 90s** — the
titles and descriptions are well written and the content layer is in good order — while
**`machine` sits at 9.5 across all four sites**, because `StaticPageDocument.razor` can emit
exactly six head elements and none of them are Open Graph or JSON-LD. That is not a content
gap anyone can close in the CMS; it is a renderer capability that does not exist yet.
