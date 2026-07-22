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
    return ["base", "view", "repo", "theme", "title-text", "min-height"];
  }

  connectedCallback() {
    if (!this._root) {
      this._root = this.attachShadow({ mode: "open" });
      this._root.innerHTML =
        '<style>' +
        ':host{display:block}' +
        'iframe{display:block;width:100%;border:0;color-scheme:normal;transition:height .15s ease-out}' +
        '</style>' +
        "<iframe title='' loading='lazy' scrolling='no' referrerpolicy='no-referrer'></iframe>";
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
    if (this._frame.getAttribute("src") !== src) {
      this._frame.setAttribute("src", src);
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
  }
}

customElements.define("wd-embed", WdEmbed);
