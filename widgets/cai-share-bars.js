var k=`
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
`;function i(s){return String(s??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function g(s){if(s==null||s==="")return"";let o=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,a="",n=0,e;for(;(e=o.exec(s))!==null;){e.index>n&&(a+=i(s.slice(n,e.index)));let d=e[0];if(d.startsWith("**"))a+=`<strong>${i(d.slice(2,-2))}</strong>`;else if(d.startsWith("`"))a+=`<code>${i(d.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(d);c?a+=`<a href="${i(c[2])}">${i(c[1])}</a>`:a+=i(d)}n=e.index+d.length}return n<s.length&&(a+=i(s.slice(n))),a}var y=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function $(s){let o=s.getAttribute("kicker"),a=s.getAttribute("heading"),n=s.getAttribute("lede");if(!o&&!a&&!n)return"";let e='<div class="mk-section-head">';return o&&(e+=`<span class="mk-kicker">${i(o)}</span>`),a&&(e+=`<h2>${g(a)}</h2>`),n&&(e+=`<p>${g(n)}</p>`),e+="</div>",e}var S=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,x=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let o=this.dataset.theme;this.#t(),this.dataset.theme!==o&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let o=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=o;let a=(this.getAttribute("brand")||"").trim().toLowerCase();a==="assay"||a==="cai"||a==="watchdog"?this.dataset.brand=a:delete this.dataset.brand}json(o,a){let n=this.getAttribute(o);if(n==null||n.trim()==="")return a;try{return JSON.parse(n)}catch{return a}}};var A=`
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
`;var M=`
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
`;function C(s,{right:o=!1,label:a="More information"}={}){let n=s==null?"":String(s).trim();if(n==="")return"";let e=n.split(/\n\s*\n/).map(c=>c.trim()).filter(c=>c!=="").map(c=>`<p>${g(c)}</p>`).join("");return`<span class="${o?"info-hint hint-right":"info-hint"}" tabindex="0" role="note" aria-label="${i(a||"More information")}"><span class="info-hint-dot" aria-hidden="true">i</span><span class="info-hint-tip">${e}</span></span>`}var T=new Set(["exemplary","healthy","fair","poor","critical"]);function z(s){let o=String(s||"").trim().toLowerCase();return o==="accent"?"is-accent":T.has(o)?`is-band fill-${o}`:"is-track"}var H=k+y+S+A+M+`
.sb-mono { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.sb-cap { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase; color: var(--muted); font-weight: 700; }
/* The sentence that stands where a bar would be. Muted, never band-coloured: it is the absence
   of a measurement, and a colour from the band vocabulary would make it look like one. */
.sb-unmeasured { color: var(--muted); font-size: var(--fs-xs); line-height: 1.4; }
.sb-note { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 11.5px; color: var(--muted); }
.sb-label-row { display: flex; align-items: flex-start; min-width: 0; }
.sb-label { font-size: var(--fs-sm); font-weight: 600; color: var(--ink); overflow-wrap: anywhere; }
a.sb-label { color: var(--ink); }
a.sb-label:hover { color: var(--accent); text-decoration: none; }

/* \u2500\u2500 wide: one population, full width \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500 */
.sb-wide-row { display: flex; flex-direction: column; gap: 9px; position: relative; }
.sb-wide-row + .sb-wide-row { margin-top: 18px; }
.sb-wide-bar { display: flex; height: 28px; gap: 2px; border-radius: 3px; overflow: hidden; }
.sb-wide-part { display: flex; align-items: center; min-width: 0; padding: 0 11px; }
.sb-wide-part.is-accent { background: var(--accent); }
.sb-wide-part.is-band { /* fill-* supplies the background */ }
.sb-wide-part.is-track { background: var(--surface-2); }
.sb-wide-part > span { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 11.5px; font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.sb-wide-part.is-accent > span, .sb-wide-part.is-band > span { color: var(--on-accent); }
.sb-wide-part.is-track > span { color: var(--muted); }
/* A part too narrow to hold its own label states it underneath instead, with a swatch so the
   reader can tell which slice it belongs to. Truncating it inside the bar would leave "1,5\u2026".
   \u2605 The 18% share test is necessary but NOT sufficient, and 400px is where that shows: the
   population bar's 42.9% part clears 18% comfortably and still could not hold "1,511 resolved \xB7
   42.9%" in 151px \u2014 it rendered "1,511 resolved \xB7 4\u2026", which is a truncated FIGURE, the one
   thing a sheet of figures must never show. A share is a fraction of the bar; whether a string
   fits is a fraction of the viewport, and no render-time percentage can know it. So below 560px
   every part states itself in the legend and none of them state themselves inside. Each label is
   emitted once and placed by CSS, not emitted twice \u2014 display:none takes it out of the
   accessibility tree too, so a screen reader hears one label per part at either width. */
.sb-legend { display: flex; flex-wrap: wrap; gap: 6px 16px; }
.sb-legend-item.is-inside { display: none; }
@media (max-width: 560px) {
  .sb-wide-part > span { display: none; }
  .sb-legend-item.is-inside { display: inline-flex; }
}
.sb-legend-item { display: inline-flex; align-items: center; gap: 6px;
  font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-size: 11.5px; color: var(--muted); }
.sb-swatch { width: 9px; height: 9px; border-radius: 2px; flex: none; display: block; }
.sb-swatch.is-accent { background: var(--accent); }
.sb-swatch.is-track { background: var(--surface-2); border: 1px solid var(--border); }

/* \u2500\u2500 table: a bar per row \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500 */
.sb-head, .sb-row { display: grid; grid-template-columns: var(--sb-grid); gap: 12px; align-items: center; }
.sb-head { padding-bottom: 6px; border-bottom: 1px solid var(--border-strong); align-items: end; }
.sb-row { padding: 7px 0; border-bottom: 1px solid var(--border); position: relative; }
/* A row is the subject of its own (i): the tip spans the row rather than hanging off the dot,
   so it can never reach past the island \u2014 and a table of seventeen rows never widens the page.
   See hint.js. The z-index lifts the open tip above the rows it opens across. */
.sb-wide-row .info-hint, .sb-row .info-hint { position: static; }
.sb-wide-row .info-hint-tip, .sb-row .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
.sb-wide-row:hover, .sb-wide-row:focus-within, .sb-row:hover, .sb-row:focus-within { z-index: 2; }
.sb-cell { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-xs); text-align: right; color: var(--muted); }
.sb-head .sb-cell { color: var(--muted); }
.sb-bar-cell { display: flex; align-items: center; gap: 9px; min-width: 0; }
.sb-track { flex: 1; display: flex; height: 10px; background: var(--surface-2);
  border-radius: 4px; overflow: hidden; min-width: 40px; }
.sb-part { display: block; height: 100%; border-radius: 4px; }
.sb-part.is-accent { background: var(--accent); }
.sb-part.is-track { background: transparent; }

@media (max-width: 560px) {
  /* The cells take their own line under the label, each carrying its heading, and the bar takes
     a third. The header row has nothing left to head, so it goes. */
  .sb-head { display: none; }
  .sb-row { grid-template-columns: repeat(auto-fit, minmax(74px, 1fr)); gap: 8px 12px; padding: 11px 0; }
  .sb-label-row { grid-column: 1 / -1; }
  .sb-bar-cell { grid-column: 1 / -1; }
  .sb-cell { text-align: left; }
  .sb-cell[data-label]::before { content: attr(data-label) " "; display: block;
    font-family: var(--font-ui); font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
    font-weight: 700; color: var(--muted); }
}
`;function N(s,{wide:o}){let a=(s||[]).filter(e=>e&&Number.isFinite(Number(e.weight))&&Number(e.weight)>0);if(a.length===0)return"";let n="";for(let e of a){let d=z(e.tone),c=Number(e.weight);if(o){let b=[e.count,e.label].filter(m=>m!=null&&String(m)!=="").join(" ");n+=`<span class="sb-wide-part ${d}" style="flex:${c} 1 0">`,n+=b?`<span>${i(b)}</span>`:"",n+="</span>"}else n+=`<span class="sb-part ${d}" style="flex:${c} 1 0"></span>`}return n}customElements.define("cai-share-bars",class extends x{render(s){let o=(this.getAttribute("layout")||"table").trim().toLowerCase()==="wide"?"wide":"table",a=(this.json("columns",[])||[]).map(t=>t==null?"":String(t)),n=(this.json("rows",[])||[]).filter(t=>t&&t.label!=null&&String(t.label)!==""),e=`<style>${H}</style>`;if(e+=$(this),n.length===0){s.innerHTML=e;return}let d=(t,p)=>{let h=String(t.label),r='<div class="sb-label-row">';return r+=t.href?`<a class="sb-label" href="${i(String(t.href))}">${i(h)}</a>`:`<span class="sb-label">${i(h)}</span>`,r+=C(t.tip,{right:p,label:h}),r+="</div>",r};if(o==="wide"){for(let t of n){if(e+='<div class="sb-wide-row">',e+=d(t,!1),t.unmeasured)e+=`<p class="sb-unmeasured">${i(String(t.unmeasured))}</p>`;else{let p=(t.parts||[]).filter(r=>r&&Number.isFinite(Number(r.weight))&&Number(r.weight)>0),h=p.reduce((r,f)=>r+Number(f.weight),0);if(p.length>0){let f=p.map(l=>Number(l.weight)/h>=.18);e+='<div class="sb-wide-bar">',e+=N(p.map((l,v)=>f[v]?l:{weight:l.weight,tone:l.tone}),{wide:!0}),e+="</div>";let u=p.map((l,v)=>({text:[l.count,l.label].filter(w=>w!=null&&String(w)!=="").join(" "),cls:z(l.tone),inside:f[v]})).filter(l=>l.text!=="");if(u.length>0){e+='<div class="sb-legend">';for(let l of u)e+=`<span class="sb-legend-item${l.inside?" is-inside":""}"><i class="sb-swatch ${l.cls}"></i>${i(l.text)}</span>`;e+="</div>"}}t.note&&(e+=`<span class="sb-note">${i(String(t.note))}</span>`)}e+="</div>"}s.innerHTML=e;return}let c=Math.max(0,Math.min(12,Math.trunc(a.length))),b=`minmax(90px,1.1fr) repeat(${c}, minmax(52px,auto)) minmax(120px,2fr)`,m=this.getAttribute("label-heading")||"",F=this.getAttribute("bar-heading")||"";e+=`<div class="sb-table" style="--sb-grid:${b}">`,e+='<div class="sb-head">',e+=`<span class="sb-cap">${i(m)}</span>`;for(let t of a)e+=`<span class="sb-cap sb-cell">${i(t)}</span>`;e+=`<span class="sb-cap">${i(F)}</span>`,e+="</div>";for(let t of n){e+='<div class="sb-row">',e+=d(t,!1);let p=(t.cells||[]).map(r=>r==null?"":String(r)),h=t.cellTones||[];for(let r=0;r<c;r++){let f=String(h[r]||"").trim().toLowerCase(),u=T.has(f)?` ink-${f}`:"",l=a[r]||"";e+=`<span class="sb-cell${u}" data-label="${i(l)}">${i(p[r]??"")}</span>`}if(e+='<div class="sb-bar-cell">',t.unmeasured)e+=`<span class="sb-unmeasured">${i(String(t.unmeasured))}</span>`;else{let r=N(t.parts,{wide:!1});r&&(e+=`<span class="sb-track">${r}</span>`),t.note&&(e+=`<span class="sb-note">${i(String(t.note))}</span>`)}e+="</div>",e+="</div>"}e+="</div>",s.innerHTML=e}});
