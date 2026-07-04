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

/**
 * A card is an INSPECTABLE PUBLIC report when it has both an open source URL (so a
 * visitor can read the code — the private/internal canine repos have `sourceUrl:null`)
 * AND a best run id (so a full report exists to link to). The marketing surfaces only
 * ever flag / link repos that pass this gate; the internal canine repos, which the
 * kennel corpus also carries, never surface as a flagship or a gallery peer.
 */
export function isPublicWithReport(c) {
  if (!c) return false;
  const src = c.sourceUrl || c.SourceUrl;
  const run = c.bestRunId || c.BestRunId;
  return !!(src && run);
}

/**
 * The ABSOLUTE public report URL for a card — mirrors PublicFindingsEndpoint /
 * _SurveyCard's `/api/oss/{owner}/{name}/report?run={BestRunId}`, but resolved against
 * the kennel origin (`apiBase`). The marketing islands render on a STATIC cross-origin
 * host, so an origin-less relative href would 404 there — it must carry the api-base.
 * Returns "" for any card without an inspectable public report (no dead links).
 */
export function reportUrl(c, apiBase) {
  if (!isPublicWithReport(c)) return "";
  const base = (apiBase || "").trim().replace(/\/$/, "");
  const run = c.bestRunId || c.BestRunId;
  return (
    base +
    "/api/oss/" +
    encodeURIComponent(c.owner) +
    "/" +
    encodeURIComponent(c.name) +
    "/report?run=" +
    encodeURIComponent(run)
  );
}

/**
 * The curated HERO pick from a corpus — the highest-quality INSPECTABLE PUBLIC repo
 * (peak score, LoC as the tie-break), mirroring the survey-card `pick:"best"` demo
 * default the landing hero uses, but never a private/internal canine repo (those have
 * no open source to inspect). `owner`/`name` (when both given) select an exact repo,
 * still gated to public-with-report so a named private repo never becomes the flagship.
 */
export function pickCard(cards, { owner, name } = {}) {
  if (!Array.isArray(cards) || cards.length === 0) return null;
  const publicCards = cards.filter(isPublicWithReport);
  if (owner && name) {
    const exact = publicCards.find((c) => c.owner === owner && c.name === name);
    if (exact) return exact;
  }
  return publicRanked(cards)[0] || null;
}

/**
 * Every INSPECTABLE PUBLIC repo, ranked best-first (peak score, LoC tie-break) — the
 * flagship-quality ordering. Callers that must LINK a report (the hero card, the
 * gallery) walk this and probe {@link reportOk} to skip any whose bundle hasn't landed
 * yet, so the highest-quality repo with a *live* report wins.
 */
export function publicRanked(cards) {
  return cards
    .filter(isPublicWithReport)
    .slice()
    .sort((a, b) => {
      const sa = a.bestScore != null ? a.bestScore : a.headlineScore;
      const sb = b.bestScore != null ? b.bestScore : b.headlineScore;
      return (sb - sa) || ((b.productionLoc || 0) - (a.productionLoc || 0));
    });
}

/**
 * The QUALITY-forward gallery ordering — the "real reports, fully open, not a logo
 * wall" showcase leads with the strongest repos, so it reads as a wall of proof, not a
 * recency feed. Among INSPECTABLE PUBLIC repos only (never a private/internal canine
 * repo), best score first, LoC as the tie-break. `exclude` (an {owner,name}) drops the
 * hero so the gallery never duplicates the flagship card.
 */
export function galleryOrder(cards, { exclude } = {}) {
  return cards
    .filter(isPublicWithReport)
    .filter((c) =>
      exclude ? !(c.owner === exclude.owner && c.name === exclude.name) : true
    )
    .sort((a, b) => {
      const sa = a.bestScore != null ? a.bestScore : a.headlineScore;
      const sb = b.bestScore != null ? b.bestScore : b.headlineScore;
      return (sb - sa) || ((b.productionLoc || 0) - (a.productionLoc || 0));
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

/**
 * Does a repo's public report actually resolve right now? The corpus lists every
 * opted-in repo, but a report bundle is only linkable once it exists on the public API
 * (bundles are copied in asynchronously). A GET that 404s means "not yet" — the widgets
 * probe before they link so a marketing card NEVER carries a dead "read the report" href.
 * Any network error resolves false (better a card with no link than a broken one).
 */
export async function reportOk(url) {
  if (!url) return false;
  try {
    const r = await fetch(url, { method: "GET" });
    return r.ok;
  } catch {
    return false;
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
