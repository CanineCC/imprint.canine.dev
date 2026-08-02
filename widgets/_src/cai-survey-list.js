// <cai-survey-list projects='[{"owner":"jasperfx","name":"marten","score":72.4,"loc":173789,
//                              "busFactor":1,"day":"21 July 2026","href":"/surveys/github/jasperfx/marten/"}]'
//                  kicker="…" heading="…" lede="…" empty-text="…">
//
// A measured corpus you can actually work through: the band distribution as a bar you can
// click, a search box, a sort, and the projects as cards.
//
// The distribution IS the filter. A separate chart and a separate row of filter chips say the
// same thing twice and then disagree the moment one of them is wrong; here the segment you
// click is the population you get, so the picture cannot drift from the list.
//
// DATA ONLY, deliberately: no api-base, no liveLoad, no fetch. Every row arrives as a prop from
// the page that renders it. That is what makes it structurally impossible for a language page
// to list another language's projects — the failure that put one repository's figures under a
// thousand repositories' names came from an island that could go and get its own data.
//
// The full list ships in the attribute, so a reader whose browser never runs the script — and a
// crawler that never runs one either — still finds every project in the served HTML.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml,
} from "./tokens.js";
import { SCORECARD_CSS } from "./scorecard.js";
import { CAI_BANDS, bandFor } from "./cai.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + SCORECARD_CSS + `
.mk-list { max-width: 60rem; margin: 0 auto; }

/* the distribution, which is also the band filter */
.mk-dist { display: flex; height: 34px; border-radius: var(--r-sm); overflow: hidden; gap: 2px; }
.mk-dist button { flex: none; border: 0; padding: 0; cursor: pointer; position: relative;
  font: inherit; color: var(--on-accent); font-size: var(--fs-2xs); font-weight: 700;
  transition: opacity 120ms ease; }
.mk-dist button[aria-pressed="false"] { opacity: 0.28; }
.mk-dist button:focus-visible { outline: 2px solid var(--accent-strong); outline-offset: -3px; }
.mk-dist-key { display: flex; flex-wrap: wrap; gap: 0.35rem 1.1rem; margin-top: 0.6rem;
  font-size: var(--fs-xs); color: var(--muted); }
.mk-dist-key span { display: inline-flex; align-items: center; gap: 0.4rem; }
.mk-dist-key i { width: 9px; height: 9px; border-radius: 2px; flex: none; }

/* controls */
.mk-ctl { display: flex; flex-wrap: wrap; gap: 0.6rem; margin: 1.3rem 0 0.5rem; align-items: center; }
.mk-ctl input, .mk-ctl select { font: inherit; font-size: var(--fs-sm); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm);
  padding: 7px 11px; }
.mk-ctl input { flex: 1 1 15rem; min-width: 0; }
.mk-ctl input:focus-visible, .mk-ctl select:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-ctl-clear { background: none; border: 0; color: var(--accent-ink); font: inherit;
  font-size: var(--fs-sm); cursor: pointer; padding: 4px 2px; text-decoration: underline; }
.mk-count { font-size: var(--fs-xs); color: var(--muted); margin: 0 0 0.9rem; }

/* the cards */
.mk-cards { display: grid; gap: 10px; grid-template-columns: repeat(auto-fill, minmax(19rem, 1fr)); }
a.mk-card { display: grid; gap: 7px; padding: 14px 16px; text-decoration: none; color: inherit;
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 120ms ease; }
a.mk-card:hover, a.mk-card:focus-visible { border-color: var(--accent); text-decoration: none; }
.mk-card-top { display: flex; align-items: baseline; justify-content: space-between; gap: 0.8rem; }
.mk-card-name { font-weight: 650; font-size: var(--fs-md); overflow-wrap: anywhere; }
.mk-card-owner { color: var(--muted); font-weight: 500; }
.mk-card-score { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-lg); flex: none; }
.mk-card-meta { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }
.mk-empty { padding: 2rem 0; text-align: center; color: var(--muted); font-size: var(--fs-sm); }
@media (prefers-reduced-motion: reduce) { .mk-dist button, a.mk-card { transition: none; } }
`;

// "Most recently measured" is offered only when the rows carry a sortable `at` (ISO 8601).
// The human date is "21 July 2026", and sorting THAT as text orders by its first digit — a
// silently wrong order is worse than an option a reader never sees.
const SORTS = [
  ["score-desc", "Highest score first"],
  ["score-asc", "Lowest score first"],
  ["name", "Name A–Z"],
  ["size-desc", "Largest first"],
];
const RECENT = ["recent", "Most recently measured"];

customElements.define(
  "cai-survey-list",
  class extends CaiIsland {
    render(root) {
      const rows = (this.json("projects", []) || []).filter((p) => p && p.name);
      this._rows = rows;
      this._q = this._q || "";
      this._sort = this._sort || "score-desc";
      // A null band set means "no band filter", which is not the same as "none selected".
      this._bands = this._bands || null;

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (rows.length === 0) {
        root.innerHTML = html;
        return;
      }

      const counts = new Map(CAI_BANDS.map((b) => [b.key, 0]));
      for (const r of rows) {
        const k = bandFor(Number(r.score)).key;
        counts.set(k, (counts.get(k) || 0) + 1);
      }
      const present = CAI_BANDS.filter((b) => counts.get(b.key) > 0);

      html += `<div class="mk-list">`;

      // Worst→best, the same direction as the band scale everywhere else on the site.
      html += `<div class="mk-dist" role="group" aria-label="Filter by band">`;
      for (const b of present) {
        const n = counts.get(b.key);
        const on = !this._bands || this._bands.has(b.key);
        html += `<button type="button" class="fill-${b.key}" data-band="${b.key}"`
          + ` style="flex: ${n} 0 0" aria-pressed="${on}"`
          + ` title="${escapeHtml(`${b.label}: ${n} project${n === 1 ? "" : "s"}`)}">${n}</button>`;
      }
      html += `</div><p class="mk-dist-key">`;
      for (const b of present) {
        html += `<span><i class="fill-${b.key}"></i>${escapeHtml(b.label)} ${counts.get(b.key)}</span>`;
      }
      html += `</p>`;

      html += `<div class="mk-ctl">`;
      html += `<input type="search" class="mk-q" placeholder="Search these projects" aria-label="Search these projects"`
        + ` value="${escapeHtml(this._q)}">`;
      const sorts = rows.some((r) => r.at) ? [...SORTS, RECENT] : SORTS;
      html += `<select class="mk-sort" aria-label="Sort">`;
      for (const [v, label] of sorts) {
        html += `<option value="${v}"${v === this._sort ? " selected" : ""}>${escapeHtml(label)}</option>`;
      }
      html += `</select>`;
      if (this._q || this._bands) {
        html += `<button type="button" class="mk-ctl-clear">Clear</button>`;
      }
      html += `</div>`;

      html += `<p class="mk-count" role="status"></p>`;
      html += `<div class="mk-cards"></div>`;
      html += `</div>`;

      root.innerHTML = html;
      this.wire(root);
      this.paint(root);
    }

    wire(root) {
      const q = root.querySelector(".mk-q");
      const sort = root.querySelector(".mk-sort");
      q?.addEventListener("input", () => { this._q = q.value; this.paint(root); });
      sort?.addEventListener("change", () => { this._sort = sort.value; this.paint(root); });
      root.querySelector(".mk-ctl-clear")?.addEventListener("click", () => {
        this._q = ""; this._bands = null; this.render(root);
      });
      for (const btn of root.querySelectorAll(".mk-dist button")) {
        btn.addEventListener("click", () => {
          const key = btn.getAttribute("data-band");
          // First click on a full bar means "only this one" — the obvious reading of clicking a
          // segment. After that it toggles, so several bands can be held together.
          const set = this._bands ? new Set(this._bands) : new Set();
          if (!this._bands) {
            set.add(key);
          } else if (set.has(key)) {
            set.delete(key);
          } else {
            set.add(key);
          }
          this._bands = set.size === 0 ? null : set;
          this.render(root);
        });
      }
    }

    paint(root) {
      const cards = root.querySelector(".mk-cards");
      const count = root.querySelector(".mk-count");
      if (!cards || !count) { return; }

      const q = this._q.trim().toLowerCase();
      let shown = this._rows.filter((r) => {
        if (this._bands && !this._bands.has(bandFor(Number(r.score)).key)) { return false; }
        if (!q) { return true; }
        return `${r.owner || ""}/${r.name}`.toLowerCase().includes(q);
      });

      const by = {
        "score-desc": (a, b) => Number(b.score) - Number(a.score),
        "score-asc": (a, b) => Number(a.score) - Number(b.score),
        name: (a, b) => String(a.name).localeCompare(String(b.name)),
        "size-desc": (a, b) => Number(b.loc || 0) - Number(a.loc || 0),
        recent: (a, b) => String(b.at || "").localeCompare(String(a.at || "")),
      }[this._sort];
      shown = [...shown].sort(by || (() => 0));

      const total = this._rows.length;
      count.textContent = shown.length === total
        ? `${total} project${total === 1 ? "" : "s"}`
        : `${shown.length} of ${total} projects`;

      if (shown.length === 0) {
        cards.innerHTML = `<p class="mk-empty">${escapeHtml(
          this.getAttribute("empty-text") || "No project here matches that.")}</p>`;
        return;
      }

      let html = "";
      for (const r of shown) {
        const score = Number(r.score);
        const band = bandFor(score);
        const meta = [];
        if (r.loc) { meta.push(`${compact(Number(r.loc))} lines`); }
        if (Number(r.busFactor) === 1) { meta.push("bus factor 1"); }
        if (r.day) { meta.push(`measured ${r.day}`); }

        html += `<a class="mk-card" href="${escapeHtml(r.href || "#")}">`;
        html += `<span class="mk-card-top">`;
        html += `<span class="mk-card-name">`;
        if (r.owner) { html += `<span class="mk-card-owner">${escapeHtml(r.owner)}/</span>`; }
        html += `${escapeHtml(r.name)}</span>`;
        html += `<span class="mk-card-score ink-${band.key}">${score.toFixed(1)}</span>`;
        html += `</span>`;
        html += `<span class="cai-lens-bar"><span class="cai-lens-fill fill-${band.key}"`
          + ` style="width:${Math.max(2, Math.round(score))}%"></span></span>`;
        html += `<span class="mk-card-meta">${escapeHtml([band.label, ...meta].join(" · "))}</span>`;
        html += `</a>`;
      }
      cards.innerHTML = html;
    }
  }
);

function compact(n) {
  if (!Number.isFinite(n) || n <= 0) { return "0"; }
  if (n >= 1_000_000) { return `${(n / 1_000_000).toFixed(1)}m`; }
  if (n >= 1_000) { return `${Math.round(n / 1_000)}k`; }
  return String(n);
}
