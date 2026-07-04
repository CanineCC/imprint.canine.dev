// <cai-findings api-base="…" owner="…" name="…" count="1" kicker="…" heading="…"
//               lede="…" footnote="…" brand="watchdog|assay|cai">
//
// The findings a scanner can't produce — the deterministic architecture / domain-model /
// event findings located to file:line, from real PUBLISHED reports (the insight gallery's
// "Findings a scanner can't produce" widget). Each repo shows its curated hero findings
// with the honest "showing N of M" and a link to the full report.
//
// LIVE by default: when `api-base` is set it fetches {api}/api/public/findings and renders
// the curated repo(s) — the one named by `owner`+`name` if given, else the top `count`
// (the endpoint already ranks by finding weight). Without `api-base`, or on failure, it
// renders a small labelled SAMPLE so the section is never empty in a static preview.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchJson } from "./live.js";

const DEFAULT_COUNT = 1;

// The labelled fallback — clearly illustrative (acme/checkout-service, the sample repo the
// score card also uses), shown only when no live source resolves. Never a fake-live read.
const SAMPLE = [
  {
    repo: "acme/checkout-service",
    owner: "acme",
    name: "checkout-service",
    reportUrl: "",
    shown: 3,
    total: 11,
    more: 8,
    findings: [
      { lensLabel: "Architecture", dim: "D07", title: "Bounded context leak: Orders reaches into Billing's aggregate", file: "src/Orders/OrderService.cs", line: 142 },
      { lensLabel: "Domain Modelling", dim: "D22", title: "Anemic aggregate — invariants enforced in the service, not the entity", file: "src/Billing/Invoice.cs", line: 31 },
      { lensLabel: "Event Sourcing", dim: "D31", title: "Event carries a mutable reference type; replay is not deterministic", file: "src/Orders/Events/OrderPlaced.cs", line: 18 },
    ],
  },
];

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-findings { max-width: 62rem; margin: 0 auto; display: grid; gap: 1.1rem; }
.mk-find-repo { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem 1.15rem; }
.mk-find-head { display: flex; align-items: baseline; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 0.75rem; }
.mk-find-repo-name strong { color: var(--heading); font-size: var(--fs-md); }
.mk-find-repo-name span { color: var(--muted); font-size: var(--fs-xs); }
.mk-find-count { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-find-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.7rem; }
.mk-find-item { display: grid; grid-template-columns: 92px 1fr; gap: 0.75rem; align-items: start; }
.mk-find-lens { font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase; color: var(--accent-ink); padding-top: 2px; }
.mk-find-body { min-width: 0; }
.mk-find-title { margin: 0; color: var(--ink); font-size: var(--fs-sm); line-height: 1.45; }
.mk-find-loc { margin: 0.2rem 0 0; font-family: var(--font-mono); font-size: var(--fs-xs); color: var(--muted); overflow: hidden; text-overflow: ellipsis; }
.mk-find-more { margin: 0.75rem 0 0; font-size: var(--fs-xs); }
.mk-find-more a { color: var(--accent-ink); }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;

customElements.define(
  "cai-findings",
  class extends CaiIsland {
    #count() {
      const raw = this.getAttribute("count");
      const n = Number(raw);
      return raw != null && raw !== "" && Number.isFinite(n) && n > 0 ? Math.floor(n) : DEFAULT_COUNT;
    }

    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const owner = this.getAttribute("owner") || "";
      const name = this.getAttribute("name") || "";
      const data = await fetchJson(api, "/api/public/findings", null);
      const items = (data && Array.isArray(data.items)) ? data.items : [];
      if (items.length === 0) return;

      let chosen;
      if (owner && name) {
        const exact = items.find((it) => it.owner === owner && it.name === name);
        chosen = exact ? [exact] : items.slice(0, this.#count());
      } else {
        chosen = items.slice(0, this.#count());
      }
      if (chosen.length === 0) return;
      this._live = chosen;
      this.render(this.shadowRoot);
    }

    render(root) {
      const repos = this._live || SAMPLE;

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-findings">`;
      for (const it of repos) {
        const findings = (it.findings || []).filter((f) => f && f.title);
        if (findings.length === 0) continue;
        const shown = it.shown != null ? it.shown : findings.length;
        const total = it.total != null ? it.total : findings.length;
        const owner = it.owner || "";
        const name = it.name || it.repo || "";

        html += `<div class="mk-find-repo">`;
        html += `<div class="mk-find-head">`;
        html += `<span class="mk-find-repo-name"><strong>${escapeHtml(name)}</strong>`;
        if (owner) html += `<span> by ${escapeHtml(owner)}</span>`;
        html += `</span>`;
        html += `<span class="mk-find-count">showing ${shown} of ${total}</span>`;
        html += `</div>`;

        html += `<ul class="mk-find-list">`;
        for (const f of findings) {
          const lens = f.lensLabel || f.lens || "Architecture";
          html += `<li class="mk-find-item">`;
          html += `<span class="mk-find-lens">${escapeHtml(lens)}</span>`;
          html += `<div class="mk-find-body">`;
          html += `<p class="mk-find-title">${escapeHtml(f.title || "")}</p>`;
          if (f.file) {
            html += `<p class="mk-find-loc">${escapeHtml(f.file)}${f.line ? ":" + escapeHtml(String(f.line)) : ""}</p>`;
          }
          html += `</div></li>`;
        }
        html += `</ul>`;

        const more = it.more != null ? it.more : Math.max(0, total - shown);
        if (more > 0 && it.reportUrl) {
          html += `<p class="mk-find-more"><a href="${escapeHtml(it.reportUrl)}" target="_blank" rel="noopener">+ ${more} more in the full report →</a></p>`;
        } else if (more > 0) {
          html += `<p class="mk-find-more">+ ${more} more in the full report</p>`;
        }
        html += `</div>`;
      }
      html += `</div>`;

      const footnote = this.getAttribute("footnote");
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;
    }
  }
);
