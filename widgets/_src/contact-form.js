// <contact-form topics='["Book an appraisal","Sales & pricing",…]'
//               fallback-email="sales@canine.dev" action=""
//               kicker="…" heading="…" lede="…" privacy-note="…"
//               brand="watchdog|assay|cai">
//
// Port of packages/ui/src/ContactForm.tsx markup: the topic <select> (with the
// ?topic= prefill and the same TOPIC_KEY_MATCHERS), name / email / org /
// message fields, and the hidden `website` honeypot. The CMS posts JSON to a
// server /api/contact route; imprint publishes static pages with NO API, so
// this island submits with a plain HTML form POST to a configurable `action`,
// defaulting to a mailto: to `fallback-email`. The provenance is honest: with
// no action it opens the visitor's mail client pre-addressed.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  sectionHeadHtml,
  SECTION_HEAD_CSS,
  renderInline,
  escapeHtml,
} from "./tokens.js";

// CTA ?topic=<key> → topic-option prefill (ContactForm.tsx TOPIC_KEY_MATCHERS,
// ported verbatim).
const TOPIC_KEY_MATCHERS = {
  sales: ["sales"],
  demo: ["demo"],
  onprem: ["on-prem", "self-hosted"],
  compliance: ["compliance"],
  general: ["something else"],
  security: ["security"],
  appraisal: ["appraisal"],
  attestation: ["attestation"],
  consequences: ["consequences"],
  dd: ["due-diligence", "due diligence"],
  portfolio: ["portfolio"],
  tender: ["tender"],
};

/** Resolve a ?topic= key (or verbatim option label) to one of the form's options. */
function matchTopicOption(key, topics) {
  if (!key) return null;
  const norm = String(key).trim().toLowerCase();
  if (!norm) return null;
  const exact = topics.find((t) => t.trim().toLowerCase() === norm);
  if (exact) return exact;
  for (const needle of TOPIC_KEY_MATCHERS[norm] || []) {
    const hit = topics.find((t) => t.toLowerCase().includes(needle));
    if (hit) return hit;
  }
  return null;
}

const CSS = TOKENS_CSS + BASE_CSS + SECTION_HEAD_CSS + `
.mk-form { display: grid; gap: 1rem; max-width: 40rem; margin: 0 auto; }
.mk-form label { display: grid; gap: 0.35rem; font-size: var(--fs-sm); font-weight: 500; color: var(--heading); }
.mk-form input, .mk-form select, .mk-form textarea {
  font: 400 var(--fs-md)/1.4 var(--font-ui);
  color: var(--ink); background: var(--surface);
  border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 9px 11px;
}
.mk-form textarea { min-height: 130px; resize: vertical; }
.mk-form-hp { position: absolute; left: -9999px; height: 0; overflow: hidden; }
.mk-form-status { font-size: var(--fs-sm); color: var(--muted); }
.mk-form-status.is-error { color: var(--band-poor-text); }
.mk-form-status.is-ok { color: var(--band-exemplary-text); }
.mk-cta-row { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: flex-start; }
.btn { display: inline-flex; align-items: center; justify-content: center; gap: 8px; font: 500 var(--fs-md)/1.2 var(--font-ui); color: var(--ink); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 8px 14px; cursor: pointer; text-decoration: none; }
.btn:hover { background: var(--surface-2); }
.btn-primary { background: var(--accent-strong); border-color: var(--accent-strong); color: var(--on-accent); font-weight: 600; }
.btn-primary:hover { opacity: 0.92; }
.btn-lg { padding: 12px 22px; font-size: var(--fs-lg); font-weight: 600; border-radius: var(--r-md); }
`;

customElements.define(
  "contact-form",
  class extends CaiIsland {
    render(root) {
      const topics = (this.json("topics", []) || []).map(String);
      const fallback = this.getAttribute("fallback-email") || "sales@canine.dev";
      const action = (this.getAttribute("action") || "").trim();
      const privacyNote = this.getAttribute("privacy-note");

      // Prefill from ?topic= after hydration (static pages carry no query at build).
      let selected = topics[0] || "";
      try {
        const key = new URLSearchParams(window.location.search).get("topic");
        const hit = matchTopicOption(key, topics);
        if (hit) selected = hit;
      } catch {
        /* no window/URL — keep the first option */
      }

      // No API on imprint: post to a configured endpoint, else mailto the fallback.
      const formAction = action || `mailto:${fallback}`;
      const isMailto = !action;

      const topicSelect =
        topics.length > 0
          ? `<label>What&rsquo;s this about?<select name="topic">` +
            topics
              .map(
                (t) =>
                  `<option value="${escapeHtml(t)}"${t === selected ? " selected" : ""}>${escapeHtml(t)}</option>`
              )
              .join("") +
            `</select></label>`
          : "";

      let html = `<style>${CSS}</style>`;
      html += sectionHeadHtml(this);
      // enctype text/plain keeps a mailto: body human-readable; a real endpoint
      // can override `action` and receive standard form-encoded fields.
      html +=
        `<form class="mk-form" method="post" action="${escapeHtml(formAction)}"` +
        (isMailto ? ` enctype="text/plain"` : "") +
        `>`;
      html += topicSelect;
      html += `<label>Your name<input name="name" autocomplete="name" required></label>`;
      html += `<label>Work email<input name="email" type="email" autocomplete="email" required></label>`;
      html += `<label>Organisation <span style="font-weight:400">(optional)</span><input name="org" autocomplete="organization"></label>`;
      html += `<label>How can we help?<textarea name="message" required></textarea></label>`;
      // Honeypot — humans never see it; bots fill it.
      html += `<label class="mk-form-hp" aria-hidden="true">Website<input name="website" tabindex="-1" autocomplete="off"></label>`;
      html += `<div class="mk-cta-row"><button class="btn btn-primary btn-lg" type="submit">Send message</button></div>`;
      html += `<p class="mk-form-status">This form opens your mail app addressed to <a href="mailto:${escapeHtml(fallback)}">${escapeHtml(fallback)}</a>.</p>`;
      if (privacyNote)
        html += `<p class="mk-form-status">${renderInline(privacyNote)}</p>`;
      html += `</form>`;
      root.innerHTML = html;

      // Drop the honeypot from a mailto: submission (a filled honeypot is a bot;
      // and even empty it needn't clutter the mail body). Native submit otherwise.
      const form = root.querySelector("form");
      if (!form) return;
      form.addEventListener("submit", (e) => {
        const hp = form.querySelector('input[name="website"]');
        if (hp && hp.value.trim() !== "") {
          // Looks like a bot — swallow the submit silently.
          e.preventDefault();
          return;
        }
        if (hp) hp.disabled = true; // keep it out of the serialized body
      });
    }
  }
);
