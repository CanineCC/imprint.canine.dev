// Shared LIVE-fetch plumbing for the CAI marketing islands.
//
// Each widget fetches its OWN real, already-published-gated kennel endpoint — the same
// endpoints watchdog.canine.dev serves its own marketing surface from (the API is gated to
// GalleryOptIn repos server-side, so the widget renders whatever it returns; NO client-side
// sourceUrl/visibility filtering):
//
//   • /api/oss            → GalleryCard[]  — the published gallery cards. The hero picks the
//                           highest bestScore (tie-break productionLoc), mirroring the app's
//                           GalleryHeroViewComponent. The home 3-card gallery picks second-
//                           best-by-score + most-improved-by-delta + a random card.
//   • /api/public/c4      → { items:[{repo:"owner/name", runId}] } — the C4-eligible
//                           published repos, LoC-ordered (richest first). Drives the C4
//                           carousel, which then loads /api/public/oss/{owner}/{name}/c4.svg.
//   • /api/public/findings→ { items:[{repo,owner,name,reportUrl,sourceUrl,shown,total,more,
//                           findings:[…]}] } — the DDD-moat weighted list. Drives the
//                           findings carousel.
//
// When `api-base` is unset, or the fetch fails, each widget falls back to the labelled
// SAMPLE it was seeded with (never a fake-live read). That is the ONLY time a sample shows.
//
// The GalleryCard JSON shape is the authoritative kennel contract (the /api/oss response).
// cardFromGallery() maps it onto the scorecard.js `card` model the renderers already
// understand, so a live card and a seeded sample card render through the identical path.

// The five public (scanner-overlap) lenses, in report order — the wd-card.js LENSES
// table, byte-for-byte. `value` may be null (not measured that run) → an em-dash row.
const LENSES = [
  ["codeHealth", "Code health"],
  ["architecture", "Architecture"],
  ["maturity", "Maturity"],
  ["productionReadiness", "Readiness"],
  ["securityCompliance", "Security"],
];

// Manual thousands grouping — NOT toLocaleString (the server prerenderer runs this
// same shape via Jint and the two must be byte-identical). Mirrors wd-card.js.
function grouped(n) {
  return String(n).replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// A short "measured" line for the card's detail rows: the published date + LoC.
function measuredRow(c) {
  const when = c.publishedAt || c.lastUpdated;
  let date = "";
  if (when) {
    const d = new Date(when);
    if (!Number.isNaN(d.getTime())) {
      date = d.toLocaleDateString("en-GB", { day: "numeric", month: "long", year: "numeric" });
    }
  }
  const loc = c.productionLoc > 0 ? `${grouped(c.productionLoc)} lines` : "";
  const parts = [date, loc].filter(Boolean);
  return parts.length ? parts.join(" · ") : null;
}

/**
 * Map ONE GalleryCard (the hero, or a gallery item — same lens + arc fields)
 * onto the scorecard.js `card` model. The published FACE of a repo is its PEAK run:
 * score = bestScore (headlineScore fallback), the arc is firstScore→bestScore, the series
 * is the climb history. Lens bars come from the five public lenses; the detail rows carry
 * the honest measured/rebuild-cost facts.
 */
export function cardFromGallery(c) {
  if (!c) return null;
  const score = c.bestScore != null ? c.bestScore : c.headlineScore;
  if (score == null) return null;
  const hasArc = Array.isArray(c.series) && c.series.length >= 2;

  const lenses = LENSES
    .map(([k, label]) => ({ label, value: c[k] == null ? null : Number(c[k]) }))
    .filter((l) => l.value != null);

  const rows = [];
  const measured = measuredRow(c);
  if (measured) rows.push({ label: "Measured", value: measured });
  if (c.costApprox) rows.push({ label: "Rebuild cost", value: c.costApprox });
  if (c.busFactor > 0 && c.authorCount > 0) {
    rows.push({ label: "Bus factor", value: `${c.busFactor} of ${c.authorCount} devs` });
  }

  // `display` is the repo's DisplayName: its ALIAS when the owner set one, else "owner/name". An alias
  // stands alone — prefixing it with the owner would print a name its owner deliberately replaced (and
  // for an unaliased repo, `display` already CONTAINS "owner/", so pairing the two double-prints it).
  const aliased = typeof c.display === "string" && c.display.length > 0
    && c.display !== (c.owner ? c.owner + "/" + c.name : c.name);

  return {
    name: aliased ? c.display : c.name,
    owner: aliased ? undefined : (c.owner || undefined),
    score: Number(score),
    series: hasArc ? c.series.map(Number) : undefined,
    arcFirst: hasArc ? c.firstScore : null,
    arcBest: hasArc ? c.bestScore : null,
    lenses,
    rows,
  };
}

/**
 * The ABSOLUTE public report URL for a GalleryCard — mirrors the app's
 * `/api/oss/{owner}/{name}/report?run={bestRunId}`, resolved against the kennel origin
 * (`apiBase`). The marketing islands render on a STATIC cross-origin host, so an
 * origin-less relative href would 404 there — it must carry the api-base. The gallery only
 * ever contains published-with-report repos, so every card carries a bestRunId; returns ""
 * defensively if one is somehow missing (no dead links).
 */
export function reportUrl(c, apiBase) {
  if (!c) return "";
  const run = c.bestRunId || c.BestRunId;
  const owner = c.owner || c.Owner;
  const name = c.name || c.Name;
  if (!run || !owner || !name) return "";
  const base = (apiBase || "").trim().replace(/\/$/, "");
  return (
    base +
    "/api/oss/" +
    encodeURIComponent(owner) +
    "/" +
    encodeURIComponent(name) +
    "/report?run=" +
    encodeURIComponent(run)
  );
}

// ── shared, cached JSON GETs against the real published endpoints ────────────
//
// Every island on a page that reads the same endpoint shares ONE GET: we fetch each
// (api-base, path) once and hand every island the cached promise, so N islands = 1 request
// per endpoint. Resolves to a fallback ([] / null) on any failure OR when `apiBase` is
// empty — an empty base means the widget was seeded without a live URL and must show its
// labelled sample, never attempt a same-origin fetch that would 404 on the static host.

const _jsonCache = new Map();

function fetchJsonCached(apiBase, path, onEmpty) {
  const base = (apiBase || "").trim().replace(/\/$/, "");
  if (!base) return Promise.resolve(onEmpty);
  const key = base + " " + path;
  let pending = _jsonCache.get(key);
  if (pending) return pending;
  pending = (async () => {
    try {
      const r = await fetch(base + path);
      if (!r.ok) return onEmpty;
      return await r.json();
    } catch {
      return onEmpty;
    }
  })();
  _jsonCache.set(key, pending);
  return pending;
}

/**
 * Fetch the published gallery cards from `{apiBase}/api/oss` — the `GalleryCard[]`. Already
 * published-gated server-side (Visible().Where(GalleryPolicy.IsPublic)); the widget renders
 * whatever it returns, with NO client-side sourceUrl/visibility filtering. Resolves to `[]`
 * on any failure or empty base.
 */
export async function fetchGallery(apiBase) {
  const cards = await fetchJsonCached(apiBase, "/api/oss", []);
  return Array.isArray(cards) ? cards : [];
}

/**
 * Fetch the LoC-ordered C4 wheel list from `{apiBase}/api/public/c4` — `{ items:[{repo,
 * runId}] }`, richest first. `repo` is "owner/name". Resolves to `[]` on any failure.
 */
export async function fetchC4(apiBase) {
  const d = await fetchJsonCached(apiBase, "/api/public/c4", { items: [] });
  return (d && Array.isArray(d.items)) ? d.items : [];
}

/**
 * Fetch the DDD-moat weighted findings list from `{apiBase}/api/public/findings` —
 * `{ items:[{repo,owner,name,reportUrl,sourceUrl,shown,total,more,findings:[…]}] }`, ordered
 * by weighted score. Resolves to `[]` on any failure.
 */
export async function fetchFindings(apiBase) {
  const d = await fetchJsonCached(apiBase, "/api/public/findings", { items: [] });
  return (d && Array.isArray(d.items)) ? d.items : [];
}

/**
 * The HERO card — the app's GalleryHeroViewComponent default (pickBy=bestScore):
 * OrderByDescending(BestScore).ThenByDescending(ProductionLoc).First(). No filter beyond the
 * published cards the endpoint already returned. Returns null on an empty corpus.
 */
export function pickHero(cards) {
  if (!Array.isArray(cards) || cards.length === 0) return null;
  const best = (c) => (c.bestScore != null ? Number(c.bestScore) : Number(c.headlineScore) || 0);
  let hero = null;
  for (const c of cards) {
    if (
      hero === null ||
      best(c) > best(hero) ||
      (best(c) === best(hero) && (Number(c.productionLoc) || 0) > (Number(hero.productionLoc) || 0))
    ) {
      hero = c;
    }
  }
  return hero;
}

/**
 * The HOME 3-card gallery — the FOUNDER OVERRIDE port: THREE distinct published cards
 * excluding the hero — [1] the SECOND-BEST by bestScore, [2] the MOST-IMPROVED by delta
 * (biggest climb), [3] a RANDOM published card — all distinct, none equal to the hero or
 * each other. Fewer than 3 published-besides-hero ⇒ returns as many distinct as exist.
 */
export function pickHomeGallery(cards, hero) {
  const rest = (Array.isArray(cards) ? cards : []).filter((c) => c && c !== hero);
  if (rest.length === 0) return [];
  const best = (c) => (c.bestScore != null ? Number(c.bestScore) : Number(c.headlineScore) || 0);
  const delta = (c) => (c.delta != null ? Number(c.delta) : 0);
  const chosen = [];
  const take = (c) => {
    if (c && !chosen.includes(c)) chosen.push(c);
  };

  // [1] second-best by score (best among the non-hero cards — the hero is already excluded).
  take([...rest].sort((a, b) => best(b) - best(a))[0]);
  // [2] most-improved by delta, distinct from [1].
  take([...rest].sort((a, b) => delta(b) - delta(a)).find((c) => !chosen.includes(c)));
  // [3] a random card, distinct from the first two.
  const pool = rest.filter((c) => !chosen.includes(c));
  if (pool.length > 0) take(pool[Math.floor(Math.random() * pool.length)]);
  return chosen;
}

/**
 * Fetch the language survey-clarity (FIT) matrix from `{apiBase}/api/public/language-support`
 * — `{ note, languages:[{code,displayName,applicability,supportKind,band,bandLabel,summary,
 * coveredLenses[],notApplicableLenses[],signedOffOn}], bands:[{band,label,why}] }`, languages
 * best-first. Resolves to `{ languages:[], bands:[] }` on any failure or empty base (the widget
 * then shows its labelled sample).
 */
export async function fetchLanguageSupport(apiBase) {
  const d = await fetchJsonCached(apiBase, "/api/public/language-support", { languages: [], bands: [] });
  return d && Array.isArray(d.languages) ? d : { languages: [], bands: [] };
}

/** Fetch text (an SVG) from `{apiBase}${path}`, resolving to null on any failure. */
export async function fetchText(apiBase, path) {
  const base = (apiBase || "").trim().replace(/\/$/, "");
  if (!base) return null;
  try {
    const r = await fetch(base + path);
    if (!r.ok) return null;
    return await r.text();
  } catch {
    return null;
  }
}

export { LENSES, grouped };
