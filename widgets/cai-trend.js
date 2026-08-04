var N=`
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
`;function c(n){return String(n??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function x(n){if(n==null||n==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",a=0,i;for(;(i=r.exec(n))!==null;){i.index>a&&(e+=c(n.slice(a,i.index)));let t=i[0];if(t.startsWith("**"))e+=`<strong>${c(t.slice(2,-2))}</strong>`;else if(t.startsWith("`"))e+=`<code>${c(t.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(t);s?e+=`<a href="${c(s[2])}">${c(s[1])}</a>`:e+=c(t)}a=i.index+t.length}return a<n.length&&(e+=c(n.slice(a))),e}var H=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function T(n){let r=n.getAttribute("kicker"),e=n.getAttribute("heading"),a=n.getAttribute("lede");if(!r&&!e&&!a)return"";let i='<div class="mk-section-head">';return r&&(i+=`<span class="mk-kicker">${c(r)}</span>`),e&&(i+=`<h2>${x(e)}</h2>`),a&&(i+=`<p>${x(a)}</p>`),i+="</div>",i}var B=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,y=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#t(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let a=this.getAttribute(r);if(a==null||a.trim()==="")return e;try{return JSON.parse(a)}catch{return e}}};var u=[{label:"Critical",key:"critical",floor:0},{label:"Weak",key:"poor",floor:25},{label:"Adequate",key:"fair",floor:50},{label:"Strong",key:"healthy",floor:70},{label:"Exemplary",key:"exemplary",floor:90}];function w(n){return n>=90?u[4]:n>=70?u[3]:n>=50?u[2]:n>=25?u[1]:u[0]}var L=`
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
`;var g=[0,25,50,70,90,100],j=.05;function _(n){let r=[],e=0;for(;e<n.length;){let a=e;for(;a+1<n.length&&Math.abs(n[a+1]-n[e])<j;)a++;let i=a-e+1;r.push({i:e,v:n[e],run:i}),a>e&&r.push({i:a,v:n[a],run:i}),e=a+1}return r}var $=720,M=240,l={top:26,right:18,bottom:34,left:40},I=N+H+B+L+`
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
.mk-trend-solo { display: flex; align-items: baseline; justify-content: center; gap: 0.6rem;
  padding: 1.6rem 0 0.4rem; }
.mk-trend-solo-num { font-family: var(--font-mono); font-variant-numeric: tabular-nums;
  font-weight: 700; font-size: var(--fs-4xl); line-height: 1; }
.mk-trend-solo-date { font-size: var(--fs-sm); color: var(--muted); }
.mk-trend-sum { margin: 0.9rem auto 0; max-width: 46rem; font-size: var(--fs-xs);
  color: var(--muted); line-height: 1.6; text-align: center; }
@media (prefers-reduced-motion: reduce) { .mk-trend-tip { transition: none; } }
`;function R(n){let r=Math.min(...n),e=Math.max(...n),a=0,i=100;for(let t of g)t<=r&&(a=t);for(let t=g.length-1;t>=0;t--)g[t]>=e&&(i=g[t]);return i-a<10&&(a=Math.max(0,Math.min(a,i-25))),i-a<10&&(i=Math.min(100,a+25)),{min:a,max:i}}function f(n){return(Math.round(n*10)/10).toFixed(1)}customElements.define("cai-trend",class extends y{render(n){let r=(this.json("series",[])||[]).map(Number).filter(o=>Number.isFinite(o)),e=this.getAttribute("first-date"),a=this.getAttribute("last-date"),i=this.getAttribute("caption"),t=`<style>${I}</style>`;if(t+=T(this),r.length===0){n.innerHTML=t;return}if(r.length===1){let o=r[0],m=w(o);t+='<div class="mk-trend"><p class="mk-trend-solo">',t+=`<span class="mk-trend-solo-num ink-${m.key}">${f(o)}</span>`,(a||e)&&(t+=`<span class="mk-trend-solo-date">measured ${c(a||e)}</span>`),t+="</p></div>",i&&(t+=`<p class="mk-trend-sum">${x(i)}</p>`),n.innerHTML=t;return}let{min:s,max:v}=R(r),b=$-l.left-l.right,k=M-l.top-l.bottom,d=o=>l.left+b*o/(r.length-1),p=o=>l.top+k*(1-(o-s)/(v-s)),h=r[r.length-1],C=w(h),z=_(r),E=z.map(o=>`${d(o.i).toFixed(1)},${p(o.v).toFixed(1)}`);t+='<div class="mk-trend"><div class="mk-trend-plot">',t+=`<svg viewBox="0 0 ${$} ${M}" role="img" aria-label="${c(`${r.length} measurements, from ${f(r[0])} to ${f(h)}.`)}">`;for(let o of g){if(o<s||o>v)continue;let m=p(o).toFixed(1);t+=`<line class="mk-trend-grid" x1="${l.left}" y1="${m}" x2="${$-l.right}" y2="${m}"></line>`,t+=`<text class="mk-trend-cut" x="${l.left-8}" y="${m}" text-anchor="end" dominant-baseline="middle">${o}</text>`}t+=`<path class="mk-trend-area" d="M${d(0).toFixed(1)},${p(s).toFixed(1)} L${E.join(" L")} L${d(r.length-1).toFixed(1)},${p(s).toFixed(1)} Z"></path>`,t+=`<polyline class="mk-trend-line" points="${E.join(" ")}" vector-effect="non-scaling-stroke"></polyline>`,e&&(t+=`<text class="mk-trend-date" x="${l.left}" y="${M-10}" text-anchor="start">${c(e)}</text>`),a&&(t+=`<text class="mk-trend-date" x="${$-l.right}" y="${M-10}" text-anchor="end">${c(a)}</text>`),z.forEach(o=>{let m=o.i===r.length-1,F=d(o.i).toFixed(1),A=p(o.v).toFixed(1);t+=`<circle class="mk-trend-hit" cx="${F}" cy="${A}" r="18" tabindex="0" data-i="${o.i}" data-v="${f(o.v)}" data-run="${o.run}"></circle>`,t+=m?`<circle class="mk-trend-end fill-${C.key}" cx="${F}" cy="${A}" r="5.5"></circle>`:`<circle class="mk-trend-dot" cx="${F}" cy="${A}" r="4"></circle>`}),t+=`<text class="mk-trend-endlabel ink-${C.key}" x="${d(r.length-1).toFixed(1)}" y="${(p(h)-14).toFixed(1)}" text-anchor="end">${f(h)}</text>`,t+="</svg>",t+='<div class="mk-trend-tip" hidden></div>',t+="</div>";let S=h-r[0],D=Math.abs(S)<.05?"unchanged":`${S>0?"up":"down"} ${f(Math.abs(S))}`;t+=`<p class="mk-trend-sum">${c(`${r.length} measurements${e?`, from ${e}`:""}${a?` to ${a}`:""}: ${f(r[0])} to ${f(h)} \u2014 ${D}.`)}</p>`,i&&(t+=`<p class="mk-trend-sum">${x(i)}</p>`),t+="</div>",n.innerHTML=t,this.wireTips(n,r)}wireTips(n,r){let e=n.querySelector(".mk-trend-tip"),a=n.querySelector(".mk-trend-plot");if(!e||!a)return;let i=s=>{let v=Number(s.getAttribute("data-i")),b=s.getBoundingClientRect(),k=a.getBoundingClientRect(),d=Number(s.getAttribute("data-run"))||1,p=d>=3?`unchanged across ${d} scans`:`scan ${v+1} of ${r.length}`;e.hidden=!1,e.innerHTML=`<b>${c(s.getAttribute("data-v")||"")}</b> \xB7 ${c(p)}`,e.style.left=`${b.left+b.width/2-k.left}px`,e.style.top=`${b.top-k.top-6}px`,e.classList.add("on")},t=()=>{e.classList.remove("on")};for(let s of n.querySelectorAll(".mk-trend-hit"))s.addEventListener("pointerenter",()=>i(s)),s.addEventListener("focus",()=>i(s)),s.addEventListener("pointerleave",t),s.addEventListener("blur",t)}});export{_ as collapseFlatRuns};
