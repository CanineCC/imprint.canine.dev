var m=`
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
`;function o(t){return String(t??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function l(t){if(t==null||t==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",a=0,n;for(;(n=r.exec(t))!==null;){n.index>a&&(e+=o(t.slice(a,n.index)));let i=n[0];if(i.startsWith("**"))e+=`<strong>${o(i.slice(2,-2))}</strong>`;else if(i.startsWith("`"))e+=`<code>${o(i.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(i);s?e+=`<a href="${o(s[2])}">${o(s[1])}</a>`:e+=o(i)}a=n.index+i.length}return a<t.length&&(e+=o(t.slice(a))),e}var h=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function u(t){let r=t.getAttribute("kicker"),e=t.getAttribute("heading"),a=t.getAttribute("lede");if(!r&&!e&&!a)return"";let n='<div class="mk-section-head">';return r&&(n+=`<span class="mk-kicker">${o(r)}</span>`),e&&(n+=`<h2>${l(e)}</h2>`),a&&(n+=`<p>${l(a)}</p>`),n+="</div>",n}var b=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,d=class extends HTMLElement{#a;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#a=new MutationObserver(()=>{let r=this.dataset.theme;this.#e(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#a.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#a?.disconnect()}#e(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let a=this.getAttribute(r);if(a==null||a.trim()==="")return e;try{return JSON.parse(a)}catch{return e}}};var c=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function p(t){return t>=90?c[4]:t>=70?c[3]:t>=50?c[2]:t>=25?c[1]:c[0]}var g=`
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
`;var x=m+h+b+g+`
.mk-gauges { max-width: 46rem; margin: 0 auto; display: grid; gap: 2px;
  border: 1px solid var(--line); border-radius: 8px; overflow: hidden; background: var(--line); }
.mk-gauge { background: var(--ground); padding: 13px 16px;
  display: grid; grid-template-columns: 1fr auto; gap: 4px 16px; align-items: baseline; }
.mk-gauge-name { font-weight: 650; font-size: var(--fs-sm); }
.mk-gauge-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-sm); }
.mk-gauge .cai-lens-bar { grid-column: 1 / -1; height: 6px; }
.mk-gauge-note { grid-column: 1 / -1; font-size: var(--fs-xs); color: var(--muted); line-height: 1.5; }
.mk-gauges-foot { max-width: 46rem; margin: 0.9rem auto 0; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; }
`;customElements.define("cai-lens-gauges",class extends d{render(t){let r=(this.json("lenses",[])||[]).filter(n=>n&&n.label),e=this.getAttribute("footnote"),a=`<style>${x}</style>`;if(a+=u(this),r.length===0){t.innerHTML=a;return}a+='<div class="mk-gauges">';for(let n of r){let i=n.value==null?null:Number(n.value),s=i!=null&&Number.isFinite(i)?i:null,f=s==null?null:p(s);a+='<div class="mk-gauge">',a+=`<span class="mk-gauge-name">${o(n.label)}</span>`,a+=s==null?'<span class="mk-gauge-num cai-muted">\u2014</span>':`<span class="mk-gauge-num ink-${f.key}">${s.toFixed(1)}</span>`,a+='<span class="cai-lens-bar">',s!=null&&(a+=`<span class="cai-lens-fill fill-${f.key}" style="width:${Math.max(2,Math.round(s))}%"></span>`),a+="</span>",n.note&&(a+=`<span class="mk-gauge-note">${l(String(n.note))}</span>`),a+="</div>"}a+="</div>",e&&(a+=`<p class="mk-gauges-foot">${l(e)}</p>`),t.innerHTML=a}});
