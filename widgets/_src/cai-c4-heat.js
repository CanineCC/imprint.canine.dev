// <cai-c4-heat api-base="…" kicker="…" heading="…" lede="…"
//              caption="…" brand="watchdog|assay|cai">
//
// The C4 architecture heat-map — the "no line-scanner produces this" system view that
// watchdog.canine.dev shows in the insight gallery's C4 widget: bounded contexts and how
// they talk, coloured by health. A LIVE-ONLY island (there is no meaningful static twin
// of an architecture map, so it renders nothing when no live source resolves).
//
// Data (mirrors the app's C4 wheel): fetch {api}/api/public/showcase and read the
// SERVER-CURATED c4 slice { owner, name, runId } — the public repo whose architecture map
// is the richest / most illustrative (the server does the picking; NO hardcoded repo). Then
// load the PUBLIC C4-heat SVG from Track K's endpoint
// {api}/api/public/oss/{owner}/{name}/c4.svg and render it inline. Empty ⇒ nothing shown.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchShowcase, fetchText } from "./live.js";

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

      try {
        // The SERVER-CURATED c4 candidate — { owner, name, runId } — the richest public
        // architecture map. No hardcoded repo, no client picking; the server chose it.
        const showcase = await fetchShowcase(api);
        const c4 = showcase && showcase.c4;
        const owner = c4 && c4.owner;
        const name = c4 && c4.name;
        if (!owner || !name) return;

        const svg = await fetchText(
          api,
          "/api/public/oss/" + encodeURIComponent(owner) + "/" + encodeURIComponent(name) + "/c4.svg"
        );
        if (!svg) return;

        this._live = { owner, name, svg };
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
