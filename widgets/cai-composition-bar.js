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
`;function s(r){return String(r??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function h(r){if(r==null||r==="")return"";let a=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",t=0,n;for(;(n=a.exec(r))!==null;){n.index>t&&(e+=s(r.slice(t,n.index)));let o=n[0];if(o.startsWith("**"))e+=`<strong>${s(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${s(o.slice(1,-1))}</code>`;else{let l=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);l?e+=`<a href="${s(l[2])}">${s(l[1])}</a>`:e+=s(o)}t=n.index+o.length}return t<r.length&&(e+=s(r.slice(t))),e}var b=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function g(r){let a=r.getAttribute("kicker"),e=r.getAttribute("heading"),t=r.getAttribute("lede");if(!a&&!e&&!t)return"";let n='<div class="mk-section-head">';return a&&(n+=`<span class="mk-kicker">${s(a)}</span>`),e&&(n+=`<h2>${h(e)}</h2>`),t&&(n+=`<p>${h(t)}</p>`),n+="</div>",n}var x=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,p=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let a=this.dataset.theme;this.#t(),this.dataset.theme!==a&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let a=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=a;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(a,e){let t=this.getAttribute(a);if(t==null||t.trim()==="")return e;try{return JSON.parse(t)}catch{return e}}};function y(r,{owner:a,name:e}={}){if(!Array.isArray(r)||r.length===0)return null;if(a&&e){let t=r.find(n=>n.owner===a&&n.name===e);if(t)return t}return r.slice().sort((t,n)=>{let o=t.bestScore!=null?t.bestScore:t.headlineScore;return(n.bestScore!=null?n.bestScore:n.headlineScore)-o||(n.productionLoc||0)-(t.productionLoc||0)})[0]}async function v(r,a,e){if(!r)return e;try{let t=await fetch(r.replace(/\/$/,"")+a);return t.ok?await t.json():e}catch{return e}}var C={exemplary:"var(--band-exemplary)",healthy:"var(--band-healthy)",fair:"var(--band-fair)",poor:"var(--band-poor)",critical:"var(--band-critical)"},N={exemplary:.85,healthy:.85,fair:.7,poor:.8,critical:.8},E=m+b+x+`
.mk-compbar { max-width: 46rem; margin: 0 auto; }
.mk-compbar svg { width: 100%; height: auto; display: block; }
.mk-compbar-label { font-family: var(--font-ui); font-size: 16px; fill: var(--surface); }
.mk-compbar-pct { font-family: var(--font-mono); font-size: 13px; fill: var(--muted); }
.mk-compbar-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.4rem; text-align: center; }
`;function L(r){if(!r)return null;let a=r.brilliantPct==null?null:Number(r.brilliantPct),e=r.slopPct==null?null:Number(r.slopPct);if(a==null||e==null)return null;let t=Math.max(0,100-a-e),n=o=>Math.round(o*10)/10;return[{label:"Brilliant",pct:n(a),band:"exemplary"},{label:"Fine",pct:n(t),band:"fair"},{label:"Slop",pct:n(e),band:"critical"}].filter(o=>o.pct>0)}customElements.define("cai-composition-bar",class extends p{async liveLoad(){let r=this.apiBase();if(!r)return;let a=this.getAttribute("owner")||"",e=this.getAttribute("name")||"",t=await v(r,"/api/oss",null);if(!Array.isArray(t)||t.length===0)return;let n=L(y(t,{owner:a,name:e}));!n||n.length===0||(this._live=n,this.render(this.shadowRoot))}render(r){let a=(this._live||this.json("segments",[])||[]).filter(c=>c&&Number(c.pct)>0),e=a.reduce((c,i)=>c+(Number(i.pct)||0),0)||100,t=760,n=72,o=this.getAttribute("caption"),l=0,k=a.map(c=>{let i=Number(c.pct)/e*t,u={seg:c,x:l,w:i};return l+=i,u}),w="Code composition: "+a.map(c=>`${c.pct}% ${String(c.label||"").toLowerCase()}`).join(", "),d=`<svg viewBox="0 0 ${t} 116" role="img" aria-label="${s(w)}">`;for(let{seg:c,x:i,w:u}of k){let S=C[c.band]||"var(--band-fair)",A=N[c.band]??.8,$=c.band==="fair"?500:700;d+="<g>",d+=`<rect x="${i}" y="0" width="${u}" height="${n}" fill="${S}" opacity="${A}"></rect>`,d+=`<text class="mk-compbar-label" x="${i+u/2}" y="${n/2+5}" text-anchor="middle" font-weight="${$}">${s(c.label)}</text>`,d+=`<text class="mk-compbar-pct" x="${i+2}" y="${n+30}">${s(String(c.pct))}%</text>`,d+="</g>"}d+="</svg>";let f=`<style>${E}</style>`;f+=g(this),f+=`<figure class="mk-compbar">${d}`,o&&(f+=`<figcaption class="mk-compbar-cap">${h(o)}</figcaption>`),f+="</figure>",r.innerHTML=f}});
