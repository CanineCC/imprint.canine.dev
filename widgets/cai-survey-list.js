var x=`
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
`;function c(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function b(a){if(a==null||a==="")return"";let i=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",s=0,n;for(;(n=i.exec(a))!==null;){n.index>s&&(e+=c(a.slice(s,n.index)));let l=n[0];if(l.startsWith("**"))e+=`<strong>${c(l.slice(2,-2))}</strong>`;else if(l.startsWith("`"))e+=`<code>${c(l.slice(1,-1))}</code>`;else{let t=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(l);t?e+=`<a href="${c(t[2])}">${c(t[1])}</a>`:e+=c(l)}s=n.index+l.length}return s<a.length&&(e+=c(a.slice(s))),e}var g=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function v(a){let i=a.getAttribute("kicker"),e=a.getAttribute("heading"),s=a.getAttribute("lede");if(!i&&!e&&!s)return"";let n='<div class="mk-section-head">';return i&&(n+=`<span class="mk-kicker">${c(i)}</span>`),e&&(n+=`<h2>${b(e)}</h2>`),s&&(n+=`<p>${b(s)}</p>`),n+="</div>",n}var k=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,h=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let i=this.dataset.theme;this.#a(),this.dataset.theme!==i&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let i=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=i;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(i,e){let s=this.getAttribute(i);if(s==null||s.trim()==="")return e;try{return JSON.parse(s)}catch{return e}}};var p=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function f(a){return a>=90?p[4]:a>=70?p[3]:a>=50?p[2]:a>=25?p[1]:p[0]}var y=`
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
`;var $=x+g+k+y+`
.mk-list { max-width: 60rem; margin: 0 auto; }

/* the distribution, which is also the band filter */
.mk-dist { display: flex; height: 34px; border-radius: var(--r-sm); overflow: hidden; gap: 2px; }
.mk-dist button { flex: none; border: 0; padding: 0; cursor: pointer; position: relative;
  font: inherit; color: var(--on-accent); font-size: var(--fs-2xs); font-weight: 700;
  transition: opacity 120ms ease; }
.mk-dist button[aria-pressed="false"] { opacity: 0.28; }
.mk-dist button:focus-visible { outline: 2px solid var(--accent-strong); outline-offset: -3px; }
.mk-dist-key { display: flex; flex-wrap: wrap; gap: 0.35rem 1.1rem; margin-top: 0.6rem;
  font-size: var(--fs-xs); color: var(--muted); }
.mk-dist-key span { display: inline-flex; align-items: center; gap: 0.4rem; }
.mk-dist-key i { width: 9px; height: 9px; border-radius: 2px; flex: none; }

/* controls */
.mk-ctl { display: flex; flex-wrap: wrap; gap: 0.6rem; margin: 1.3rem 0 0.5rem; align-items: center; }
.mk-ctl input, .mk-ctl select { font: inherit; font-size: var(--fs-sm); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm);
  padding: 7px 11px; }
.mk-ctl input { flex: 1 1 15rem; min-width: 0; }
.mk-ctl input:focus-visible, .mk-ctl select:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-ctl-clear { background: none; border: 0; color: var(--accent-ink); font: inherit;
  font-size: var(--fs-sm); cursor: pointer; padding: 4px 2px; text-decoration: underline; }
.mk-count { font-size: var(--fs-xs); color: var(--muted); margin: 0 0 0.9rem; }

/* the cards */
.mk-cards { display: grid; gap: 10px; grid-template-columns: repeat(auto-fill, minmax(19rem, 1fr)); }
a.mk-card { display: grid; gap: 7px; padding: 14px 16px; text-decoration: none; color: inherit;
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--r-md);
  transition: border-color 120ms ease; }
a.mk-card:hover, a.mk-card:focus-visible { border-color: var(--accent); text-decoration: none; }
.mk-card-top { display: flex; align-items: baseline; justify-content: space-between; gap: 0.8rem; }
.mk-card-name { font-weight: 650; font-size: var(--fs-md); overflow-wrap: anywhere; }
.mk-card-owner { color: var(--muted); font-weight: 500; }
/* Quiet, not a warning colour: closed source is a fact about what a reader can go and verify, not a fault.
   Its own row, so it never competes with the name for a narrow grid column and cannot break it mid-word. */
.mk-card-closed { justify-self: start; padding: 0.05rem 0.4rem; border-radius: var(--r-sm, 4px);
  border: 1px solid var(--border); color: var(--muted); font-size: var(--fs-xs); font-weight: 500;
  white-space: nowrap; }
.mk-card-score { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-lg); flex: none; }
.mk-card-meta { font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }
.mk-empty { padding: 2rem 0; text-align: center; color: var(--muted); font-size: var(--fs-sm); }
@media (prefers-reduced-motion: reduce) { .mk-dist button, a.mk-card { transition: none; } }
`,w=[["score-desc","Highest score first"],["score-asc","Lowest score first"],["name","Name A\u2013Z"],["size-desc","Largest first"]],S=["recent","Most recently measured"];customElements.define("cai-survey-list",class extends h{render(a){let i=(this.json("projects",[])||[]).filter(t=>t&&t.name);this._rows=i,this._q=this._q||"",this._sort=this._sort||"score-desc",this._bands=this._bands||null;let e=`<style>${$}</style>`;if(e+=v(this),i.length===0){a.innerHTML=e;return}let s=new Map(p.map(t=>[t.key,0]));for(let t of i){let o=f(Number(t.score)).key;s.set(o,(s.get(o)||0)+1)}let n=p.filter(t=>s.get(t.key)>0);e+='<div class="mk-list">',e+='<div class="mk-dist" role="group" aria-label="Filter by band">';for(let t of n){let o=s.get(t.key),r=!this._bands||this._bands.has(t.key);e+=`<button type="button" class="fill-${t.key}" data-band="${t.key}" style="flex: ${o} 0 0" aria-pressed="${r}" title="${c(`${t.label}: ${o} project${o===1?"":"s"}`)}">${o}</button>`}e+='</div><p class="mk-dist-key">';for(let t of n)e+=`<span><i class="fill-${t.key}"></i>${c(t.label)} ${s.get(t.key)}</span>`;e+="</p>",e+='<div class="mk-ctl">',e+=`<input type="search" class="mk-q" placeholder="Search these projects" aria-label="Search these projects" value="${c(this._q)}">`;let l=i.some(t=>t.at)?[...w,S]:w;e+='<select class="mk-sort" aria-label="Sort">';for(let[t,o]of l)e+=`<option value="${t}"${t===this._sort?" selected":""}>${c(o)}</option>`;e+="</select>",(this._q||this._bands)&&(e+='<button type="button" class="mk-ctl-clear">Clear</button>'),e+="</div>",e+='<p class="mk-count" role="status"></p>',e+='<div class="mk-cards"></div>',e+="</div>",a.innerHTML=e,this.wire(a),this.paint(a)}wire(a){let i=a.querySelector(".mk-q"),e=a.querySelector(".mk-sort");i?.addEventListener("input",()=>{this._q=i.value,this.paint(a)}),e?.addEventListener("change",()=>{this._sort=e.value,this.paint(a)}),a.querySelector(".mk-ctl-clear")?.addEventListener("click",()=>{this._q="",this._bands=null,this.render(a)});for(let s of a.querySelectorAll(".mk-dist button"))s.addEventListener("click",()=>{let n=s.getAttribute("data-band"),l=this._bands?new Set(this._bands):new Set;this._bands&&l.has(n)?l.delete(n):l.add(n),this._bands=l.size===0?null:l,this.render(a)})}paint(a){let i=a.querySelector(".mk-cards"),e=a.querySelector(".mk-count");if(!i||!e)return;let s=this._q.trim().toLowerCase(),n=this._rows.filter(r=>this._bands&&!this._bands.has(f(Number(r.score)).key)?!1:s?`${r.owner||""}/${r.name}`.toLowerCase().includes(s):!0),l={"score-desc":(r,d)=>Number(d.score)-Number(r.score),"score-asc":(r,d)=>Number(r.score)-Number(d.score),name:(r,d)=>String(r.name).localeCompare(String(d.name)),"size-desc":(r,d)=>Number(d.loc||0)-Number(r.loc||0),recent:(r,d)=>String(d.at||"").localeCompare(String(r.at||""))}[this._sort];n=[...n].sort(l||(()=>0));let t=this._rows.length;if(e.textContent=n.length===t?`${t} project${t===1?"":"s"}`:`${n.length} of ${t} projects`,n.length===0){i.innerHTML=`<p class="mk-empty">${c(this.getAttribute("empty-text")||"No project here matches that.")}</p>`;return}let o="";for(let r of n){let d=Number(r.score),u=f(d),m=[];r.loc&&m.push(`${_(Number(r.loc))} lines`),r.day&&m.push(`measured ${r.day}`),r.lastPublished&&r.lastPublished!==r.day&&m.push(`last published ${r.lastPublished}`),o+=`<a class="mk-card" href="${c(r.href||"#")}">`,o+='<span class="mk-card-top">',o+='<span class="mk-card-name">',r.owner&&(o+=`<span class="mk-card-owner">${c(r.owner)}/</span>`),o+=`${c(r.name)}</span>`,o+=`<span class="mk-card-score ink-${u.key}">${d.toFixed(1)}</span>`,o+="</span>",r.source==="closed"&&(o+='<span class="mk-card-closed">Closed source</span>'),o+=`<span class="cai-lens-bar"><span class="cai-lens-fill fill-${u.key}" style="width:${Math.max(2,Math.round(d))}%"></span></span>`,o+=`<span class="mk-card-meta">${c([u.label,...m].join(" \xB7 "))}</span>`,o+="</a>"}i.innerHTML=o}});function _(a){return!Number.isFinite(a)||a<=0?"0":a>=1e6?`${(a/1e6).toFixed(1)}m`:a>=1e3?`${Math.round(a/1e3)}k`:String(a)}
