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
`;function s(t){return String(t??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function h(t){if(t==null||t==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",r=0,a;for(;(a=n.exec(t))!==null;){a.index>r&&(e+=s(t.slice(r,a.index)));let o=a[0];if(o.startsWith("**"))e+=`<strong>${s(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${s(o.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);c?e+=`<a href="${s(c[2])}">${s(c[1])}</a>`:e+=s(o)}r=a.index+o.length}return r<t.length&&(e+=s(t.slice(r))),e}var b=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function g(t){let n=t.getAttribute("kicker"),e=t.getAttribute("heading"),r=t.getAttribute("lede");if(!n&&!e&&!r)return"";let a='<div class="mk-section-head">';return n&&(a+=`<span class="mk-kicker">${s(n)}</span>`),e&&(a+=`<h2>${h(e)}</h2>`),r&&(a+=`<p>${h(r)}</p>`),a+="</div>",a}var x=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,p=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let n=this.dataset.theme;this.#t(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(n,e){let r=this.getAttribute(n);if(r==null||r.trim()==="")return e;try{return JSON.parse(r)}catch{return e}}};var y=new Map;function N(t,n,e){let r=(t||"").trim().replace(/\/$/,"");if(!r)return Promise.resolve(e);let a=r+" "+n,o=y.get(a);return o||(o=(async()=>{try{let c=await fetch(r+n);return c.ok?await c.json():e}catch{return e}})(),y.set(a,o),o)}async function k(t){let n=await N(t,"/api/oss",[]);return Array.isArray(n)?n:[]}function w(t){if(!Array.isArray(t)||t.length===0)return null;let n=r=>r.bestScore!=null?Number(r.bestScore):Number(r.headlineScore)||0,e=null;for(let r of t)(e===null||n(r)>n(e)||n(r)===n(e)&&(Number(r.productionLoc)||0)>(Number(e.productionLoc)||0))&&(e=r);return e}var H={exemplary:"var(--band-exemplary)",healthy:"var(--band-healthy)",fair:"var(--band-fair)",poor:"var(--band-poor)",critical:"var(--band-critical)"},B={exemplary:.85,healthy:.85,fair:.7,poor:.8,critical:.8},E=m+b+x+`
.mk-compbar { max-width: 46rem; margin: 0 auto; }
.mk-compbar svg { width: 100%; height: auto; display: block; }
.mk-compbar-label { font-family: var(--font-ui); font-size: 16px; fill: var(--surface); }
.mk-compbar-pct { font-family: var(--font-mono); font-size: 13px; fill: var(--muted); }
.mk-compbar-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.4rem; text-align: center; }
`;function L(t){if(!t)return null;let n=t.brilliantPct==null?null:Number(t.brilliantPct),e=t.slopPct==null?null:Number(t.slopPct);if(n==null||e==null)return null;let r=t.finePct!=null?Math.max(0,Number(t.finePct)):Math.max(0,100-n-e),a=o=>Math.round(o*10)/10;return[{label:"Brilliant",pct:a(n),band:"exemplary"},{label:"Fine",pct:a(r),band:"fair"},{label:"Slop",pct:a(e),band:"critical"}].filter(o=>o.pct>0)}customElements.define("cai-composition-bar",class extends p{async liveLoad(){let t=this.apiBase();if(!t)return;let n=L(w(await k(t)));!n||n.length===0||(this._live=n,this.render(this.shadowRoot))}render(t){let n=(this._live||this.json("segments",[])||[]).filter(i=>i&&Number(i.pct)>0),e=n.reduce((i,l)=>i+(Number(l.pct)||0),0)||100,r=760,a=72,o=this.getAttribute("caption"),c=0,v=n.map(i=>{let l=Number(i.pct)/e*r,u={seg:i,x:c,w:l};return c+=l,u}),S="Code composition: "+n.map(i=>`${i.pct}% ${String(i.label||"").toLowerCase()}`).join(", "),d=`<svg viewBox="0 0 ${r} 116" role="img" aria-label="${s(S)}">`;for(let{seg:i,x:l,w:u}of v){let A=H[i.band]||"var(--band-fair)",$=B[i.band]??.8,C=i.band==="fair"?500:700;d+="<g>",d+=`<rect x="${l}" y="0" width="${u}" height="${a}" fill="${A}" opacity="${$}"></rect>`,d+=`<text class="mk-compbar-label" x="${l+u/2}" y="${a/2+5}" text-anchor="middle" font-weight="${C}">${s(i.label)}</text>`,d+=`<text class="mk-compbar-pct" x="${l+2}" y="${a+30}">${s(String(i.pct))}%</text>`,d+="</g>"}d+="</svg>";let f=`<style>${E}</style>`;f+=g(this),f+=`<figure class="mk-compbar">${d}`,o&&(f+=`<figcaption class="mk-compbar-cap">${h(o)}</figcaption>`),f+="</figure>",t.innerHTML=f}});
