// The CAI score card renderer — ported from packages/ui/src/CaiScoreCard.tsx
// to HTML-string builders (server-render parity, no React). Every DOM shape,
// class name and geometry matches the source component byte-close so the
// island reads identically to the CMS card.

import { CAI_BANDS, bandFor, markerFor } from "./cai.js";
import { escapeHtml } from "./tokens.js";

// A monotonically increasing id source so each sparkline's gradient is unique
// within a document (React.useId's role).
let UID = 0;
function nextUid() {
  return "s" + (UID++).toString(36);
}

/**
 * The fixed five-band rail with a marker. variant "diamond" (cards — the big
 * score number is the headline, the diamond is purely "you are here") or "pin"
 * (labelled score badge — the explainer variant). caps adds Worst/Best captions.
 */
export function ladderHtml(score, { variant = "diamond", caps = false, className } = {}) {
  const mk = markerFor(score);
  const b = bandFor(score);
  const left = `${mk.leftPct.toFixed(2)}%`;
  const segs = CAI_BANDS.map((band) => `<i class="seg-${band.key}"></i>`).join("");
  let marker;
  if (variant === "diamond") {
    marker =
      `<div class="cai-mk cai-diamond" style="left:${left};--dia:var(--band-${b.key})">` +
      `<span class="cai-diamond-foot"></span></div>`;
  } else {
    marker =
      `<div class="cai-mk cai-pin" style="left:${left}">` +
      `<span class="cai-pin-badge">${Math.round(score)}</span>` +
      `<span class="cai-pin-line"></span>` +
      `<span class="cai-pin-foot"></span></div>`;
  }
  const capsHtml = caps
    ? `<div class="cai-caps"><span>Worst</span><span>Best</span></div>`
    : "";
  const cls =
    "cai-ladder" + (caps ? "" : " compact") + (className ? ` ${className}` : "");
  return (
    `<div class="${cls}">` +
    `<div class="cai-rail" role="img" aria-label="${Math.round(score)} of 100 on a fixed worst-to-best scale">` +
    `<div class="cai-segs">${segs}</div>${marker}</div>${capsHtml}</div>`
  );
}

/** Screen-reader trend direction (the sparkline itself is decoration). */
function trendText(series) {
  const first = series[0];
  const last = series[series.length - 1];
  const delta = Math.round(last) - Math.round(first);
  const dir =
    delta >= 1
      ? `improving (up ${delta})`
      : delta <= -1
        ? `declining (down ${-delta})`
        : "steady";
  return `Trend: ${dir} over the last ${series.length} scans.`;
}

/**
 * The trend sparkline: a smooth SVG curve whose stroke is a value-mapped
 * gradient, so the line changes colour as it crosses band cutlines — the old
 * uPlot trend's per-band colouring, server-rendered.
 */
function sparkHtml(series) {
  const uid = nextUid();
  const W = 320;
  const H = 36;
  const PAD = 4;
  const n = series.length;
  const lo = Math.min(...series);
  const hi = Math.max(...series);
  const min = Math.max(0, lo - 3);
  const max = Math.min(100, hi + 3);
  const span = max - min || 1;
  const px = (i) => (i / (n - 1)) * (W - 2 * PAD) + PAD;
  const py = (v) => PAD + (1 - (v - min) / span) * (H - 2 * PAD);
  const pts = series.map((v, i) => [px(i), py(v)]);

  // Catmull-Rom → cubic bézier for a gentle spline through every point.
  let d = `M ${pts[0][0].toFixed(1)} ${pts[0][1].toFixed(1)}`;
  for (let i = 0; i < n - 1; i++) {
    const p0 = pts[Math.max(0, i - 1)];
    const p1 = pts[i];
    const p2 = pts[i + 1];
    const p3 = pts[Math.min(n - 1, i + 2)];
    const c1 = [p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6];
    const c2 = [p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6];
    d += ` C ${c1[0].toFixed(1)} ${c1[1].toFixed(1)}, ${c2[0].toFixed(1)} ${c2[1].toFixed(1)}, ${p2[0].toFixed(1)} ${p2[1].toFixed(1)}`;
  }

  // Hard gradient stops at the band cutlines inside the value range.
  const stops = [];
  let current = bandFor(min).key;
  stops.push({ at: 0, key: current });
  for (const band of CAI_BANDS) {
    if (band.floor > min && band.floor < max) {
      const t = (band.floor - min) / span;
      stops.push({ at: t, key: current });
      current = band.key;
      stops.push({ at: t, key: current });
    }
  }
  stops.push({ at: 1, key: current });

  const stopEls = stops
    .map(
      (s) =>
        `<stop offset="${(s.at * 100).toFixed(1)}%" stop-color="var(--band-${s.key})"></stop>`
    )
    .join("");

  return (
    `<svg class="cai-spark" viewBox="0 0 ${W} ${H}" preserveAspectRatio="none" aria-hidden="true" focusable="false">` +
    `<defs><linearGradient id="caisg-${uid}" gradientUnits="userSpaceOnUse" x1="0" y1="${H - PAD}" x2="0" y2="${PAD}">${stopEls}</linearGradient></defs>` +
    `<path d="${d}" fill="none" stroke="url(#caisg-${uid})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path>` +
    `</svg><span class="sr-only">${escapeHtml(trendText(series))}</span>`
  );
}

/**
 * The full score card body — ported from CaiScoreCard.tsx. `data` is the
 * scoreCard object ({ name, owner, score, series, arcFirst, arcBest, lenses,
 * rows, sealText, href }). Returns the inner HTML of a .cai-card element.
 */
export function scoreCardBodyHtml(data) {
  const score = Math.max(0, Math.min(100, Number(data.score) || 0));
  const b = bandFor(score);
  const series =
    Array.isArray(data.series) && data.series.length >= 2 ? data.series : null;
  const hasArc =
    data.arcFirst != null &&
    data.arcBest != null &&
    Number.isFinite(Number(data.arcFirst)) &&
    Number.isFinite(Number(data.arcBest));
  const delta = hasArc
    ? Math.round(Number(data.arcBest)) - Math.round(Number(data.arcFirst))
    : 0;
  const lenses = (data.lenses || []).filter((l) => l && l.label);
  const rows = (data.rows || []).filter((r) => r && r.label);

  let h = "";
  if (data.sealText) h += `<span class="cai-seal">${escapeHtml(data.sealText)}</span>`;
  h +=
    `<div class="cai-top"><span class="cai-name">` +
    `<span class="cai-repo">${escapeHtml(data.name)}</span>` +
    (data.owner ? `<span class="cai-owner">by ${escapeHtml(data.owner)}</span>` : "") +
    `</span><span class="cai-chip band-${b.key}">${escapeHtml(b.label)}</span></div>`;
  h +=
    `<div class="cai-scoreline"><span class="cai-cai">CAI</span>` +
    `<span class="cai-score ink-${b.key}">${Math.round(score)}</span>` +
    `<span class="cai-unit"> / 100</span></div>`;
  h += ladderHtml(score, { variant: "diamond" });
  if (series) h += sparkHtml(series);
  if (hasArc) {
    h +=
      `<div class="cai-arc">` +
      `<span class="cai-arc-from">${Math.round(Number(data.arcFirst))}</span>` +
      `<span class="cai-arc-arrow" aria-hidden="true">→</span>` +
      `<span class="cai-arc-to ink-${b.key}">${Math.round(Number(data.arcBest))}</span>` +
      (delta >= 1 ? `<span class="cai-arc-up">↑ +${delta}</span>` : "") +
      `</div>`;
  }
  if (lenses.length > 0) {
    h += `<div class="cai-lenses">`;
    for (const l of lenses) {
      const v = l.value == null ? null : Number(l.value);
      const lb = v == null ? null : bandFor(v);
      h += `<div class="cai-lens"><span class="cai-lens-name">${escapeHtml(l.label)}</span>`;
      h += `<span class="cai-lens-bar">`;
      if (v != null && lb) {
        h += `<span class="cai-lens-fill fill-${lb.key}" style="width:${Math.max(2, Math.round(v))}%"></span>`;
      }
      h += `</span>`;
      if (v == null || !lb) {
        h += `<span class="cai-lens-num cai-muted">—</span>`;
      } else {
        h += `<span class="cai-lens-num ink-${lb.key}">${Math.round(v)}</span>`;
      }
      h += `</div>`;
    }
    h += `</div>`;
  }
  if (rows.length > 0) {
    h += `<div class="cai-rows">`;
    for (const r of rows) {
      h +=
        `<div class="cai-row"><span>${escapeHtml(r.label)}</span>` +
        `<b class="${r.mono ? "mono" : ""}">${escapeHtml(r.value)}</b></div>`;
    }
    h += `</div>`;
  }
  return h;
}

// ── The CAI visual-layer CSS, lifted verbatim from canine.css §CAI ───────────
// Only the card / ladder / spark / arc / lens / rows selectors are included;
// gallery-grid and live-pill CSS live with their own widgets.
export const SCORECARD_CSS = `
.ink-exemplary { color: var(--band-exemplary-text); }
.ink-healthy { color: var(--band-healthy-text); }
.ink-fair { color: var(--band-fair-text); }
.ink-poor { color: var(--band-poor-text); }
.ink-critical { color: var(--band-critical-text); }
.fill-exemplary { background: var(--band-exemplary); }
.fill-healthy { background: var(--band-healthy); }
.fill-fair { background: var(--band-fair); }
.fill-poor { background: var(--band-poor); }
.fill-critical { background: var(--band-critical); }

.cai-card {
  position: relative; display: block; width: 100%; max-width: 460px;
  background: var(--surface); border: 1.5px solid var(--accent); border-radius: 16px;
  padding: 20px 22px; box-shadow: var(--shadow-overlay); color: var(--ink);
}
a.cai-card { color: var(--ink); }
a.cai-card:hover { text-decoration: none; border-color: var(--accent-strong); }
.cai-seal { position: absolute; top: -13px; right: 20px; background: var(--accent-strong); color: var(--on-accent); font-size: var(--fs-2xs); font-weight: 650; letter-spacing: 0.04em; padding: 5px 11px; border-radius: var(--r-full); }
.cai-card-cap { max-width: 460px; margin: 0.85rem 0 0; font-size: var(--fs-xs); color: var(--muted); text-align: center; line-height: 1.5; }

.cai-top { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.cai-name { min-width: 0; line-height: 1.25; }
.cai-repo { display: block; font-weight: 600; font-size: 15px; color: var(--heading); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cai-owner { display: block; color: var(--muted); font-weight: 400; font-size: var(--fs-xs); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cai-chip { display: inline-flex; align-items: center; font-size: var(--fs-xs); font-weight: 600; line-height: 1.4; border-radius: var(--r-full); padding: 2px 10px; white-space: nowrap; flex: none; }
.cai-chip.band-exemplary { background: color-mix(in srgb, var(--band-exemplary) 16%, transparent); color: var(--band-exemplary-text); }
.cai-chip.band-healthy { background: color-mix(in srgb, var(--band-healthy) 16%, transparent); color: var(--band-healthy-text); }
.cai-chip.band-fair { background: color-mix(in srgb, var(--band-fair) 16%, transparent); color: var(--band-fair-text); }
.cai-chip.band-poor { background: color-mix(in srgb, var(--band-poor) 16%, transparent); color: var(--band-poor-text); }
.cai-chip.band-critical { background: color-mix(in srgb, var(--band-critical) 16%, transparent); color: var(--band-critical-text); }

.cai-scoreline { margin-top: 6px; }
.cai-cai { font: 700 var(--fs-xs)/1 var(--font-ui); letter-spacing: 0.08em; color: var(--muted); margin-right: 8px; vertical-align: 6px; }
.cai-score { font-size: 44px; font-weight: 700; line-height: 1.1; letter-spacing: -0.02em; font-variant-numeric: tabular-nums lining-nums; }
.cai-unit { font-size: var(--fs-lg); color: var(--muted); font-weight: 400; }
.cai-muted { color: var(--muted); }

.cai-ladder { --mk-foot: 9px; margin: 6px 0 2px; }
.cai-card .cai-ladder { margin: 14px 0 12px; }
.cai-rail { position: relative; height: 11px; overflow: visible; }
.cai-segs { display: flex; height: 11px; border-radius: 6px; overflow: hidden; }
.cai-segs > i { flex: 1; display: block; }
.cai-segs > i.seg-critical { background: var(--band-critical); }
.cai-segs > i.seg-poor { background: var(--band-poor); }
.cai-segs > i.seg-fair { background: var(--band-fair); }
.cai-segs > i.seg-healthy { background: var(--band-healthy); }
.cai-segs > i.seg-exemplary { background: var(--band-exemplary); }
.cai-caps { display: flex; justify-content: space-between; font-size: var(--fs-2xs); color: var(--muted); margin-top: 9px; }
.cai-ladder.compact .cai-caps { display: none; }
.cai-mk { position: absolute; top: 0; bottom: 0; width: 0; z-index: 3; pointer-events: none; color: var(--mk); }
.cai-diamond .cai-diamond-foot {
  position: absolute; top: 50%; left: 0; width: 14px; height: 14px;
  transform: translate(-50%, -50%) rotate(45deg);
  background: var(--dia, var(--mk-on)); border: 2.5px solid var(--mk-on);
  border-radius: 2px; box-shadow: 0 1px 4px rgb(15 25 20 / 0.45);
}
.cai-diamond::before {
  content: ""; position: absolute; left: 0; bottom: calc(50% + 6px); width: 2px; height: 10px;
  transform: translateX(-50%); background: var(--dia, var(--mk)); border-radius: 1px 1px 0 0;
  box-shadow: 0 0 0 1px var(--mk-on);
}
.cai-pin .cai-pin-foot {
  position: absolute; top: 50%; left: 0; width: var(--mk-foot); height: var(--mk-foot);
  transform: translate(-50%, -50%) rotate(45deg); background: var(--mk); box-shadow: 0 0 0 2px var(--mk-on);
}
.cai-pin .cai-pin-line {
  position: absolute; bottom: 50%; left: 0; width: 3px; height: 12px; transform: translateX(-50%);
  background: var(--mk); border-radius: 2px 2px 0 0; box-shadow: 0 0 0 1.5px var(--mk-on);
}
.cai-pin .cai-pin-badge {
  position: absolute; bottom: calc(50% + 12px); left: 0; transform: translateX(-50%);
  min-width: 25px; height: 22px; padding: 0 7px; display: flex; align-items: center; justify-content: center;
  background: var(--mk); color: var(--mk-on); font: 700 13px/1 var(--font-ui); border-radius: 6px; white-space: nowrap;
  box-shadow: 0 0 0 2px var(--mk-on), 0 2px 5px rgb(20 40 30 / 0.3);
}
.cai-pin .cai-pin-badge::after {
  content: ""; position: absolute; top: 100%; left: 50%; transform: translateX(-50%);
  border: 5px solid transparent; border-top-color: var(--mk);
}

.cai-spark { width: 100%; height: 36px; display: block; margin: 2px 0 4px; }
.cai-arc { display: flex; align-items: baseline; gap: 8px; margin: 2px 0; }
.cai-arc-from { color: var(--muted); font-size: 17px; font-weight: 700; font-variant-numeric: tabular-nums; }
.cai-arc-arrow { color: var(--muted); }
.cai-arc-to { font-size: 24px; font-weight: 700; font-variant-numeric: tabular-nums; }
.cai-arc-up { margin-left: auto; color: var(--band-exemplary-text); font-size: var(--fs-md); font-weight: 700; }

.cai-lenses { display: grid; gap: 7px; margin-top: 14px; }
.cai-lens { display: grid; grid-template-columns: 92px 1fr 30px; align-items: center; gap: 10px; font-size: var(--fs-xs); }
.cai-lens-name { color: var(--ink-soft); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.cai-lens-bar { display: block; height: 7px; border-radius: var(--r-full); background: var(--surface-2); overflow: hidden; }
.cai-lens-fill { display: block; height: 100%; border-radius: var(--r-full); }
.cai-lens-num { text-align: right; font-weight: 600; font-variant-numeric: tabular-nums; }

.cai-rows { margin-top: 14px; border-top: 1px solid var(--border); padding-top: 4px; }
.cai-row { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; font-size: var(--fs-sm); padding: 6px 0; border-bottom: 1px dashed var(--hairline); color: var(--muted); }
.cai-row:last-child { border-bottom: 0; }
.cai-row b { color: var(--heading); font-weight: 600; text-align: right; }
.cai-row .mono { font-family: var(--font-mono); font-size: var(--fs-xs); }
`;

/**
 * Parse a scoreCard object from the widget's `card` attribute JSON — mirrors
 * blocks.tsx cardData(): defensive series parse, arc pass-through, lens/row
 * filtering. Returns null when the object lacks a usable name + score.
 */
export function parseCard(card) {
  if (!card || !card.name || card.score == null) return null;
  let series;
  if (Array.isArray(card.series)) {
    series = card.series.map((s) => Number(s)).filter((n) => Number.isFinite(n));
  } else {
    series = String(card.series || "")
      .split(",")
      .map((s) => Number(s.trim()))
      .filter((n) => Number.isFinite(n));
  }
  return {
    name: card.name,
    owner: card.owner || undefined,
    score: Number(card.score),
    series: series.length >= 2 ? series : undefined,
    arcFirst: card.arcFirst ?? null,
    arcBest: card.arcBest ?? null,
    lenses: (card.lenses || [])
      .filter((l) => l && l.label)
      .map((l) => ({ label: l.label, value: l.value ?? null })),
    rows: (card.rows || [])
      .filter((r) => r && r.label)
      .map((r) => ({ label: r.label, value: r.value || "", mono: !!r.mono })),
    href: card.href || undefined,
    sealText: card.sealText || undefined,
  };
}
