// The CAI presentation band table — ported verbatim from packages/ui/src/cai.ts.
// Pinned to Kennel.Cai/CaiBands.cs. Five display words, positional CSS keys,
// parked cutlines 90/70/50/25 (inclusive lower bounds). Banding is
// presentation-only; it never moves a number.

/** Worst → best — the canonical render order (lower bound ascending). */
export const CAI_BANDS = [
  { label: "Critical", key: "critical", floor: 0 },
  { label: "Weak", key: "poor", floor: 25 },
  { label: "Adequate", key: "fair", floor: 50 },
  { label: "Strong", key: "healthy", floor: 70 },
  { label: "Exemplary", key: "exemplary", floor: 90 },
];

/**
 * A CAI score as the product writes it (owner, 12 November 2026; ADR-0028 in kennel.canine.dev): one decimal,
 * always — "97.6", and "98.0" when it lands on a whole number — rounded half to even as the CAI standard states
 * (§7), the same tenth CaiScoreText and the app write. Never "-0.0"; a value that is not a number is "—". On a Danish
 * page (<html lang="da">) with the Danish decimal comma, "97,6" (owner, 13 November 2026: "use comma in danish").
 */
export function caiText(score) {
  const n = Number(score);
  return Number.isFinite(n) ? local(caiTenths(n).toFixed(1)) : "—";
}

/** True on a Danish page: the document says lang="da" (or "da-DK"). False where there is no document. */
export function danishPage() {
  return typeof document !== "undefined" && /^da(-|$)/i.test(document.documentElement?.lang ?? "");
}

/** A one-decimal figure in the page's language: the decimal comma on a Danish page. */
function local(text) {
  return danishPage() ? text.replace(".", ",") : text;
}

/** The shown tenth of a score as a number: half to even on the scaled value, as .NET's Math.Round(x, 1) does. */
export function caiTenths(score) {
  const scaled = Number(score) * 10;
  const floor = Math.floor(scaled);
  const tenths = (scaled - floor === 0.5 ? (floor % 2 === 0 ? floor : floor + 1) : Math.round(scaled)) / 10;
  return tenths === 0 ? 0 : tenths;
}

/** The size of a change between two scores as the reader sees them: the difference of the two shown figures. */
export function caiChange(from, to) {
  return local(Math.abs(caiTenths(to) - caiTenths(from)).toFixed(1));
}

/** "up", "down" or "same", read off the two shown figures, so a move that shows as 0.0 has no direction. */
export function caiMove(from, to) {
  const d = caiTenths(to) - caiTenths(from);
  return Math.abs(d) < 0.05 ? "same" : d > 0 ? "up" : "down";
}

/** The band for a 0–100 score via the parked cutlines (CaiBands.FromScore). */
export function bandFor(score) {
  return score >= 90
    ? CAI_BANDS[4]
    : score >= 70
      ? CAI_BANDS[3]
      : score >= 50
        ? CAI_BANDS[2]
        : score >= 25
          ? CAI_BANDS[1]
          : CAI_BANDS[0];
}

/**
 * "You are here" marker geometry on the fixed worst→best five-segment band
 * ladder — mirrors WdCard.markerFor byte-for-byte: equal-width 20% segments,
 * position = band index + fractional progress through the band's PARKED score
 * range. Pure function of the absolute score.
 */
export function markerFor(score) {
  const s = Math.max(0, Math.min(100, score));
  let i = CAI_BANDS.length - 1;
  while (i > 0 && s < CAI_BANDS[i].floor) i--;
  const upper = i < CAI_BANDS.length - 1 ? CAI_BANDS[i + 1].floor : 100;
  const span = upper - CAI_BANDS[i].floor;
  const frac =
    span > 0 ? Math.max(0, Math.min(1, (s - CAI_BANDS[i].floor) / span)) : 0.5;
  return {
    leftPct: i * 20 + frac * 20,
    third: frac < 1 / 3 ? 0 : frac < 2 / 3 ? 1 : 2,
    key: CAI_BANDS[i].key,
  };
}
