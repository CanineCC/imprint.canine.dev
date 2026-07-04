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
`;function d(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function w(e){if(e==null||e==="")return"";let t=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,a="",r=0,n;for(;(n=t.exec(e))!==null;){n.index>r&&(a+=d(e.slice(r,n.index)));let o=n[0];if(o.startsWith("**"))a+=`<strong>${d(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))a+=`<code>${d(o.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);c?a+=`<a href="${d(c[2])}">${d(c[1])}</a>`:a+=d(o)}r=n.index+o.length}return r<e.length&&(a+=d(e.slice(r))),a}var N=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function B(e){let t=e.getAttribute("kicker"),a=e.getAttribute("heading"),r=e.getAttribute("lede");if(!t&&!a&&!r)return"";let n='<div class="mk-section-head">';return t&&(n+=`<span class="mk-kicker">${d(t)}</span>`),a&&(n+=`<h2>${w(a)}</h2>`),r&&(n+=`<p>${w(r)}</p>`),n+="</div>",n}var E=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,$=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let t=this.dataset.theme;this.#a(),this.dataset.theme!==t&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let t=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=t;let a=(this.getAttribute("brand")||"").trim().toLowerCase();a==="assay"||a==="cai"||a==="watchdog"?this.dataset.brand=a:delete this.dataset.brand}json(t,a){let r=this.getAttribute(t);if(r==null||r.trim()==="")return a;try{return JSON.parse(r)}catch{return a}}};var p=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function k(e){return e>=90?p[4]:e>=70?p[3]:e>=50?p[2]:e>=25?p[1]:p[0]}function z(e){let t=Math.max(0,Math.min(100,e)),a=p.length-1;for(;a>0&&t<p[a].floor;)a--;let n=(a<p.length-1?p[a+1].floor:100)-p[a].floor,o=n>0?Math.max(0,Math.min(1,(t-p[a].floor)/n)):.5;return{leftPct:a*20+o*20,third:o<1/3?0:o<2/3?1:2,key:p[a].key}}var j=0;function G(){return"s"+(j++).toString(36)}function P(e,{variant:t="diamond",caps:a=!1,className:r}={}){let n=z(e),o=k(e),c=`${n.leftPct.toFixed(2)}%`,u=p.map(h=>`<i class="seg-${h.key}"></i>`).join(""),i;t==="diamond"?i=`<div class="cai-mk cai-diamond" style="left:${c};--dia:var(--band-${o.key})"><span class="cai-diamond-foot"></span></div>`:i=`<div class="cai-mk cai-pin" style="left:${c}"><span class="cai-pin-badge">${Math.round(e)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let l=a?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(a?"":" compact")+(r?` ${r}`:"")}"><div class="cai-rail" role="img" aria-label="${Math.round(e)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${u}</div>${i}</div>${l}</div>`}function W(e){let t=e[0],a=e[e.length-1],r=Math.round(a)-Math.round(t);return`Trend: ${r>=1?`improving (up ${r})`:r<=-1?`declining (down ${-r})`:"steady"} over the last ${e.length} scans.`}function X(e){let t=G(),a=320,r=36,n=4,o=e.length,c=Math.min(...e),u=Math.max(...e),i=Math.max(0,c-3),l=Math.min(100,u+3),f=l-i||1,h=s=>s/(o-1)*(a-2*n)+n,U=s=>n+(1-(s-i)/f)*(r-2*n),b=e.map((s,m)=>[h(m),U(s)]),S=`M ${b[0][0].toFixed(1)} ${b[0][1].toFixed(1)}`;for(let s=0;s<o-1;s++){let m=b[Math.max(0,s-1)],y=b[s],x=b[s+1],A=b[Math.min(o-1,s+2)],C=[y[0]+(x[0]-m[0])/6,y[1]+(x[1]-m[1])/6],M=[x[0]-(A[0]-y[0])/6,x[1]-(A[1]-y[1])/6];S+=` C ${C[0].toFixed(1)} ${C[1].toFixed(1)}, ${M[0].toFixed(1)} ${M[1].toFixed(1)}, ${x[0].toFixed(1)} ${x[1].toFixed(1)}`}let g=[],v=k(i).key;g.push({at:0,key:v});for(let s of p)if(s.floor>i&&s.floor<l){let m=(s.floor-i)/f;g.push({at:m,key:v}),v=s.key,g.push({at:m,key:v})}g.push({at:1,key:v});let O=g.map(s=>`<stop offset="${(s.at*100).toFixed(1)}%" stop-color="var(--band-${s.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${a} ${r}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${t}" gradientUnits="userSpaceOnUse" x1="0" y1="${r-n}" x2="0" y2="${n}">${O}</linearGradient></defs><path d="${S}" fill="none" stroke="url(#caisg-${t})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${d(W(e))}</span>`}function H(e){let t=Math.max(0,Math.min(100,Number(e.score)||0)),a=k(t),r=Array.isArray(e.series)&&e.series.length>=2?e.series:null,n=e.arcFirst!=null&&e.arcBest!=null&&Number.isFinite(Number(e.arcFirst))&&Number.isFinite(Number(e.arcBest)),o=n?Math.round(Number(e.arcBest))-Math.round(Number(e.arcFirst)):0,c=(e.lenses||[]).filter(l=>l&&l.label),u=(e.rows||[]).filter(l=>l&&l.label),i="";if(e.sealText&&(i+=`<span class="cai-seal">${d(e.sealText)}</span>`),i+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${d(e.name)}</span>`+(e.owner?`<span class="cai-owner">by ${d(e.owner)}</span>`:"")+`</span><span class="cai-chip band-${a.key}">${d(a.label)}</span></div>`,i+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${a.key}">${Math.round(t)}</span><span class="cai-unit"> / 100</span></div>`,i+=P(t,{variant:"diamond"}),r&&(i+=X(r)),n&&(i+=`<div class="cai-arc"><span class="cai-arc-from">${Math.round(Number(e.arcFirst))}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${a.key}">${Math.round(Number(e.arcBest))}</span>`+(o>=1?`<span class="cai-arc-up">\u2191 +${o}</span>`:"")+"</div>"),c.length>0){i+='<div class="cai-lenses">';for(let l of c){let f=l.value==null?null:Number(l.value),h=f==null?null:k(f);i+=`<div class="cai-lens"><span class="cai-lens-name">${d(l.label)}</span>`,i+='<span class="cai-lens-bar">',f!=null&&h&&(i+=`<span class="cai-lens-fill fill-${h.key}" style="width:${Math.max(2,Math.round(f))}%"></span>`),i+="</span>",f==null||!h?i+='<span class="cai-lens-num cai-muted">\u2014</span>':i+=`<span class="cai-lens-num ink-${h.key}">${Math.round(f)}</span>`,i+="</div>"}i+="</div>"}if(u.length>0){i+='<div class="cai-rows">';for(let l of u)i+=`<div class="cai-row"><span>${d(l.label)}</span><b class="${l.mono?"mono":""}">${d(l.value)}</b></div>`;i+="</div>"}return i}var R=`
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
`;function T(e){if(!e||!e.name||e.score==null)return null;let t;return Array.isArray(e.series)?t=e.series.map(a=>Number(a)).filter(a=>Number.isFinite(a)):t=String(e.series||"").split(",").map(a=>Number(a.trim())).filter(a=>Number.isFinite(a)),{name:e.name,owner:e.owner||void 0,score:Number(e.score),series:t.length>=2?t:void 0,arcFirst:e.arcFirst??null,arcBest:e.arcBest??null,lenses:(e.lenses||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value??null})),rows:(e.rows||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value||"",mono:!!a.mono})),href:e.href||void 0,sealText:e.sealText||void 0}}var q=[["codeHealth","Code health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Readiness"],["securityCompliance","Security"]];function J(e){return String(e).replace(/\B(?=(\d{3})+(?!\d))/g,",")}function K(e){let t=e.publishedAt||e.lastUpdated,a="";if(t){let o=new Date(t);Number.isNaN(o.getTime())||(a=o.toLocaleDateString("en-GB",{day:"numeric",month:"long",year:"numeric"}))}let r=e.productionLoc>0?`${J(e.productionLoc)} lines`:"",n=[a,r].filter(Boolean);return n.length?n.join(" \xB7 "):null}function _(e){if(!e)return null;let t=e.bestScore!=null?e.bestScore:e.headlineScore;if(t==null)return null;let a=Array.isArray(e.series)&&e.series.length>=2,r=q.map(([c,u])=>({label:u,value:e[c]==null?null:Number(e[c])})).filter(c=>c.value!=null),n=[],o=K(e);return o&&n.push({label:"Measured",value:o}),e.costApprox&&n.push({label:"Rebuild cost",value:e.costApprox}),e.busFactor>0&&e.authorCount>0&&n.push({label:"Bus factor",value:`${e.busFactor} of ${e.authorCount} devs`}),{name:e.name,owner:e.owner||void 0,score:Number(t),series:a?e.series.map(Number):void 0,arcFirst:a?e.firstScore:null,arcBest:a?e.bestScore:null,lenses:r,rows:n}}function D(e){let t=e.bestRunId||e.BestRunId||"";return"/api/oss/"+encodeURIComponent(e.owner)+"/"+encodeURIComponent(e.name)+"/report?run="+t}function I(e){return e.slice().sort((t,a)=>{let r=Date.parse(t.lastUpdated||t.publishedAt||"")||0,n=Date.parse(a.lastUpdated||a.publishedAt||"")||0;if(n!==r)return n-r;let o=t.bestScore!=null?t.bestScore:t.headlineScore;return(a.bestScore!=null?a.bestScore:a.headlineScore)-o})}async function L(e,t,a){if(!e)return a;try{let r=await fetch(e.replace(/\/$/,"")+t);return r.ok?await r.json():a}catch{return a}}var Q=6,V=F+N+E+R+`
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers \u2014 the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;customElements.define("cai-card-gallery",class extends ${#e(){let e=this.getAttribute("count"),t=Number(e);return e!=null&&e!==""&&Number.isFinite(t)&&t>0?Math.floor(t):Q}async liveLoad(){let e=this.apiBase();if(!e)return;let t=await L(e,"/api/oss",null);if(!Array.isArray(t)||t.length===0)return;let r=I(t).slice(0,this.#e()).map(n=>{let o=_(n);return o&&(o.href=D(n)),o}).filter(Boolean);r.length!==0&&(this._live=r,this.render(this.shadowRoot))}render(e){let t=this._live?this._live:(this.json("cards",[])||[]).map(n=>T(n)).filter(Boolean),a=this.getAttribute("footnote"),r=`<style>${V}</style>`;r+=B(this),r+='<div class="mk-cardgallery">';for(let n of t){let o=n.href?"a":"div",c=n.href?` href="${n.href.replace(/"/g,"&quot;")}"`:"";r+=`<${o} class="cai-card"${c}>${H(n)}</${o}>`}r+="</div>",a&&(r+=`<p class="mk-grid-foot">${w(a)}</p>`),e.innerHTML=r}});
