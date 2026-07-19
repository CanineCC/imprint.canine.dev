// <cai-verifier api-base="https://cai.canine.dev" kicker="…" heading="…" lede="…" brand="cai">
//
// Paste a signed CAI delivery package; get the two answers that matter, kept apart:
//   1. Is it authentically ours and unedited?  (Ed25519 over the canonical payload)
//   2. Does the number it states actually follow from the evidence it carries?
//
// Those are independent claims and this widget never lets one vouch for the other. A package can be
// genuinely signed by us and still state a headline its own evidence does not produce — signed
// BEFORE the number was edited — and that case is reported as "signed, but does not reproduce",
// never as trustworthy. A signature attests the author, never the arithmetic.
//
// Why this is an island rather than an iframe of the app's own /verify page: cai.canine.dev is
// served by the CMS, so the app's pages are not reachable there, and the app sets
// frame-ancestors 'self' — deliberately, because a page whose job is verifying signatures is
// exactly the wrong page to let a stranger frame. Calling the open API from a widget keeps the
// clickjacking guard intact and inherits the site's own theme.

import { CaiIsland, TOKENS_CSS, BASE_CSS, sectionHeadHtml, SECTION_HEAD_CSS } from "./tokens.js";

const CSS =
  TOKENS_CSS +
  BASE_CSS +
  SECTION_HEAD_CSS +
  `
.mk-verify { display: grid; gap: 0.9rem; }
.mk-verify textarea {
  width: 100%; min-height: 11rem; padding: 0.7rem 0.8rem; border-radius: 8px;
  border: 1px solid var(--line); background: var(--surface); color: var(--ink);
  font-family: var(--mono, ui-monospace, monospace); font-size: 0.8rem; line-height: 1.5;
  resize: vertical;
}
.mk-verify textarea:focus { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-actions { display: flex; gap: 0.6rem; align-items: center; flex-wrap: wrap; }
.mk-btn {
  padding: 0.5rem 1rem; border-radius: 8px; border: 1px solid var(--accent);
  background: var(--accent); color: var(--on-accent, #fff); font: inherit; font-weight: 600;
  cursor: pointer;
}
.mk-btn[disabled] { opacity: 0.6; cursor: progress; }
.mk-hint { color: var(--muted); font-size: 0.85rem; }
.mk-result { border: 1px solid var(--line); border-left-width: 4px; border-radius: 8px; padding: 0.85rem 1rem; }
.mk-result.ok { border-left-color: var(--good, #2e9e6b); }
.mk-result.bad { border-left-color: var(--crit, #d05353); }
.mk-result h4 { margin: 0 0 0.35rem; font-size: 1rem; }
.mk-result p { margin: 0; color: var(--muted); font-size: 0.9rem; }
.mk-facts { margin: 0.7rem 0 0; padding: 0; list-style: none; display: grid; gap: 0.25rem; font-size: 0.85rem; }
.mk-facts b { color: var(--ink); font-weight: 600; }
.mk-facts code { font-family: var(--mono, ui-monospace, monospace); font-size: 0.82em; }
`;

class CaiVerifier extends CaiIsland {
  static get observedAttributes() {
    return ["api-base", "kicker", "heading", "lede", "brand"];
  }

  /// The author's heading/lede when they set any, else a sensible default. sectionHeadHtml() reads the HOST's
  /// attributes and returns "" when none are set, so an unconfigured widget would otherwise render headless.
  #head() {
    const authored = sectionHeadHtml(this);
    if (authored) return authored;
    return `<div class="mk-section-head"><h2>Verify a signed survey</h2><p>Paste a signed CAI delivery package. We check two things separately: that it is authentically ours and unedited, and that the number it states follows from the evidence it carries.</p></div>`;
  }

  render(root) {
    
    root.innerHTML = `
      <style>${CSS}</style>
      ${this.#head()}
      <div class="mk-verify">
        <label class="mk-hint" for="pkg">Signed CAI delivery package (JSON)</label>
        <textarea id="pkg" spellcheck="false" placeholder='{ "payload": { "schemaVersion": "1.0", … }, "signature": { "alg": "Ed25519", … } }'></textarea>
        <div class="mk-actions">
          <button class="mk-btn" type="button" id="go">Verify the signature</button>
          <span class="mk-hint" id="status"></span>
        </div>
        <div id="out"></div>
      </div>
    `;

    root.getElementById("go").addEventListener("click", () => this.#verify(root));
  }

  async #verify(root) {
    const base = this.apiBase().replace(/\/$/, "");
    const raw = root.getElementById("pkg").value.trim();
    const out = root.getElementById("out");
    const status = root.getElementById("status");
    const button = root.getElementById("go");

    if (!raw) {
      out.innerHTML = this.#message("bad", "Nothing to check", "Paste a signed delivery package first.");
      return;
    }
    if (!base) {
      // Configuration, not user error — say which so an editor can fix the widget rather than the reader retrying.
      out.innerHTML = this.#message("bad", "Not configured", "This widget has no API base set, so it cannot verify anything.");
      return;
    }

    button.disabled = true;
    status.textContent = "Checking…";
    out.innerHTML = "";

    try {
      const res = await fetch(`${base}/api/verify-delivery`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: raw,
      });
      const data = await res.json().catch(() => null);

      if (res.status === 429) {
        out.innerHTML = this.#message("bad", "Rate limited", "The open API is busy right now — wait a moment and try again.");
        return;
      }
      if (!res.ok || !data) {
        out.innerHTML = this.#message(
          "bad",
          "That is not a delivery package",
          (data && data.error) || "It could not be read as a signed CAI delivery package.",
        );
        return;
      }

      out.innerHTML = this.#verdict(data);
    } catch {
      // Network/CORS: never blame the reader's package for our own reachability problem.
      out.innerHTML = this.#message("bad", "Could not reach the verifier", "The standard's API did not respond. Nothing about your package is implied.");
    } finally {
      button.disabled = false;
      status.textContent = "";
    }
  }

  #verdict(d) {
    const esc = (v) => String(v ?? "").replace(/[<>&"]/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;" })[c]);

    let body;
    if (d.trustworthy) {
      body = this.#message(
        "ok",
        "✓ Authentic and reproducing",
        "This package was signed by a published CAI key and has not been altered since — and the CAI it states follows from the evidence it carries.",
      );
    } else if (!d.signatureValid) {
      body = this.#message(
        "bad",
        "✗ Not authentic",
        `${esc(d.reason || "the signature did not verify")} — treat this document as unattributed. Either it was not issued by cai.canine.dev, or it has been edited since it was signed.`,
      );
    } else {
      body = this.#message(
        "bad",
        "✗ Signed, but the number does not reproduce",
        `The signature is genuine, so the document really was issued this way — but folding its own evidence gives ${esc(
          (d.computedCai ?? 0).toFixed(2),
        )}, not the ${esc((d.claimedCai ?? 0).toFixed(2))} it claims. A valid signature attests the author, never the arithmetic.`,
      );
    }

    const s = d.subject || {};
    // A valid signature proves the document is ours — NOT that it describes the code the reader was shown.
    // Confirming the subject is the reader's half of the check, so it is always displayed.
    const facts = `
      <p class="mk-hint" style="margin:.7rem 0 0">A signature checks the document, not that it describes the code you were shown — confirm the subject:</p>
      <ul class="mk-facts">
        <li><b>Repository:</b> <code>${esc(s.repository)}</code>${s.commit ? ` at commit <code>${esc(s.commit)}</code>` : ""}</li>
        <li><b>Rubric:</b> <code>${esc(d.rubricVersion)}</code></li>
        <li><b>Issued:</b> ${esc(d.issuedAt)} · <b>key</b> <code>${esc(d.keyId)}</code></li>
        <li><b>Produced by:</b> ${esc((d.producer && d.producer.name) || "—")}</li>
        <li><b>Stated verdict:</b> CAI ${esc((d.verdict && d.verdict.cai) ?? "—")} (${esc((d.verdict && d.verdict.band) || "—")})</li>
      </ul>`;

    return body + facts;
  }

  #message(kind, title, detail) {
    const esc = (v) => String(v ?? "").replace(/[<>&]/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;" })[c]);
    return `<div class="mk-result ${kind}"><h4>${esc(title)}</h4><p>${detail}</p></div>`;
  }
}

if (!customElements.get("cai-verifier")) customElements.define("cai-verifier", CaiVerifier);
