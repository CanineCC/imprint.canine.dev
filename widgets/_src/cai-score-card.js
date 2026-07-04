// <cai-score-card api-base="…" owner="…" name="…" card='{…scoreCard…}'
//                 caption="…" seal-text="…" href="…" brand="watchdog|assay|cai">
//
// The Codebase Assurance Index score card: name + band chip, band-inked score, the
// fixed worst→best five-band ladder with the "you are here" diamond, the value-coloured
// trend sparkline, the first→best arc, the lens bars and the detail rows. Port of
// packages/ui/src/CaiScoreCard.tsx.
//
// LIVE by default (the watchdog.canine.dev survey-card pattern): when `api-base` is set
// it fetches {api}/api/oss and renders a REAL published card — the exact repo named by
// `owner`+`name`, else the curated hero (the highest-quality published repo: peak score,
// LoC tie-break). Without `api-base`, or if the fetch fails, it renders the labelled
// SAMPLE from the `card` attribute (never a fake-live read). Scalar attributes override.

import { CaiIsland, TOKENS_CSS, BASE_CSS, escapeHtml } from "./tokens.js";
import { scoreCardBodyHtml, parseCard, SCORECARD_CSS } from "./scorecard.js";
import {
  cardFromGallery,
  reportUrl,
  pickCard,
  publicRanked,
  reportOk,
  fetchJson,
} from "./live.js";

// The built-in sample — the same illustrative artifact the CMS hero ships
// (blocks.tsx SAMPLE_CARD): acme/checkout-service, an Adequate verdict, a
// reproducible fingerprint. Clearly a sample; shown only as the no-live fallback.
const SAMPLE_CARD = {
  name: "checkout-service",
  owner: "acme",
  score: 62,
  series: [45, 48, 47, 52, 55, 58, 60, 62],
  arcFirst: 45,
  arcBest: 62,
  lenses: [
    { label: "Code health", value: 68 },
    { label: "Architecture", value: 55 },
    { label: "Maturity", value: 63 },
    { label: "Readiness", value: 52 },
    { label: "Security", value: 71 },
  ],
  rows: [
    { label: "Measured", value: "1 July 2026 · 4.2M lines" },
    { label: "Reproducible fingerprint", value: "a3f9…e021", mono: true },
    { label: "Shared with", value: "3 parties" },
  ],
};

const CSS = TOKENS_CSS + BASE_CSS + SCORECARD_CSS + `
:host { display: block; }
.cai-card-cap { margin-top: 0.85rem; }
`;

customElements.define(
  "cai-score-card",
  class extends CaiIsland {
    // Fetch a real published card from the corpus, map it to the card model, cache it,
    // and re-render. Selected by owner/name if both set, else the curated hero. On any
    // failure this leaves _live unset and the sample stays (render() already drew it).
    async liveLoad() {
      const api = this.apiBase();
      if (!api) return;
      const owner = this.getAttribute("owner") || "";
      const name = this.getAttribute("name") || "";
      const cards = await fetchJson(api, "/api/oss", null);
      if (!Array.isArray(cards) || cards.length === 0) return;

      // An author-named repo is honoured as-is; otherwise the curated hero is the
      // highest-quality public repo whose report ACTUALLY resolves — the corpus lists
      // opted-in repos before their report bundle has necessarily been copied in, and a
      // hero that links a 404 is worse than the next-best repo that links a live report.
      let chosen = null;
      let href = "";
      if (owner && name) {
        chosen = pickCard(cards, { owner, name });
        if (chosen) {
          const u = reportUrl(chosen, api);
          if (u && (await reportOk(u))) href = u;
        }
      } else {
        for (const c of publicRanked(cards)) {
          const u = reportUrl(c, api);
          if (u && (await reportOk(u))) {
            chosen = c;
            href = u;
            break;
          }
        }
      }
      if (!chosen) return;

      const mapped = cardFromGallery(chosen);
      if (!mapped) return;
      if (href) mapped.href = href;
      this._live = mapped;
      this.render(this.shadowRoot);
    }

    render(root) {
      // Prefer the LIVE card once it has arrived; else the seeded sample. Then apply
      // scalar overrides (an editor dial always wins over the data source's field).
      let data;
      if (this._live) {
        data = { ...this._live };
      } else {
        const parsed = parseCard(this.json("card", null));
        data = parsed ? { ...parsed } : { ...SAMPLE_CARD };
      }

      const name = this.getAttribute("name");
      const owner = this.getAttribute("owner");
      const score = this.getAttribute("score");
      const seal = this.getAttribute("seal-text");
      const href = this.getAttribute("href");
      // owner/name are ALSO the live selectors, so only let them override the label
      // text when we are NOT rendering a live card (a live card already IS that repo).
      if (!this._live) {
        if (name != null && name !== "") data.name = name;
        if (owner != null && owner !== "") data.owner = owner;
      }
      if (score != null && score !== "" && Number.isFinite(Number(score)))
        data.score = Number(score);
      if (seal != null && seal !== "") data.sealText = seal;
      if (href != null && href !== "") data.href = href;

      // The `caption` the CMS seeds is the labelled-SAMPLE caption ("A sample evidence
      // artifact…"). It is honest ONLY for the offline sample — a live card is a REAL
      // repo, so suppress it and let the card's own measured/repo rows carry provenance.
      const caption = this._live ? null : this.getAttribute("caption");

      const tag = data.href ? "a" : "div";
      const hrefAttr = data.href ? ` href="${escapeHtml(data.href)}"` : "";
      let html = `<style>${CSS}</style>`;
      html += `<${tag} class="cai-card"${hrefAttr}>${scoreCardBodyHtml(data)}</${tag}>`;
      if (caption) html += `<p class="cai-card-cap">${escapeHtml(caption)}</p>`;
      root.innerHTML = html;
    }
  }
);
