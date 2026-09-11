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
`;function s(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function f(e){if(e==null||e==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",a=0,i;for(;(i=n.exec(e))!==null;){i.index>a&&(t+=s(e.slice(a,i.index)));let o=i[0];if(o.startsWith("**"))t+=`<strong>${s(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))t+=`<code>${s(o.slice(1,-1))}</code>`;else{let l=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);l?t+=`<a href="${s(l[2])}">${s(l[1])}</a>`:t+=s(o)}a=i.index+o.length}return a<e.length&&(t+=s(e.slice(a))),t}var R=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function O(e){let n=e.getAttribute("kicker"),t=e.getAttribute("heading"),a=e.getAttribute("lede");if(!n&&!t&&!a)return"";let i='<div class="mk-section-head">';return n&&(i+=`<span class="mk-kicker">${s(n)}</span>`),t&&(i+=`<h2>${f(t)}</h2>`),a&&(i+=`<p>${f(a)}</p>`),i+="</div>",i}var j=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,T=class extends HTMLElement{#t;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#t=new MutationObserver(()=>{let n=this.dataset.theme;this.#e(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#t.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#t?.disconnect()}#e(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(n,t){let a=this.getAttribute(n);if(a==null||a.trim()==="")return t;try{return JSON.parse(a)}catch{return t}}};var v=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function E(e){return e>=90?v[4]:e>=70?v[3]:e>=50?v[2]:e>=25?v[1]:v[0]}var D=`
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
`;var L=`
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
`;function B(e,{right:n=!1,label:t="More information"}={}){let a=e==null?"":String(e).trim();if(a==="")return"";let i=a.split(/\n\s*\n/).map(l=>l.trim()).filter(l=>l!=="").map(l=>`<p>${f(l)}</p>`).join("");return`<span class="${n?"info-hint hint-right":"info-hint"}" tabindex="0" role="note" aria-label="${s(t||"More information")}"><span class="info-hint-dot" aria-hidden="true">i</span><span class="info-hint-tip">${i}</span></span>`}var W=`
/* \u2605 THE ONE CONTAINER, DECLARED ONCE FOR THE FOUR ISLANDS THAT TAKE THIS RAIL.
   Every width question an island asks is about a box inside the page, never about the page: a
   rail takes 112px and a gutter out of the column, so an island 648px wide has 512px of content
   and a viewport media query calls that a wide page. It is named rather than left to
   nearest-ancestor resolution because naming is what makes the answer stable \u2014 an unnamed
   @container binds to whichever container is nearest when the rule RUNS, so adding a container
   anywhere inside an island would silently re-point every unnamed query beneath it at a
   different box. That is not hypothetical: it is what would have happened to the wide bar's
   legend in cai-share-bars the moment a container was put on .rail-body.
   \u2605 AND IT IS ON :host, NOT ON .rail \u2014 a @container rule styles the DESCENDANTS of its
   container and never the container itself, so the stacking rule at the foot of this file could
   not have asked a container that was .rail. */
:host { container-type: inline-size; container-name: island; }
.rail { display: grid; grid-template-columns: 112px minmax(0, 1fr); gap: 0 24px;
  align-items: start; position: relative; }
.rail-side { display: flex; align-items: flex-start; padding-top: 3px; min-width: 0; }
.rail-kicker { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
  color: var(--muted); font-weight: 700; overflow-wrap: anywhere; }
.rail-body { min-width: 0; }
/* The KICKER's (i) \u2014 and only that one \u2014 takes the rail as its subject, so its tip can never
   reach past the island. Every other (i) inside keeps the subject its own island gave it. */
.rail-side .info-hint { position: static; }
.rail-side .info-hint-tip { left: 0; right: auto; max-width: min(320px, 100%); }
/* A tip opens across the content beside it, which would otherwise paint over it. */
.rail:hover, .rail:focus-within { z-index: 2; }
/* The label takes its own line under 560px OF THE ISLAND \u2014 the same width at which
   cai-share-bars reflows, so two adjacent sections never disagree about when a page has become
   narrow. A 112px column plus a 24px gutter is a third of a 400px screen spent on two words.
   Of the island, because an island is not the page: dropped into a 380px column of a 1280px
   page, a viewport query kept the rail beside 244px of content and nothing failed. */
@container island (max-width: 560px) {
  .rail { grid-template-columns: minmax(0, 1fr); gap: 10px; }
  .rail-side { padding-top: 0; }
}
`;function _({kicker:e,tip:n,content:t}){return`<div class="rail"><div class="rail-side"><span class="rail-kicker">${s(e)}</span>`+B(n,{label:e})+`</div><div class="rail-body">${t}</div></div>`}function q(e){let n=e.getAttribute("heading"),t=e.getAttribute("lede");if(!n&&!t)return"";let a='<div class="mk-section-head">';return n&&(a+=`<h2>${f(n)}</h2>`),t&&(a+=`<p>${f(t)}</p>`),a+="</div>",a}var y=[0,25,50,70,90,100],G=.05;function K(e){let n=[],t=0;for(;t<e.length;){let a=t;for(;a+1<e.length&&Math.abs(e[a+1]-e[t])<G;)a++;let i=a-t+1;n.push({i:t,v:e[t],run:i}),a>t&&n.push({i:a,v:e[a],run:i}),t=a+1}return n}var N=720,H=240,p={top:26,right:18,bottom:34,left:40},X=z+R+j+D+L+W+`
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
/* What a sampled point IS, under what it says: a second line rather than a longer first one,
   because the tip does not wrap and a single 380px line beside the last mark of a narrow column
   reaches past the island. */
.mk-trend-tip-note { display: block; color: var(--muted); font-size: var(--fs-2xs); }
.mk-trend-solo { display: flex; align-items: baseline; justify-content: center; gap: 0.6rem;
  padding: 1.6rem 0 0.4rem; }
.mk-trend-solo-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-4xl); line-height: 1; }
.mk-trend-solo-date { font-size: var(--fs-sm); color: var(--muted); }
.mk-trend-sum { margin: 0.9rem auto 0; max-width: 46rem; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; text-align: center; }

/* \u2500\u2500 the endpoints, BESIDE the chart \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   The chart takes what the figures do not need (flex: 1), which is the design's shape: a line
   long enough to read a slope off, and the two quantities stated beside it. Each stack is capped
   at 220px so a basis sentence wraps instead of pushing the line down to a stub. */
/* \u2605\u2605 THE CHART GETS ITS OWN WIDTH OR IT GETS THE WHOLE ROW, AND NO BREAKPOINT DECIDES THAT.
   The plot is a 720-unit viewBox and every label inside it is set in real pixels \u2014 12px dates,
   11px cutlines \u2014 so an SVG drawn 329px wide draws them at 5.5px. Nothing in the CSS says so:
   computed style still reports 12px, which is why this was reported as "unreadable" by a person
   rather than caught by a rule. A percentage basis cannot express it either, because 60% of a
   column is not a number of pixels.
   The flex basis below says the thing itself: the chart asks for a width it can be drawn at.
   648px is 90% of the 720-unit plot \u2014 the point below which its 12px dates are drawn under 11px
   and its 11px cutlines under 10. While the row can give it that AND hold the pairs beside it,
   they sit beside it and the chart takes the surplus; when it cannot, the pairs wrap onto their
   own line and the chart has the whole column. The basis decides only WHERE THEY WRAP: beside
   the pairs the chart is still 692px in the sheet's 984px column, or 801px now that the pairs
   have lost their basis sentences and take 151px instead of 260.
   It is the same answer cai-figure-band and cai-link-cards already give with auto-fit: a layout
   that responds to its own box needs no query to be told how wide it is. Two of the four sheet
   islands never needed a width query at all, and this one now does not either. */
.mk-trend-row { display: flex; flex-wrap: wrap; gap: 32px; align-items: flex-start; }
.mk-trend-row > .mk-trend { flex: 1 1 648px; min-width: 0; }
/* The pairs are a COLUMN beside the line, at every width, and that is a decision rather than
   what the wrapping happened to do. The design puts its two stacks in a row because it prints no
   basis; ours each carry a sentence naming the population, and two of those side by side in the
   third of the column the chart leaves over wrap into ragged blocks \u2014 and then unwrap into a row
   again at some other width, so the section would be a different shape on a laptop and on a
   desktop. One column reads the same everywhere. */
.mk-trend-figs { display: flex; flex-direction: column; gap: 16px; flex: 0 1 auto; min-width: 0; }
.mk-trend-fig { display: flex; flex-direction: column; gap: 2px; flex: none;
  min-width: 0; max-width: 260px; }
/* The same eyebrow the section rail uses: a label over a figure, never a second heading. */
.mk-trend-fig-label { font-size: 10.5px; letter-spacing: .16em; text-transform: uppercase;
  color: var(--muted); font-weight: 700; }
.mk-trend-fig-pair { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: 18px; font-weight: 700; line-height: 1.2; color: var(--ink); overflow-wrap: anywhere; }
.mk-trend-fig-sub { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-size: var(--fs-2xs); color: var(--muted); line-height: 1.45; }
/* The pairs are beneath the line by then \u2014 the wrap above puts them there \u2014 so all this width
   has left to say is that a basis sentence may use the whole of it. On the ISLAND, because that
   is the box a reader's screen is, and because a cap released against the wrong box is the
   difference between a sentence wrapping and a sentence wrapping twice. */
@container island (max-width: 560px) {
  .mk-trend-row { gap: 16px; }
  .mk-trend-figs { flex-direction: column; gap: 14px; }
  .mk-trend-fig { max-width: none; }
}

/* \u2500\u2500 inside the rail \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
   The chart's 46rem measure and its auto margins are a CARD centred on a page that has no other
   column. In the rail the content column IS the measure, and a 736px card inside a 950px column
   is a second, narrower page drawn inside the first. These are descendant selectors rather than
   a modifier class on purpose: the no-kicker markup then cannot change at all, which is what the
   snapshot tests assert. */
.rail-body .mk-trend { max-width: none; margin-left: 0; margin-right: 0; }
.rail-body .mk-trend-sum { max-width: none; margin-left: 0; margin-right: 0; text-align: left; }
@media (prefers-reduced-motion: reduce) { .mk-trend-tip { transition: none; } }
`;function J(e){let n=Math.min(...e),t=Math.max(...e),a=0,i=100;for(let o of y)o<=n&&(a=o);for(let o=y.length-1;o>=0;o--)y[o]>=t&&(i=y[o]);return i-a<10&&(a=Math.max(0,Math.min(a,i-25))),i-a<10&&(i=Math.min(100,a+25)),{min:a,max:i}}function w(e){return(Math.round(e*10)/10).toFixed(1)}customElements.define("cai-trend",class extends T{render(e){let n=(this.json("series",[])||[]).map(Number).filter(d=>Number.isFinite(d)),t=(this.getAttribute("kicker")||"").trim(),a=this.figuresHtml(),i=this.plotHtml(n),o=a?`<div class="mk-trend-row">${i}${a}</div>`:i,l=`<style>${X}</style>`;l+=t?_({kicker:t,tip:this.getAttribute("tip"),content:q(this)+o}):O(this)+o,e.innerHTML=l,n.length>1&&this.wireTips(e,n)}figuresHtml(){let e=(this.json("figures",[])||[]).filter(t=>t&&t.from!=null&&t.from.value!=null&&String(t.from.value)!==""&&t.to!=null&&t.to.value!=null&&String(t.to.value)!=="");if(e.length===0)return"";let n='<div class="mk-trend-figs">';for(let t of e){n+='<div class="mk-trend-fig">',t.label!=null&&String(t.label)!==""&&(n+=`<span class="mk-trend-fig-label">${s(String(t.label))}</span>`),n+=`<span class="mk-trend-fig-pair">${s(String(t.from.value))} \u2192 ${s(String(t.to.value))}</span>`;let a=t.from.sub==null?"":String(t.from.sub),i=t.to.sub==null?"":String(t.to.sub),o=a===i?[a]:[a,i];for(let l of o.filter(d=>d!==""))n+=`<span class="mk-trend-fig-sub">${s(l)}</span>`;n+="</div>"}return n+"</div>"}plotHtml(e){let n=this.getAttribute("first-date"),t=this.getAttribute("last-date"),a=this.getAttribute("caption"),i=(this.getAttribute("sampled")||"").trim();if(e.length===0)return"";if(e.length===1){let r=e[0],u=E(r),g='<div class="mk-trend"><p class="mk-trend-solo">';return g+=`<span class="mk-trend-solo-num ink-${u.key}">${w(r)}</span>`,(t||n)&&(g+=`<span class="mk-trend-solo-date">measured ${s(t||n)}</span>`),g+="</p></div>",a&&(g+=`<p class="mk-trend-sum">${f(a)}</p>`),g}let{min:o,max:l}=J(e),d=N-p.left-p.right,$=H-p.top-p.bottom,m=r=>p.left+d*r/(e.length-1),h=r=>p.top+$*(1-(r-o)/(l-o)),b=e[e.length-1],S=E(b),A=i?e.map((r,u)=>({i:u,v:r,run:1})):K(e),k=A.map(r=>`${m(r.i).toFixed(1)},${h(r.v).toFixed(1)}`),c=`<div class="mk-trend"><div class="mk-trend-plot"${i?` data-sampled="${s(i)}"`:""}>`,M=i?`${e.length} ${i} samples`:`${e.length} measurements`,x=[w(e[0]),w(b)],I=(this.json("figures",[])||[]).some(r=>r&&r.from&&r.to&&String(r.from.value??"").trim()===x[0]&&String(r.to.value??"").trim()===x[1])&&!!n&&!!t;c+=`<svg viewBox="0 0 ${N} ${H}" role="img" aria-label="${s(I?`${M}, from ${n} to ${t}: ${x[0]} to ${x[1]}.`:`${M}, from ${x[0]} to ${x[1]}.`)}">`;for(let r of y){if(r<o||r>l)continue;let u=h(r).toFixed(1);c+=`<line class="mk-trend-grid" x1="${p.left}" y1="${u}" x2="${N-p.right}" y2="${u}"></line>`,c+=`<text class="mk-trend-cut" x="${p.left-8}" y="${u}" text-anchor="end" dominant-baseline="middle">${r}</text>`}c+=`<path class="mk-trend-area" d="M${m(0).toFixed(1)},${h(o).toFixed(1)} L${k.join(" L")} L${m(e.length-1).toFixed(1)},${h(o).toFixed(1)} Z"></path>`,c+=`<polyline class="mk-trend-line" points="${k.join(" ")}" vector-effect="non-scaling-stroke"></polyline>`,n&&(c+=`<text class="mk-trend-date" x="${p.left}" y="${H-10}" text-anchor="start">${s(n)}</text>`),t&&(c+=`<text class="mk-trend-date" x="${N-p.right}" y="${H-10}" text-anchor="end">${s(t)}</text>`),A.forEach(r=>{let u=r.i===e.length-1,g=m(r.i).toFixed(1),F=h(r.v).toFixed(1);c+=`<circle class="mk-trend-hit" cx="${g}" cy="${F}" r="18" tabindex="0" data-i="${r.i}" data-v="${w(r.v)}" data-run="${r.run}"></circle>`,c+=u?`<circle class="mk-trend-end fill-${S.key}" cx="${g}" cy="${F}" r="5.5"></circle>`:`<circle class="mk-trend-dot" cx="${g}" cy="${F}" r="4"></circle>`}),c+=`<text class="mk-trend-endlabel ink-${S.key}" x="${m(e.length-1).toFixed(1)}" y="${(h(b)-14).toFixed(1)}" text-anchor="end">${w(b)}</text>`,c+="</svg>",c+='<div class="mk-trend-tip" hidden></div>',c+="</div>";let C=b-e[0],U=Math.abs(C)<.05?"unchanged":`${C>0?"up":"down"} ${w(Math.abs(C))}`,P=`${n?`, from ${n}`:""}${t?` to ${t}`:""}: ${x[0]} to ${x[1]} \u2014 ${U}.`;return c+=`<p class="mk-trend-sum">${s(M+(I?".":P)+(i?" Each point is the newest reading on or before its own date, not a reading taken on it.":""))}</p>`,a&&(c+=`<p class="mk-trend-sum">${f(a)}</p>`),c+="</div>",c}wireTips(e,n){let t=e.querySelector(".mk-trend-tip"),a=e.querySelector(".mk-trend-plot");if(!t||!a)return;let i=a.getAttribute("data-sampled")||"",o=d=>{let $=Number(d.getAttribute("data-i")),m=d.getBoundingClientRect(),h=a.getBoundingClientRect(),b=Number(d.getAttribute("data-run"))||1,S=i?`${i} sample ${$+1} of ${n.length}`:b>=3?`unchanged across ${b} scans`:`scan ${$+1} of ${n.length}`;t.hidden=!1,t.innerHTML=`<b>${s(d.getAttribute("data-v")||"")}</b> \xB7 ${s(S)}`+(i?'<span class="mk-trend-tip-note">the newest reading on or before that date</span>':"");let A=m.left+m.width/2-h.left,k=t.offsetWidth/2,c=Math.min(Math.max(A-k,0),Math.max(0,h.width-t.offsetWidth));t.style.left=`${c+k}px`,t.style.top=`${m.top-h.top-6}px`,t.classList.add("on")},l=()=>{t.classList.remove("on")};for(let d of e.querySelectorAll(".mk-trend-hit"))d.addEventListener("pointerenter",()=>o(d)),d.addEventListener("focus",()=>o(d)),d.addEventListener("pointerleave",l),d.addEventListener("blur",l)}});export{K as collapseFlatRuns};
