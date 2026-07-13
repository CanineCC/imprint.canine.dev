// <cai-language-support api-base="…" kicker="…" heading="…" lede="…"
//                       footnote="…" brand="watchdog|assay|cai">
//
// The language survey-clarity (FIT) matrix — every language Watchdog supports, grouped by
// FIT band, best-first, with each language's applicability (0–10), the lenses that apply,
// and the ones that are N/A. FIT is SURVEY CLARITY (how clearly a language leads to a
// complete DDD / event-sourcing / event-driven / vertical-slice survey), NOT a grade of the
// language or the code — so the gauge is a single-hue "focus" reading in the brand accent,
// never a good/bad traffic-light.
//
// Fetches {api}/api/public/language-support → { note, languages:[{code,displayName,
// applicability,supportKind,band,bandLabel,summary,coveredLenses[],notApplicableLenses[],
// signedOffOn}], bands:[{band,label,why}] }. AUTO-UPDATES: a newly signed-off language
// appears the moment it is seeded in the product, with no republish. Without an api-base it
// renders a small labelled SAMPLE so a static preview is never empty.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchLanguageSupport } from "./live.js";

// The labelled fallback — a representative slice spanning the four bands, shown only when no
// live source resolves. The live matrix carries every supported language.
const SAMPLE = {
  note: "FIT is survey clarity — how clearly a language leads to a complete architecture survey. It is NOT a measure of language or code quality.",
  languages: [
    { code: "csharp", displayName: "C#", applicability: 10, supportKind: "Deep", band: "PERFECT", summary: "Native symbols resolve every lens with full fidelity.", coveredLenses: ["DDD", "Event sourcing", "Event-driven", "Vertical slice"], notApplicableLenses: [] },
    { code: "java", displayName: "Java", applicability: 9, supportKind: "Deep", band: "PERFECT", summary: "Source-resolution is strong; the full domain survey fires.", coveredLenses: ["DDD", "Event sourcing", "Event-driven", "Vertical slice"], notApplicableLenses: ["strongly-typed-id (field-based)"] },
    { code: "python", displayName: "Python", applicability: 7, supportKind: "Deep", band: "HIGH", summary: "Marker lenses are strong; call-owner lenses partial on untyped code — declined, never guessed.", coveredLenses: ["DDD", "Event sourcing", "Event-driven"], notApplicableLenses: ["Sealed/DU lens"] },
    { code: "ruby", displayName: "Ruby", applicability: 6, supportKind: "Deep", band: "MEDIUM", summary: "The hardest static target; markers and block-fold DSLs still resolve.", coveredLenses: ["DDD", "Event sourcing", "Event-driven"], notApplicableLenses: ["god-class/LCOM"] },
    { code: "javascript", displayName: "JavaScript", applicability: 4, supportKind: "Structural", band: "LOW", summary: "Structural suite only, without types — the least architecture signal.", coveredLenses: ["Structural", "Module graph"], notApplicableLenses: ["DDD", "Event sourcing"] },
  ],
  bands: [
    { band: "PERFECT", label: "PERFECT", why: "Every lens applies and resolves cleanly — the clearest surveys we produce. About survey clarity, not language quality." },
    { band: "HIGH", label: "HIGH", why: "Most lenses apply and resolve — a clear, well-populated survey. About survey clarity, not language quality." },
    { band: "MEDIUM", label: "MEDIUM", why: "A meaningful slice of the survey is populated; some lenses are N/A or best-effort. About survey clarity, not language quality." },
    { band: "LOW", label: "LOW", why: "The structural lenses apply but not the deep domain catalogue. What we report is real; there is simply less of it. About survey clarity, not language quality." },
  ],
};

// Band display order (best-first) and the "focus" dot count that reads as clarity.
const BAND_ORDER = ["PERFECT", "HIGH", "MEDIUM", "LOW"];
const FOCUS = { PERFECT: 4, HIGH: 3, MEDIUM: 2, LOW: 1 };

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-fit { max-width: 66rem; margin: 0 auto; }
.mk-fit-note { color: var(--muted); font-size: var(--fs-sm); line-height: 1.6; margin: 0 0 1.6rem; max-width: 60ch; }
.mk-fit-note strong { color: var(--ink-soft); }
.mk-fit-band { margin-top: 1.8rem; }
.mk-fit-band:first-of-type { margin-top: 0; }
.mk-fit-bandhead { display: flex; align-items: baseline; gap: 0.7rem; flex-wrap: wrap; }
.mk-fit-bandname { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.11em; text-transform: uppercase; color: var(--ink-soft); }
.mk-fit-dots { display: inline-flex; gap: 3px; }
.mk-fit-dots i { width: 6px; height: 6px; border-radius: var(--r-full); background: var(--border-strong); display: block; }
.mk-fit-dots i.on { background: var(--accent); box-shadow: 0 0 6px var(--accent-wash); }
.mk-fit-bandcount { color: var(--muted); font-size: var(--fs-2xs); font-variant-numeric: tabular-nums; }
.mk-fit-why { color: var(--muted); font-size: var(--fs-xs); line-height: 1.55; margin: 0.4rem 0 1rem; max-width: 76ch; }
.mk-fit-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr)); gap: 0.75rem; }
.mk-fit-card { position: relative; border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 0.95rem 1rem 0.85rem; display: flex; flex-direction: column; gap: 0.6rem; overflow: hidden; }
.mk-fit-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 3px; background: var(--accent); opacity: var(--edge, 0.85); }
.mk-fit-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.6rem; }
.mk-fit-name { font-size: var(--fs-lg); font-weight: 600; color: var(--heading); letter-spacing: -0.01em; }
.mk-fit-code { font-family: var(--font-mono); font-size: var(--fs-2xs); color: var(--muted); margin-top: 2px; }
.mk-fit-kind { font-family: var(--font-mono); font-size: 9.5px; font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 3px 6px; white-space: nowrap; }
.mk-fit-gauge { display: flex; align-items: center; gap: 0.6rem; }
.mk-fit-track { flex: 1; height: 7px; border-radius: var(--r-full); background: var(--surface-2); border: 1px solid var(--border); overflow: hidden; }
.mk-fit-fill { height: 100%; border-radius: var(--r-full); background: linear-gradient(90deg, var(--accent), var(--accent-strong)); box-shadow: 0 0 8px var(--accent-wash); }
.mk-fit-num { font-family: var(--font-mono); font-size: var(--fs-sm); font-weight: 600; color: var(--accent-ink); min-width: 2.6rem; text-align: right; font-variant-numeric: tabular-nums; }
.mk-fit-num small { color: var(--muted); font-weight: 400; }
.mk-fit-pill { align-self: flex-start; display: inline-flex; align-items: center; gap: 0.4rem; font-family: var(--font-mono); font-size: 10px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--accent-ink); background: var(--accent-wash); border: 1px solid var(--border-strong); border-radius: var(--r-full); padding: 4px 9px; }
.mk-fit-pill i { width: 5px; height: 5px; border-radius: var(--r-full); background: var(--accent); box-shadow: 0 0 6px var(--accent-wash); }
.mk-fit-summary { margin: 0; color: var(--ink-soft); font-size: var(--fs-xs); line-height: 1.5; }
.mk-fit-lenses { display: flex; flex-direction: column; gap: 0.4rem; }
.mk-fit-lensrow { display: flex; gap: 0.5rem; align-items: baseline; }
.mk-fit-lenslabel { font-family: var(--font-mono); font-size: 9px; font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); min-width: 3.1rem; padding-top: 3px; }
.mk-fit-chips { display: flex; flex-wrap: wrap; gap: 4px; }
.mk-fit-chip { font-size: var(--fs-2xs); padding: 3px 7px; border-radius: var(--r-sm); background: var(--surface-2); border: 1px solid var(--border); color: var(--ink-soft); }
.mk-fit-chip.na { color: var(--muted); border-style: dashed; }
.mk-fit-foot { display: flex; justify-content: flex-end; margin-top: 0.1rem; padding-top: 0.5rem; border-top: 1px solid var(--hairline); font-family: var(--font-mono); font-size: 10px; color: var(--muted); }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.4rem auto 0; max-width: 60ch; text-align: center; }
`;

function focusDots(band) {
  const on = FOCUS[band] || 0;
  let h = '<span class="mk-fit-dots" aria-hidden="true">';
  for (let i = 1; i <= 4; i++) h += `<i class="${i <= on ? "on" : ""}"></i>`;
  return h + "</span>";
}

function cardHtml(l) {
  const edge = { PERFECT: 1, HIGH: 0.7, MEDIUM: 0.45, LOW: 0.22 }[l.band] ?? 0.5;
  const covered = (l.coveredLenses || []).map((x) => `<span class="mk-fit-chip">${escapeHtml(x)}</span>`).join("");
  const na = (l.notApplicableLenses || []).map((x) => `<span class="mk-fit-chip na">${escapeHtml(x)}</span>`).join("");
  const naRow = na ? `<div class="mk-fit-lensrow"><span class="mk-fit-lenslabel">N/A</span><div class="mk-fit-chips">${na}</div></div>` : "";
  const signed = l.signedOffOn ? `signed off ${escapeHtml(l.signedOffOn)}` : "baseline coverage";
  const pct = Math.max(0, Math.min(100, (Number(l.applicability) || 0) * 10));
  return (
    `<article class="mk-fit-card" style="--edge:${edge}">` +
    `<div class="mk-fit-top"><div><div class="mk-fit-name">${escapeHtml(l.displayName)}</div>` +
    `<div class="mk-fit-code">${escapeHtml(l.code)}</div></div>` +
    `<span class="mk-fit-kind">${escapeHtml(l.supportKind || "")}</span></div>` +
    `<div class="mk-fit-gauge"><div class="mk-fit-track"><div class="mk-fit-fill" style="width:${pct}%"></div></div>` +
    `<span class="mk-fit-num">${escapeHtml(String(l.applicability))}<small>/10</small></span></div>` +
    `<span class="mk-fit-pill"><i></i>${escapeHtml(l.bandLabel || l.band)} fit</span>` +
    `<p class="mk-fit-summary">${escapeHtml(l.summary || "")}</p>` +
    `<div class="mk-fit-lenses"><div class="mk-fit-lensrow"><span class="mk-fit-lenslabel">Lenses</span>` +
    `<div class="mk-fit-chips">${covered}</div></div>${naRow}</div>` +
    `<div class="mk-fit-foot">${signed}</div></article>`
  );
}

customElements.define(
  "cai-language-support",
  class extends CaiIsland {
    #data = null;

    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const data = await fetchLanguageSupport(api);
      if (!data || !Array.isArray(data.languages) || data.languages.length === 0) return;
      this.#data = data;
      this._live = true;
      this.render(this.shadowRoot);
    }

    render(root) {
      const data = this._live && this.#data ? this.#data : SAMPLE;
      const whyOf = {};
      for (const b of data.bands || []) whyOf[b.band] = b.why;

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-fit">`;

      // The survey-clarity framing note (from the endpoint) — unless the author supplied a lede.
      if (data.note && !this.getAttribute("lede")) {
        const parts = data.note.split("—");
        const lead = escapeHtml(parts[0].trim());
        const rest = escapeHtml(parts.slice(1).join("—").trim());
        html += `<p class="mk-fit-note"><strong>${lead}</strong>${rest ? " — " + rest : ""}</p>`;
      }

      for (const band of BAND_ORDER) {
        const langs = data.languages.filter((l) => l.band === band);
        if (langs.length === 0) continue;
        html += `<section class="mk-fit-band"><div class="mk-fit-bandhead">`;
        html += `<span class="mk-fit-bandname">${focusDots(band)} ${escapeHtml(band)} fit</span>`;
        html += `<span class="mk-fit-bandcount">${langs.length} language${langs.length > 1 ? "s" : ""}</span>`;
        html += `</div>`;
        if (whyOf[band]) html += `<p class="mk-fit-why">${escapeHtml(whyOf[band])}</p>`;
        html += `<div class="mk-fit-grid">${langs.map(cardHtml).join("")}</div>`;
        html += `</section>`;
      }
      html += `</div>`;

      const footnote = this.getAttribute("footnote");
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;
    }
  }
);
