// Shared design tokens + theme plumbing for the six CAI marketing islands.
//
// The CMS renders these widgets inside a page that sets the Fjeld tokens on
// :root (canine.css). Inside a shadow root, :root[data-theme] / :root[data-brand]
// selectors do NOT reach in, so each island carries its OWN copy of the token
// table (lifted verbatim from packages/ui/styles/canine.css) scoped to :host,
// and reflects the document's theme + the widget's `brand` onto its own host so
// the right :host([data-theme]) / :host([data-brand]) rule wins. Custom
// properties still inherit through the boundary, so authors who set --band-* on
// the page continue to override — the inlined table is only a self-contained
// fallback.

// The token blocks below are byte-for-byte the values in canine.css.
export const TOKENS_CSS = `
:host {
  /* neutrals — dark "graphite" */
  --bg: #15191e;
  --surface: #1c2127;
  --surface-2: #232a31;
  --border: #2d353e;
  --border-strong: #3a444f;
  --ink: #e4e9ed;
  --ink-soft: #b9c2cb;
  --muted: #8694a1;
  --heading: #f2f5f8;
  /* accent — watchdog "steel" is the family default */
  --accent: #7faace;
  --accent-ink: #9bbedb;
  --accent-wash: #1e2c39;
  --accent-strong: #b7d2e8;
  --on-accent: #15191e;
  /* bands (identical across products — the CAI vocabulary). dark = DarkHex. */
  --band-exemplary: #3fb97c;
  --band-healthy: #62c088;
  --band-fair: #d6a93a;
  --band-poor: #e08a5c;
  --band-critical: #d8635c;
  --band-exemplary-text: #3fb97c;
  --band-healthy-text: #62c088;
  --band-fair-text: #d6a93a;
  --band-poor-text: #e08a5c;
  --band-critical-text: #d8635c;
  /* CAI ladder marker — THEME-FIXED dark ink + explicit white casing. */
  --mk: #1c2522;
  --mk-on: #ffffff;
  /* shape & depth */
  --r-sm: 6px;
  --r-md: 10px;
  --r-lg: 14px;
  --r-full: 999px;
  --shadow-overlay: 0 4px 16px rgb(0 0 0 / 0.35);
  /* type */
  --font-ui: "Schibsted Grotesk", system-ui, sans-serif;
  --font-mono: "JetBrains Mono", ui-monospace, monospace;
  --fs-2xs: 11px;
  --fs-xs: 12px;
  --fs-sm: 13px;
  --fs-md: 14px;
  --fs-lg: 16px;
  --fs-xl: 20px;
  --fs-2xl: 25px;
  --fs-3xl: 31px;
  --fs-4xl: 39px;
  --hairline: var(--border);
}
:host([data-theme="light"]) {
  --bg: #fcfcfd;
  --surface: #f5f7f9;
  --surface-2: #edf0f3;
  --border: #e1e6eb;
  --border-strong: #cbd3da;
  --ink: #1c2126;
  --ink-soft: #434b54;
  --muted: #616b76;
  --heading: #14181d;
  --accent: #4682b4;
  --accent-ink: #2f5d85;
  --accent-wash: #eaf1f7;
  --accent-strong: #264b6b;
  --on-accent: #ffffff;
  --band-exemplary: #0e5c3a;
  --band-healthy: #3c8f59;
  --band-fair: #ad8217;
  --band-poor: #cf6b3a;
  --band-critical: #9c2d2a;
  --band-exemplary-text: #0e5c3a;
  --band-healthy-text: #2e6e45;
  --band-fair-text: #7e5f10;
  --band-poor-text: #a84e22;
  --band-critical-text: #9c2d2a;
  --shadow-overlay: 0 4px 16px rgb(20 25 30 / 0.1);
}
/* Per-product accents (harmonized siblings of the watchdog steel). */
:host([data-brand="assay"]) {
  --accent: #8fa2d4;
  --accent-ink: #a9b8de;
  --accent-wash: #232a44;
  --accent-strong: #c2cdea;
  --on-accent: #15191e;
}
:host([data-brand="assay"][data-theme="light"]) {
  --accent: #4a5d96;
  --accent-ink: #35456f;
  --accent-wash: #eceff7;
  --accent-strong: #2c3a61;
  --on-accent: #ffffff;
}
:host([data-brand="cai"]) {
  --accent: #6fbfa4;
  --accent-ink: #8fcdb8;
  --accent-wash: #1b332c;
  --accent-strong: #aedccb;
  --on-accent: #15191e;
}
:host([data-brand="cai"][data-theme="light"]) {
  --accent: #2e7d64;
  --accent-ink: #226050;
  --accent-wash: #e6f1ec;
  --accent-strong: #1c4f41;
  --on-accent: #ffffff;
}
`;

// The tiny inline-markup subset from packages/ui/src/inline.tsx — **bold**,
// `code`, [label](href) — ported verbatim, returning safe HTML (all literal
// text escaped; only the three recognised tokens become tags). Used by the
// copy strings (lede / body / caption / footnote / privacyNote).
export function escapeHtml(s) {
  return String(s ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

export function renderInline(text) {
  if (text == null || text === "") return "";
  const re = /(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g;
  let out = "";
  let last = 0;
  let m;
  while ((m = re.exec(text)) !== null) {
    if (m.index > last) out += escapeHtml(text.slice(last, m.index));
    const tok = m[0];
    if (tok.startsWith("**")) {
      out += `<strong>${escapeHtml(tok.slice(2, -2))}</strong>`;
    } else if (tok.startsWith("`")) {
      out += `<code>${escapeHtml(tok.slice(1, -1))}</code>`;
    } else {
      const mm = /^\[([^\]]+)\]\(([^)]+)\)$/.exec(tok);
      if (mm) {
        // href goes into an attribute value — escape quotes/angle brackets.
        out += `<a href="${escapeHtml(mm[2])}">${escapeHtml(mm[1])}</a>`;
      } else {
        out += escapeHtml(tok);
      }
    }
    last = m.index + tok.length;
  }
  if (last < text.length) out += escapeHtml(text.slice(last));
  return out;
}

// A minimal shared style block reused across every card/figure widget: base
// typography, links, code, and the sr-only helper — the slice of canine.css
// §base that the CMS renderers rely on inside .mk-* content.
export const BASE_CSS = `
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;

// The section-head fragment (kicker / heading / lede) shared by every block
// that carries one — ported from blocks.tsx SectionHead. Returns HTML.
export function sectionHeadHtml(host) {
  const kicker = host.getAttribute("kicker");
  const heading = host.getAttribute("heading");
  const lede = host.getAttribute("lede");
  if (!kicker && !heading && !lede) return "";
  let h = '<div class="mk-section-head">';
  if (kicker) h += `<span class="mk-kicker">${escapeHtml(kicker)}</span>`;
  if (heading) h += `<h2>${renderInline(heading)}</h2>`;
  if (lede) h += `<p>${renderInline(lede)}</p>`;
  h += "</div>";
  return h;
}

export const SECTION_HEAD_CSS = `
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`;

// Shared base class: attaches a shadow root, reflects the document theme and
// the widget's `brand` attribute onto the host (so the scoped :host token
// rules resolve), and keeps them live via a MutationObserver on <html
// data-theme> and observedAttributes. Subclasses implement render(root).
export class CaiIsland extends HTMLElement {
  #observer;

  connectedCallback() {
    if (!this.shadowRoot) this.attachShadow({ mode: "open" });
    this.#reflectContext();
    // Render the fallback/sample FIRST — zero layout shift, and the honest content
    // when no live source is configured. A live subclass then upgrades in place.
    this.render(this.shadowRoot);
    // Live widgets fetch real curated data from `api-base` and re-render on arrival.
    // liveLoad() is a no-op on static widgets (evidence-flow, contact-form), and a
    // no-op here when `api-base` is unset (the fetch helpers short-circuit to sample).
    if (typeof this.liveLoad === "function") {
      Promise.resolve(this.liveLoad()).catch(() => {});
    }
    // Keep in visual sync with the document theme, changed by any control.
    this.#observer = new MutationObserver(() => {
      const before = this.dataset.theme;
      this.#reflectContext();
      if (this.dataset.theme !== before) this.render(this.shadowRoot);
    });
    this.#observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["data-theme"],
    });
  }

  /** The configured kennel public-API origin, or "" (fall back to the sample). */
  apiBase() {
    return (this.getAttribute("api-base") || "").trim();
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
  }

  #reflectContext() {
    const theme =
      document.documentElement.dataset.theme ||
      (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    this.dataset.theme = theme;
    const brand = (this.getAttribute("brand") || "").trim().toLowerCase();
    if (brand === "assay" || brand === "cai" || brand === "watchdog") {
      this.dataset.brand = brand;
    } else {
      delete this.dataset.brand;
    }
  }

  // Convenience: parse an attribute that carries inline JSON (arrays/objects),
  // tolerant of an empty/absent value.
  json(attr, fallback) {
    const raw = this.getAttribute(attr);
    if (raw == null || raw.trim() === "") return fallback;
    try {
      return JSON.parse(raw);
    } catch {
      return fallback;
    }
  }
}
