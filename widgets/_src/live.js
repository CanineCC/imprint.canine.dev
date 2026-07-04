// Shared LIVE-fetch plumbing for the CAI marketing islands.
//
// The proven pattern from watchdog.canine.dev's R4 islands (../rearch
// watchdog-survey-card.js / watchdog-insight-gallery.js): each widget reads an
// `api-base` attribute (the kennel public-API origin) and fetches the SAME real
// endpoints the app serves — /api/oss, /api/public/insights, /api/public/findings,
// /api/public/c4 — then renders REAL curated data. When `api-base` is unset, or the
// fetch fails, the widget falls back to the labelled SAMPLE it was seeded with
// (never a fake-live read). That is the ONLY time the sample shows.
//
// The GalleryCard JSON shape (List<GalleryCard> from /api/oss) is the authoritative
// kennel contract — Kennel.Watchdog.PublicGallery.GalleryCard. This module maps it
// onto the scorecard.js `card` model the renderers already understand, so a live
// card and a seeded sample card render through the identical code path.

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
 * Map ONE /api/oss GalleryCard (or an /api/public/insights item, which carries the
 * same lens + arc fields) onto the scorecard.js `card` model. The published FACE of a
 * repo is its PEAK run: score = bestScore (headlineScore fallback), the arc is
 * firstScore→bestScore, the series is the climb history. Lens bars come from the five
 * public lenses; the detail rows carry the honest measured/rebuild-cost facts.
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

/** The public report URL for a card — mirrors PublicFindingsEndpoint / _SurveyCard. */
export function reportUrl(c) {
  const run = c.bestRunId || c.BestRunId || "";
  return (
    "/api/oss/" +
    encodeURIComponent(c.owner) +
    "/" +
    encodeURIComponent(c.name) +
    "/report?run=" +
    run
  );
}

/**
 * The curated HERO pick from a corpus — the highest-quality published repo (peak
 * score, LoC as the tie-break), mirroring the survey-card `pick:"best"` demo default
 * the landing hero uses. `owner`/`name` (when both given) select an exact repo.
 */
export function pickCard(cards, { owner, name } = {}) {
  if (!Array.isArray(cards) || cards.length === 0) return null;
  if (owner && name) {
    const exact = cards.find((c) => c.owner === owner && c.name === name);
    if (exact) return exact;
  }
  return cards
    .slice()
    .sort((a, b) => {
      const sa = a.bestScore != null ? a.bestScore : a.headlineScore;
      const sb = b.bestScore != null ? b.bestScore : b.headlineScore;
      return (sb - sa) || ((b.productionLoc || 0) - (a.productionLoc || 0));
    })[0];
}

/**
 * The "quiet-peer" gallery ordering the public gallery uses: newest-published first,
 * then best score — so the grid reads as a living public record, not a leaderboard.
 * Mirrors PublicContent's Year*12+Month, then best-score sort.
 */
export function galleryOrder(cards) {
  return cards.slice().sort((a, b) => {
    const ta = Date.parse(a.lastUpdated || a.publishedAt || "") || 0;
    const tb = Date.parse(b.lastUpdated || b.publishedAt || "") || 0;
    if (tb !== ta) return tb - ta;
    const sa = a.bestScore != null ? a.bestScore : a.headlineScore;
    const sb = b.bestScore != null ? b.bestScore : b.headlineScore;
    return sb - sa;
  });
}

/**
 * Fetch JSON from `${apiBase}${path}`, resolving to `fallback` on any failure (no
 * network, non-2xx, bad JSON) OR when `apiBase` is empty. An empty base means the
 * widget was seeded without a live URL — it must show its labelled sample, never
 * attempt a same-origin fetch that would 404 on the static marketing host.
 */
export async function fetchJson(apiBase, path, fallback) {
  if (!apiBase) return fallback;
  try {
    const r = await fetch(apiBase.replace(/\/$/, "") + path);
    if (!r.ok) return fallback;
    return await r.json();
  } catch {
    return fallback;
  }
}

/** Fetch text (an SVG) with the same fallback discipline as {@link fetchJson}. */
export async function fetchText(apiBase, path) {
  if (!apiBase) return null;
  try {
    const r = await fetch(apiBase.replace(/\/$/, "") + path);
    if (!r.ok) return null;
    return await r.text();
  } catch {
    return null;
  }
}

export { LENSES, grouped };
