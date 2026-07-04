var B=`
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
`;function u(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}var E=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;var w=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let a=this.dataset.theme;this.#t(),this.dataset.theme!==a&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let a=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=a;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(a,t){let r=this.getAttribute(a);if(r==null||r.trim()==="")return t;try{return JSON.parse(r)}catch{return t}}};var p=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function k(e){return e>=90?p[4]:e>=70?p[3]:e>=50?p[2]:e>=25?p[1]:p[0]}function z(e){let a=Math.max(0,Math.min(100,e)),t=p.length-1;for(;t>0&&a<p[t].floor;)t--;let n=(t<p.length-1?p[t+1].floor:100)-p[t].floor,i=n>0?Math.max(0,Math.min(1,(a-p[t].floor)/n)):.5;return{leftPct:t*20+i*20,third:i<1/3?0:i<2/3?1:2,key:p[t].key}}var O=0;function G(){return"s"+(O++).toString(36)}function P(e,{variant:a="diamond",caps:t=!1,className:r}={}){let n=z(e),i=k(e),s=`${n.leftPct.toFixed(2)}%`,d=p.map(h=>`<i class="seg-${h.key}"></i>`).join(""),o;a==="diamond"?o=`<div class="cai-mk cai-diamond" style="left:${s};--dia:var(--band-${i.key})"><span class="cai-diamond-foot"></span></div>`:o=`<div class="cai-mk cai-pin" style="left:${s}"><span class="cai-pin-badge">${Math.round(e)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let c=t?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(t?"":" compact")+(r?` ${r}`:"")}"><div class="cai-rail" role="img" aria-label="${Math.round(e)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${d}</div>${o}</div>${c}</div>`}function W(e){let a=e[0],t=e[e.length-1],r=Math.round(t)-Math.round(a);return`Trend: ${r>=1?`improving (up ${r})`:r<=-1?`declining (down ${-r})`:"steady"} over the last ${e.length} scans.`}function J(e){let a=G(),t=320,r=36,n=4,i=e.length,s=Math.min(...e),d=Math.max(...e),o=Math.max(0,s-3),c=Math.min(100,d+3),f=c-o||1,h=l=>l/(i-1)*(t-2*n)+n,U=l=>n+(1-(l-o)/f)*(r-2*n),b=e.map((l,m)=>[h(m),U(l)]),M=`M ${b[0][0].toFixed(1)} ${b[0][1].toFixed(1)}`;for(let l=0;l<i-1;l++){let m=b[Math.max(0,l-1)],y=b[l],x=b[l+1],F=b[Math.min(i-1,l+2)],N=[y[0]+(x[0]-m[0])/6,y[1]+(x[1]-m[1])/6],R=[x[0]-(F[0]-y[0])/6,x[1]-(F[1]-y[1])/6];M+=` C ${N[0].toFixed(1)} ${N[1].toFixed(1)}, ${R[0].toFixed(1)} ${R[1].toFixed(1)}, ${x[0].toFixed(1)} ${x[1].toFixed(1)}`}let g=[],v=k(o).key;g.push({at:0,key:v});for(let l of p)if(l.floor>o&&l.floor<c){let m=(l.floor-o)/f;g.push({at:m,key:v}),v=l.key,g.push({at:m,key:v})}g.push({at:1,key:v});let j=g.map(l=>`<stop offset="${(l.at*100).toFixed(1)}%" stop-color="var(--band-${l.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${t} ${r}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${a}" gradientUnits="userSpaceOnUse" x1="0" y1="${r-n}" x2="0" y2="${n}">${j}</linearGradient></defs><path d="${M}" fill="none" stroke="url(#caisg-${a})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${u(W(e))}</span>`}function H(e){let a=Math.max(0,Math.min(100,Number(e.score)||0)),t=k(a),r=Array.isArray(e.series)&&e.series.length>=2?e.series:null,n=e.arcFirst!=null&&e.arcBest!=null&&Number.isFinite(Number(e.arcFirst))&&Number.isFinite(Number(e.arcBest)),i=n?Math.round(Number(e.arcBest))-Math.round(Number(e.arcFirst)):0,s=(e.lenses||[]).filter(c=>c&&c.label),d=(e.rows||[]).filter(c=>c&&c.label),o="";if(e.sealText&&(o+=`<span class="cai-seal">${u(e.sealText)}</span>`),o+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${u(e.name)}</span>`+(e.owner?`<span class="cai-owner">by ${u(e.owner)}</span>`:"")+`</span><span class="cai-chip band-${t.key}">${u(t.label)}</span></div>`,o+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${t.key}">${Math.round(a)}</span><span class="cai-unit"> / 100</span></div>`,o+=P(a,{variant:"diamond"}),r&&(o+=J(r)),n&&(o+=`<div class="cai-arc"><span class="cai-arc-from">${Math.round(Number(e.arcFirst))}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${t.key}">${Math.round(Number(e.arcBest))}</span>`+(i>=1?`<span class="cai-arc-up">\u2191 +${i}</span>`:"")+"</div>"),s.length>0){o+='<div class="cai-lenses">';for(let c of s){let f=c.value==null?null:Number(c.value),h=f==null?null:k(f);o+=`<div class="cai-lens"><span class="cai-lens-name">${u(c.label)}</span>`,o+='<span class="cai-lens-bar">',f!=null&&h&&(o+=`<span class="cai-lens-fill fill-${h.key}" style="width:${Math.max(2,Math.round(f))}%"></span>`),o+="</span>",f==null||!h?o+='<span class="cai-lens-num cai-muted">\u2014</span>':o+=`<span class="cai-lens-num ink-${h.key}">${Math.round(f)}</span>`,o+="</div>"}o+="</div>"}if(d.length>0){o+='<div class="cai-rows">';for(let c of d)o+=`<div class="cai-row"><span>${u(c.label)}</span><b class="${c.mono?"mono":""}">${u(c.value)}</b></div>`;o+="</div>"}return o}var I=`
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
`;function T(e){if(!e||!e.name||e.score==null)return null;let a;return Array.isArray(e.series)?a=e.series.map(t=>Number(t)).filter(t=>Number.isFinite(t)):a=String(e.series||"").split(",").map(t=>Number(t.trim())).filter(t=>Number.isFinite(t)),{name:e.name,owner:e.owner||void 0,score:Number(e.score),series:a.length>=2?a:void 0,arcFirst:e.arcFirst??null,arcBest:e.arcBest??null,lenses:(e.lenses||[]).filter(t=>t&&t.label).map(t=>({label:t.label,value:t.value??null})),rows:(e.rows||[]).filter(t=>t&&t.label).map(t=>({label:t.label,value:t.value||"",mono:!!t.mono})),href:e.href||void 0,sealText:e.sealText||void 0}}var X=[["codeHealth","Code health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Readiness"],["securityCompliance","Security"]];function q(e){return String(e).replace(/\B(?=(\d{3})+(?!\d))/g,",")}function K(e){let a=e.publishedAt||e.lastUpdated,t="";if(a){let i=new Date(a);Number.isNaN(i.getTime())||(t=i.toLocaleDateString("en-GB",{day:"numeric",month:"long",year:"numeric"}))}let r=e.productionLoc>0?`${q(e.productionLoc)} lines`:"",n=[t,r].filter(Boolean);return n.length?n.join(" \xB7 "):null}function _(e){if(!e)return null;let a=e.bestScore!=null?e.bestScore:e.headlineScore;if(a==null)return null;let t=Array.isArray(e.series)&&e.series.length>=2,r=X.map(([s,d])=>({label:d,value:e[s]==null?null:Number(e[s])})).filter(s=>s.value!=null),n=[],i=K(e);return i&&n.push({label:"Measured",value:i}),e.costApprox&&n.push({label:"Rebuild cost",value:e.costApprox}),e.busFactor>0&&e.authorCount>0&&n.push({label:"Bus factor",value:`${e.busFactor} of ${e.authorCount} devs`}),{name:e.name,owner:e.owner||void 0,score:Number(a),series:t?e.series.map(Number):void 0,arcFirst:t?e.firstScore:null,arcBest:t?e.bestScore:null,lenses:r,rows:n}}function $(e){if(!e)return!1;let a=e.sourceUrl||e.SourceUrl,t=e.bestRunId||e.BestRunId;return!!(a&&t)}function S(e,a){if(!$(e))return"";let t=(a||"").trim().replace(/\/$/,""),r=e.bestRunId||e.BestRunId;return t+"/api/oss/"+encodeURIComponent(e.owner)+"/"+encodeURIComponent(e.name)+"/report?run="+encodeURIComponent(r)}function L(e,{owner:a,name:t}={}){if(!Array.isArray(e)||e.length===0)return null;let r=e.filter($);if(a&&t){let n=r.find(i=>i.owner===a&&i.name===t);if(n)return n}return A(e)[0]||null}function A(e){return e.filter($).slice().sort((a,t)=>{let r=a.bestScore!=null?a.bestScore:a.headlineScore;return(t.bestScore!=null?t.bestScore:t.headlineScore)-r||(t.productionLoc||0)-(a.productionLoc||0)})}async function D(e,a,t){if(!e)return t;try{let r=await fetch(e.replace(/\/$/,"")+a);return r.ok?await r.json():t}catch{return t}}async function C(e){if(!e)return!1;try{return(await fetch(e,{method:"GET"})).ok}catch{return!1}}var Q={name:"checkout-service",owner:"acme",score:62,series:[45,48,47,52,55,58,60,62],arcFirst:45,arcBest:62,lenses:[{label:"Code health",value:68},{label:"Architecture",value:55},{label:"Maturity",value:63},{label:"Readiness",value:52},{label:"Security",value:71}],rows:[{label:"Measured",value:"1 July 2026 \xB7 4.2M lines"},{label:"Reproducible fingerprint",value:"a3f9\u2026e021",mono:!0},{label:"Shared with",value:"3 parties"}]},V=B+E+I+`
:host { display: block; }
.cai-card-cap { margin-top: 0.85rem; }
`;customElements.define("cai-score-card",class extends w{async liveLoad(){let e=this.apiBase();if(!e)return;let a=this.getAttribute("owner")||"",t=this.getAttribute("name")||"",r=await D(e,"/api/oss",null);if(!Array.isArray(r)||r.length===0)return;let n=null,i="";if(a&&t){if(n=L(r,{owner:a,name:t}),n){let d=S(n,e);d&&await C(d)&&(i=d)}}else for(let d of A(r)){let o=S(d,e);if(o&&await C(o)){n=d,i=o;break}}if(!n)return;let s=_(n);s&&(i&&(s.href=i),this._live=s,this.render(this.shadowRoot))}render(e){let a;if(this._live)a={...this._live};else{let h=T(this.json("card",null));a=h?{...h}:{...Q}}let t=this.getAttribute("name"),r=this.getAttribute("owner"),n=this.getAttribute("score"),i=this.getAttribute("seal-text"),s=this.getAttribute("href");this._live||(t!=null&&t!==""&&(a.name=t),r!=null&&r!==""&&(a.owner=r)),n!=null&&n!==""&&Number.isFinite(Number(n))&&(a.score=Number(n)),i!=null&&i!==""&&(a.sealText=i),s!=null&&s!==""&&(a.href=s);let d=this._live?null:this.getAttribute("caption"),o=a.href?"a":"div",c=a.href?` href="${u(a.href)}"`:"",f=`<style>${V}</style>`;f+=`<${o} class="cai-card"${c}>${H(a)}</${o}>`,d&&(f+=`<p class="cai-card-cap">${u(d)}</p>`),e.innerHTML=f}});
