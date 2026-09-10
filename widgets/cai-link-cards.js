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
`;function i(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function c(a){if(a==null||a==="")return"";let o=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",t=0,n;for(;(n=o.exec(a))!==null;){n.index>t&&(e+=i(a.slice(t,n.index)));let r=n[0];if(r.startsWith("**"))e+=`<strong>${i(r.slice(2,-2))}</strong>`;else if(r.startsWith("`"))e+=`<code>${i(r.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(r);s?e+=`<a href="${i(s[2])}">${i(s[1])}</a>`:e+=i(r)}t=n.index+r.length}return t<a.length&&(e+=i(a.slice(t))),e}var h=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function f(a){let o=a.getAttribute("kicker"),e=a.getAttribute("heading"),t=a.getAttribute("lede");if(!o&&!e&&!t)return"";let n='<div class="mk-section-head">';return o&&(n+=`<span class="mk-kicker">${i(o)}</span>`),e&&(n+=`<h2>${c(e)}</h2>`),t&&(n+=`<p>${c(t)}</p>`),n+="</div>",n}var p=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,l=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let o=this.dataset.theme;this.#t(),this.dataset.theme!==o&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let o=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=o;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(o,e){let t=this.getAttribute(o);if(t==null||t.trim()==="")return e;try{return JSON.parse(t)}catch{return e}}};var m=`
.info-hint { position: relative; display: inline-flex; align-items: center; vertical-align: middle; margin-left: 6px; cursor: help; }
.info-hint:focus { outline: none; }
.info-hint-dot { width: 15px; height: 15px; border-radius: 50%; border: 1px solid var(--border-strong); color: var(--muted);
  font: italic 700 10px/1 Georgia, "Times New Roman", serif; display: grid; place-items: center; }
.info-hint:hover .info-hint-dot, .info-hint:focus .info-hint-dot { border-color: var(--accent); color: var(--accent); }
.info-hint-tip { position: absolute; left: 0; top: calc(100% + 8px); z-index: 60; width: max-content;
  max-width: min(320px, calc(100vw - 32px));
  background: var(--surface); color: var(--muted); border: 1px solid var(--border-strong); border-radius: 8px;
  box-shadow: var(--shadow-overlay); padding: 10px 12px; font-size: var(--fs-sm); font-weight: 400; line-height: 1.5;
  white-space: normal; text-align: left; opacity: 0; visibility: hidden; transform: translateY(-3px);
  transition: opacity .12s ease, transform .12s ease; pointer-events: none; }
.info-hint-tip a { pointer-events: auto; }
/* Paragraphs inside a tip sit tight \u2014 the UA's 1em block margins read as gaps in a small pop. */
.info-hint-tip > p { margin: 4px 0 0; }
.info-hint-tip > p:first-child { margin-top: 0; }
.info-hint-tip > p:last-child { margin-bottom: 0; }
.info-hint:hover .info-hint-tip, .info-hint:focus .info-hint-tip, .info-hint:focus-within .info-hint-tip {
  opacity: 1; visibility: visible; transform: none; pointer-events: auto; }
/* Flip to the right edge when the dot sits at the end of a row or the last column of a grid \u2014
   a tip anchored left:0 there hangs off the island, and in the last column off the page. */
.info-hint.hint-right .info-hint-tip { left: auto; right: 0; }

/* \u2500\u2500 Anchoring the tip to its SUBJECT rather than its dot \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   app.css carries a third variant, .hint-card, for the case neither left:0 nor right:0 can
   solve, and its comment states the reason: "the dot sits at the card's right edge, so any tip
   wider than the dot's offset overhangs the card's LEFT edge and, in the first column, the
   viewport. Spanning the card makes the tip exactly as wide as its subject, so it can never
   overflow at any column or width."

   An island hits that case constantly, because an absolutely positioned tip STILL COUNTS toward
   the document's scroll width even while it is hidden. A single 320px tip hanging off the last
   column of a four-up grid is enough to make the whole page scroll sideways at every viewport \u2014
   measured, not assumed: it was the only thing over the line at both 1280 and 400.

   app.css pins .hint-card's insets to one particular card's padding, which does not generalise.
   Here the geometry belongs to the SUBJECT \u2014 a grid cell, a table row, a figures row \u2014 so each
   island scopes these three declarations to its own container instead, and this comment is the
   one place that says why they all look the same:

     <subject> { position: relative; }              the tip's containing block
     <subject> .info-hint { position: static; }     so left/right resolve against the subject
     <subject> .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }

   The max-width is the whole trick: 320px where the subject has room for it, the subject's own
   width where it does not, and never a pixel past either. A subject that is itself inside the
   island cannot then push the page.

   One consequence to hold onto: a positioned subject paints its tip inside its own stacking
   order, so a LATER sibling row would draw over a tip that opens across it. Each island lifts
   the subject on :hover/:focus-within for that reason \u2014 see the z-index bumps at the call sites. */
@media (prefers-reduced-motion: reduce) { .info-hint-tip { transition: none; } }
`;function g(a,{right:o=!1,label:e="More information"}={}){let t=a==null?"":String(a).trim();if(t==="")return"";let n=t.split(/\n\s*\n/).map(s=>s.trim()).filter(s=>s!=="").map(s=>`<p>${c(s)}</p>`).join("");return`<span class="${o?"info-hint hint-right":"info-hint"}" tabindex="0" role="note" aria-label="${i(e||"More information")}"><span class="info-hint-dot" aria-hidden="true">i</span><span class="info-hint-tip">${n}</span></span>`}var b="/brand/canine-badge.svg",k=d+h+p+m+`
/* Two columns, so the usual four land as a 2x2 block rather than a row of three and an
   orphan. minmax keeps it one column when there is no room for two. */
.mk-links { max-width: 46rem; margin: 0 auto; display: grid; gap: 10px;
  grid-template-columns: repeat(auto-fit, minmax(19rem, 1fr)); }
a.mk-link { display: grid; grid-template-columns: auto 1fr; gap: 2px 12px; align-items: start;
  padding: 14px 16px; text-decoration: none; color: inherit; background: var(--surface);
  border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 120ms ease, background 120ms ease, transform 120ms ease; }
a.mk-link:hover, a.mk-link:focus-visible { border-color: var(--accent); background: var(--surface-2);
  transform: translateY(-1px); text-decoration: none; }
a.mk-link:hover .mk-link-go, a.mk-link:focus-visible .mk-link-go { color: var(--accent); }
.mk-link-ico { grid-row: 1 / span 2; width: 26px; height: 26px; display: block; flex: none; }
.mk-link-ico svg { width: 100%; height: 100%; display: block; }
.mk-link-badge { width: 24px; height: 26px; display: block;
  -webkit-mask: var(--badge) no-repeat center / contain; mask: var(--badge) no-repeat center / contain; }
.mk-link-badge.is-watchdog { background: var(--wd-accent); }
.mk-link-badge.is-cai { background: var(--cai-accent); }
/* Every text row is pinned to column 2. The icon reserves only rows 1-2, so once a card can
   carry four rows (label, figure, note, go) auto-placement would drop the fourth into column 1
   under the icon. Pinning is what keeps a three-field card and a plain one the same shape. */
.mk-link-label-row, .mk-link-label, .mk-link-figure, .mk-link-note, .mk-link-go { grid-column: 2; }
.mk-link-label { font-weight: 650; font-size: var(--fs-sm); line-height: 1.4; }
/* A long forge path breaks between segments rather than mid-word: "\u2026/Captur a" reads as a
   typo, and these notes are addresses a reader may want to recognise. */
.mk-link-note { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5;
  word-break: break-word; overflow-wrap: break-word; }
/* The label and its (i) share one grid cell, the dot pushed to the card's right edge \u2014 which is
   why the tip is hint-right. */
.mk-link-label-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; min-width: 0; }
.mk-link-figure { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 22px; font-weight: 700; color: var(--ink); line-height: 1.15; }
.mk-link-go { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-2xs); color: var(--muted); padding-top: 2px;
  transition: color 120ms ease; }
:host { --wd-accent: #7faace; --cai-accent: #6fbfa4; }
:host([data-theme="light"]) { --wd-accent: #35618a; --cai-accent: #2e7d64; }
@media (prefers-reduced-motion: reduce) {
  a.mk-link { transition: none; }
  a.mk-link:hover, a.mk-link:focus-visible { transform: none; }
  .mk-link-go { transition: none; }
}
`,u={github:'<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"/></svg>',gitlab:'<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="m15.73 6.49-.02-.06-2.17-5.66a.57.57 0 0 0-.22-.27.58.58 0 0 0-.88.27L10.98 5.2H5.03L3.57.77a.58.58 0 0 0-.88-.27.57.57 0 0 0-.22.27L.3 6.43l-.02.06a4.03 4.03 0 0 0 1.34 4.65l.01.01.02.01 3.3 2.47 1.63 1.23.99.75a.68.68 0 0 0 .82 0l.99-.75 1.64-1.23 3.32-2.48.01-.01a4.03 4.03 0 0 0 1.34-4.65Z"/></svg>',html:'<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M1.5 0h21l-1.91 21.563L11.977 24l-8.564-2.438L1.5 0zm7.031 9.75l-.232-2.718 10.059.003.23-2.622L5.412 4.41l.698 8.01h9.126l-.326 3.426-2.91.804-2.955-.81-.188-2.11H6.248l.33 4.171L12 19.351l5.379-1.443.744-8.157H8.531z"/></svg>',doc:'<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9.2 1.4H4a1.6 1.6 0 0 0-1.6 1.6v10A1.6 1.6 0 0 0 4 14.6h8a1.6 1.6 0 0 0 1.6-1.6V5.8Z"/><path d="M9.2 1.4v4.4h4.4"/><path d="M5.6 8.4h4.8M5.6 11h4.8"/></svg>'};customElements.define("cai-link-cards",class extends l{render(a){let o=(this.json("links",[])||[]).filter(t=>t&&t.href&&t.label),e=`<style>${k}</style>`;if(e+=f(this),o.length===0){a.innerHTML=e;return}e+='<div class="mk-links">';for(let t of o){let n=String(t.icon||"").toLowerCase();e+=`<a class="mk-link" href="${i(t.href)}" rel="noopener noreferrer">`,e+='<span class="mk-link-ico" aria-hidden="true">',n==="watchdog"||n==="cai"?e+=`<span class="mk-link-badge is-${n}" style="--badge:url('${b}')"></span>`:e+=u[n]||u.doc,e+="</span>";let r=g(t.tip,{right:!0,label:t.label});e+=r?`<span class="mk-link-label-row"><span class="mk-link-label">${i(t.label)}</span>${r}</span>`:`<span class="mk-link-label">${i(t.label)}</span>`,t.figure!=null&&String(t.figure)!==""&&(e+=`<span class="mk-link-figure">${i(String(t.figure))}</span>`),e+=`<span class="mk-link-note">${i(t.note||"")}</span>`,t.go!=null&&String(t.go)!==""&&(e+=`<span class="mk-link-go">${i(String(t.go))} \u2192</span>`),e+="</a>"}e+="</div>",a.innerHTML=e}});
