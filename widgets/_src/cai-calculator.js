// <cai-calculator api-base="https://cai.canine.dev" kicker="…" heading="…" lede="…" brand="cai">
//
// Paste an evidence bundle; the open scorer folds it into the ten lens scores and the 0–100 CAI,
// in front of the reader. When the bundle also carries a published `headlineScore`, the result is
// checked against it — so this is the calculator AND the falsifiability demonstration: a mismatch
// is proof the published number does not follow from the evidence.
//
// The fold happens server-side through the SAME Cai.Scoring the surveyor uses. Reimplementing the
// maths in JavaScript would produce a second implementation that can silently disagree with the
// standard — which is the one thing a reference scorer exists to prevent.

import { CaiIsland, TOKENS_CSS, BASE_CSS, sectionHeadHtml, SECTION_HEAD_CSS } from "./tokens.js";

const CSS =
  TOKENS_CSS +
  BASE_CSS +
  SECTION_HEAD_CSS +
  `
.mk-calc { display: grid; gap: 0.9rem; }
.mk-calc textarea {
  width: 100%; min-height: 11rem; padding: 0.7rem 0.8rem; border-radius: 8px;
  border: 1px solid var(--line); background: var(--surface); color: var(--ink);
  font-family: var(--mono, ui-monospace, monospace); font-size: 0.8rem; line-height: 1.5; resize: vertical;
}
.mk-calc textarea:focus { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-actions { display: flex; gap: 0.6rem; align-items: center; flex-wrap: wrap; }
.mk-btn { padding: 0.5rem 1rem; border-radius: 8px; border: 1px solid var(--accent); background: var(--accent);
          color: var(--on-accent, #fff); font: inherit; font-weight: 600; cursor: pointer; }
.mk-btn.ghost { background: transparent; color: var(--ink); border-color: var(--line); }
.mk-btn[disabled] { opacity: 0.6; cursor: progress; }
.mk-hint { color: var(--muted); font-size: 0.85rem; }
.mk-head { display: flex; align-items: baseline; gap: 0.6rem; margin: 0.2rem 0 0.5rem; }
.mk-cai { font-size: 2rem; font-weight: 700; font-variant-numeric: tabular-nums; }
.mk-band { padding: 2px 10px; border-radius: 20px; font-size: 0.78rem; font-weight: 700;
           border: 1px solid var(--line); color: var(--muted); }
.mk-lenses { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.mk-lenses th { text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em;
                color: var(--muted); border-bottom: 1px solid var(--line); padding: 6px 8px; }
.mk-lenses td { padding: 6px 8px; border-bottom: 1px solid var(--line); }
.mk-lenses td.n { text-align: right; font-variant-numeric: tabular-nums; }
.mk-note { border: 1px solid var(--line); border-left-width: 4px; border-radius: 8px; padding: 0.75rem 0.9rem; }
.mk-note.ok { border-left-color: var(--good, #2e9e6b); }
.mk-note.bad { border-left-color: var(--crit, #d05353); }
.mk-note h4 { margin: 0 0 0.3rem; font-size: 0.95rem; }
.mk-note p { margin: 0; color: var(--muted); font-size: 0.88rem; }
`;

/// A minimal, clearly-labelled sample so the widget is usable without hunting for a bundle first.
/// A labelled sample so the widget is usable without hunting for a bundle first. Taken from the standard's
/// own examples/evidence.sample.json, so the property names can never drift from what the API accepts.
const SAMPLE = `{
  "rubricVersion": "rubric-2026.08.15",
  "qualityBar": "production",
  "analyzableProjects": 3,
  "productionLoc": 1500,
  "dimensions": [
    {
      "id": "D1",
      "category": "code-quality",
      "score": 7.5,
      "confidence": 0.95
    },
    {
      "id": "D3",
      "category": "code-quality",
      "score": 8.2,
      "confidence": 0.95
    },
    {
      "id": "D8",
      "category": "code-quality",
      "score": 6.4,
      "confidence": 0.9
    },
    {
      "id": "D17",
      "category": "explicit-debt",
      "score": 8.8,
      "confidence": 0.9
    },
    {
      "id": "D5",
      "category": "architecture",
      "score": 7.1,
      "confidence": 0.95
    },
    {
      "id": "D7",
      "category": "architecture",
      "score": 8.4,
      "confidence": 0.9
    }
  ]
}`;

const esc = (v) => String(v ?? "").replace(/[<>&"]/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;" })[c]);

class CaiCalculator extends CaiIsland {
  static get observedAttributes() {
    return ["api-base", "kicker", "heading", "lede", "brand"];
  }

  /// The author's heading/lede when they set any, else a sensible default. sectionHeadHtml() reads the HOST's
  /// attributes and returns "" when none are set, so an unconfigured widget would otherwise render headless.
  #head() {
    const authored = sectionHeadHtml(this);
    if (authored) return authored;
    return `<div class="mk-section-head"><h2>Score an evidence bundle</h2><p>Paste an evidence bundle — the measured dimensions — and the open scorer folds them into the lens scores and the CAI, worst-first, in the open. If the bundle also states a published headline, it is checked against it.</p></div>`;
  }

  render(root) {
    
    root.innerHTML = `
      <style>${CSS}</style>
      ${this.#head()}
      <div class="mk-calc">
        <label class="mk-hint" for="ev">Evidence bundle (JSON) — dimensions, each scored 0–10</label>
        <textarea id="ev" spellcheck="false" placeholder='{ "rubricVersion": "…", "dimensions": [ { "id": "D1", "lens": "code-quality", "score": 7.5, "confidence": 0.95 } ] }'></textarea>
        <div class="mk-actions">
          <button class="mk-btn" type="button" id="go">Compute the CAI</button>
          <button class="mk-btn ghost" type="button" id="sample">Load a sample bundle</button>
          <span class="mk-hint" id="status"></span>
        </div>
        <div id="out"></div>
      </div>
    `;

    root.getElementById("go").addEventListener("click", () => this.#score(root));
    root.getElementById("sample").addEventListener("click", () => {
      root.getElementById("ev").value = SAMPLE;
      root.getElementById("out").innerHTML =
        `<p class="mk-hint">Sample bundle loaded — a labelled example, not a real survey. Press “Compute the CAI”.</p>`;
    });
  }

  async #score(root) {
    const base = this.apiBase().replace(/\/$/, "");
    const raw = root.getElementById("ev").value.trim();
    const out = root.getElementById("out");
    const status = root.getElementById("status");
    const button = root.getElementById("go");

    if (!raw) {
      out.innerHTML = this.#note("bad", "Nothing to score", "Paste an evidence bundle, or load the sample.");
      return;
    }
    if (!base) {
      out.innerHTML = this.#note("bad", "Not configured", "This widget has no API base set, so it cannot score anything.");
      return;
    }

    button.disabled = true;
    status.textContent = "Folding…";
    out.innerHTML = "";

    try {
      const res = await fetch(`${base}/api/score`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: raw,
      });
      const data = await res.json().catch(() => null);

      if (res.status === 429) {
        out.innerHTML = this.#note("bad", "Rate limited", "The open API is busy right now — wait a moment and try again.");
        return;
      }
      if (!res.ok || !data) {
        out.innerHTML = this.#note("bad", "That bundle could not be scored", (data && (data.error || data.title)) || "It could not be read as an evidence bundle.");
        return;
      }

      out.innerHTML = this.#result(data);
    } catch {
      out.innerHTML = this.#note("bad", "Could not reach the scorer", "The standard's API did not respond. Nothing about your bundle is implied.");
    } finally {
      button.disabled = false;
      status.textContent = "";
    }
  }

  #result(d) {
    const headline = typeof d.headline === "number" ? d.headline : d.cai;
    const lenses = Array.isArray(d.lenses) ? d.lenses : [];

    const rows = lenses
      .slice()
      .sort((a, b) => (b.contribution ?? 0) - (a.contribution ?? 0))
      .map(
        (l) => `<tr>
            <td>${esc(l.lens)}</td>
            <td class="n">${esc((l.score ?? 0).toFixed(1))}</td>
            <td>${esc(l.band ?? "")}</td>
            <td class="n">${esc((l.weight ?? 0).toFixed(3))}</td>
          </tr>`,
      )
      .join("");

    const table = rows
      ? `<table class="mk-lenses">
           <thead><tr><th>Lens</th><th class="n">Score</th><th>Band</th><th class="n">Weight</th></tr></thead>
           <tbody>${rows}</tbody>
         </table>`
      : "";

    // The falsifiability half: when the bundle states a published headline, say plainly whether it follows.
    let verify = "";
    if (d.verification && typeof d.verification.reproduced === "boolean") {
      const v = d.verification;
      verify = v.reproduced
        ? this.#note("ok", "✓ Reproduced", `The published headline ${esc((v.claimed ?? 0).toFixed(1))} follows from this evidence.`)
        : this.#note(
            "bad",
            "✗ Does not reproduce",
            `This evidence folds to ${esc((v.computed ?? 0).toFixed(2))}, but the bundle claims ${esc(
              (v.claimed ?? 0).toFixed(2),
            )}. That is falsifiable proof the published number does not follow from the evidence.`,
          );
    }

    return `
      <div class="mk-head">
        <span class="mk-cai">${esc((headline ?? 0).toFixed(1))}</span>
        <span class="mk-band">${esc(d.band ?? "")}</span>
      </div>
      <p class="mk-hint">The headline is the worst-first ordered-weighted average of the lens scores${
        d.rubricVersion ? ` under <code>${esc(d.rubricVersion)}</code>` : ""
      }.</p>
      ${table}
      ${verify}`;
  }

  #note(kind, title, detail) {
    return `<div class="mk-note ${kind}"><h4>${esc(title)}</h4><p>${esc(detail)}</p></div>`;
  }
}

if (!customElements.get("cai-calculator")) customElements.define("cai-calculator", CaiCalculator);
