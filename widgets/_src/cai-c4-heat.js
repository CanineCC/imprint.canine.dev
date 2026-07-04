// <cai-c4-heat api-base="…" owner="…" name="…" kicker="…" heading="…" lede="…"
//              caption="…" brand="watchdog|assay|cai">
//
// The C4 architecture heat-map — the "no line-scanner produces this" system view that
// watchdog.canine.dev shows in the insight gallery's C4 widget: bounded contexts and how
// they talk, coloured by health. A LIVE-ONLY island (there is no meaningful static twin
// of an architecture map, so it renders nothing when no live source resolves).
//
// Data (mirrors the app's C4 wheel): fetch {api}/api/public/c4 for the curated set of
// gallery-opt-in repos whose latest published run passes the C4 gate (DDD + ≥2 bounded
// contexts), biggest codebase first. Each item carries a "owner/name" repo label. Pick
// the repo named by `owner`+`name` if given, else the first INSPECTABLE-PUBLIC item
// (skipping the private/internal canine repos, which also pass the gate), then load the
// PUBLIC C4-heat SVG from Track K's endpoint {api}/api/public/oss/{owner}/{name}/c4.svg
// and render it inline. Empty ⇒ nothing shown.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchJson, fetchText, isPublicWithReport } from "./live.js";

// The public C4 endpoint labels each item with a "owner/name" repo string and a per-run
// id (NOT the corpus best-run id) — Track K's SVG endpoint is addressed by owner/name, so
// we key off the label. Split it into { owner, name }, tolerant of owners with a slash.
function splitRepo(repo) {
  const s = String(repo || "");
  const i = s.indexOf("/");
  if (i <= 0 || i >= s.length - 1) return null;
  return { owner: s.slice(0, i), name: s.slice(i + 1) };
}

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-c4 { max-width: 62rem; margin: 0 auto; }
.mk-c4-frame { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem; overflow: hidden; }
.mk-c4-frame svg { width: 100%; height: auto; display: block; }
.mk-c4-repo { display: flex; align-items: baseline; gap: 0.5rem; margin-bottom: 0.6rem; }
.mk-c4-repo strong { color: var(--heading); font-size: var(--fs-md); }
.mk-c4-repo span { color: var(--muted); font-size: var(--fs-xs); }
.mk-c4-loading { color: var(--muted); font-size: var(--fs-sm); margin: 0; padding: 1.5rem 0; text-align: center; }
.mk-c4-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;

customElements.define(
  "cai-c4-heat",
  class extends CaiIsland {
    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      // We have a live source and are fetching: the reserved frame reads "loading"
      // (rather than a permanent placeholder on hosts that never set an api-base).
      this._pending = true;
      this.render(this.shadowRoot);
      const owner = this.getAttribute("owner") || "";
      const name = this.getAttribute("name") || "";

      try {
        const c4 = await fetchJson(api, "/api/public/c4", null);
        const items = (c4 && Array.isArray(c4.items)) ? c4.items : [];
        if (items.length === 0) return;

        // Each c4 item carries a "owner/name" repo label; split it to address the SVG
        // endpoint. The corpus tells us which repos are inspectable-public (the private/
        // internal canine repos also pass the C4 gate but must never be the flagship map).
        const cards = await fetchJson(api, "/api/oss", null);
        const publicNames = new Set();
        if (Array.isArray(cards)) {
          for (const c of cards) {
            if (isPublicWithReport(c)) publicNames.add(c.owner + "/" + c.name);
          }
        }

        const candidates = items
          .map((it) => splitRepo(it.repo))
          .filter(Boolean);

        // The curated pick: the named repo when given; else the first PUBLIC item (the c4
        // endpoint already orders biggest-codebase first = the densest, most convincing
        // heat-map). A named repo is honoured as-is (an author picked it deliberately).
        let picked = null;
        if (owner && name) {
          picked =
            candidates.find((r) => r.owner === owner && r.name === name) || null;
        } else {
          picked =
            candidates.find((r) => publicNames.has(r.owner + "/" + r.name)) ||
            null;
        }
        if (!picked) return;

        const svg = await fetchText(
          api,
          "/api/public/oss/" + encodeURIComponent(picked.owner) + "/" + encodeURIComponent(picked.name) + "/c4.svg"
        );
        if (!svg) return;

        this._live = { owner: picked.owner, name: picked.name, svg };
      } finally {
        // Fetch settled: drop the loading state. If no live map arrived (empty corpus,
        // failed svg), render() falls through to the section head alone — never a stuck
        // spinner. If it did arrive, render() paints the real heat-map.
        this._pending = false;
        this.render(this.shadowRoot);
      }
    }

    render(root) {
      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (!this._live) {
        // While a live fetch is in flight, reserve the frame with a quiet loading note
        // (never a fake map). With no api-base configured, or after a failed/empty fetch,
        // render only the section head — the map is simply unavailable, not "loading".
        if (this._pending) {
          html += `<figure class="mk-c4"><div class="mk-c4-frame"><p class="mk-c4-loading">Loading the architecture map…</p></div></figure>`;
        }
        root.innerHTML = html;
        return;
      }

      const { owner, name, svg } = this._live;
      const caption = this.getAttribute("caption");
      html += `<figure class="mk-c4">`;
      html += `<div class="mk-c4-frame">`;
      html += `<div class="mk-c4-repo"><strong>${escapeHtml(name)}</strong><span>by ${escapeHtml(owner)}</span></div>`;
      // The SVG is a trusted server-rendered artifact from the kennel public API — the
      // same guid-gated map the app renders. Inserted as-is, exactly like the app's wheel.
      html += svg;
      html += `</div>`;
      if (caption) html += `<figcaption class="mk-c4-cap">${renderInline(caption)}</figcaption>`;
      html += `</figure>`;
      root.innerHTML = html;
    }
  }
);
