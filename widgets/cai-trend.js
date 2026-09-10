var z=`
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
`;function o(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function p(e){if(e==null||e==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",a=0,i;for(;(i=n.exec(e))!==null;){i.index>a&&(t+=o(e.slice(a,i.index)));let r=i[0];if(r.startsWith("**"))t+=`<strong>${o(r.slice(2,-2))}</strong>`;else if(r.startsWith("`"))t+=`<code>${o(r.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(r);s?t+=`<a href="${o(s[2])}">${o(s[1])}</a>`:t+=o(r)}a=i.index+r.length}return a<e.length&&(t+=o(e.slice(a))),t}var T=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function E(e){let n=e.getAttribute("kicker"),t=e.getAttribute("heading"),a=e.getAttribute("lede");if(!n&&!t&&!a)return"";let i='<div class="mk-section-head">';return n&&(i+=`<span class="mk-kicker">${o(n)}</span>`),t&&(i+=`<h2>${p(t)}</h2>`),a&&(i+=`<p>${p(a)}</p>`),i+="</div>",i}var N=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,w=class extends HTMLElement{#t;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#t=new MutationObserver(()=>{let n=this.dataset.theme;this.#e(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#t.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#t?.disconnect()}#e(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(n,t){let a=this.getAttribute(n);if(a==null||a.trim()==="")return t;try{return JSON.parse(a)}catch{return t}}};var x=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function y(e){return e>=90?x[4]:e>=70?x[3]:e>=50?x[2]:e>=25?x[1]:x[0]}var j=`
.ink-exemplary { color: var(--band-exemplary-text); }
.ink-healthy { color: var(--band-healthy-text); }
.ink-fair { color: var(--band-fair-text); }
.ink-poor { color: var(--band-poor-text); }
.ink-critical { color: var(--band-critical-text); }
.fill-exemplary { background: var(--band-exemplary); }
.fill-healthy { background: var(--band-healthy); }
.fill-fair { background: var(--band-fair); }
.fill-poor { background: var(--band-poor); }
.fill-critical { background: var(--band-critical); }

.cai-card {
  position: relative; display: block; width: 100%; max-width: 460px;
  background: var(--surface); border: 1.5px solid var(--accent); border-radius: 16px;
  padding: 20px 22px; box-shadow: var(--shadow-overlay); color: var(--ink);
}
a.cai-card { color: var(--ink); }
a.cai-card:hover { text-decoration: none; border-color: var(--accent-strong); }
.cai-seal { position: absolute; top: -13px; right: 20px; background: var(--accent-strong); color: var(--on-accent); font-size: var(--fs-2xs); font-weight: 650; letter-spacing: 0.04em; padding: 5px 11px; border-radius: var(--r-full); }
.cai-card-cap { max-width: 460px; margin: 0.85rem 0 0; font-size: var(--fs-xs); color: var(--muted); text-align: center; line-height: 1.5; }

.cai-top { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.cai-name { min-width: 0; line-height: 1.25; }
.cai-repo { display: block; font-weight: 600; font-size: 15px; color: var(--heading); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cai-owner { display: block; color: var(--muted); font-weight: 400; font-size: var(--fs-xs); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cai-chip { display: inline-flex; align-items: center; font-size: var(--fs-xs); font-weight: 600; line-height: 1.4; border-radius: var(--r-full); padding: 2px 10px; white-space: nowrap; flex: none; }
.cai-chip.band-exemplary { background: color-mix(in srgb, var(--band-exemplary) 16%, transparent); color: var(--band-exemplary-text); }
.cai-chip.band-healthy { background: color-mix(in srgb, var(--band-healthy) 16%, transparent); color: var(--band-healthy-text); }
.cai-chip.band-fair { background: color-mix(in srgb, var(--band-fair) 16%, transparent); color: var(--band-fair-text); }
.cai-chip.band-poor { background: color-mix(in srgb, var(--band-poor) 16%, transparent); color: var(--band-poor-text); }
.cai-chip.band-critical { background: color-mix(in srgb, var(--band-critical) 16%, transparent); color: var(--band-critical-text); }

.cai-scoreline { margin-top: 6px; }
.cai-cai { font: 700 var(--fs-xs)/1 var(--font-ui); letter-spacing: 0.08em; color: var(--muted); margin-right: 8px; vertical-align: 6px; }
.cai-score { font-size: 44px; font-weight: 700; line-height: 1.1; letter-spacing: -0.02em; font-variant-numeric: tabular-nums lining-nums; }
.cai-unit { font-size: var(--fs-lg); color: var(--muted); font-weight: 400; }
.cai-muted { color: var(--muted); }

.cai-ladder { --mk-foot: 9px; margin: 6px 0 2px; }
.cai-card .cai-ladder { margin: 14px 0 12px; }
.cai-rail { position: relative; height: 11px; overflow: visible; }
.cai-segs { display: flex; height: 11px; border-radius: 6px; overflow: hidden; }
.cai-segs > i { flex: 1; display: block; }
.cai-segs > i.seg-critical { background: var(--band-critical); }
.cai-segs > i.seg-poor { background: var(--band-poor); }
.cai-segs > i.seg-fair { background: var(--band-fair); }
.cai-segs > i.seg-healthy { background: var(--band-healthy); }
.cai-segs > i.seg-exemplary { background: var(--band-exemplary); }
.cai-caps { display: flex; justify-content: space-between; font-size: var(--fs-2xs); color: var(--muted); margin-top: 9px; }
.cai-ladder.compact .cai-caps { display: none; }
.cai-mk { position: absolute; top: 0; bottom: 0; width: 0; z-index: 3; pointer-events: none; color: var(--mk); }
.cai-diamond .cai-diamond-foot {
  position: absolute; top: 50%; left: 0; width: 14px; height: 14px;
  transform: translate(-50%, -50%) rotate(45deg);
  background: var(--dia, var(--mk-on)); border: 2.5px solid var(--mk-on);
  border-radius: 2px; box-shadow: 0 1px 4px rgb(15 25 20 / 0.45);
}
.cai-diamond::before {
  content: ""; position: absolute; left: 0; bottom: calc(50% + 6px); width: 2px; height: 10px;
  transform: translateX(-50%); background: var(--dia, var(--mk)); border-radius: 1px 1px 0 0;
  box-shadow: 0 0 0 1px var(--mk-on);
}
.cai-pin .cai-pin-foot {
  position: absolute; top: 50%; left: 0; width: var(--mk-foot); height: var(--mk-foot);
  transform: translate(-50%, -50%) rotate(45deg); background: var(--mk); box-shadow: 0 0 0 2px var(--mk-on);
}
.cai-pin .cai-pin-line {
  position: absolute; bottom: 50%; left: 0; width: 3px; height: 12px; transform: translateX(-50%);
  background: var(--mk); border-radius: 2px 2px 0 0; box-shadow: 0 0 0 1.5px var(--mk-on);
}
.cai-pin .cai-pin-badge {
  position: absolute; bottom: calc(50% + 12px); left: 0; transform: translateX(-50%);
  min-width: 25px; height: 22px; padding: 0 7px; display: flex; align-items: center; justify-content: center;
  background: var(--mk); color: var(--mk-on); font: 700 13px/1 var(--font-ui); border-radius: 6px; white-space: nowrap;
  box-shadow: 0 0 0 2px var(--mk-on), 0 2px 5px rgb(20 40 30 / 0.3);
}
.cai-pin .cai-pin-badge::after {
  content: ""; position: absolute; top: 100%; left: 50%; transform: translateX(-50%);
  border: 5px solid transparent; border-top-color: var(--mk);
}

.cai-spark { width: 100%; height: 36px; display: block; margin: 2px 0 4px; }
.cai-arc { display: flex; align-items: baseline; gap: 8px; margin: 2px 0; }
.cai-arc-from { color: var(--muted); font-size: 17px; font-weight: 700; font-variant-numeric: tabular-nums; }
.cai-arc-arrow { color: var(--muted); }
.cai-arc-to { font-size: 24px; font-weight: 700; font-variant-numeric: tabular-nums; }
.cai-arc-up { margin-left: auto; color: var(--band-exemplary-text); font-size: var(--fs-md); font-weight: 700; }

.cai-lenses { display: grid; gap: 7px; margin-top: 14px; }
.cai-lens { display: grid; grid-template-columns: 92px 1fr 30px; align-items: center; gap: 10px; font-size: var(--fs-xs); }
.cai-lens-name { color: var(--ink-soft); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.cai-lens-bar { display: block; height: 7px; border-radius: var(--r-full); background: var(--surface-2); overflow: hidden; }
.cai-lens-fill { display: block; height: 100%; border-radius: var(--r-full); }
.cai-lens-num { text-align: right; font-weight: 600; font-variant-numeric: tabular-nums; }

.cai-rows { margin-top: 14px; border-top: 1px solid var(--border); padding-top: 4px; }
.cai-row { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; font-size: var(--fs-sm); padding: 6px 0; border-bottom: 1px dashed var(--hairline); color: var(--muted); }
.cai-row:last-child { border-bottom: 0; }
.cai-row b { color: var(--heading); font-weight: 600; text-align: right; }
.cai-row .mono { font-family: var(--font-mono); font-size: var(--fs-xs); }
`;var B=`
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
`;function L(e,{right:n=!1,label:t="More information"}={}){let a=e==null?"":String(e).trim();if(a==="")return"";let i=a.split(/\n\s*\n/).map(s=>s.trim()).filter(s=>s!=="").map(s=>`<p>${p(s)}</p>`).join("");return`<span class="${n?"info-hint hint-right":"info-hint"}" tabindex="0" role="note" aria-label="${o(t||"More information")}"><span class="info-hint-dot" aria-hidden="true">i</span><span class="info-hint-tip">${i}</span></span>`}var I=`
.rail { display: grid; grid-template-columns: 112px minmax(0, 1fr); gap: 0 24px;
  align-items: start; position: relative; }
.rail-side { display: flex; align-items: flex-start; padding-top: 3px; min-width: 0; }
.rail-kicker { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
  color: var(--muted); font-weight: 700; overflow-wrap: anywhere; }
.rail-body { min-width: 0; }
/* The (i)'s subject is the rail, so its tip can never reach past the island. See hint.js. */
.rail .info-hint { position: static; }
.rail .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
/* A tip opens across the content beside it, which would otherwise paint over it. */
.rail:hover, .rail:focus-within { z-index: 2; }
/* The label takes its own line under 560px \u2014 the same width at which cai-share-bars reflows,
   so two adjacent sections never disagree about when a page has become narrow. A 112px column
   plus a 24px gutter is a third of a 400px screen spent on two words. */
@media (max-width: 560px) {
  .rail { grid-template-columns: minmax(0, 1fr); gap: 10px; }
  .rail-side { padding-top: 0; }
}
`;function R({kicker:e,tip:n,content:t}){return`<div class="rail"><div class="rail-side"><span class="rail-kicker">${o(e)}</span>`+L(n,{label:e})+`</div><div class="rail-body">${t}</div></div>`}function _(e){let n=e.getAttribute("heading"),t=e.getAttribute("lede");if(!n&&!t)return"";let a='<div class="mk-section-head">';return n&&(a+=`<h2>${p(n)}</h2>`),t&&(a+=`<p>${p(t)}</p>`),a+="</div>",a}var v=[0,25,50,70,90,100],O=.05;function U(e){let n=[],t=0;for(;t<e.length;){let a=t;for(;a+1<e.length&&Math.abs(e[a+1]-e[t])<O;)a++;let i=a-t+1;n.push({i:t,v:e[t],run:i}),a>t&&n.push({i:a,v:e[a],run:i}),t=a+1}return n}var $=720,S=240,d={top:26,right:18,bottom:34,left:40},q=z+T+N+j+B+I+`
.mk-trend { max-width: 46rem; margin: 0 auto; }
.mk-trend-plot { position: relative; }
.mk-trend svg { display: block; width: 100%; height: auto; overflow: visible; }
.mk-trend-grid { stroke: var(--border); stroke-width: 1; }
.mk-trend-cut { fill: var(--muted); font-family: var(--font-mono); font-size: 11px; }
.mk-trend-line { fill: none; stroke: var(--accent); stroke-width: 2;
  stroke-linejoin: round; stroke-linecap: round; }
.mk-trend-area { fill: var(--accent); opacity: 0.10; }
.mk-trend-dot { fill: var(--accent); stroke: var(--bg); stroke-width: 2; }
.mk-trend-end { stroke: var(--bg); stroke-width: 2.5; }
/* The shared .fill-* classes set the background property, which an SVG circle ignores. */
.mk-trend-end.fill-exemplary { fill: var(--band-exemplary); }
.mk-trend-end.fill-healthy { fill: var(--band-healthy); }
.mk-trend-end.fill-fair { fill: var(--band-fair); }
.mk-trend-end.fill-poor { fill: var(--band-poor); }
.mk-trend-end.fill-critical { fill: var(--band-critical); }
.mk-trend-endlabel { font-family: var(--font-mono); font-size: 15px; font-weight: 700; }
.mk-trend-date { fill: var(--muted); font-size: 12px; }
.mk-trend-hit { fill: transparent; cursor: default; }
.mk-trend-hit:hover + .mk-trend-dot, .mk-trend-hit:focus + .mk-trend-dot { stroke: var(--accent-strong); }
.mk-trend-tip { position: absolute; transform: translate(-50%, -100%);
  background: var(--surface-2); border: 1px solid var(--border-strong); border-radius: var(--r-sm);
  box-shadow: var(--shadow-overlay); padding: 6px 10px; pointer-events: none; white-space: nowrap;
  font-size: var(--fs-xs); color: var(--ink); opacity: 0; transition: opacity 90ms ease; }
.mk-trend-tip.on { opacity: 1; }
.mk-trend-tip b { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.mk-trend-solo { display: flex; align-items: baseline; justify-content: center; gap: 0.6rem;
  padding: 1.6rem 0 0.4rem; }
.mk-trend-solo-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-4xl); line-height: 1; }
.mk-trend-solo-date { font-size: var(--fs-sm); color: var(--muted); }
.mk-trend-sum { margin: 0.9rem auto 0; max-width: 46rem; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; text-align: center; }

/* The movement's endpoints, stated. auto-fit collapses them to one column on a phone without a
   media query \u2014 two figures side by side in 368px would each get 184px for a sentence. */
.mk-trend-figs { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
  gap: 18px 28px; margin: 1.1rem auto 0; max-width: 46rem; }
.mk-trend-fig { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.mk-trend-fig-value { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 22px; font-weight: 700; line-height: 1.1; color: var(--ink); overflow-wrap: anywhere; }
.mk-trend-fig-label { font-size: var(--fs-sm); color: var(--muted); line-height: 1.45; }
.mk-trend-fig-sub { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-2xs); color: var(--muted); }

/* \u2500\u2500 inside the rail \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   The chart's 46rem measure and its auto margins are a CARD centred on a page that has no other
   column. In the rail the content column IS the measure, and a 736px card inside a 950px column
   is a second, narrower page drawn inside the first. These are descendant selectors rather than
   a modifier class on purpose: the no-kicker markup then cannot change at all, which is what the
   snapshot tests assert. */
.rail-body .mk-trend, .rail-body .mk-trend-figs { max-width: none; margin-left: 0; margin-right: 0; }
.rail-body .mk-trend-sum { max-width: none; margin-left: 0; margin-right: 0; text-align: left; }
@media (prefers-reduced-motion: reduce) { .mk-trend-tip { transition: none; } }
`;function P(e){let n=Math.min(...e),t=Math.max(...e),a=0,i=100;for(let r of v)r<=n&&(a=r);for(let r=v.length-1;r>=0;r--)v[r]>=t&&(i=v[r]);return i-a<10&&(a=Math.max(0,Math.min(a,i-25))),i-a<10&&(i=Math.min(100,a+25)),{min:a,max:i}}function g(e){return(Math.round(e*10)/10).toFixed(1)}customElements.define("cai-trend",class extends w{render(e){let n=(this.json("series",[])||[]).map(Number).filter(r=>Number.isFinite(r)),t=(this.getAttribute("kicker")||"").trim(),a=this.plotHtml(n)+this.figuresHtml(),i=`<style>${q}</style>`;i+=t?R({kicker:t,tip:this.getAttribute("tip"),content:_(this)+a}):E(this)+a,e.innerHTML=i,n.length>1&&this.wireTips(e,n)}figuresHtml(){let e=(this.json("figures",[])||[]).filter(t=>t&&t.value!=null&&String(t.value)!=="");if(e.length===0)return"";let n='<div class="mk-trend-figs">';for(let t of e)n+='<div class="mk-trend-fig">',n+=`<span class="mk-trend-fig-value">${o(String(t.value))}</span>`,t.label!=null&&String(t.label)!==""&&(n+=`<span class="mk-trend-fig-label">${o(String(t.label))}</span>`),t.sub!=null&&String(t.sub)!==""&&(n+=`<span class="mk-trend-fig-sub">${o(String(t.sub))}</span>`),n+="</div>";return n+"</div>"}plotHtml(e){let n=this.getAttribute("first-date"),t=this.getAttribute("last-date"),a=this.getAttribute("caption");if(e.length===0)return"";if(e.length===1){let l=e[0],b=y(l),u='<div class="mk-trend"><p class="mk-trend-solo">';return u+=`<span class="mk-trend-solo-num ink-${b.key}">${g(l)}</span>`,(t||n)&&(u+=`<span class="mk-trend-solo-date">measured ${o(t||n)}</span>`),u+="</p></div>",a&&(u+=`<p class="mk-trend-sum">${p(a)}</p>`),u}let{min:i,max:r}=P(e),s=$-d.left-d.right,A=S-d.top-d.bottom,h=l=>d.left+s*l/(e.length-1),f=l=>d.top+A*(1-(l-i)/(r-i)),m=e[e.length-1],k=y(m),C=U(e),H=C.map(l=>`${h(l.i).toFixed(1)},${f(l.v).toFixed(1)}`),c='<div class="mk-trend"><div class="mk-trend-plot">';c+=`<svg viewBox="0 0 ${$} ${S}" role="img" aria-label="${o(`${e.length} measurements, from ${g(e[0])} to ${g(m)}.`)}">`;for(let l of v){if(l<i||l>r)continue;let b=f(l).toFixed(1);c+=`<line class="mk-trend-grid" x1="${d.left}" y1="${b}" x2="${$-d.right}" y2="${b}"></line>`,c+=`<text class="mk-trend-cut" x="${d.left-8}" y="${b}" text-anchor="end" dominant-baseline="middle">${l}</text>`}c+=`<path class="mk-trend-area" d="M${h(0).toFixed(1)},${f(i).toFixed(1)} L${H.join(" L")} L${h(e.length-1).toFixed(1)},${f(i).toFixed(1)} Z"></path>`,c+=`<polyline class="mk-trend-line" points="${H.join(" ")}" vector-effect="non-scaling-stroke"></polyline>`,n&&(c+=`<text class="mk-trend-date" x="${d.left}" y="${S-10}" text-anchor="start">${o(n)}</text>`),t&&(c+=`<text class="mk-trend-date" x="${$-d.right}" y="${S-10}" text-anchor="end">${o(t)}</text>`),C.forEach(l=>{let b=l.i===e.length-1,u=h(l.i).toFixed(1),F=f(l.v).toFixed(1);c+=`<circle class="mk-trend-hit" cx="${u}" cy="${F}" r="18" tabindex="0" data-i="${l.i}" data-v="${g(l.v)}" data-run="${l.run}"></circle>`,c+=b?`<circle class="mk-trend-end fill-${k.key}" cx="${u}" cy="${F}" r="5.5"></circle>`:`<circle class="mk-trend-dot" cx="${u}" cy="${F}" r="4"></circle>`}),c+=`<text class="mk-trend-endlabel ink-${k.key}" x="${h(e.length-1).toFixed(1)}" y="${(f(m)-14).toFixed(1)}" text-anchor="end">${g(m)}</text>`,c+="</svg>",c+='<div class="mk-trend-tip" hidden></div>',c+="</div>";let M=m-e[0],D=Math.abs(M)<.05?"unchanged":`${M>0?"up":"down"} ${g(Math.abs(M))}`;return c+=`<p class="mk-trend-sum">${o(`${e.length} measurements${n?`, from ${n}`:""}${t?` to ${t}`:""}: ${g(e[0])} to ${g(m)} \u2014 ${D}.`)}</p>`,a&&(c+=`<p class="mk-trend-sum">${p(a)}</p>`),c+="</div>",c}wireTips(e,n){let t=e.querySelector(".mk-trend-tip"),a=e.querySelector(".mk-trend-plot");if(!t||!a)return;let i=s=>{let A=Number(s.getAttribute("data-i")),h=s.getBoundingClientRect(),f=a.getBoundingClientRect(),m=Number(s.getAttribute("data-run"))||1,k=m>=3?`unchanged across ${m} scans`:`scan ${A+1} of ${n.length}`;t.hidden=!1,t.innerHTML=`<b>${o(s.getAttribute("data-v")||"")}</b> \xB7 ${o(k)}`,t.style.left=`${h.left+h.width/2-f.left}px`,t.style.top=`${h.top-f.top-6}px`,t.classList.add("on")},r=()=>{t.classList.remove("on")};for(let s of e.querySelectorAll(".mk-trend-hit"))s.addEventListener("pointerenter",()=>i(s)),s.addEventListener("focus",()=>i(s)),s.addEventListener("pointerleave",r),s.addEventListener("blur",r)}});export{U as collapseFlatRuns};
