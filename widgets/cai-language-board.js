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
`;function c(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function v(e){if(e==null||e==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,n="",i=0,a;for(;(a=r.exec(e))!==null;){a.index>i&&(n+=c(e.slice(i,a.index)));let s=a[0];if(s.startsWith("**"))n+=`<strong>${c(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))n+=`<code>${c(s.slice(1,-1))}</code>`;else{let l=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);l?n+=`<a href="${c(l[2])}">${c(l[1])}</a>`:n+=c(s)}i=a.index+s.length}return i<e.length&&(n+=c(e.slice(i))),n}var w=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function y(e){let r=e.getAttribute("kicker"),n=e.getAttribute("heading"),i=e.getAttribute("lede");if(!r&&!n&&!i)return"";let a='<div class="mk-section-head">';return r&&(a+=`<span class="mk-kicker">${c(r)}</span>`),n&&(a+=`<h2>${v(n)}</h2>`),i&&(a+=`<p>${v(i)}</p>`),a+="</div>",a}var $=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,b=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#a(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let n=(this.getAttribute("brand")||"").trim().toLowerCase();n==="assay"||n==="cai"||n==="watchdog"?this.dataset.brand=n:delete this.dataset.brand}json(r,n){let i=this.getAttribute(r);if(i==null||i.trim()==="")return n;try{return JSON.parse(i)}catch{return n}}};var p=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function g(e){return e>=90?p[4]:e>=70?p[3]:e>=50?p[2]:e>=25?p[1]:p[0]}var S=`
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
`;var N=[25,50,70,90],A=k+w+$+S+`
.mk-board { max-width: 56rem; margin: 0 auto; }
.mk-board-ctl { display: flex; flex-wrap: wrap; gap: 0.6rem; align-items: center; margin-bottom: 0.5rem; }
.mk-board-ctl input, .mk-board-ctl select { font: inherit; font-size: var(--fs-sm); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 7px 11px; }
.mk-board-ctl input { flex: 1 1 14rem; min-width: 0; }
.mk-board-ctl input:focus-visible, .mk-board-ctl select:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-board-count { font-size: var(--fs-xs); color: var(--muted); margin: 0 0 0.8rem; }

/* One grid, declared once and shared by the column heads, every row and the axis \u2014 so a heading
   can never drift off the column it names. */
.mk-rows { display: grid; gap: 2px; }
a.mk-row, div.mk-row, .mk-head, .mk-axis {
  display: grid; grid-template-columns: 9.5rem 1fr 4.4rem 3.4rem; gap: 0 0.9rem; align-items: center; }
a.mk-row, div.mk-row { padding: 9px 12px; border-radius: var(--r-sm); text-decoration: none; color: inherit; }
a.mk-row:hover, a.mk-row:focus-visible { background: var(--surface-2); text-decoration: none; }
.mk-head, .mk-axis { padding: 0 12px; }
.mk-head { margin-bottom: 0.35rem; font-size: var(--fs-2xs); color: var(--muted);
  text-transform: uppercase; letter-spacing: 0.06em; }
.mk-head span:nth-child(3), .mk-head span:nth-child(4) { text-align: right; }
.mk-row-name { font-weight: 650; font-size: var(--fs-sm); }
.mk-row-n { display: block; font-size: var(--fs-2xs); color: var(--muted); font-weight: 500; }

/* The distribution. Bars grow from the baseline, each bin fixed to its ten points of the axis. */
.mk-dist { position: relative; height: 26px; background: var(--surface-2);
  border-radius: var(--r-sm); overflow: hidden; }
/* The middle half, shaded behind the bars. Faint on purpose \u2014 it is context for the bars, not a
   sixth series \u2014 but not so faint it cannot be found: at 7% it was invisible in both themes. */
.mk-dist-iqr { position: absolute; top: 0; bottom: 0;
  background: color-mix(in srgb, var(--ink) 12%, transparent); }
.mk-dist-bin { position: absolute; bottom: 0; border-radius: 1px 1px 0 0; }
/* The cutlines must read on BOTH sides of a bar. A ground-coloured tick disappears against the
   empty track in light mode, where track and ground are nearly the same value. */
.mk-dist-cut { position: absolute; top: 0; bottom: 0; width: 1px;
  background: color-mix(in srgb, var(--muted) 55%, transparent); }
.mk-dist-med { position: absolute; top: 0; bottom: 0; width: 2px; transform: translateX(-1px);
  background: var(--ink); opacity: 0.75; }
.mk-row-med { font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-weight: 700;
  font-size: var(--fs-sm); text-align: right; }
.mk-row-iqr { display: block; font-family: var(--font-mono); font-size: var(--fs-2xs);
  color: var(--muted); font-weight: 500; }
.mk-row-depth { font-family: var(--font-mono); font-variant-numeric: tabular-nums; font-weight: 650;
  font-size: var(--fs-sm); text-align: right; }
.mk-row-depth.is-none { color: var(--muted); font-weight: 500; }
.mk-row-depth span { display: block; font-family: var(--font-body, inherit); font-size: var(--fs-2xs);
  color: var(--muted); font-weight: 500; }

.mk-axis { margin-top: 0.4rem; }
.mk-axis-scale { position: relative; height: 15px; grid-column: 2; }
.mk-axis-scale span { position: absolute; transform: translateX(-50%); font-family: var(--font-mono);
  font-size: var(--fs-2xs); color: var(--muted); }
/* The end labels sit ON the ends of the axis; centring them would hang half of each outside it. */
.mk-axis-scale span:first-child { transform: none; }
.mk-axis-scale span:last-child { transform: translateX(-100%); }
.mk-board-note { margin: 1.1rem 0 0; padding-top: 0.9rem; border-top: 1px solid var(--border);
  font-size: var(--fs-xs); color: var(--muted); line-height: 1.6; }
.mk-board-note + .mk-board-note { margin-top: 0.5rem; padding-top: 0; border-top: 0; }
.mk-board-empty { padding: 1.6rem 0; text-align: center; color: var(--muted); font-size: var(--fs-sm); }
@media (max-width: 40rem) {
  a.mk-row, div.mk-row, .mk-head, .mk-axis { grid-template-columns: 7rem 1fr 3.6rem 2.8rem; gap: 0 0.5rem; }
}
`,C=[["count","Most projects first"],["median-desc","Highest median first"],["median-asc","Lowest median first"],["depth","Deepest survey first"],["name","Name A\u2013Z"]];function d(e){let r=Number(e);return e==null||e===""||Number.isNaN(r)?null:r}function f(e){return Math.max(0,Math.min(100,Number(e)))}customElements.define("cai-language-board",class extends b{render(e){let r=(this.json("languages",[])||[]).filter(t=>t&&t.name),n=this.json("ungrouped",null),i=this.getAttribute("depth-note");this._hasDepth=r.some(t=>d(t.depth)!==null),this._sort=this._sort||"count",this._q=this._q||"";let a=`<style>${A}</style>`;if(a+=y(this),r.length===0){e.innerHTML=a;return}a+='<div class="mk-board">',a+='<div class="mk-board-ctl">',a+=`<input type="search" class="mk-q" placeholder="Search languages" aria-label="Search languages" value="${c(this._q)}">`,a+='<select class="mk-sort" aria-label="Sort">';for(let[t,o]of C)t==="depth"&&!this._hasDepth||(a+=`<option value="${t}"${t===this._sort?" selected":""}>${c(o)}</option>`);a+="</select></div>",a+='<p class="mk-board-count" role="status"></p>',a+=`<div class="mk-head" aria-hidden="true"><span>Language</span><span>Distribution of CAI scores</span><span>Median</span><span>${this._hasDepth?"Depth":""}</span></div>`,a+='<div class="mk-rows"></div>',a+='<div class="mk-axis"><div class="mk-axis-scale">';for(let t of[0,...N,100])a+=`<span style="left:${t}%">${t}</span>`;a+="</div></div>",i&&(a+=`<p class="mk-board-note">${c(i)}</p>`),n&&Number(n.count)>0&&(a+=`<p class="mk-board-note">${c(`${n.count} measured project${Number(n.count)===1?" has":"s have"} no primary language the scan could name, so ${Number(n.count)===1?"it is":"they are"} not grouped above. They are counted in the total.`)}</p>`),a+="</div>",e.innerHTML=a;let s=e.querySelector(".mk-q"),l=e.querySelector(".mk-sort");s?.addEventListener("input",()=>{this._q=s.value,this.paint(e,r)}),l?.addEventListener("change",()=>{this._sort=l.value,this.paint(e,r)}),this.paint(e,r)}paint(e,r){let n=e.querySelector(".mk-rows"),i=e.querySelector(".mk-board-count");if(!n||!i)return;let a=this._q.trim().toLowerCase(),s=r.filter(t=>!a||String(t.name).toLowerCase().includes(a)).sort({count:(t,o)=>Number(o.count)-Number(t.count),"median-desc":(t,o)=>Number(o.median)-Number(t.median),"median-asc":(t,o)=>Number(t.median)-Number(o.median),depth:(t,o)=>(d(o.depth)??-1)-(d(t.depth)??-1),name:(t,o)=>String(t.name).localeCompare(String(o.name))}[this._sort]||(()=>0));if(i.textContent=s.length===r.length?`${r.length} language${r.length===1?"":"s"} with a field guide`:`${s.length} of ${r.length} languages`,s.length===0){n.innerHTML=`<p class="mk-board-empty">${c(this.getAttribute("empty-text")||"No language here matches that.")}</p>`;return}let l="";for(let t of s){let o=Number(t.median),u=g(o),m=Number(t.count)||0,h=t.href?"a":"div",x=t.href?` href="${c(t.href)}"`:"";l+=`<${h} class="mk-row"${x}>`,l+=`<span class="mk-row-name">${c(t.name)}<span class="mk-row-n">${m} project${m===1?"":"s"}</span></span>`,l+=this.distribution(t,o,u),l+=`<span class="mk-row-med ink-${u.key}">${o.toFixed(1)}`+this.spread(t)+"</span>",this._hasDepth?l+=this.depth(t):l+="<span></span>",l+=`</${h}>`}n.innerHTML=l}distribution(e,r,n){let i=Array.isArray(e.dist)?e.dist.map(o=>Number(o)||0):null,a=d(e.low),s=d(e.high),l=`median ${r.toFixed(1)}, ${n.label}`;a!==null&&s!==null&&(l+=`; half of them between ${a.toFixed(1)} and ${s.toFixed(1)}`);let t=`<span class="mk-dist" role="img" aria-label="${c(l)}">`;if(!i||i.length===0||i.every(o=>o===0))t+=`<span class="mk-dist-bin fill-${n.key}" style="left:0;width:${f(r)}%;height:100%"></span>`;else{a!==null&&s!==null&&s>a&&(t+=`<span class="mk-dist-iqr" style="left:${f(a)}%;width:${f(s)-f(a)}%"></span>`);let o=100/i.length,u=Math.max(...i);i.forEach((m,h)=>{if(m<=0)return;let x=Math.max(8,Math.round(m/u*100)),M=g((h+.5)*o).key;t+=`<span class="mk-dist-bin fill-${M}" style="left:${h*o}%;width:${o}%;height:${x}%"></span>`})}for(let o of N)t+=`<span class="mk-dist-cut" style="left:${o}%"></span>`;return t+=`<span class="mk-dist-med" style="left:${f(r)}%"></span>`,t+"</span>"}spread(e){let r=d(e.low),n=d(e.high);return r===null||n===null?"":`<span class="mk-row-iqr">${r.toFixed(0)}\u2013${n.toFixed(0)}</span>`}depth(e){let r=d(e.depth);if(r===null)return'<span class="mk-row-depth is-none" title="No project in this language has been surveyed since per-dimension outcomes were recorded.">\u2014</span>';let n=d(e.depthOf),i=n===null?`median depth ${r} dimensions resolved`:`median depth ${r} dimensions resolved, recorded for ${n} project${n===1?"":"s"}`;return`<span class="mk-row-depth" aria-label="${c(i)}">${r}<span>dims</span></span>`}});
