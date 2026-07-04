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
