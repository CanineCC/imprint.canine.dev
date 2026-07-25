// <cai-public-reports api-base="…" kicker="…" heading="…" lede="…" footnote="…" brand="watchdog">
//
// The public-reports shop window: a CURATED, CAPPED slice of Watchdog's surveyed OSS corpus —
// never the whole thing. Leads with the corpus scale ("3,200+ repositories surveyed"), shows a
// language-diverse best-first set of at most 24 survey cards, and lets a visitor filter by
// language/lens or search by name — the filter hits the whole corpus server-side, so search
// implies the depth the grid never dumps. Each card deep-links to the full signed survey on the app.
//
// Fetches {api}/api/public/reports → { totals:{repositories,publishedSurveys}, facets:{languages,
// lenses}, cap, matched, capped, curated, reports:[…] }. Without an api-base it renders a labelled
// SAMPLE so a static preview is never empty (filtering is inert in that mode).

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchPublicReports } from "./live.js";

// A few common language codes → display labels; anything else renders as-is (the census already
// stores readable names, so this is only a safety net for code-style values).
const LANG_LABELS = { csharp: "C#", fsharp: "F#", vbnet: "VB.NET", cpp: "C++", typescript: "TypeScript", javascript: "JavaScript" };
const langLabel = (l) => LANG_LABELS[String(l || "").toLowerCase()] || l || "";

// Conditional/differentiating lenses worth a chip (the always-on five are on every card).
const LENS_CHIP = { domainModelling: "DDD", eventSourcing: "Event sourcing", eventDriven: "Event-driven" };

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-rep { max-width: 72rem; margin: 0 auto; }
.mk-rep-scale { display: flex; align-items: baseline; gap: 0.6rem; flex-wrap: wrap; margin: 0 0 1.3rem; }
.mk-rep-big { font-family: var(--font-mono); font-size: clamp(1.8rem, 4vw, 2.6rem); font-weight: 700; color: var(--accent-ink); letter-spacing: -0.02em; font-variant-numeric: tabular-nums; line-height: 1; }
.mk-rep-scale-cap { color: var(--muted); font-size: var(--fs-sm); line-height: 1.5; }
.mk-rep-scale-cap strong { color: var(--ink-soft); font-weight: 600; }
.mk-rep-controls { display: flex; gap: 0.6rem; flex-wrap: wrap; align-items: center; margin: 0 0 1.2rem; }
.mk-rep-search { flex: 1 1 14rem; min-width: 10rem; display: flex; align-items: center; gap: 0.5rem; border: 1px solid var(--border-strong); border-radius: var(--r-full); background: var(--surface); padding: 0.45rem 0.9rem; }
.mk-rep-search input { flex: 1; border: 0; background: transparent; color: var(--ink); font: inherit; font-size: var(--fs-sm); outline: none; min-width: 4rem; }
.mk-rep-search svg { width: 15px; height: 15px; color: var(--muted); flex: none; }
.mk-rep select { appearance: none; border: 1px solid var(--border-strong); border-radius: var(--r-full); background: var(--surface); color: var(--ink); font: inherit; font-size: var(--fs-sm); padding: 0.45rem 2.1rem 0.45rem 0.95rem; cursor: pointer;
  background-image: linear-gradient(45deg, transparent 50%, var(--muted) 50%), linear-gradient(135deg, var(--muted) 50%, transparent 50%); background-position: calc(100% - 15px) 52%, calc(100% - 10px) 52%; background-size: 5px 5px, 5px 5px; background-repeat: no-repeat; }
.mk-rep-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(15.5rem, 1fr)); gap: 0.75rem; }
.mk-rep-card { position: relative; display: flex; flex-direction: column; gap: 0.6rem; border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 0.9rem 1rem; text-decoration: none; color: inherit; overflow: hidden; transition: border-color 0.15s, transform 0.15s; }
.mk-rep-card:hover { border-color: var(--accent); transform: translateY(-1px); }
.mk-rep-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 3px; background: var(--b, var(--accent)); }
.mk-rep-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.6rem; }
.mk-rep-name { font-size: var(--fs-md); font-weight: 600; color: var(--heading); letter-spacing: -0.01em; word-break: break-word; line-height: 1.25; }
.mk-rep-owner { color: var(--muted); font-weight: 400; }
.mk-rep-score { flex: none; display: inline-flex; align-items: baseline; gap: 1px; font-family: var(--font-mono); font-size: var(--fs-lg); font-weight: 700; color: var(--b, var(--accent-ink)); font-variant-numeric: tabular-nums; }
.mk-rep-score small { font-size: 9px; font-weight: 500; color: var(--muted); }
.mk-rep-chips { display: flex; flex-wrap: wrap; gap: 0.35rem; }
.mk-rep-chip { font-family: var(--font-mono); font-size: 9.5px; font-weight: 600; letter-spacing: 0.04em; color: var(--muted); border: 1px solid var(--border); border-radius: var(--r-sm); padding: 2px 6px; white-space: nowrap; }
.mk-rep-chip.lens { color: var(--accent-ink); background: var(--accent-wash); border-color: var(--border-strong); }
.mk-rep-read { margin-top: auto; font-size: var(--fs-xs); font-weight: 600; color: var(--accent-ink); }
.mk-rep-foot { color: var(--muted); font-size: var(--fs-xs); line-height: 1.55; margin: 1.2rem 0 0; text-align: center; }
.mk-rep-empty { color: var(--muted); font-size: var(--fs-sm); text-align: center; padding: 2rem 1rem; border: 1px dashed var(--border-strong); border-radius: var(--r-lg); }
`;

// The labelled fallback for the first paint and the no-api static preview — defined BEFORE the
// element is registered, because customElements.define() upgrades any already-parsed element
// synchronously (its connectedCallback → render → sample runs immediately).
const SAMPLE_FALLBACK = {
  totals: { repositories: 3200, publishedSurveys: 3800 },
  facets: { languages: [{ language: "C#", count: 640 }, { language: "TypeScript", count: 410 }, { language: "Go", count: 300 }, { language: "Rust", count: 240 }, { language: "Python", count: 380 }, { language: "Java", count: 260 }], lenses: [{ key: "domainModelling", label: "Domain modelling" }, { key: "eventSourcing", label: "Event sourcing" }, { key: "eventDriven", label: "Event-driven" }] },
  cap: 24, matched: 6, capped: false, curated: true,
  reports: [
    { display: "ardalis/CleanArchitecture", owner: "ardalis", name: "CleanArchitecture", score: 64, bandHex: "#b0872f", primaryLanguage: "C#", secondaryLanguages: [], lenses: ["domainModelling"], reportPath: "", sourceUrl: "https://github.com/ardalis/CleanArchitecture" },
    { display: "gin-gonic/gin", owner: "gin-gonic", name: "gin", score: 72, bandHex: "#1f7a5a", primaryLanguage: "Go", secondaryLanguages: [], lenses: [], reportPath: "", sourceUrl: "https://github.com/gin-gonic/gin" },
    { display: "tokio-rs/axum", owner: "tokio-rs", name: "axum", score: 76, bandHex: "#1f7a5a", primaryLanguage: "Rust", secondaryLanguages: [], lenses: [], reportPath: "", sourceUrl: "https://github.com/tokio-rs/axum" },
    { display: "oskardudycz/EventSourcing.NetCore", owner: "oskardudycz", name: "EventSourcing.NetCore", score: 68, bandHex: "#b0872f", primaryLanguage: "C#", secondaryLanguages: [], lenses: ["eventSourcing", "domainModelling"], reportPath: "", sourceUrl: "https://github.com/oskardudycz/EventSourcing.NetCore" },
    { display: "tiangolo/fastapi", owner: "tiangolo", name: "fastapi", score: 74, bandHex: "#1f7a5a", primaryLanguage: "Python", secondaryLanguages: [], lenses: [], reportPath: "", sourceUrl: "https://github.com/tiangolo/fastapi" },
    { display: "spring-projects/spring-petclinic", owner: "spring-projects", name: "spring-petclinic", score: 66, bandHex: "#b0872f", primaryLanguage: "Java", secondaryLanguages: ["TypeScript"], lenses: ["domainModelling"], reportPath: "", sourceUrl: "https://github.com/spring-projects/spring-petclinic" },
  ],
};

const nf = (n) => (typeof n === "number" ? n.toLocaleString("en-US") : n);

function cardHtml(c, api) {
  const href = api ? api.replace(/\/$/, "") + c.reportPath : (c.sourceUrl || "#");
  const langs = [c.primaryLanguage, ...(c.secondaryLanguages || [])].filter(Boolean).slice(0, 3);
  const lensChips = (c.lenses || []).filter((k) => LENS_CHIP[k]).map((k) => `<span class="mk-rep-chip lens">${escapeHtml(LENS_CHIP[k])}</span>`);
  const langChips = langs.map((l) => `<span class="mk-rep-chip">${escapeHtml(langLabel(l))}</span>`);
  const [owner, name] = c.display.includes("/") ? c.display.split("/") : [c.owner, c.name];
  return `<a class="mk-rep-card" style="--b:${escapeHtml(c.bandHex || "")}" href="${escapeHtml(href)}" target="_blank" rel="noopener">
      <div class="mk-rep-top">
        <span class="mk-rep-name"><span class="mk-rep-owner">${escapeHtml(owner)}/</span>${escapeHtml(name)}</span>
        <span class="mk-rep-score">${escapeHtml(String(c.score))}<small>/100</small></span>
      </div>
      <div class="mk-rep-chips">${langChips.join("")}${lensChips.join("")}</div>
      <span class="mk-rep-read">Read the survey →</span>
    </a>`;
}

function footHtml(data) {
  if (data.curated) {
    return `A curated window on the corpus — filter or search to reach the rest of the <strong>${nf(data.totals.repositories)}+</strong> surveyed.`;
  }
  if (data.capped) {
    return `Showing the top ${data.reports.length} of <strong>${nf(data.matched)}</strong> matches — refine to narrow it.`;
  }
  return `${nf(data.matched)} match${data.matched === 1 ? "" : "es"}.`;
}

customElements.define(
  "cai-public-reports",
  class extends CaiIsland {
    #data = null;
    _state = { lang: "", lens: "", q: "" };

    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const data = await fetchPublicReports(api, this._state);
      if (!data || !Array.isArray(data.reports)) return;
      this.#data = data;
      this._live = true;
      this.render(this.shadowRoot);
    }

    render(root) {
      const api = this.apiBase();
      const data = this._live && this.#data ? this.#data : null;
      // Sample (no api-base or pre-live) — imported lazily via the fetch helper's fallback.
      const d = data || this.#sample();
      const langOpts = (d.facets?.languages || []).map((l) => `<option value="${escapeHtml(l.language)}">${escapeHtml(langLabel(l.language))} (${l.count})</option>`).join("");
      const lensOpts = (d.facets?.lenses || []).map((l) => `<option value="${escapeHtml(l.key)}">${escapeHtml(l.label)}</option>`).join("");

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-rep">
        <div class="mk-rep-scale">
          <span class="mk-rep-big">${nf(d.totals.repositories)}+</span>
          <span class="mk-rep-scale-cap"><strong>repositories surveyed</strong> across ${(d.facets?.languages || []).length} languages — a public, signed record you can open and check.</span>
        </div>
        <div class="mk-rep-controls">
          <label class="mk-rep-search"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>
            <input type="search" data-f="q" placeholder="Search the corpus…" value="${escapeHtml(this._state.q)}" ${api ? "" : "disabled"} /></label>
          <select data-f="lang" ${api ? "" : "disabled"}><option value="">All languages</option>${langOpts}</select>
          <select data-f="lens" ${api ? "" : "disabled"}><option value="">All lenses</option>${lensOpts}</select>
        </div>
        <div class="mk-rep-grid" data-grid>${d.reports.map((c) => cardHtml(c, api)).join("")}</div>
        <p class="mk-rep-foot" data-foot>${footHtml(d)}</p>
      </div>`;

      const footnote = this.getAttribute("footnote");
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;
      this.#wire(root, api);
    }

    #wire(root, api) {
      if (!api) return; // sample mode: filtering is inert
      const grid = root.querySelector("[data-grid]");
      const foot = root.querySelector("[data-foot]");
      const q = root.querySelector('[data-f="q"]');
      const lang = root.querySelector('[data-f="lang"]');
      const lens = root.querySelector('[data-f="lens"]');
      const apply = async () => {
        this._state = { lang: lang.value, lens: lens.value, q: q.value.trim() };
        const d = await fetchPublicReports(api, this._state);
        this.#data = d;
        grid.innerHTML = d.reports.length ? d.reports.map((c) => cardHtml(c, api)).join("") : "";
        if (!d.reports.length) grid.innerHTML = `<div class="mk-rep-empty">No surveys match — clear the filters to browse the corpus.</div>`;
        foot.innerHTML = footHtml(d);
      };
      lang.addEventListener("change", apply);
      lens.addEventListener("change", apply);
      let t;
      q.addEventListener("input", () => { clearTimeout(t); t = setTimeout(apply, 250); });
    }

    #sample() {
      return SAMPLE_FALLBACK;
    }
  }
);
