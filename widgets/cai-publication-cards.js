var d=`
:host {
  /* neutrals \u2014 dark "graphite" */
  --bg: #15191e;
  --surface: #1c2127;
  --surface-2: #232a31;
  --border: #2d353e;
  --border-strong: #3a444f;
  --ink: #e4e9ed;
  --ink-soft: #b9c2cb;
  --muted: #8694a1;
  --heading: #f2f5f8;
  /* accent \u2014 watchdog "steel" is the family default */
  --accent: #7faace;
  --accent-ink: #9bbedb;
  --accent-wash: #1e2c39;
  --accent-strong: #b7d2e8;
  --on-accent: #15191e;
  /* bands (identical across products \u2014 the CAI vocabulary). dark = DarkHex. */
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
  /* CAI ladder marker \u2014 THEME-FIXED dark ink + explicit white casing. */
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
`;function n(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function p(a){if(a==null||a==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",t=0,o;for(;(o=r.exec(a))!==null;){o.index>t&&(e+=n(a.slice(t,o.index)));let s=o[0];if(s.startsWith("**"))e+=`<strong>${n(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))e+=`<code>${n(s.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);c?e+=`<a href="${n(c[2])}">${n(c[1])}</a>`:e+=n(s)}t=o.index+s.length}return t<a.length&&(e+=n(a.slice(t))),e}var l=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function m(a){let r=a.getAttribute("kicker"),e=a.getAttribute("heading"),t=a.getAttribute("lede");if(!r&&!e&&!t)return"";let o='<div class="mk-section-head">';return r&&(o+=`<span class="mk-kicker">${n(r)}</span>`),e&&(o+=`<h2>${p(e)}</h2>`),t&&(o+=`<p>${p(t)}</p>`),o+="</div>",o}var f=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,i=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#t(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let t=this.getAttribute(r);if(t==null||t.trim()==="")return e;try{return JSON.parse(t)}catch{return e}}};var b=d+l+f+`
/* auto-fill with a CAPPED track: a cover is never narrower than 17rem and never wider than
   20rem, so a row of two and a row of three carry the same card. The leftover width is a margin
   at the end of the row, not extra card. */
.mk-pubs { display: grid; gap: 22px; justify-content: start;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 20rem)); }

a.mk-pub { display: block; text-decoration: none; color: inherit;
  transition: transform 140ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-3px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 4px; }

.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: grid; overflow: hidden;
  border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 140ms ease, box-shadow 140ms ease; }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face {
  border-color: var(--accent); box-shadow: var(--shadow-overlay); }

.mk-pub-title { display: block; font-weight: 600; letter-spacing: -0.012em; color: var(--heading); }
.mk-pub-note { display: block; font-size: var(--fs-sm); line-height: 1.55; color: var(--ink-soft); }
.mk-pub-meta { display: block; font-size: var(--fs-xs); line-height: 1.5; color: var(--muted); }
.mk-pub-go { display: block; margin-top: 7px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); letter-spacing: 0.02em; color: var(--accent-ink);
  transition: color 140ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }
.mk-pub-series { font-size: var(--fs-2xs); font-weight: 700; letter-spacing: 0.1em;
  text-transform: uppercase; }

/* \u2500\u2500 the paper: a title page \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   Head, body, foot. The wash lifts from the masthead down, so the weight sits where a bound
   document announces itself, and the double rule under the band is the title-page convention
   that says "this is the front of something", not a decoration. */
.mk-pub.is-paper .mk-pub-face { grid-template-rows: auto 1fr auto;
  background: linear-gradient(180deg, var(--accent-wash) 0%, var(--surface) 76%); }
.mk-pub.is-paper .mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0;
  width: 5px; background: var(--accent); }
.mk-pub.is-paper .mk-pub-band { padding: 11px 18px 11px 22px; background: var(--accent); }
.mk-pub.is-paper .mk-pub-series { color: var(--on-accent); }
.mk-pub.is-paper .mk-pub-body { padding: 0 18px 0 22px; }
.mk-pub.is-paper .mk-pub-body::before { content: ""; display: block; height: 3px;
  border-top: 1px solid var(--border-strong); border-bottom: 1px solid var(--border-strong);
  margin: 16px 0 15px; }
.mk-pub.is-paper .mk-pub-title { font-size: clamp(1.1rem, 0.92rem + 0.55vw, 1.4rem); line-height: 1.24; }
.mk-pub.is-paper .mk-pub-note { margin-top: 11px; }
.mk-pub.is-paper .mk-pub-foot { padding: 12px 18px 15px 22px; border-top: 1px solid var(--hairline); }

/* \u2500\u2500 the article: a journal page \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   The series runs up the fore-edge in a ruled column of its own, which is where a bound
   periodical carries its name so a reader can find it side-on. That leaves the whole face for
   the title, and the short accent rule under the title is the paragraph opener a journal uses
   instead of a masthead. */
.mk-pub.is-article .mk-pub-face { grid-template-columns: 40px 1fr; grid-template-rows: 1fr auto;
  background: linear-gradient(200deg, var(--surface-2) 0%, var(--surface) 62%); }
.mk-pub.is-article .mk-pub-band { grid-row: 1 / span 2; display: flex; align-items: flex-end;
  justify-content: center; padding: 0 0 18px; border-right: 1px solid var(--accent); }
.mk-pub.is-article .mk-pub-series { writing-mode: vertical-rl; transform: rotate(180deg);
  color: var(--accent-ink); white-space: nowrap; }
.mk-pub.is-article .mk-pub-body { padding: 22px 20px 0 18px; }
.mk-pub.is-article .mk-pub-title { font-size: clamp(1.15rem, 0.95rem + 0.6vw, 1.5rem); line-height: 1.2; }
.mk-pub.is-article .mk-pub-title::after { content: ""; display: block; width: 34px; height: 2px;
  background: var(--accent); margin: 14px 0 0; }
.mk-pub.is-article .mk-pub-note { margin-top: 13px; }
.mk-pub.is-article .mk-pub-foot { padding: 12px 20px 15px 18px; }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;function h(a){let r=[a.byline,a.length,a.date].map(e=>e==null?"":String(e).trim()).filter(e=>e!=="");return r.length===0?"":r.join(" \xB7 ")}customElements.define("cai-publication-cards",class extends i{render(a){let r=(this.json("items",[])||[]).filter(t=>t&&t.href&&t.title),e="";if(r.length>0){e='<div class="mk-pubs">';for(let t of r){let o=String(t.tone||"").toLowerCase()==="article"?"article":"paper",s=h(t);e+=`<a class="mk-pub is-${o}" href="${n(t.href)}">`,e+='<span class="mk-pub-face">',t.series&&(e+=`<span class="mk-pub-band"><span class="mk-pub-series">${n(t.series)}</span></span>`),e+='<span class="mk-pub-body">',e+=`<span class="mk-pub-title">${n(t.title)}</span>`,t.note&&(e+=`<span class="mk-pub-note">${n(t.note)}</span>`),e+="</span>",e+='<span class="mk-pub-foot">',s!==""&&(e+=`<span class="mk-pub-meta">${n(s)}</span>`),t.go&&(e+=`<span class="mk-pub-go">${n(String(t.go))} \u2192</span>`),e+="</span></span></a>"}e+="</div>"}a.innerHTML=`<style>${b}</style>`+m(this)+e}});
