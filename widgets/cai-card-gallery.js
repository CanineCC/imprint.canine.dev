var F=`
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
`;function l(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function w(a){if(a==null||a==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",t=0,r;for(;(r=n.exec(a))!==null;){r.index>t&&(e+=l(a.slice(t,r.index)));let o=r[0];if(o.startsWith("**"))e+=`<strong>${l(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${l(o.slice(1,-1))}</code>`;else{let p=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);p?e+=`<a href="${l(p[2])}">${l(p[1])}</a>`:e+=l(o)}t=r.index+o.length}return t<a.length&&(e+=l(a.slice(t))),e}var z=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function N(a){let n=a.getAttribute("kicker"),e=a.getAttribute("heading"),t=a.getAttribute("lede");if(!n&&!e&&!t)return"";let r='<div class="mk-section-head">';return n&&(r+=`<span class="mk-kicker">${l(n)}</span>`),e&&(r+=`<h2>${w(e)}</h2>`),t&&(r+=`<p>${w(t)}</p>`),r+="</div>",r}var E=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,$=class extends HTMLElement{#a;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),this.#a=new MutationObserver(()=>{let n=this.dataset.theme;this.#e(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#a.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}disconnectedCallback(){this.#a?.disconnect()}#e(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(n,e){let t=this.getAttribute(n);if(t==null||t.trim()==="")return e;try{return JSON.parse(t)}catch{return e}}};var d=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function k(a){return a>=90?d[4]:a>=70?d[3]:a>=50?d[2]:a>=25?d[1]:d[0]}function H(a){let n=Math.max(0,Math.min(100,a)),e=d.length-1;for(;e>0&&n<d[e].floor;)e--;let r=(e<d.length-1?d[e+1].floor:100)-d[e].floor,o=r>0?Math.max(0,Math.min(1,(n-d[e].floor)/r)):.5;return{leftPct:e*20+o*20,third:o<1/3?0:o<2/3?1:2,key:d[e].key}}var j=0;function O(){return"s"+(j++).toString(36)}function R(a,{variant:n="diamond",caps:e=!1,className:t}={}){let r=H(a),o=k(a),p=`${r.leftPct.toFixed(2)}%`,b=d.map(m=>`<i class="seg-${m.key}"></i>`).join(""),i;n==="diamond"?i=`<div class="cai-mk cai-diamond" style="left:${p};--dia:var(--band-${o.key})"><span class="cai-diamond-foot"></span></div>`:i=`<div class="cai-mk cai-pin" style="left:${p}"><span class="cai-pin-badge">${Math.round(a)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let c=e?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(e?"":" compact")+(t?` ${t}`:"")}"><div class="cai-rail" role="img" aria-label="${Math.round(a)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${b}</div>${i}</div>${c}</div>`}function W(a){let n=a[0],e=a[a.length-1],t=Math.round(e)-Math.round(n);return`Trend: ${t>=1?`improving (up ${t})`:t<=-1?`declining (down ${-t})`:"steady"} over the last ${a.length} scans.`}function X(a){let n=O(),e=320,t=36,r=4,o=a.length,p=Math.min(...a),b=Math.max(...a),i=Math.max(0,p-3),c=Math.min(100,b+3),f=c-i||1,m=s=>s/(o-1)*(e-2*r)+r,I=s=>r+(1-(s-i)/f)*(t-2*r),x=a.map((s,h)=>[m(h),I(s)]),S=`M ${x[0][0].toFixed(1)} ${x[0][1].toFixed(1)}`;for(let s=0;s<o-1;s++){let h=x[Math.max(0,s-1)],y=x[s],u=x[s+1],M=x[Math.min(o-1,s+2)],C=[y[0]+(u[0]-h[0])/6,y[1]+(u[1]-h[1])/6],A=[u[0]-(M[0]-y[0])/6,u[1]-(M[1]-y[1])/6];S+=` C ${C[0].toFixed(1)} ${C[1].toFixed(1)}, ${A[0].toFixed(1)} ${A[1].toFixed(1)}, ${u[0].toFixed(1)} ${u[1].toFixed(1)}`}let g=[],v=k(i).key;g.push({at:0,key:v});for(let s of d)if(s.floor>i&&s.floor<c){let h=(s.floor-i)/f;g.push({at:h,key:v}),v=s.key,g.push({at:h,key:v})}g.push({at:1,key:v});let D=g.map(s=>`<stop offset="${(s.at*100).toFixed(1)}%" stop-color="var(--band-${s.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${e} ${t}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${n}" gradientUnits="userSpaceOnUse" x1="0" y1="${t-r}" x2="0" y2="${r}">${D}</linearGradient></defs><path d="${S}" fill="none" stroke="url(#caisg-${n})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${l(W(a))}</span>`}function B(a){let n=Math.max(0,Math.min(100,Number(a.score)||0)),e=k(n),t=Array.isArray(a.series)&&a.series.length>=2?a.series:null,r=a.arcFirst!=null&&a.arcBest!=null&&Number.isFinite(Number(a.arcFirst))&&Number.isFinite(Number(a.arcBest)),o=r?Math.round(Number(a.arcBest))-Math.round(Number(a.arcFirst)):0,p=(a.lenses||[]).filter(c=>c&&c.label),b=(a.rows||[]).filter(c=>c&&c.label),i="";if(a.sealText&&(i+=`<span class="cai-seal">${l(a.sealText)}</span>`),i+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${l(a.name)}</span>`+(a.owner?`<span class="cai-owner">by ${l(a.owner)}</span>`:"")+`</span><span class="cai-chip band-${e.key}">${l(e.label)}</span></div>`,i+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${e.key}">${Math.round(n)}</span><span class="cai-unit"> / 100</span></div>`,i+=R(n,{variant:"diamond"}),t&&(i+=X(t)),r&&(i+=`<div class="cai-arc"><span class="cai-arc-from">${Math.round(Number(a.arcFirst))}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${e.key}">${Math.round(Number(a.arcBest))}</span>`+(o>=1?`<span class="cai-arc-up">\u2191 +${o}</span>`:"")+"</div>"),p.length>0){i+='<div class="cai-lenses">';for(let c of p){let f=c.value==null?null:Number(c.value),m=f==null?null:k(f);i+=`<div class="cai-lens"><span class="cai-lens-name">${l(c.label)}</span>`,i+='<span class="cai-lens-bar">',f!=null&&m&&(i+=`<span class="cai-lens-fill fill-${m.key}" style="width:${Math.max(2,Math.round(f))}%"></span>`),i+="</span>",f==null||!m?i+='<span class="cai-lens-num cai-muted">\u2014</span>':i+=`<span class="cai-lens-num ink-${m.key}">${Math.round(f)}</span>`,i+="</div>"}i+="</div>"}if(b.length>0){i+='<div class="cai-rows">';for(let c of b)i+=`<div class="cai-row"><span>${l(c.label)}</span><b class="${c.mono?"mono":""}">${l(c.value)}</b></div>`;i+="</div>"}return i}var T=`
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
`;function _(a){if(!a||!a.name||a.score==null)return null;let n;return Array.isArray(a.series)?n=a.series.map(e=>Number(e)).filter(e=>Number.isFinite(e)):n=String(a.series||"").split(",").map(e=>Number(e.trim())).filter(e=>Number.isFinite(e)),{name:a.name,owner:a.owner||void 0,score:Number(a.score),series:n.length>=2?n:void 0,arcFirst:a.arcFirst??null,arcBest:a.arcBest??null,lenses:(a.lenses||[]).filter(e=>e&&e.label).map(e=>({label:e.label,value:e.value??null})),rows:(a.rows||[]).filter(e=>e&&e.label).map(e=>({label:e.label,value:e.value||"",mono:!!e.mono})),href:a.href||void 0,sealText:a.sealText||void 0}}var q=F+z+E+T+`
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers \u2014 the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;customElements.define("cai-card-gallery",class extends ${render(a){let n=(this.json("cards",[])||[]).map(r=>_(r)).filter(Boolean),e=this.getAttribute("footnote"),t=`<style>${q}</style>`;t+=N(this),t+='<div class="mk-cardgallery">';for(let r of n){let o=r.href?"a":"div",p=r.href?` href="${r.href.replace(/"/g,"&quot;")}"`:"";t+=`<${o} class="cai-card"${p}>${B(r)}</${o}>`}t+="</div>",e&&(t+=`<p class="mk-grid-foot">${w(e)}</p>`),a.innerHTML=t}});
