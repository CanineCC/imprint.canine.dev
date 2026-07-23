// <wd-embed> — frames a LIVE view rendered by the product itself.
//
// Every other island in this folder draws its own version of a Watchdog component: its own markup,
// its own copy of the CSS, its own copy of the design tokens. That is a second implementation kept
// in step by hand, and it drifted. This one draws nothing. It points an iframe at
//
//     {base}/embed/{view}?repo={owner/name}&theme={dark|light}
//
// which the app serves by instantiating its OWN Razor components against real published data, with
// its OWN stylesheet. Change the component in kennel and this changes with it — no rebuild here, no
// republish of the marketing site.
//
// The iframe is sized from the inside: an embedded document has no intrinsic height, so the embed
// posts its measured height (`{type:"wd-embed-size"}`) on load and on every reflow, and we apply it.
// We accept that message only from the frame we created, and only from the origin we pointed it at.
//
// The server refuses (403) any repo that is not published, so a wrong or private `repo` renders the
// app's own "not authorized" card rather than leaking anything — nothing to guard for on this side.

const THEME_ATTR = "data-theme";

class WdEmbed extends HTMLElement {
  static get observedAttributes() {
    return ["base", "view", "repo", "theme", "pick", "title-text", "min-height"];
  }

  connectedCallback() {
    if (!this._root) {
      this._root = this.attachShadow({ mode: "open" });
      // A skeleton while the frame loads. The page around it is static and lands well under 100ms;
      // an embed is a live query against the product and can take a second or two. Without a
      // placeholder the section collapses to nothing and then jumps — so reserve the space and show
      // that it is working. The skeleton sits behind the frame, which stays transparent until it
      // reports a height (its honest "I have rendered" signal).
      this._root.innerHTML =
        '<style>' +
        // width:100% because the marketing sections that centre their heads (`align-items: center`)
        // do NOT stretch a custom element the way they stretch a grid — the frame would size to its
        // content and render as a narrow column in the middle of a full-width section.
        ':host{display:block;width:100%}' +
        '.wrap{position:relative;width:100%}' +
        'iframe{display:block;width:100%;border:0;color-scheme:normal;' +
          'transition:height .15s ease-out,opacity .2s ease-in;opacity:0}' +
        ':host([data-ready]) iframe{opacity:1}' +
        '.skel{position:absolute;inset:0;border:1px solid #2d353e;border-radius:10px;' +
          'overflow:hidden;background:#1c2127}' +
        ':host([data-theme-light]) .skel{border-color:#e1e6eb;background:#f5f7f9}' +
        ':host([data-ready]) .skel{display:none}' +
        '.skel::after{content:"";position:absolute;inset:0;transform:translateX(-100%);' +
          'background:linear-gradient(90deg,transparent,rgba(127,170,206,.10),transparent);' +
          'animation:sweep 1.4s ease-in-out infinite}' +
        '@keyframes sweep{100%{transform:translateX(100%)}}' +
        '@media (prefers-reduced-motion:reduce){.skel::after{animation:none}}' +
        '</style>' +
        '<div class="wrap">' +
        '<div class="skel" role="status" aria-label="Loading live data"></div>' +
        "<iframe title='' loading='lazy' scrolling='no' referrerpolicy='no-referrer'></iframe>" +
        '</div>';
      this._frame = this._root.querySelector("iframe");
      this._onMessage = this._onMessage.bind(this);
    }
    window.addEventListener("message", this._onMessage);

    // theme="auto" tracks the host page instead of being told once, so the embed flips with the
    // site's own toggle. Both signals matter: the explicit data-theme the page stamps on <html>,
    // and the OS preference when the page has not chosen.
    this._observer = new MutationObserver(() => this._render());
    this._observer.observe(document.documentElement, { attributes: true, attributeFilter: [THEME_ATTR] });
    this._media = window.matchMedia("(prefers-color-scheme: light)");
    this._onScheme = () => this._render();
    this._media.addEventListener("change", this._onScheme);

    this._render();
  }

  disconnectedCallback() {
    window.removeEventListener("message", this._onMessage);
    this._observer?.disconnect();
    this._media?.removeEventListener("change", this._onScheme);
  }

  attributeChangedCallback() {
    if (this._frame) {
      this._render();
    }
  }

  /** The theme to ask for: an explicit light/dark wins; "auto" (or absent) follows the host page. */
  _theme() {
    const wanted = (this.getAttribute("theme") || "auto").toLowerCase();
    if (wanted === "light" || wanted === "dark") {
      return wanted;
    }
    const stamped = document.documentElement.getAttribute(THEME_ATTR);
    if (stamped === "light" || stamped === "dark") {
      return stamped;
    }
    return this._media && this._media.matches ? "light" : "dark";
  }

  _src() {
    const base = (this.getAttribute("base") || "").trim().replace(/\/$/, "");
    const view = (this.getAttribute("view") || "score-card").trim();
    if (!base) {
      return null;
    }
    const url = new URL(base + "/embed/" + encodeURIComponent(view));
    const repo = (this.getAttribute("repo") || "").trim();
    if (repo) {
      url.searchParams.set("repo", repo);
    }
    // Which slot of the wall this card fills, when no repo is named. The SERVER decides what each slot
    // resolves to, from one ordered decision — so four independent embeds on one page can never collide.
    const pick = (this.getAttribute("pick") || "").trim();
    if (!repo && pick) {
      url.searchParams.set("pick", pick);
    }
    url.searchParams.set("theme", this._theme());
    return url.toString();
  }

  _render() {
    const src = this._src();
    if (!src) {
      // No base configured (a preview in the editor, say) — show nothing rather than a broken frame.
      this._frame.removeAttribute("src");
      return;
    }
    this._origin = new URL(src).origin;
    this._frame.title = this.getAttribute("title-text") || "Watchdog survey";
    const min = parseInt(this.getAttribute("min-height") || "", 10);
    if (!this._frame.style.height) {
      this._frame.style.height = (Number.isFinite(min) && min > 0 ? min : 320) + "px";
    }
    // The skeleton takes the resolved theme too, so it doesn't flash dark on a light page.
    this.toggleAttribute("data-theme-light", this._theme() === "light");
    if (this._frame.getAttribute("src") !== src) {
      this.removeAttribute("data-ready");
      this._frame.setAttribute("src", src);
      // If no height ever arrives (frame blocked, offline), stop pretending to load: a permanent
      // shimmer reads as broken, where an empty card at least reads as empty.
      clearTimeout(this._giveUp);
      this._giveUp = setTimeout(() => this.setAttribute("data-ready", ""), 8000);
    }
  }

  _onMessage(event) {
    // Only the frame we created, only from the origin we pointed it at, only the message we expect.
    if (!this._frame || event.source !== this._frame.contentWindow) return;
    if (!this._origin || event.origin !== this._origin) return;
    const data = event.data;
    if (!data || data.type !== "wd-embed-size") return;
    const height = Number(data.height);
    if (!Number.isFinite(height) || height <= 0 || height > 20000) return;
    this._frame.style.height = Math.ceil(height) + "px";
    // The embed posts a height only once it has rendered — the honest "ready" moment.
    clearTimeout(this._giveUp);
    this.setAttribute("data-ready", "");
  }
}

customElements.define("wd-embed", WdEmbed);
