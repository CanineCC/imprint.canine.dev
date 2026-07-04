// <cai-c4-heat api-base="…" kicker="…" heading="…" lede="…"
//              caption="…" brand="watchdog|assay|cai">
//
// The C4 architecture heat-map CAROUSEL — the "no line-scanner produces this" system view
// that watchdog.canine.dev shows in the app's C4 wheel: bounded contexts and how they talk,
// coloured by health. A LIVE-ONLY island (there is no meaningful static twin of an
// architecture map, so it renders nothing when no live source resolves).
//
// Port of wwwroot/wd-c4-wheel.js: fetch {api}/api/public/c4 → the LoC-ordered list of
// published C4-eligible repos ({ repo:"owner/name", runId }), richest first. Render a
// swipeable wheel (prev/next cycling with wrap-around); for the CURRENT item, load the
// PUBLIC C4-heat SVG from {api}/api/public/oss/{owner}/{name}/c4.svg and render it inline —
// the same map the app renders. The richest (first) repo shows initially. Nav is hidden when
// fewer than two items. Empty list ⇒ nothing shown (no empty wheel).

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";
import { fetchC4, fetchText } from "./live.js";

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-c4 { max-width: 62rem; margin: 0 auto; }
.mk-c4-bar { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; margin-bottom: 0.6rem; }
.mk-c4-repo { display: flex; align-items: baseline; gap: 0.5rem; min-width: 0; }
.mk-c4-repo strong { color: var(--heading); font-size: var(--fs-md); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mk-c4-repo span { color: var(--muted); font-size: var(--fs-xs); white-space: nowrap; }
.mk-c4-nav { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
.mk-c4-count { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-c4-btn { appearance: none; cursor: pointer; border: 1px solid var(--border-strong); background: var(--surface); color: var(--ink); border-radius: var(--r-full); width: 30px; height: 30px; font-size: var(--fs-md); line-height: 1; display: inline-flex; align-items: center; justify-content: center; }
.mk-c4-btn:hover { border-color: var(--accent-ink); color: var(--accent-ink); }
.mk-c4-frame { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem; overflow: hidden; }
.mk-c4-frame svg { width: 100%; height: auto; display: block; }
.mk-c4-loading { color: var(--muted); font-size: var(--fs-sm); margin: 0; padding: 1.5rem 0; text-align: center; }
.mk-c4-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;

// "owner/name" → { owner, name }. The /api/public/c4 `repo` field is the app's display
// label (owner/name, or a repo's DisplayName). Split on the FIRST slash; if there is no
// slash we cannot address the public SVG endpoint, so drop the item.
function splitRepo(label) {
  const s = String(label || "");
  const i = s.indexOf("/");
  if (i <= 0 || i >= s.length - 1) return null;
  return { owner: s.slice(0, i), name: s.slice(i + 1) };
}

customElements.define(
  "cai-c4-heat",
  class extends CaiIsland {
    #items = [];
    #idx = 0;
    #svgCache = new Map(); // idx → svg string ("" = tried, unavailable)

    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      // We have a live source and are fetching: the reserved frame reads "loading".
      this._pending = true;
      this.render(this.shadowRoot);

      const raw = await fetchC4(api);
      // Keep only items we can address as owner/name for the public SVG endpoint, in the
      // server's LoC order (richest first) — the first item shows initially.
      this.#items = raw
        .map((it) => {
          // Prefer the endpoint's explicit owner/name; fall back to splitting the label. A
          // display-name label (e.g. "Continuum") has no slash, so without the explicit
          // fields the (often richest) repo would be dropped from the wheel.
          const owner = it && it.owner, name = it && it.name;
          const parts = owner && name ? { owner, name } : splitRepo(it && it.repo);
          return parts ? { repo: it && it.repo, owner: parts.owner, name: parts.name } : null;
        })
        .filter(Boolean);
      this._pending = false;

      if (this.#items.length === 0) {
        // Empty corpus ⇒ nothing shown; render() falls through to the section head alone.
        this.render(this.shadowRoot);
        return;
      }
      this.#idx = 0;
      await this.#load(0);
      this.render(this.shadowRoot);
    }

    // Load (and cache) the current item's public C4 SVG. Mirrors the wheel's per-slide
    // fetch of the server-rendered map — no ?view, so the Container level (top level).
    async #load(idx) {
      if (this.#svgCache.has(idx)) return;
      const it = this.#items[idx];
      const svg = await fetchText(
        this.apiBase(),
        "/api/public/oss/" +
          encodeURIComponent(it.owner) +
          "/" +
          encodeURIComponent(it.name) +
          "/c4.svg"
      );
      this.#svgCache.set(idx, svg || "");
    }

    // prev/next cycling with wrap-around, exactly like wd-c4-wheel.js go(delta): no-op with
    // fewer than two items; loads the target slide's SVG (once) then re-renders.
    async #go(delta) {
      if (this.#items.length < 2) return;
      this.#idx = (this.#idx + delta + this.#items.length) % this.#items.length;
      await this.#load(this.#idx);
      this.render(this.shadowRoot);
    }

    render(root) {
      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);

      if (this.#items.length === 0) {
        // While a live fetch is in flight, reserve the frame with a quiet loading note
        // (never a fake map). With no api-base configured, or after a failed/empty fetch,
        // render only the section head — the map is simply unavailable, not "loading".
        if (this._pending) {
          html += `<figure class="mk-c4"><div class="mk-c4-frame"><p class="mk-c4-loading">Loading the architecture maps…</p></div></figure>`;
        }
        root.innerHTML = html;
        return;
      }

      const it = this.#items[this.#idx];
      const svg = this.#svgCache.get(this.#idx);
      const caption = this.getAttribute("caption");
      const single = this.#items.length < 2;

      html += `<figure class="mk-c4">`;
      html += `<div class="mk-c4-bar">`;
      // Heading = the app's display label: an "owner/name" label shows "name · by owner"; a
      // DisplayName label (no slash, e.g. "Continuum") shows alone.
      html += (it.repo && it.repo.indexOf("/") >= 0)
        ? `<span class="mk-c4-repo"><strong>${escapeHtml(it.name)}</strong><span>by ${escapeHtml(it.owner)}</span></span>`
        : `<span class="mk-c4-repo"><strong>${escapeHtml(it.repo || it.name)}</strong></span>`;
      // Nav is hidden when there is only one eligible repo (no empty wheel to cycle).
      if (!single) {
        html += `<span class="mk-c4-nav">`;
        html += `<button type="button" class="mk-c4-btn" data-c4-prev aria-label="Previous architecture map">‹</button>`;
        html += `<span class="mk-c4-count">${this.#idx + 1} / ${this.#items.length}</span>`;
        html += `<button type="button" class="mk-c4-btn" data-c4-next aria-label="Next architecture map">›</button>`;
        html += `</span>`;
      }
      html += `</div>`;
      html += `<div class="mk-c4-frame">`;
      if (svg === undefined) {
        html += `<p class="mk-c4-loading">Loading the architecture map…</p>`;
      } else if (svg === "") {
        html += `<p class="mk-c4-loading">Couldn't load this architecture map — try the next one.</p>`;
      } else {
        // The SVG is a trusted server-rendered artifact from the kennel public API — the
        // same map the app renders. Inserted as-is, exactly like the app's wheel.
        html += svg;
      }
      html += `</div>`;
      if (caption) html += `<figcaption class="mk-c4-cap">${renderInline(caption)}</figcaption>`;
      html += `</figure>`;
      root.innerHTML = html;

      // Wire the (freshly re-rendered) nav buttons — listeners on the old nodes are
      // discarded with the innerHTML swap, so we re-attach each render (no double-wiring).
      const prev = root.querySelector("[data-c4-prev]");
      const next = root.querySelector("[data-c4-next]");
      if (prev) prev.addEventListener("click", () => { this.#go(-1); });
      if (next) next.addEventListener("click", () => { this.#go(1); });
    }
  }
);
