// <cai-evidence-flow nodes='[{"title":"…","body":"…","tone":"default|on|muted"},…]'
//                    loop-label="…" kicker="…" heading="…" lede="…"
//                    footnote="…" brand="watchdog|assay|cai">
//
// Port of the Flow renderer (packages/ui/src/blocks.tsx): a horizontal
// node → node strip with arrow connectors (the measure→fix→prove loop / the
// produce→hold→consume binding diagram). An optional loop-label draws a dashed
// return path that turns the strip into a loop. Stacks on mobile.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";

const ARROW = `<svg viewBox="0 0 24 24" focusable="false"><path d="M3 12h16m0 0-5.5-5.5M19 12l-5.5 5.5" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"></path></svg>`;

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-flow { max-width: 62rem; margin: 0 auto; }
.mk-flow-nodes { list-style: none; display: flex; align-items: stretch; gap: 10px; margin: 0; padding: 0; }
.mk-flow-node { flex: 1 1 0; border: 1px solid var(--hairline); border-radius: var(--r-lg); padding: 0.9rem 1rem; background: var(--surface); display: flex; flex-direction: column; gap: 0.25rem; min-width: 0; }
.mk-flow-node strong { color: var(--heading); font-size: var(--fs-md); }
.mk-flow-node span { color: var(--muted); font-size: var(--fs-xs); line-height: 1.45; }
.mk-flow-node.tone-on { border-color: var(--accent); box-shadow: inset 0 0 0 1px var(--accent); }
.mk-flow-node.tone-muted { border-style: dashed; }
.mk-flow-arrow { flex: 0 0 26px; display: flex; align-items: center; justify-content: center; color: var(--muted); }
.mk-flow-arrow svg { width: 22px; height: 22px; }
.mk-flow-return { position: relative; height: 34px; margin: 0 11%; border: 1px dashed var(--border-strong); border-top: 0; border-radius: 0 0 14px 14px; }
.mk-flow-return::before { content: ""; position: absolute; top: -8px; left: -5.5px; border: 5px solid transparent; border-bottom-color: var(--muted); }
.mk-flow-return-label { position: absolute; left: 50%; top: 100%; transform: translate(-50%, -50%); background: var(--bg); padding: 0 10px; font-size: var(--fs-xs); color: var(--muted); white-space: nowrap; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
@media (max-width: 760px) {
  .mk-flow-nodes { flex-direction: column; }
  .mk-flow-arrow { flex-basis: auto; height: 24px; }
  .mk-flow-arrow svg { transform: rotate(90deg); }
  .mk-flow-return { height: auto; border: 0; margin: 0; }
  .mk-flow-return::before { display: none; }
  .mk-flow-return-label { position: static; transform: none; display: block; text-align: center; white-space: normal; padding-top: 0.6rem; }
}
`;

customElements.define(
  "cai-evidence-flow",
  class extends CaiIsland {
    render(root) {
      const nodes = (this.json("nodes", []) || []).filter((n) => n && n.title);
      const loopLabel = this.getAttribute("loop-label");
      const footnote = this.getAttribute("footnote");

      let items = "";
      nodes.forEach((n, i) => {
        if (i > 0) items += `<li class="mk-flow-arrow" aria-hidden="true">${ARROW}</li>`;
        const tone = n.tone && n.tone !== "default" ? ` tone-${n.tone}` : "";
        items += `<li class="mk-flow-node${tone}"><strong>${escapeHtml(n.title)}</strong>`;
        if (n.body) items += `<span>${renderInline(n.body)}</span>`;
        items += `</li>`;
      });

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      html += `<div class="mk-flow${loopLabel ? " has-loop" : ""}">`;
      html += `<ol class="mk-flow-nodes">${items}</ol>`;
      if (loopLabel) {
        html +=
          `<div class="mk-flow-return"><span class="mk-flow-return-label">${escapeHtml(loopLabel)}</span></div>`;
      }
      html += `</div>`;
      if (footnote) html += `<p class="mk-grid-foot">${renderInline(footnote)}</p>`;
      root.innerHTML = html;
    }
  }
);
