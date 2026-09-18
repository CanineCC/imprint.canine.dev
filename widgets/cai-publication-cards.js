var p=`
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
`;function n(t){return String(t??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function d(t){if(t==null||t==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",a=0,s;for(;(s=r.exec(t))!==null;){s.index>a&&(e+=n(t.slice(a,s.index)));let o=s[0];if(o.startsWith("**"))e+=`<strong>${n(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${n(o.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);c?e+=`<a href="${n(c[2])}">${n(c[1])}</a>`:e+=n(o)}a=s.index+o.length}return a<t.length&&(e+=n(t.slice(a))),e}var l=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function f(t){let r=t.getAttribute("kicker"),e=t.getAttribute("heading"),a=t.getAttribute("lede");if(!r&&!e&&!a)return"";let s='<div class="mk-section-head">';return r&&(s+=`<span class="mk-kicker">${n(r)}</span>`),e&&(s+=`<h2>${d(e)}</h2>`),a&&(s+=`<p>${d(a)}</p>`),s+="</div>",s}var h=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,i=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#a(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let a=this.getAttribute(r);if(a==null||a.trim()==="")return e;try{return JSON.parse(a)}catch{return e}}};var m=p+l+h+`
/* auto-fill with a CAPPED track: a cover is never narrower than 17rem and never wider than
   20rem, so a row of two and a row of three carry the same card. The leftover width is a margin
   at the end of the row, not extra card. */
.mk-pubs { display: grid; gap: 22px; justify-content: start;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 20rem)); }

a.mk-pub { display: block; text-decoration: none; color: inherit;
  transition: transform 140ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-3px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 4px; }

/* The face IS the card: a page, not a panel. The wash lifts from the head so the masthead sits
   on the darker end and the imprint on the lighter, which is the way a printed page carries its
   weight. */
.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: flex; flex-direction: column;
  border: 1px solid var(--border); border-radius: var(--r-md); overflow: hidden;
  background: linear-gradient(180deg, var(--pub-wash) 0%, var(--surface) 78%);
  transition: border-color 140ms ease, box-shadow 140ms ease; }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face {
  border-color: var(--accent); box-shadow: var(--shadow-overlay); }

/* The spine: a solid bar down the binding edge, on the bound shelf only. */
.mk-pub.is-paper .mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0;
  width: 5px; background: var(--accent); }

/* The masthead. Solid on a paper, hollow on an article: the one place the two shelves differ. */
.mk-pub-band { padding: 11px 18px 11px 22px; }
.mk-pub.is-paper .mk-pub-band { background: var(--accent); }
.mk-pub.is-article .mk-pub-band { border-bottom: 1px solid var(--accent); }
.mk-pub-series { font-size: var(--fs-2xs); font-weight: 700; letter-spacing: 0.1em;
  text-transform: uppercase; }
.mk-pub.is-paper .mk-pub-series { color: var(--on-accent); }
.mk-pub.is-article .mk-pub-series { color: var(--accent-ink); }

.mk-pub-body { padding: 18px 18px 0 22px; }
/* The title is the cover. It may be big and wrap as far as it needs; the face grows no taller,
   because the aspect ratio is fixed, so a long title simply fills more of it. */
.mk-pub-title { display: block; font-size: clamp(1.1rem, 0.92rem + 0.55vw, 1.4rem);
  line-height: 1.24; font-weight: 600; letter-spacing: -0.012em; color: var(--heading); }
.mk-pub-note { display: block; margin-top: 11px; font-size: var(--fs-sm); line-height: 1.55;
  color: var(--ink-soft); }

/* The imprint, on the baseline under a hairline, where a title page carries it. */
.mk-pub-foot { margin-top: auto; padding: 12px 18px 15px 22px; border-top: 1px solid var(--hairline); }
.mk-pub-meta { display: block; font-size: var(--fs-xs); line-height: 1.5; color: var(--muted); }
.mk-pub-go { display: block; margin-top: 7px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); letter-spacing: 0.02em; color: var(--accent-ink);
  transition: color 140ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }

:host { --pub-wash: var(--surface-2); }
.mk-pub.is-paper { --pub-wash: var(--accent-wash); }
.mk-pub.is-article { --pub-wash: var(--surface-2); }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;function b(t){let r=[t.byline,t.length,t.date].map(e=>e==null?"":String(e).trim()).filter(e=>e!=="");return r.length===0?"":r.join(" \xB7 ")}customElements.define("cai-publication-cards",class extends i{render(t){let r=(this.json("items",[])||[]).filter(a=>a&&a.href&&a.title),e="";if(r.length>0){e='<div class="mk-pubs">';for(let a of r){let s=String(a.tone||"").toLowerCase()==="article"?"article":"paper",o=b(a);e+=`<a class="mk-pub is-${s}" href="${n(a.href)}">`,e+='<span class="mk-pub-face">',a.series&&(e+=`<span class="mk-pub-band"><span class="mk-pub-series">${n(a.series)}</span></span>`),e+='<span class="mk-pub-body">',e+=`<span class="mk-pub-title">${n(a.title)}</span>`,a.note&&(e+=`<span class="mk-pub-note">${n(a.note)}</span>`),e+="</span>",e+='<span class="mk-pub-foot">',o!==""&&(e+=`<span class="mk-pub-meta">${n(o)}</span>`),a.go&&(e+=`<span class="mk-pub-go">${n(String(a.go))} \u2192</span>`),e+="</span></span></a>"}e+="</div>"}t.innerHTML=`<style>${m}</style>`+f(this)+e}});
