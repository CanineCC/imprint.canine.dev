var l=`
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
`;function n(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function d(a){if(a==null||a==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",t=0,s;for(;(s=r.exec(a))!==null;){s.index>t&&(e+=n(a.slice(t,s.index)));let o=s[0];if(o.startsWith("**"))e+=`<strong>${n(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${n(o.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);c?e+=`<a href="${n(c[2])}">${n(c[1])}</a>`:e+=n(o)}t=s.index+o.length}return t<a.length&&(e+=n(a.slice(t))),e}var p=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function f(a){let r=a.getAttribute("kicker"),e=a.getAttribute("heading"),t=a.getAttribute("lede");if(!r&&!e&&!t)return"";let s='<div class="mk-section-head">';return r&&(s+=`<span class="mk-kicker">${n(r)}</span>`),e&&(s+=`<h2>${d(e)}</h2>`),t&&(s+=`<p>${d(t)}</p>`),s+="</div>",s}var h=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,i=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#t(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let t=this.getAttribute(r);if(t==null||t.trim()==="")return e;try{return JSON.parse(t)}catch{return e}}};var m=l+p+h+`
/* The track floor is max(18rem, (100% - two gaps) / 3): 18rem where the column is narrow, and a
   full third of the row once a third would be wider than that \u2014 which caps the shelf at three
   without hard-coding a count that would then never collapse. */
.mk-pubs { display: grid; gap: 18px;
  grid-template-columns: repeat(auto-fit, minmax(max(18rem, (100% - 36px) / 3), 1fr)); }

a.mk-pub { display: flex; flex-direction: column; gap: 12px; text-decoration: none; color: inherit;
  border-radius: var(--r-md); transition: transform 120ms ease; }
a.mk-pub:hover, a.mk-pub:focus-visible { text-decoration: none; transform: translateY(-2px); }
a.mk-pub:focus-visible { outline: 2px solid var(--accent); outline-offset: 3px; }

/* The face. The spine is a solid bar down the binding edge: it is what makes a rectangle of type
   read as a document rather than a panel, and it takes the tone colour so the shelf a card
   belongs to is legible before the words are. */
.mk-pub-face { position: relative; aspect-ratio: 3 / 4; display: flex; flex-direction: column;
  padding: 22px 20px 20px 26px; border: 1px solid var(--border); border-radius: var(--r-md);
  background: var(--pub-wash); overflow: hidden;
  transition: border-color 120ms ease, background 120ms ease; }
.mk-pub-face::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 5px;
  background: var(--pub-spine); }
a.mk-pub:hover .mk-pub-face, a.mk-pub:focus-visible .mk-pub-face { border-color: var(--pub-spine); }

.mk-pub-series { font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em;
  text-transform: uppercase; color: var(--muted); }
.mk-pub-rule { height: 1px; background: var(--border-strong); margin: 12px 0 14px; }
/* The title is the cover. It is allowed to be big and to wrap as far as it needs; the face grows
   no taller because the aspect ratio is fixed, so a long title simply fills more of it. */
.mk-pub-title { font-size: clamp(1.15rem, 0.95rem + 0.8vw, 1.6rem); line-height: 1.22;
  font-weight: 600; letter-spacing: -0.01em; color: var(--heading); }
/* The go line sits on the baseline of the face, where the strapline sits on a real cover. */
.mk-pub-go { margin-top: auto; padding-top: 14px; font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); transition: color 120ms ease; }
a.mk-pub:hover .mk-pub-go, a.mk-pub:focus-visible .mk-pub-go { color: var(--accent); }

.mk-pub-note { font-size: var(--fs-sm); line-height: 1.5; color: var(--ink-soft); }
.mk-pub-meta { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }

:host { --pub-wash: var(--surface); --pub-spine: var(--accent); }
.mk-pub.is-paper { --pub-wash: var(--accent-wash); --pub-spine: var(--accent); }
.mk-pub.is-article { --pub-wash: var(--surface-2); --pub-spine: var(--muted); }

/* The article face, same box and same spans, set from the baseline up: no rule under the series,
   and the title carries the auto margin instead of the strapline so the pair falls to the foot of
   the cover together. */
.mk-pub.is-article .mk-pub-rule { display: none; }
.mk-pub.is-article .mk-pub-title { margin-top: auto; }
.mk-pub.is-article .mk-pub-go { margin-top: 0; padding-top: 10px; }

@media (prefers-reduced-motion: reduce) {
  a.mk-pub, .mk-pub-face, .mk-pub-go { transition: none; }
  a.mk-pub:hover, a.mk-pub:focus-visible { transform: none; }
}
`;function b(a){let r=[a.byline,a.length,a.date].map(e=>e==null?"":String(e).trim()).filter(e=>e!=="");return r.length===0?"":r.join(" \xB7 ")}customElements.define("cai-publication-cards",class extends i{render(a){let r=(this.json("items",[])||[]).filter(t=>t&&t.href&&t.title),e="";if(r.length>0){e='<div class="mk-pubs">';for(let t of r){let s=String(t.tone||"").toLowerCase()==="article"?"article":"paper",o=b(t);e+=`<a class="mk-pub is-${s}" href="${n(t.href)}">`,e+='<span class="mk-pub-face">',t.series&&(e+=`<span class="mk-pub-series">${n(t.series)}</span>`,e+='<span class="mk-pub-rule"></span>'),e+=`<span class="mk-pub-title">${n(t.title)}</span>`,t.go&&(e+=`<span class="mk-pub-go">${n(String(t.go))} \u2192</span>`),e+="</span>",t.note&&(e+=`<span class="mk-pub-note">${n(t.note)}</span>`),o!==""&&(e+=`<span class="mk-pub-meta">${n(o)}</span>`),e+="</a>"}e+="</div>"}a.innerHTML=`<style>${m}</style>`+f(this)+e}});
