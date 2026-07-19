var M=`
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
`;function d(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function w(e){if(e==null||e==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,a="",t=0,o;for(;(o=r.exec(e))!==null;){o.index>t&&(a+=d(e.slice(t,o.index)));let i=o[0];if(i.startsWith("**"))a+=`<strong>${d(i.slice(2,-2))}</strong>`;else if(i.startsWith("`"))a+=`<code>${d(i.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(i);s?a+=`<a href="${d(s[2])}">${d(s[1])}</a>`:a+=d(i)}t=o.index+i.length}return t<e.length&&(a+=d(e.slice(t))),a}var F=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function B(e){let r=e.getAttribute("kicker"),a=e.getAttribute("heading"),t=e.getAttribute("lede");if(!r&&!a&&!t)return"";let o='<div class="mk-section-head">';return r&&(o+=`<span class="mk-kicker">${d(r)}</span>`),a&&(o+=`<h2>${w(a)}</h2>`),t&&(o+=`<p>${w(t)}</p>`),o+="</div>",o}var H=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,$=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#a(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let a=(this.getAttribute("brand")||"").trim().toLowerCase();a==="assay"||a==="cai"||a==="watchdog"?this.dataset.brand=a:delete this.dataset.brand}json(r,a){let t=this.getAttribute(r);if(t==null||t.trim()==="")return a;try{return JSON.parse(t)}catch{return a}}};var f=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function k(e){return e>=90?f[4]:e>=70?f[3]:e>=50?f[2]:e>=25?f[1]:f[0]}function z(e){let r=Math.max(0,Math.min(100,e)),a=f.length-1;for(;a>0&&r<f[a].floor;)a--;let o=(a<f.length-1?f[a+1].floor:100)-f[a].floor,i=o>0?Math.max(0,Math.min(1,(r-f[a].floor)/o)):.5;return{leftPct:a*20+i*20,third:i<1/3?0:i<2/3?1:2,key:f[a].key}}var P=0;function W(){return"s"+(P++).toString(36)}function X(e,{variant:r="diamond",caps:a=!1,className:t}={}){let o=z(e),i=k(e),s=`${o.leftPct.toFixed(2)}%`,p=f.map(h=>`<i class="seg-${h.key}"></i>`).join(""),n;r==="diamond"?n=`<div class="cai-mk cai-diamond" style="left:${s};--dia:var(--band-${i.key})"><span class="cai-diamond-foot"></span></div>`:n=`<div class="cai-mk cai-pin" style="left:${s}"><span class="cai-pin-badge">${Math.round(e)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let c=a?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(a?"":" compact")+(t?` ${t}`:"")}"><div class="cai-rail" role="img" aria-label="${Math.round(e)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${p}</div>${n}</div>${c}</div>`}function q(e){let r=e[0],a=e[e.length-1],t=Math.round(a)-Math.round(r);return`Trend: ${t>=1?`improving (up ${t})`:t<=-1?`declining (down ${-t})`:"steady"} over the last ${e.length} scans.`}function J(e){let r=W(),a=320,t=36,o=4,i=e.length,s=Math.min(...e),p=Math.max(...e),n=Math.max(0,s-3),c=Math.min(100,p+3),u=c-n||1,h=l=>l/(i-1)*(a-2*o)+o,O=l=>o+(1-(l-n)/u)*(t-2*o),b=e.map((l,m)=>[h(m),O(l)]),S=`M ${b[0][0].toFixed(1)} ${b[0][1].toFixed(1)}`;for(let l=0;l<i-1;l++){let m=b[Math.max(0,l-1)],y=b[l],g=b[l+1],A=b[Math.min(i-1,l+2)],C=[y[0]+(g[0]-m[0])/6,y[1]+(g[1]-m[1])/6],N=[g[0]-(A[0]-y[0])/6,g[1]-(A[1]-y[1])/6];S+=` C ${C[0].toFixed(1)} ${C[1].toFixed(1)}, ${N[0].toFixed(1)} ${N[1].toFixed(1)}, ${g[0].toFixed(1)} ${g[1].toFixed(1)}`}let x=[],v=k(n).key;x.push({at:0,key:v});for(let l of f)if(l.floor>n&&l.floor<c){let m=(l.floor-n)/u;x.push({at:m,key:v}),v=l.key,x.push({at:m,key:v})}x.push({at:1,key:v});let G=x.map(l=>`<stop offset="${(l.at*100).toFixed(1)}%" stop-color="var(--band-${l.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${a} ${t}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${r}" gradientUnits="userSpaceOnUse" x1="0" y1="${t-o}" x2="0" y2="${o}">${G}</linearGradient></defs><path d="${S}" fill="none" stroke="url(#caisg-${r})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${d(q(e))}</span>`}function E(e){let r=Math.max(0,Math.min(100,Number(e.score)||0)),a=k(r),t=Array.isArray(e.series)&&e.series.length>=2?e.series:null,o=e.arcFirst!=null&&e.arcBest!=null&&Number.isFinite(Number(e.arcFirst))&&Number.isFinite(Number(e.arcBest)),i=o?Math.round(Number(e.arcBest))-Math.round(Number(e.arcFirst)):0,s=(e.lenses||[]).filter(c=>c&&c.label),p=(e.rows||[]).filter(c=>c&&c.label),n="";if(e.sealText&&(n+=`<span class="cai-seal">${d(e.sealText)}</span>`),n+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${d(e.name)}</span>`+(e.owner?`<span class="cai-owner">by ${d(e.owner)}</span>`:"")+`</span><span class="cai-chip band-${a.key}">${d(a.label)}</span></div>`,n+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${a.key}">${Math.round(r)}</span><span class="cai-unit"> / 100</span></div>`,n+=X(r,{variant:"diamond"}),t&&(n+=J(t)),o&&(n+=`<div class="cai-arc"><span class="cai-arc-from">${Math.round(Number(e.arcFirst))}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${a.key}">${Math.round(Number(e.arcBest))}</span>`+(i>=1?`<span class="cai-arc-up">\u2191 +${i}</span>`:"")+"</div>"),s.length>0){n+='<div class="cai-lenses">';for(let c of s){let u=c.value==null?null:Number(c.value),h=u==null?null:k(u);n+=`<div class="cai-lens"><span class="cai-lens-name">${d(c.label)}</span>`,n+='<span class="cai-lens-bar">',u!=null&&h&&(n+=`<span class="cai-lens-fill fill-${h.key}" style="width:${Math.max(2,Math.round(u))}%"></span>`),n+="</span>",u==null||!h?n+='<span class="cai-lens-num cai-muted">\u2014</span>':n+=`<span class="cai-lens-num ink-${h.key}">${Math.round(u)}</span>`,n+="</div>"}n+="</div>"}if(p.length>0){n+='<div class="cai-rows">';for(let c of p)n+=`<div class="cai-row"><span>${d(c.label)}</span><b class="${c.mono?"mono":""}">${d(c.value)}</b></div>`;n+="</div>"}return n}var R=`
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
`;function T(e){if(!e||!e.name||e.score==null)return null;let r;return Array.isArray(e.series)?r=e.series.map(a=>Number(a)).filter(a=>Number.isFinite(a)):r=String(e.series||"").split(",").map(a=>Number(a.trim())).filter(a=>Number.isFinite(a)),{name:e.name,owner:e.owner||void 0,score:Number(e.score),series:r.length>=2?r:void 0,arcFirst:e.arcFirst??null,arcBest:e.arcBest??null,lenses:(e.lenses||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value??null})),rows:(e.rows||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value||"",mono:!!a.mono})),href:e.href||void 0,sealText:e.sealText||void 0}}var K=[["codeHealth","Code health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Readiness"],["securityCompliance","Security"]];function Q(e){return String(e).replace(/\B(?=(\d{3})+(?!\d))/g,",")}function V(e){let r=e.publishedAt||e.lastUpdated,a="";if(r){let i=new Date(r);Number.isNaN(i.getTime())||(a=i.toLocaleDateString("en-GB",{day:"numeric",month:"long",year:"numeric"}))}let t=e.productionLoc>0?`${Q(e.productionLoc)} lines`:"",o=[a,t].filter(Boolean);return o.length?o.join(" \xB7 "):null}function I(e){if(!e)return null;let r=e.bestScore!=null?e.bestScore:e.headlineScore;if(r==null)return null;let a=Array.isArray(e.series)&&e.series.length>=2,t=K.map(([s,p])=>({label:p,value:e[s]==null?null:Number(e[s])})).filter(s=>s.value!=null),o=[],i=V(e);return i&&o.push({label:"Measured",value:i}),e.costApprox&&o.push({label:"Rebuild cost",value:e.costApprox}),e.busFactor>0&&e.authorCount>0&&o.push({label:"Bus factor",value:`${e.busFactor} of ${e.authorCount} devs`}),{name:e.name,owner:e.owner||void 0,score:Number(r),series:a?e.series.map(Number):void 0,arcFirst:a?e.firstScore:null,arcBest:a?e.bestScore:null,lenses:t,rows:o}}function L(e,r){if(!e)return"";let a=e.bestRunId||e.BestRunId,t=e.owner||e.Owner,o=e.name||e.Name;return!a||!t||!o?"":(r||"").trim().replace(/\/$/,"")+"/api/oss/"+encodeURIComponent(t)+"/"+encodeURIComponent(o)+"/report?run="+encodeURIComponent(a)}var _=new Map;function Y(e,r,a){let t=(e||"").trim().replace(/\/$/,"");if(!t)return Promise.resolve(a);let o=t+" "+r,i=_.get(o);return i||(i=(async()=>{try{let s=await fetch(t+r);return s.ok?await s.json():a}catch{return a}})(),_.set(o,i),i)}async function D(e){let r=await Y(e,"/api/oss",[]);return Array.isArray(r)?r:[]}function j(e){if(!Array.isArray(e)||e.length===0)return null;let r=t=>t.bestScore!=null?Number(t.bestScore):Number(t.headlineScore)||0,a=null;for(let t of e)(a===null||r(t)>r(a)||r(t)===r(a)&&(Number(t.productionLoc)||0)>(Number(a.productionLoc)||0))&&(a=t);return a}function U(e,r){let a=(Array.isArray(e)?e:[]).filter(n=>n&&n!==r);if(a.length===0)return[];let t=n=>n.bestScore!=null?Number(n.bestScore):Number(n.headlineScore)||0,o=n=>n.delta!=null?Number(n.delta):0,i=[],s=n=>{n&&!i.includes(n)&&i.push(n)};s([...a].sort((n,c)=>t(c)-t(n))[0]),s([...a].sort((n,c)=>o(c)-o(n)).find(n=>!i.includes(n)));let p=a.filter(n=>!i.includes(n));return p.length>0&&s(p[Math.floor(Math.random()*p.length)]),i}var Z=6,ee=M+F+H+R+`
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers \u2014 the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;customElements.define("cai-card-gallery",class extends ${#e(){let e=this.getAttribute("count"),r=Number(e);return e!=null&&e!==""&&Number.isFinite(r)&&r>0?Math.floor(r):Z}async liveLoad(){let e=this.apiBase();if(!e)return;let r=await D(e);if(r.length===0)return;let a=j(r),t=U(r,a);if(t.length===0)return;let o=this.#e(),i=[];for(let s of t){if(i.length>=o)break;let p=I(s);if(!p)continue;let n=L(s,e);n&&(p.href=n),i.push(p)}i.length!==0&&(this._live=i,this.render(this.shadowRoot))}render(e){let r=this._live?this._live:(this.json("cards",[])||[]).map(o=>T(o)).filter(Boolean),a=this.getAttribute("footnote"),t=`<style>${ee}</style>`;t+=B(this),t+='<div class="mk-cardgallery">';for(let o of r){let i=o.href?"a":"div",s=o.href?` href="${o.href.replace(/"/g,"&quot;")}"`:"";t+=`<${i} class="cai-card"${s}>${E(o)}</${i}>`}t+="</div>",a&&(t+=`<p class="mk-grid-foot">${w(a)}</p>`),e.innerHTML=t}});
