// <cai-score-card card='{…scoreCard…}' name="…" owner="…" caption="…"
//                  seal-text="…" href="…" brand="watchdog|assay|cai">
//
// Port of packages/ui/src/CaiScoreCard.tsx: name + band chip, band-inked score,
// the fixed worst→best five-band ladder with the "you are here" diamond, the
// value-coloured trend sparkline, the first→best arc, the lens bars and the
// detail rows. FAITHFUL DEFAULT — renders the SAMPLE/labelled data the CMS ships
// (never a live registry read). The card comes from the `card` JSON attribute;
// individual scalar attributes override fields for editors who prefer dials.

import { CaiIsland, TOKENS_CSS, BASE_CSS, escapeHtml } from "./tokens.js";
import {
  scoreCardBodyHtml,
  parseCard,
  SCORECARD_CSS,
} from "./scorecard.js";

// The built-in sample — the same illustrative artifact the CMS hero ships
// (blocks.tsx SAMPLE_CARD): acme/checkout-service, an Adequate verdict, a
// reproducible fingerprint. Clearly a sample.
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
    render(root) {
      // Full card object from JSON, or the sample; then apply scalar overrides.
      const parsed = parseCard(this.json("card", null));
      const data = parsed ? { ...parsed } : { ...SAMPLE_CARD };

      const name = this.getAttribute("name");
      const owner = this.getAttribute("owner");
      const score = this.getAttribute("score");
      const seal = this.getAttribute("seal-text");
      const href = this.getAttribute("href");
      if (name != null && name !== "") data.name = name;
      if (owner != null && owner !== "") data.owner = owner;
      if (score != null && score !== "" && Number.isFinite(Number(score)))
        data.score = Number(score);
      if (seal != null && seal !== "") data.sealText = seal;
      if (href != null && href !== "") data.href = href;

      const caption =
        this.getAttribute("caption") ??
        (parsed ? undefined : undefined);

      const tag = data.href ? "a" : "div";
      const hrefAttr = data.href ? ` href="${escapeHtml(data.href)}"` : "";
      let html = `<style>${CSS}</style>`;
      html += `<${tag} class="cai-card"${hrefAttr}>${scoreCardBodyHtml(data)}</${tag}>`;
      if (caption) html += `<p class="cai-card-cap">${escapeHtml(caption)}</p>`;
      root.innerHTML = html;
    }
  }
);
