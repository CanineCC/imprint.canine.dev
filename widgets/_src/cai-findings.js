// <cai-findings api-base="…" kicker="…" heading="…"
//               lede="…" footnote="…" brand="watchdog|assay|cai">
//
// The findings a scanner can't produce — a CAROUSEL of the deterministic architecture /
// domain-model / event findings located to file:line, from real PUBLISHED reports (the
// app's public findings wheel). Each slide is one repo: its curated top findings (lens chip
// + title + file:line), the honest "showing X of Y", and a link to the full report.
//
// Port of wwwroot/wd-findings-wheel.js: fetch {api}/api/public/findings → the DDD-moat
// weighted list ({ items:[{repo,owner,name,reportUrl,sourceUrl,shown,total,more,findings[]}]
// }, strongest repo first). Render a swipeable wheel (prev/next cycling with wrap-around);
// the reportUrl is resolved ABSOLUTE against the api-base. Strongest repo shows initially.
// Nav is hidden when fewer than two items. Empty list ⇒ nothing shown in live mode; without
// api-base it renders a small labelled SAMPLE so a static preview is never empty.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchFindings } from "./live.js";

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
.mk-findings { max-width: 62rem; margin: 0 auto; }
.mk-find-bar { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; margin-bottom: 0.6rem; }
.mk-find-nav { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
.mk-find-navcount { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-find-btn { appearance: none; cursor: pointer; border: 1px solid var(--border-strong); background: var(--surface); color: var(--ink); border-radius: var(--r-full); width: 30px; height: 30px; font-size: var(--fs-md); line-height: 1; display: inline-flex; align-items: center; justify-content: center; }
.mk-find-btn:hover { border-color: var(--accent-ink); color: var(--accent-ink); }
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

// Resolve a possibly-origin-less /api/oss/... reportUrl to an ABSOLUTE link against the
// api-base (same-origin in the app, but cross-origin on the static host ⇒ it must carry the
// base or it 404s). Already-absolute links pass through.
function absoluteReport(url, api) {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;
  return (api || "").replace(/\/$/, "") + url;
}

customElements.define(
  "cai-findings",
  class extends CaiIsland {
    #items = [];
    #idx = 0;

    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const raw = await fetchFindings(api);
      // Resolve each repo's report link to an absolute URL; keep the server's weighted order
      // (strongest repo first — the first slide).
      const items = raw
        .filter((it) => it && Array.isArray(it.findings) && it.findings.length > 0)
        .map((it) => ({ ...it, reportUrl: absoluteReport(it.reportUrl, api) }));
      if (items.length === 0) return; // nothing published with such findings → stay hidden
      this.#items = items;
      this.#idx = 0;
      this._live = true;
      this.render(this.shadowRoot);
    }

    // prev/next cycling with wrap-around, exactly like wd-findings-wheel.js go(d).
    #go(delta) {
      if (this.#items.length < 2) return;
      this.#idx = (this.#idx + delta + this.#items.length) % this.#items.length;
      this.render(this.shadowRoot);
    }

    #repoHtml(it) {
      const findings = (it.findings || []).filter((f) => f && f.title);
      if (findings.length === 0) return "";
      const shown = it.shown != null ? it.shown : findings.length;
      const total = it.total != null ? it.total : findings.length;
      const owner = it.owner || "";
      const name = it.name || it.repo || "";

      let html = `<div class="mk-find-repo">`;
      html += `<div class="mk-find-head">`;
      html += `<span class="mk-find-repo-name"><strong>${escapeHtml(name)}</strong>`;
      if (owner) html += `<span> by ${escapeHtml(owner)}</span>`;
      html += `</span>`;
      html += `<span class="mk-find-count">showing ${escapeHtml(String(shown))} of ${escapeHtml(String(total))}</span>`;
      html += `</div>`;

      html += `<ul class="mk-find-list">`;
      for (const f of findings) {
        const lens = f.lensLabel || f.lens || "Architecture";
        html += `<li class="mk-find-item">`;
        html += `<span class="mk-find-lens">${escapeHtml(lens)}</span>`;
        html += `<div class="mk-find-body">`;
        html += `<p class="mk-find-title">${escapeHtml(f.title || "")}</p>`;
        if (f.file) {
          html += `<p class="mk-find-loc">${escapeHtml(f.file)}${f.line != null ? ":" + escapeHtml(String(f.line)) : ""}</p>`;
        }
        html += `</div></li>`;
      }
      html += `</ul>`;

      const more = it.more != null ? it.more : Math.max(0, total - shown);
      if (more > 0 && it.reportUrl) {
        html += `<p class="mk-find-more"><a href="${escapeHtml(it.reportUrl)}" target="_blank" rel="noopener">+ ${escapeHtml(String(more))} more in the full report →</a></p>`;
      } else if (more > 0) {
        html += `<p class="mk-find-more">+ ${escapeHtml(String(more))} more in the full report</p>`;
      } else if (it.reportUrl) {
        html += `<p class="mk-find-more"><a href="${escapeHtml(it.reportUrl)}" target="_blank" rel="noopener">Read the full report →</a></p>`;
      }
      html += `</div>`;
      return html;
    }

    render(root) {
      const items = this._live ? this.#items : SAMPLE;
      const idx = this._live ? this.#idx : 0;
      const single = items.length < 2;
      const it = items[idx];

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-findings">`;
      // The prev/next wheel bar — shown only in live mode with two or more repos.
      if (this._live && !single) {
        html += `<div class="mk-find-bar">`;
        html += `<span class="mk-find-navcount">${idx + 1} / ${items.length} published repos</span>`;
        html += `<span class="mk-find-nav">`;
        html += `<button type="button" class="mk-find-btn" data-find-prev aria-label="Previous repo">‹</button>`;
        html += `<button type="button" class="mk-find-btn" data-find-next aria-label="Next repo">›</button>`;
        html += `</span>`;
        html += `</div>`;
      }
      html += this.#repoHtml(it);
      html += `</div>`;

      const footnote = this.getAttribute("footnote");
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;

      // Re-wire the nav each render (the old listeners die with the innerHTML swap).
      const prev = root.querySelector("[data-find-prev]");
      const next = root.querySelector("[data-find-next]");
      if (prev) prev.addEventListener("click", () => { this.#go(-1); });
      if (next) next.addEventListener("click", () => { this.#go(1); });
    }
  }
);
