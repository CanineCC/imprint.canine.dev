var E=`
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
:host([data-brand="assert"]) {
  --accent: #b6b6d6;
  --accent-ink: #cbcbe5;
  --accent-wash: #262635;
  --accent-strong: #dfdff1;
  --on-accent: #15191e;
}
:host([data-brand="assert"][data-theme="light"]) {
  --accent: #56557a;
  --accent-ink: #444367;
  --accent-wash: #efeff9;
  --accent-strong: #3d3b5d;
  --on-accent: #ffffff;
}
`;function p(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function S(e){if(e==null||e==="")return"";let t=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,a="",r=0,o;for(;(o=t.exec(e))!==null;){o.index>r&&(a+=p(e.slice(r,o.index)));let i=o[0];if(i.startsWith("**"))a+=`<strong>${p(i.slice(2,-2))}</strong>`;else if(i.startsWith("`"))a+=`<code>${p(i.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(i);s?a+=`<a href="${p(s[2])}">${p(s[1])}</a>`:a+=p(i)}r=o.index+i.length}return r<e.length&&(a+=p(e.slice(r))),a}var L=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function z(e){let t=e.getAttribute("kicker"),a=e.getAttribute("heading"),r=e.getAttribute("lede");if(!t&&!a&&!r)return"";let o='<div class="mk-section-head">';return t&&(o+=`<span class="mk-kicker">${p(t)}</span>`),a&&(o+=`<h2>${S(a)}</h2>`),r&&(o+=`<p>${S(r)}</p>`),o+="</div>",o}var B=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,A=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let t=this.dataset.theme;this.#a(),this.dataset.theme!==t&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#a(){let t=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=t;let a=(this.getAttribute("brand")||"").trim().toLowerCase();a==="assay"||a==="cai"||a==="watchdog"||a==="assert"?this.dataset.brand=a:delete this.dataset.brand}json(t,a){let r=this.getAttribute(t);if(r==null||r.trim()==="")return a;try{return JSON.parse(r)}catch{return a}}};var u=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function b(e){let t=Number(e);return Number.isFinite(t)?R(m(t).toFixed(1)):"\u2014"}function K(){return typeof document<"u"&&/^da(-|$)/i.test(document.documentElement?.lang??"")}function R(e){return K()?e.replace(".",","):e}function m(e){let t=Number(e)*10,a=Math.floor(t),r=(t-a===.5?a%2===0?a:a+1:Math.round(t))/10;return r===0?0:r}function C(e,t){return R(Math.abs(m(t)-m(e)).toFixed(1))}function T(e,t){let a=m(t)-m(e);return Math.abs(a)<.05?"same":a>0?"up":"down"}function w(e){return e>=90?u[4]:e>=70?u[3]:e>=50?u[2]:e>=25?u[1]:u[0]}function P(e){let t=Math.max(0,Math.min(100,e)),a=u.length-1;for(;a>0&&t<u[a].floor;)a--;let o=(a<u.length-1?u[a+1].floor:100)-u[a].floor,i=o>0?Math.max(0,Math.min(1,(t-u[a].floor)/o)):.5;return{leftPct:a*20+i*20,third:i<1/3?0:i<2/3?1:2,key:u[a].key}}var Q=0;function V(){return"s"+(Q++).toString(36)}function Y(e,{variant:t="diamond",caps:a=!1,className:r}={}){let o=P(e),i=w(e),s=`${o.leftPct.toFixed(2)}%`,d=u.map(h=>`<i class="seg-${h.key}"></i>`).join(""),n;t==="diamond"?n=`<div class="cai-mk cai-diamond" style="left:${s};--dia:var(--band-${i.key})"><span class="cai-diamond-foot"></span></div>`:n=`<div class="cai-mk cai-pin" style="left:${s}"><span class="cai-pin-badge">${b(e)}</span><span class="cai-pin-line"></span><span class="cai-pin-foot"></span></div>`;let c=a?'<div class="cai-caps"><span>Worst</span><span>Best</span></div>':"";return`<div class="${"cai-ladder"+(a?"":" compact")+(r?` ${r}`:"")}"><div class="cai-rail" role="img" aria-label="${b(e)} of 100 on a fixed worst-to-best scale"><div class="cai-segs">${d}</div>${n}</div>${c}</div>`}function Z(e){let t=e[0],a=e[e.length-1],r=T(t,a);return`Trend: ${r==="up"?`improving (up ${C(t,a)})`:r==="down"?`declining (down ${C(t,a)})`:"steady"} over the last ${e.length} scans.`}function ee(e){let t=V(),a=320,r=36,o=4,i=e.length,s=Math.min(...e),d=Math.max(...e),n=Math.max(0,s-3),c=Math.min(100,d+3),f=c-n||1,h=l=>l/(i-1)*(a-2*o)+o,J=l=>o+(1-(l-n)/f)*(r-2*o),x=e.map((l,g)=>[h(g),J(l)]),N=`M ${x[0][0].toFixed(1)} ${x[0][1].toFixed(1)}`;for(let l=0;l<i-1;l++){let g=x[Math.max(0,l-1)],$=x[l],v=x[l+1],F=x[Math.min(i-1,l+2)],M=[$[0]+(v[0]-g[0])/6,$[1]+(v[1]-g[1])/6],H=[v[0]-(F[0]-$[0])/6,v[1]-(F[1]-$[1])/6];N+=` C ${M[0].toFixed(1)} ${M[1].toFixed(1)}, ${H[0].toFixed(1)} ${H[1].toFixed(1)}, ${v[0].toFixed(1)} ${v[1].toFixed(1)}`}let y=[],k=w(n).key;y.push({at:0,key:k});for(let l of u)if(l.floor>n&&l.floor<c){let g=(l.floor-n)/f;y.push({at:g,key:k}),k=l.key,y.push({at:g,key:k})}y.push({at:1,key:k});let X=y.map(l=>`<stop offset="${(l.at*100).toFixed(1)}%" stop-color="var(--band-${l.key})"></stop>`).join("");return`<svg class="cai-spark" viewBox="0 0 ${a} ${r}" preserveAspectRatio="none" aria-hidden="true" focusable="false"><defs><linearGradient id="caisg-${t}" gradientUnits="userSpaceOnUse" x1="0" y1="${r-o}" x2="0" y2="${o}">${X}</linearGradient></defs><path d="${N}" fill="none" stroke="url(#caisg-${t})" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke"></path></svg><span class="sr-only">${p(Z(e))}</span>`}function U(e){let t=Math.max(0,Math.min(100,Number(e.score)||0)),a=w(t),r=Array.isArray(e.series)&&e.series.length>=2?e.series:null,o=e.arcFirst!=null&&e.arcBest!=null&&Number.isFinite(Number(e.arcFirst))&&Number.isFinite(Number(e.arcBest)),i=o?m(Number(e.arcBest))-m(Number(e.arcFirst)):0,s=(e.lenses||[]).filter(c=>c&&c.label),d=(e.rows||[]).filter(c=>c&&c.label),n="";if(e.sealText&&(n+=`<span class="cai-seal">${p(e.sealText)}</span>`),n+=`<div class="cai-top"><span class="cai-name"><span class="cai-repo">${p(e.name)}</span>`+(e.owner?`<span class="cai-owner">by ${p(e.owner)}</span>`:"")+`</span><span class="cai-chip band-${a.key}">${p(a.label)}</span></div>`,n+=`<div class="cai-scoreline"><span class="cai-cai">CAI</span><span class="cai-score ink-${a.key}">${b(t)}</span><span class="cai-unit"> / 100</span></div>`,n+=Y(t,{variant:"diamond"}),r&&(n+=ee(r)),o&&(n+=`<div class="cai-arc"><span class="cai-arc-from">${b(e.arcFirst)}</span><span class="cai-arc-arrow" aria-hidden="true">\u2192</span><span class="cai-arc-to ink-${a.key}">${b(e.arcBest)}</span>`+(i>=.05?`<span class="cai-arc-up">\u2191 +${C(e.arcFirst,e.arcBest)}</span>`:"")+"</div>"),s.length>0){n+='<div class="cai-lenses">';for(let c of s){let f=c.value==null?null:Number(c.value),h=f==null?null:w(f);n+=`<div class="cai-lens"><span class="cai-lens-name">${p(c.label)}</span>`,n+='<span class="cai-lens-bar">',f!=null&&h&&(n+=`<span class="cai-lens-fill fill-${h.key}" style="width:${Math.max(2,Math.round(f))}%"></span>`),n+="</span>",f==null||!h?n+='<span class="cai-lens-num cai-muted">\u2014</span>':n+=`<span class="cai-lens-num ink-${h.key}">${b(f)}</span>`,n+="</div>"}n+="</div>"}if(d.length>0){n+='<div class="cai-rows">';for(let c of d)n+=`<div class="cai-row"><span>${p(c.label)}</span><b class="${c.mono?"mono":""}">${p(c.value)}</b></div>`;n+="</div>"}return n}var _=`
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
`;function j(e){if(!e||!e.name||e.score==null)return null;let t;return Array.isArray(e.series)?t=e.series.map(a=>Number(a)).filter(a=>Number.isFinite(a)):t=String(e.series||"").split(",").map(a=>Number(a.trim())).filter(a=>Number.isFinite(a)),{name:e.name,owner:e.owner||void 0,score:Number(e.score),series:t.length>=2?t:void 0,arcFirst:e.arcFirst??null,arcBest:e.arcBest??null,lenses:(e.lenses||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value??null})),rows:(e.rows||[]).filter(a=>a&&a.label).map(a=>({label:a.label,value:a.value||"",mono:!!a.mono})),href:e.href||void 0,sealText:e.sealText||void 0}}var ae=[["codeHealth","Code health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Readiness"],["securityCompliance","Security"]];function te(e){return String(e).replace(/\B(?=(\d{3})+(?!\d))/g,",")}function re(e){let t=e.publishedAt||e.lastUpdated,a="";if(t){let i=new Date(t);Number.isNaN(i.getTime())||(a=i.toLocaleDateString("en-GB",{day:"numeric",month:"long",year:"numeric"}))}let r=e.productionLoc>0?`${te(e.productionLoc)} lines`:"",o=[a,r].filter(Boolean);return o.length?o.join(" \xB7 "):null}function D(e){if(!e)return null;let t=e.bestScore!=null?e.bestScore:e.headlineScore;if(t==null)return null;let a=Array.isArray(e.series)&&e.series.length>=2,r=ae.map(([d,n])=>({label:n,value:e[d]==null?null:Number(e[d])})).filter(d=>d.value!=null),o=[],i=re(e);i&&o.push({label:"Measured",value:i}),e.costApprox&&o.push({label:"Rebuild cost",value:e.costApprox}),e.busFactor>0&&e.authorCount>0&&o.push({label:"Bus factor",value:`${e.busFactor} of ${e.authorCount} devs`});let s=typeof e.display=="string"&&e.display.length>0&&e.display!==(e.owner?e.owner+"/"+e.name:e.name);return{name:s?e.display:e.name,owner:s?void 0:e.owner||void 0,score:Number(t),series:a?e.series.map(Number):void 0,arcFirst:a?e.firstScore:null,arcBest:a?e.bestScore:null,lenses:r,rows:o}}function G(e,t){if(!e)return"";let a=e.bestRunId||e.BestRunId,r=e.owner||e.Owner,o=e.name||e.Name;return!a||!r||!o?"":(t||"").trim().replace(/\/$/,"")+"/api/oss/"+encodeURIComponent(r)+"/"+encodeURIComponent(o)+"/report?run="+encodeURIComponent(a)}var I=new Map;function ne(e,t,a){let r=(e||"").trim().replace(/\/$/,"");if(!r)return Promise.resolve(a);let o=r+" "+t,i=I.get(o);return i||(i=(async()=>{try{let s=await fetch(r+t);return s.ok?await s.json():a}catch{return a}})(),I.set(o,i),i)}async function O(e){let t=await ne(e,"/api/oss",[]);return Array.isArray(t)?t:[]}function q(e){if(!Array.isArray(e)||e.length===0)return null;let t=r=>r.bestScore!=null?Number(r.bestScore):Number(r.headlineScore)||0,a=null;for(let r of e)(a===null||t(r)>t(a)||t(r)===t(a)&&(Number(r.productionLoc)||0)>(Number(a.productionLoc)||0))&&(a=r);return a}function W(e,t){let a=(Array.isArray(e)?e:[]).filter(n=>n&&n!==t);if(a.length===0)return[];let r=n=>n.bestScore!=null?Number(n.bestScore):Number(n.headlineScore)||0,o=n=>n.delta!=null?Number(n.delta):0,i=[],s=n=>{n&&!i.includes(n)&&i.push(n)};s([...a].sort((n,c)=>r(c)-r(n))[0]),s([...a].sort((n,c)=>o(c)-o(n)).find(n=>!i.includes(n)));let d=a.filter(n=>!i.includes(n));return d.length>0&&s(d[Math.floor(Math.random()*d.length)]),i}var oe=6,ie=E+L+B+_+`
.mk-cardgallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem; align-items: start; }
/* gallery cards are quiet peers \u2014 the accent border + seal is the hero look */
.mk-cardgallery .cai-card { max-width: none; border: 1px solid var(--hairline); box-shadow: none; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;customElements.define("cai-card-gallery",class extends A{#e(){let e=this.getAttribute("count"),t=Number(e);return e!=null&&e!==""&&Number.isFinite(t)&&t>0?Math.floor(t):oe}async liveLoad(){let e=this.apiBase();if(!e)return;let t=await O(e);if(t.length===0)return;let a=q(t),r=W(t,a);if(r.length===0)return;let o=this.#e(),i=[];for(let s of r){if(i.length>=o)break;let d=D(s);if(!d)continue;let n=G(s,e);n&&(d.href=n),i.push(d)}i.length!==0&&(this._live=i,this.render(this.shadowRoot))}render(e){let t=this._live?this._live:(this.json("cards",[])||[]).map(o=>j(o)).filter(Boolean),a=this.getAttribute("footnote"),r=`<style>${ie}</style>`;r+=z(this),r+='<div class="mk-cardgallery">';for(let o of t){let i=o.href?"a":"div",s=o.href?` href="${o.href.replace(/"/g,"&quot;")}"`:"";r+=`<${i} class="cai-card"${s}>${U(o)}</${i}>`}r+="</div>",a&&(r+=`<p class="mk-grid-foot">${S(a)}</p>`),e.innerHTML=r}});
