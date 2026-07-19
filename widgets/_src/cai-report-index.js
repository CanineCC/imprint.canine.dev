// <cai-report-index api-base="https://watchdog.canine.dev" kicker="…" heading="…" lede="…" brand="watchdog">
//
// Every published survey, browsable — the page a sceptic reaches for when they say "fine, show me
// the actual surveys". The existing <cai-card-gallery> shows a curated THREE for a landing page;
// this is the whole record, with the filters that make a large corpus navigable.
//
// The filters are the interesting part. A conditional lens fires only when the architecture calls
// for it, so filtering by "Event Sourcing" is not a score filter — it is "show me the event-sourced
// codebases". That is a question no competitor's public gallery can answer.
//
// Filtering is client-side over the already-fetched list: the corpus is a few hundred rows, so one
// fetch and local filtering beats a round trip per click, and every filter stays instant.

import { CaiIsland, TOKENS_CSS, BASE_CSS, sectionHeadHtml, SECTION_HEAD_CSS } from "./tokens.js";
import { fetchGallery, reportUrl } from "./live.js";

const CSS =
  TOKENS_CSS +
  BASE_CSS +
  SECTION_HEAD_CSS +
  `
.mk-filters { display: grid; gap: 0.45rem; margin: 0 0 0.9rem; }
.mk-frow { display: flex; flex-wrap: wrap; gap: 0.35rem; align-items: center; }
.mk-flabel { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); min-width: 4.6rem; }
.mk-chip { padding: 2px 10px; border: 1px solid var(--line); border-radius: 20px; font-size: 0.8rem;
           color: var(--muted); background: transparent; cursor: pointer; font: inherit; font-size: 0.8rem; }
.mk-chip:hover { border-color: var(--accent); color: var(--accent); }
.mk-chip[aria-pressed="true"] { background: var(--accent); border-color: var(--accent); color: var(--on-accent, #fff); }
.mk-chip .n { opacity: 0.65; font-variant-numeric: tabular-nums; }
.mk-count { color: var(--muted); font-size: 0.85rem; margin: 0 0 0.5rem; }
.mk-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.mk-table th { text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em;
               color: var(--muted); border-bottom: 1px solid var(--line); padding: 8px; }
.mk-table td { padding: 10px 8px; border-bottom: 1px solid var(--line); vertical-align: middle; }
.mk-repo { font-weight: 600; }
.mk-owner { color: var(--muted); font-weight: 400; font-size: 0.85em; }
.mk-cai { font-variant-numeric: tabular-nums; font-weight: 700; }
.mk-lang { display: inline-block; margin: 0 3px 2px 0; padding: 1px 7px; border-radius: 4px; font-size: 0.75rem;
           border: 1px solid var(--line); color: var(--muted); }
.mk-lang.primary { color: var(--ink); font-weight: 600; }
.mk-empty { color: var(--muted); padding: 1.5rem 0; }
`;

const LENSES = [
  ["codeHealth", "Code Health"],
  ["architecture", "Architecture"],
  ["maturity", "Maturity"],
  ["productionReadiness", "Production Readiness"],
  ["securityCompliance", "Security & Compliance"],
  ["domainModelling", "Domain Modelling"],
  ["eventDriven", "Event-Driven"],
  ["eventSourcing", "Event Sourcing"],
  ["accessibility", "Accessibility"],
  ["performance", "Performance"],
];

const LANG_LABELS = {
  csharp: "C#", vbnet: "VB.NET", fsharp: "F#", javascript: "JavaScript", typescript: "TypeScript", php: "PHP",
};
const langLabel = (c) => LANG_LABELS[c] || (c ? c[0].toUpperCase() + c.slice(1) : c);
const esc = (v) => String(v ?? "").replace(/[<>&"]/g, (ch) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;" })[ch]);

/// The gallery card exposes only the five always-on lens scores; the conditional ones are what make a lens filter
/// worth having, so read whichever the payload carries and treat "present and not null" as "this lens fired".
function lensesOf(card) {
  return LENSES.map(([key]) => key).filter((key) => card[key] !== undefined && card[key] !== null);
}

function languagesOf(card) {
  const primary = card.primaryLanguage || null;
  const secondaries = Array.isArray(card.secondaryLanguages) ? card.secondaryLanguages : [];
  return primary ? [primary, ...secondaries] : [];
}

class CaiReportIndex extends CaiIsland {
  static get observedAttributes() {
    return ["api-base", "kicker", "heading", "lede", "brand"];
  }

  /// The author's heading/lede when they set any, else a sensible default. sectionHeadHtml() reads the HOST's
  /// attributes and returns "" when none are set, so an unconfigured widget would otherwise render headless.
  #head() {
    const authored = sectionHeadHtml(this);
    if (authored) return authored;
    return `<div class="mk-section-head"><h2>Public surveys</h2><p>Every survey whose owner chose to publish it, with the score exactly as it was measured. The number is reproducible from the evidence and the rubric it was scored under.</p></div>`;
  }

  #cards = [];
  #lang = null;
  #lens = null;

  render(root) {
    
    root.innerHTML = `<style>${CSS}</style>${this.#head()}<div id="body"><p class="mk-empty">Loading the published record…</p></div>`;
    this.#paint(root);
  }

  async liveLoad() {
    const cards = await fetchGallery(this.apiBase());
    this.#cards = Array.isArray(cards) ? cards : [];
    if (this.shadowRoot) this.#paint(this.shadowRoot);
  }

  #paint(root) {
    const body = root.getElementById("body");
    if (!body) return;

    if (this.#cards.length === 0) {
      // Honest empty state — placeholder rows on a public evidence page would be the one unforgivable thing here.
      body.innerHTML = `<p class="mk-empty">No published surveys are available right now.</p>`;
      return;
    }

    const shown = this.#cards.filter(
      (c) =>
        (!this.#lang || languagesOf(c).includes(this.#lang)) &&
        (!this.#lens || lensesOf(c).includes(this.#lens)),
    );

    body.innerHTML = this.#filtersHtml() + this.#countHtml(shown) + this.#tableHtml(shown, root);
    this.#wire(root);
  }

  #filtersHtml() {
    const langCounts = new Map();
    for (const c of this.#cards) for (const l of languagesOf(c)) langCounts.set(l, (langCounts.get(l) || 0) + 1);

    const lensCounts = LENSES.map(([key, label]) => [key, label, this.#cards.filter((c) => lensesOf(c).includes(key)).length])
      .filter(([, , n]) => n > 0);

    // Only facets the corpus actually contains are offered — a filter that returns nothing is a dead end that
    // makes the record look emptier than it is.
    const langs = [...langCounts.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]));
    if (langs.length === 0 && lensCounts.length === 0) return "";

    const chip = (kind, value, label, n, on) =>
      `<button class="mk-chip" type="button" data-kind="${kind}" data-value="${esc(value ?? "")}" aria-pressed="${on}">${esc(
        label,
      )}${n === null ? "" : ` <span class="n">${n}</span>`}</button>`;

    let html = `<div class="mk-filters">`;
    if (langs.length) {
      html += `<div class="mk-frow"><span class="mk-flabel">Language</span>${chip("lang", "", "All", null, this.#lang === null)}`;
      for (const [code, n] of langs) html += chip("lang", code, langLabel(code), n, this.#lang === code);
      html += `</div>`;
    }
    if (lensCounts.length) {
      html += `<div class="mk-frow"><span class="mk-flabel">Lens</span>${chip("lens", "", "All", null, this.#lens === null)}`;
      for (const [key, label, n] of lensCounts) html += chip("lens", key, label, n, this.#lens === key);
      html += `</div>`;
    }
    return html + `</div>`;
  }

  #countHtml(shown) {
    const filtered = this.#lang || this.#lens;
    return `<p class="mk-count">${
      filtered ? `${shown.length} of ${this.#cards.length} published surveys` : `${this.#cards.length} published surveys`
    }</p>`;
  }

  #tableHtml(shown) {
    if (shown.length === 0) return `<p class="mk-empty">No published survey matches that combination.</p>`;

    const base = this.apiBase();
    const rows = shown
      .map((c) => {
        const langs = languagesOf(c);
        const langHtml = langs.length
          ? langs.map((l, i) => `<span class="mk-lang${i === 0 ? " primary" : ""}">${esc(langLabel(l))}</span>`).join("")
          : `<span class="mk-lang">—</span>`;
        const href = reportUrl(c, base);
        return `<tr>
            <td><span class="mk-repo">${esc(c.display || c.name)}</span><br><span class="mk-owner">${esc(c.owner)}/${esc(c.name)}</span></td>
            <td>${langHtml}</td>
            <td class="mk-cai">${esc((c.headlineScore ?? 0).toFixed(1))}</td>
            <td>${esc(c.band ?? "")}</td>
            <td>${href ? `<a href="${esc(href)}" target="_blank" rel="noopener">Read the survey →</a>` : ""}</td>
          </tr>`;
      })
      .join("");

    return `<table class="mk-table">
        <thead><tr><th>Repository</th><th>Languages</th><th>CAI</th><th>Band</th><th></th></tr></thead>
        <tbody>${rows}</tbody>
      </table>`;
  }

  #wire(root) {
    for (const button of root.querySelectorAll(".mk-chip")) {
      button.addEventListener("click", () => {
        const { kind, value } = button.dataset;
        const next = value === "" ? null : value;
        if (kind === "lang") this.#lang = this.#lang === next ? null : next;
        else this.#lens = this.#lens === next ? null : next;
        this.#paint(root);
      });
    }
  }
}

if (!customElements.get("cai-report-index")) customElements.define("cai-report-index", CaiReportIndex);
