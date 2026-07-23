// <contact-form topics='["Book an appraisal","Sales & pricing",…]'
//               action="https://app.imprint.canine.dev/api/contact"
//               kicker="…" heading="…" lede="…" privacy-note="…"
//               brand="watchdog|assay|cai">
//
// Port of packages/ui/src/ContactForm.tsx markup: the topic <select> (with the
// ?topic= prefill and the same TOPIC_KEY_MATCHERS), name / email / org /
// message fields, and the hidden `website` honeypot. With an `action` set the
// island fetch()es a form-encoded POST to its endpoint (the imprint editor's
// anonymous /api/contact — the default when the attribute is absent) and reports
// the result inline in .mk-form-status. No inbox address exists anywhere in the
// page; recipients are the endpoint's server-side config.

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
/* Success gets a dialog, not a line of text. The form can sit well below the fold on a long page,
   so an inline "Thanks" is easy to miss entirely — the sender is left unsure whether it sent. A
   modal cannot be scrolled past and has to be dismissed, which is the acknowledgement they came for. */
.mk-modal { position: fixed; inset: 0; display: grid; place-items: center; z-index: 9999;
  background: rgba(0,0,0,.55); padding: 24px; }
.mk-modal[hidden] { display: none; }
.mk-modal-card { background: var(--surface); color: var(--ink); border: 1px solid var(--border);
  border-radius: var(--r-lg); padding: 22px 24px; max-width: 24rem; width: 100%;
  box-shadow: 0 12px 40px rgb(0 0 0 / .45); text-align: center; }
.mk-modal-card h3 { margin: 0 0 6px; font-size: var(--fs-lg); color: var(--heading); }
.mk-modal-card p { margin: 0 0 16px; font-size: var(--fs-sm); color: var(--muted); }
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
      const action =
        (this.getAttribute("action") || "").trim() ||
        "https://app.imprint.canine.dev/api/contact";
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
      html += `<form class="mk-form" method="post" action="${escapeHtml(action)}">`;
      html += topicSelect;
      html += `<label>Your name<input name="name" autocomplete="name" required></label>`;
      html += `<label>Work email<input name="email" type="email" autocomplete="email" required></label>`;
      html += `<label>Organisation <span style="font-weight:400">(optional)</span><input name="org" autocomplete="organization"></label>`;
      html += `<label>How can we help?<textarea name="message" required></textarea></label>`;
      // Honeypot — humans never see it; bots fill it.
      html += `<label class="mk-form-hp" aria-hidden="true">Website<input name="website" tabindex="-1" autocomplete="off"></label>`;
      {
        // The submitting site's hostname, so one endpoint triages all brands.
        let site = "";
        try {
          site = window.location.hostname;
        } catch {
          /* no window — leave it blank */
        }
        html += `<input type="hidden" name="site" value="${escapeHtml(site)}">`;
      }
      html += `<div class="mk-cta-row"><button class="btn btn-primary btn-lg" type="submit">Send message</button></div>`;
      // The status line is the inline send-result — it NEVER mentions an address.
      html += `<p class="mk-form-status" aria-live="polite"></p>`;
      html += `<div class="mk-modal" role="dialog" aria-modal="true" aria-labelledby="mk-sent-h" hidden>
        <div class="mk-modal-card">
          <h3 id="mk-sent-h">Thanks — message sent.</h3>
          <p>We'll get back to you.</p>
          <button type="button" class="mk-btn mk-btn-primary" data-mk-ok>OK</button>
        </div>
      </div>`;
      if (privacyNote)
        html += `<p class="mk-form-status">${renderInline(privacyNote)}</p>`;
      html += `</form>`;
      root.innerHTML = html;

      const form = root.querySelector("form");
      if (!form) return;
      const status = form.querySelector(".mk-form-status");
      const button = form.querySelector('button[type="submit"]');
      const showStatus = (text, ok) => {
        if (!status) return;
        status.textContent = text;
        status.classList.toggle("is-ok", ok === true);
        status.classList.toggle("is-error", ok === false);
      };

      // Success is confirmed in a dialog the sender has to dismiss; errors and progress stay inline,
      // because those belong next to the field you are about to correct.
      const modal = root.querySelector(".mk-modal");
      const okButton = modal && modal.querySelector("[data-mk-ok]");
      const closeModal = () => {
        if (!modal) return;
        modal.hidden = true;
        const first = form.querySelector("input, textarea");
        if (first) first.focus();
      };
      const confirmSent = () => {
        if (!modal) { showStatus("Thanks — we'll get back to you.", true); return; }
        showStatus("", null);
        modal.hidden = false;
        if (okButton) okButton.focus();
      };
      if (okButton) okButton.addEventListener("click", closeModal);
      if (modal) {
        modal.addEventListener("click", (ev) => { if (ev.target === modal) closeModal(); });
        modal.addEventListener("keydown", (ev) => { if (ev.key === "Escape") closeModal(); });
      }

      form.addEventListener("submit", async (e) => {
        const hp = form.querySelector('input[name="website"]');

        // fetch() the form-encoded fields (a preflight-free simple request) and
        // answer inline — the page never navigates.
        e.preventDefault();
          if (hp && hp.value.trim() !== "") {
            // A filled honeypot is a bot — pretend success, send nothing.
            confirmSent();
            return;
          }
          if (button) button.disabled = true; // no double-sends
          showStatus("Sending…", null);
          try {
            const res = await fetch(action, {
              method: "POST",
              body: new URLSearchParams(new FormData(form)),
            });
            if (!res.ok) throw new Error(String(res.status));
            form.reset();
            confirmSent();
          } catch {
            // Never reveal an address here — retry is the only recovery we offer.
            showStatus(
              "Something went wrong sending your message — please try again in a minute.",
              false
            );
          } finally {
            if (button) button.disabled = false;
          }
      });
    }
  }
);
