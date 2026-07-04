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
`;function f(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}var F=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;var w=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let t=this.dataset.theme;this.#a(),this.dataset.theme!==t&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let t=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=t;let a=(this.getAttribute("brand")||"").trim().toLowerCase();a==="assay"||a==="cai"||a==="watchdog"?this.dataset.brand=a:delete this.dataset.brand}json(t,a){let r=this.getAttribute(t);if(r==null||r.trim()==="")return a;try{return JSON.parse(r)}catch{return a}}};var d=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function k(e){return e>=90?d[4]:e>=70?d[3]:e>=50?d[2]:e>=25?d[1]:d[0]}function N(e){let t=Math.max(0,Math.min(100,e)),a=d.length-1;for(;a>0&&t<d[a].floor;)a--;let n=(a<d.length-1?d[a+1].floor:100)-d[a].floor,i=n>0?Math.max(0,Math.min(1,(t-d[a].floor)/n)):.5;return{leftPct:a*20+i*20,third:i<1/3?0:i<2/3?1:2,key:d[a].key}}var L=0;function j(){return"s"+(L++).toString(36)}function U(e,{variant:t="diamond",caps:a=!1,className:r}={}){let n=N(e),i=k(e),l=`${n.leftPct.toFixed(2)}%`,u=d.map(h=>`<i class="seg-${h.key}"></i>`).join(""),o;t==="diamond"?o=`<div class="cai-mk cai-diamond" style="left:${l};--dia:var(--band-${i.key})"><span class="cai-diamond-foot"></span></div>`:o=`<div class="cai-mk cai-pin" style="left:${l}"><span class="cai-pin-badge">${Math.round(e)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let s=a?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(a?"":" compact")+(r?` ${r}`:"")}"><div class="cai-rail" role="img" aria-label="${Math.round(e)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${u}</div>${o}</div>${s}</div>`}function O(e){let t=e[0],a=e[e.length-1],r=Math.round(a)-Math.round(t);return`Trend: ${r>=1?`improving (up ${r})`:r<=-1?`declining (down ${-r})`:"steady"} over the last ${e.length} scans.`}function G(e){let t=j(),a=320,r=36,n=4,i=e.length,l=Math.min(...e),u=Math.max(...e),o=Math.max(0,l-3),s=Math.min(100,u+3),p=s-o||1,h=c=>c/(i-1)*(a-2*n)+n,D=c=>n+(1-(c-o)/p)*(r-2*n),b=e.map((c,m)=>[h(m),D(c)]),$=`M ${b[0][0].toFixed(1)} ${b[0][1].toFixed(1)}`;for(let c=0;c<i-1;c++){let m=b[Math.max(0,c-1)],y=b[c],x=b[c+1],S=b[Math.min(i-1,c+2)],A=[y[0]+(x[0]-m[0])/6,y[1]+(x[1]-m[1])/6],C=[x[0]-(S[0]-y[0])/6,x[1]-(S[1]-y[1])/6];$+=` C ${A[0].toFixed(1)} ${A[1].toFixed(1)}, ${C[0].toFixed(1)} ${C[1].toFixed(1)}, ${x[0].toFixed(1)} ${x[1].toFixed(1)}`}let g=[],v=k(o).key;g.push({at:0,key:v});for(let c of d)if(c.floor>o&&c.floor<s){let m=(c.floor-o)/p;g.push({at:m,key:v}),v=c.key,g.push({at:m,key:v})}g.push({at:1,key:v});let I=g.map(c=>`<stop offset="${(c.at*100).toFixed(1)}%" stop-color="var(--band-${c.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${a} ${r}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${t}" gradientUnits="userSpaceOnUse" x1="0" y1="${r-n}" x2="0" y2="${n}">${I}</linearGradient></defs><path d="${$}" fill="none" stroke="url(#caisg-${t})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${f(O(e))}</span>`}function B(e){let t=Math.max(0,Math.min(100,Number(e.score)||0)),a=k(t),r=Array.isArray(e.series)&&e.series.length>=2?e.series:null,n=e.arcFirst!=null&&e.arcBest!=null&&Number.isFinite(Number(e.arcFirst))&&Number.isFinite(Number(e.arcBest)),i=n?Math.round(Number(e.arcBest))-Math.round(Number(e.arcFirst)):0,l=(e.lenses||[]).filter(s=>s&&s.label),u=(e.rows||[]).filter(s=>s&&s.label),o="";if(e.sealText&&(o+=`<span class="cai-seal">${f(e.sealText)}</span>`),o+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${f(e.name)}</span>`+(e.owner?`<span class="cai-owner">by ${f(e.owner)}</span>`:"")+`</span><span class="cai-chip band-${a.key}">${f(a.label)}</span></div>`,o+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${a.key}">${Math.round(t)}</span><span class="cai-unit"> / 100</span></div>`,o+=U(t,{variant:"diamond"}),r&&(o+=G(r)),n&&(o+=`<div class="cai-arc"><span class="cai-arc-from">${Math.round(Number(e.arcFirst))}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${a.key}">${Math.round(Number(e.arcBest))}</span>`+(i>=1?`<span class="cai-arc-up">\u2191 +${i}</span>`:"")+"</div>"),l.length>0){o+='<div class="cai-lenses">';for(let s of l){let p=s.value==null?null:Number(s.value),h=p==null?null:k(p);o+=`<div class="cai-lens"><span class="cai-lens-name">${f(s.label)}</span>`,o+='<span class="cai-lens-bar">',p!=null&&h&&(o+=`<span class="cai-lens-fill fill-${h.key}" style="width:${Math.max(2,Math.round(p))}%"></span>`),o+="</span>",p==null||!h?o+='<span class="cai-lens-num cai-muted">\u2014</span>':o+=`<span class="cai-lens-num ink-${h.key}">${Math.round(p)}</span>`,o+="</div>"}o+="</div>"}if(u.length>0){o+='<div class="cai-rows">';for(let s of u)o+=`<div class="cai-row"><span>${f(s.label)}</span><b class="${s.mono?"mono":""}">${f(s.value)}</b></div>`;o+="</div>"}return o}var z=`
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
`;function E(e){if(!e||!e.name||e.score==null)return null;let t;return Array.isArray(e.series)?t=e.series.map(a=>Number(a)).filter(a=>Number.isFinite(a)):t=String(e.series||"").split(",").map(a=>Number(a.trim())).filter(a=>Number.isFinite(a)),{name:e.name,owner:e.owner||void 0,score:Number(e.score),series:t.length>=2?t:void 0,arcFirst:e.arcFirst??null,arcBest:e.arcBest??null,lenses:(e.lenses||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value??null})),rows:(e.rows||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value||"",mono:!!a.mono})),href:e.href||void 0,sealText:e.sealText||void 0}}var P=[["codeHealth","Code health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Readiness"],["securityCompliance","Security"]];function J(e){return String(e).replace(/\B(?=(\d{3})+(?!\d))/g,",")}function W(e){let t=e.publishedAt||e.lastUpdated,a="";if(t){let i=new Date(t);Number.isNaN(i.getTime())||(a=i.toLocaleDateString("en-GB",{day:"numeric",month:"long",year:"numeric"}))}let r=e.productionLoc>0?`${J(e.productionLoc)} lines`:"",n=[a,r].filter(Boolean);return n.length?n.join(" \xB7 "):null}function R(e){if(!e)return null;let t=e.bestScore!=null?e.bestScore:e.headlineScore;if(t==null)return null;let a=Array.isArray(e.series)&&e.series.length>=2,r=P.map(([l,u])=>({label:u,value:e[l]==null?null:Number(e[l])})).filter(l=>l.value!=null),n=[],i=W(e);return i&&n.push({label:"Measured",value:i}),e.costApprox&&n.push({label:"Rebuild cost",value:e.costApprox}),e.busFactor>0&&e.authorCount>0&&n.push({label:"Bus factor",value:`${e.busFactor} of ${e.authorCount} devs`}),{name:e.name,owner:e.owner||void 0,score:Number(t),series:a?e.series.map(Number):void 0,arcFirst:a?e.firstScore:null,arcBest:a?e.bestScore:null,lenses:r,rows:n}}function H(e){let t=e.bestRunId||e.BestRunId||"";return"/api/oss/"+encodeURIComponent(e.owner)+"/"+encodeURIComponent(e.name)+"/report?run="+t}function T(e,{owner:t,name:a}={}){if(!Array.isArray(e)||e.length===0)return null;if(t&&a){let r=e.find(n=>n.owner===t&&n.name===a);if(r)return r}return e.slice().sort((r,n)=>{let i=r.bestScore!=null?r.bestScore:r.headlineScore;return(n.bestScore!=null?n.bestScore:n.headlineScore)-i||(n.productionLoc||0)-(r.productionLoc||0)})[0]}async function _(e,t,a){if(!e)return a;try{let r=await fetch(e.replace(/\/$/,"")+t);return r.ok?await r.json():a}catch{return a}}var X={name:"checkout-service",owner:"acme",score:62,series:[45,48,47,52,55,58,60,62],arcFirst:45,arcBest:62,lenses:[{label:"Code health",value:68},{label:"Architecture",value:55},{label:"Maturity",value:63},{label:"Readiness",value:52},{label:"Security",value:71}],rows:[{label:"Measured",value:"1 July 2026 \xB7 4.2M lines"},{label:"Reproducible fingerprint",value:"a3f9\u2026e021",mono:!0},{label:"Shared with",value:"3 parties"}]},q=M+F+z+`
:host { display: block; }
.cai-card-cap { margin-top: 0.85rem; }
`;customElements.define("cai-score-card",class extends w{async liveLoad(){let e=this.apiBase();if(!e)return;let t=this.getAttribute("owner")||"",a=this.getAttribute("name")||"",r=await _(e,"/api/oss",null);if(!Array.isArray(r)||r.length===0)return;let n=T(r,{owner:t,name:a}),i=R(n);i&&(i.href=H(n),this._live=i,this.render(this.shadowRoot))}render(e){let t;if(this._live)t={...this._live};else{let h=E(this.json("card",null));t=h?{...h}:{...X}}let a=this.getAttribute("name"),r=this.getAttribute("owner"),n=this.getAttribute("score"),i=this.getAttribute("seal-text"),l=this.getAttribute("href");this._live||(a!=null&&a!==""&&(t.name=a),r!=null&&r!==""&&(t.owner=r)),n!=null&&n!==""&&Number.isFinite(Number(n))&&(t.score=Number(n)),i!=null&&i!==""&&(t.sealText=i),l!=null&&l!==""&&(t.href=l);let u=this.getAttribute("caption"),o=t.href?"a":"div",s=t.href?` href="${f(t.href)}"`:"",p=`<style>${q}</style>`;p+=`<${o} class="cai-card"${s}>${B(t)}</${o}>`,u&&(p+=`<p class="cai-card-cap">${f(u)}</p>`),e.innerHTML=p}});
