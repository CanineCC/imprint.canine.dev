// Shared LIVE-fetch plumbing for the CAI marketing islands.
//
// ONE server-curated source of truth: GET {api-base}/api/public/showcase returns the
// SERVER's best candidate for EACH widget (the agreed showcase contract). The widget just
// renders its slice — no client-side candidate-picking, no /api/oss sort, no hardcoded
// repo pins. The server does all the curation (HasPublicSource-gated, deterministic,
// reusing the kennel's own curation primitives) so the widgets can stay dumb renderers.
//
// Shape returned by /api/public/showcase (Track K owns the endpoint):
//   {
//     hero:        GalleryCard,                       // the flagship card
//     gallery:     GalleryCard[],                     // the curated set, hero excluded
//     c4:          { owner, name, runId },            // the richest architecture map
//     findings:    { owner, name, reportUrl, shown, total, findings:[{lens,dim,title,file,line}] },
//     composition: { owner, name, brilliantPct, slopPct, finePct },
//     bandScale:   { owner, name, score, band }
//   }
//
// When `api-base` is unset, or the fetch fails, each widget falls back to the labelled
// SAMPLE it was seeded with (never a fake-live read). That is the ONLY time a sample shows.
//
// The GalleryCard JSON shape (hero + gallery items) is the authoritative kennel contract —
// Kennel.Watchdog.PublicGallery.GalleryCard. cardFromGallery() maps it onto the
// scorecard.js `card` model the renderers already understand, so a live card and a seeded
// sample card render through the identical code path.

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
 * Map ONE GalleryCard (the showcase hero, or a gallery item — same lens + arc fields)
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

  return {
    name: c.name,
    owner: c.owner || undefined,
    score: Number(score),
    series: hasArc ? c.series.map(Number) : undefined,
    arcFirst: hasArc ? c.firstScore : null,
    arcBest: hasArc ? c.bestScore : null,
    lenses,
    rows,
  };
}

/**
 * The ABSOLUTE public report URL for a showcase GalleryCard — mirrors the app's
 * `/api/oss/{owner}/{name}/report?run={bestRunId}`, resolved against the kennel origin
 * (`apiBase`). The marketing islands render on a STATIC cross-origin host, so an
 * origin-less relative href would 404 there — it must carry the api-base. The server only
 * ever curates public-with-report repos into the showcase, so every hero/gallery card
 * carries a bestRunId; returns "" defensively if one is somehow missing (no dead links).
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

// ── the one shared showcase fetch ────────────────────────────────────────────
//
// Every island on a page reads the SAME /api/public/showcase document. We fetch it once
// per (api-base, cohort) and hand every island the cached promise, so N islands = 1 GET.

const _showcaseCache = new Map();

/** The cache key for a given api-base + optional cohort. */
function showcaseKey(base, cohort) {
  return base + " " + (cohort || "");
}

/**
 * Fetch the server-curated showcase document from `{apiBase}/api/public/showcase`
 * (optionally scoped to a `cohort`), DEDUPED + CACHED across every island on the page.
 * Resolves to `null` on any failure (no network, non-2xx, bad JSON) OR when `apiBase` is
 * empty — an empty base means the widget was seeded without a live URL and must show its
 * labelled sample, never attempt a same-origin fetch that would 404 on the static host.
 * A failed fetch is cached as a rejected-to-null promise so islands don't each retry.
 */
export function fetchShowcase(apiBase, cohort) {
  const base = (apiBase || "").trim().replace(/\/$/, "");
  if (!base) return Promise.resolve(null);
  const key = showcaseKey(base, cohort);
  let pending = _showcaseCache.get(key);
  if (pending) return pending;
  const q = cohort ? "?cohort=" + encodeURIComponent(cohort) : "";
  pending = (async () => {
    try {
      const r = await fetch(base + "/api/public/showcase" + q);
      if (!r.ok) return null;
      return await r.json();
    } catch {
      return null;
    }
  })();
  _showcaseCache.set(key, pending);
  return pending;
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
