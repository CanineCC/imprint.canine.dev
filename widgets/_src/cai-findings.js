// <cai-findings api-base="…" kicker="…" heading="…"
//               lede="…" footnote="…" brand="watchdog|assay|cai">
//
// The findings a scanner can't produce — the deterministic architecture / domain-model /
// event findings located to file:line, from real PUBLISHED reports (the insight gallery's
// "Findings a scanner can't produce" widget). The repo shows its curated hero findings
// with the honest "showing N of M" and a link to the full report.
//
// LIVE by default: when `api-base` is set it fetches {api}/api/public/showcase and renders
// the SERVER-CURATED findings repo (showcase.findings) — the public repo whose curated
// findings the server chose as most illustrative, located to file:line, with the honest
// "showing N of M" and an absolute link to the full report (the server does the picking).
// Without `api-base`, or on failure, it renders a small labelled SAMPLE so the section is
// never empty in a static preview. No client-side pick.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchShowcase } from "./live.js";

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
    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      // The SERVER-CURATED findings repo — a single { owner, name, reportUrl, shown,
      // total, findings[] } object. No client picking; the server chose the most
      // illustrative one and curated its findings.
      const showcase = await fetchShowcase(api);
      const it = showcase && showcase.findings;
      if (!it || !Array.isArray(it.findings) || it.findings.length === 0) return;

      // The slice's reportUrl may be an origin-less /api/oss/... path (same-origin in the
      // app); on this cross-origin static host it must carry the api-base or it 404s.
      // Resolve to an ABSOLUTE link so the "+N more in the full report" href opens.
      const base = api.replace(/\/$/, "");
      const resolved = {
        ...it,
        reportUrl: it.reportUrl
          ? (/^https?:\/\//i.test(it.reportUrl) ? it.reportUrl : base + it.reportUrl)
          : "",
      };
      this._live = [resolved];
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
