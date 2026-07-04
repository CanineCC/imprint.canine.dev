var h=`
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
`;function i(n){return String(n??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function c(n){if(n==null||n==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",r=0,a;for(;(a=e.exec(n))!==null;){a.index>r&&(t+=i(n.slice(r,a.index)));let s=a[0];if(s.startsWith("**"))t+=`<strong>${i(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))t+=`<code>${i(s.slice(1,-1))}</code>`;else{let o=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);o?t+=`<a href="${i(o[2])}">${i(o[1])}</a>`:t+=i(s)}r=a.index+s.length}return r<n.length&&(t+=i(n.slice(r))),t}var f=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function u(n){let e=n.getAttribute("kicker"),t=n.getAttribute("heading"),r=n.getAttribute("lede");if(!e&&!t&&!r)return"";let a='<div class="mk-section-head">';return e&&(a+=`<span class="mk-kicker">${i(e)}</span>`),t&&(a+=`<h2>${c(t)}</h2>`),r&&(a+=`<p>${c(r)}</p>`),a+="</div>",a}var p=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,l=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let e=this.dataset.theme;this.#t(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(e,t){let r=this.getAttribute(e);if(r==null||r.trim()==="")return t;try{return JSON.parse(r)}catch{return t}}};var m=new Map;function x(n,e,t){let r=(n||"").trim().replace(/\/$/,"");if(!r)return Promise.resolve(t);let a=r+" "+e,s=m.get(a);return s||(s=(async()=>{try{let o=await fetch(r+e);return o.ok?await o.json():t}catch{return t}})(),m.set(a,s),s)}async function b(n){let e=await x(n,"/api/public/c4",{items:[]});return e&&Array.isArray(e.items)?e.items:[]}async function g(n,e){let t=(n||"").trim().replace(/\/$/,"");if(!t)return null;try{let r=await fetch(t+e);return r.ok?await r.text():null}catch{return null}}var k=h+f+p+`
.mk-c4 { max-width: 62rem; margin: 0 auto; }
.mk-c4-bar { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; margin-bottom: 0.6rem; }
.mk-c4-repo { display: flex; align-items: baseline; gap: 0.5rem; min-width: 0; }
.mk-c4-repo strong { color: var(--heading); font-size: var(--fs-md); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mk-c4-repo span { color: var(--muted); font-size: var(--fs-xs); white-space: nowrap; }
.mk-c4-nav { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
.mk-c4-count { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-c4-btn { appearance: none; cursor: pointer; border: 1px solid var(--border-strong); background: var(--surface); color: var(--ink); border-radius: var(--r-full); width: 30px; height: 30px; font-size: var(--fs-md); line-height: 1; display: inline-flex; align-items: center; justify-content: center; }
.mk-c4-btn:hover { border-color: var(--accent-ink); color: var(--accent-ink); }
.mk-c4-frame { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem; overflow: hidden; }
.mk-c4-frame svg { width: 100%; height: auto; display: block; }
.mk-c4-loading { color: var(--muted); font-size: var(--fs-sm); margin: 0; padding: 1.5rem 0; text-align: center; }
.mk-c4-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;function v(n){let e=String(n||""),t=e.indexOf("/");return t<=0||t>=e.length-1?null:{owner:e.slice(0,t),name:e.slice(t+1)}}customElements.define("cai-c4-heat",class extends l{#e=[];#t=0;#n=new Map;async liveLoad(){let n=this.apiBase();if(!n)return;this._pending=!0,this.render(this.shadowRoot);let e=await b(n);if(this.#e=e.map(t=>{let r=t&&t.owner,a=t&&t.name,s=r&&a?{owner:r,name:a}:v(t&&t.repo);return s?{repo:t&&t.repo,owner:s.owner,name:s.name}:null}).filter(Boolean),this._pending=!1,this.#e.length===0){this.render(this.shadowRoot);return}this.#t=0,await this.#r(0),this.render(this.shadowRoot)}async#r(n){if(this.#n.has(n))return;let e=this.#e[n],t=await g(this.apiBase(),"/api/public/oss/"+encodeURIComponent(e.owner)+"/"+encodeURIComponent(e.name)+"/c4.svg");this.#n.set(n,t||"")}async#a(n){this.#e.length<2||(this.#t=(this.#t+n+this.#e.length)%this.#e.length,await this.#r(this.#t),this.render(this.shadowRoot))}render(n){let e=`<style>${k}</style>`;if(e+=u(this),this.#e.length===0){this._pending&&(e+='<figure class="mk-c4"><div class="mk-c4-frame"><p class="mk-c4-loading">Loading the architecture maps\u2026</p></div></figure>'),n.innerHTML=e;return}let t=this.#e[this.#t],r=this.#n.get(this.#t),a=this.getAttribute("caption"),s=this.#e.length<2;e+='<figure class="mk-c4">',e+='<div class="mk-c4-bar">',e+=t.repo&&t.repo.indexOf("/")>=0?`<span class="mk-c4-repo"><strong>${i(t.name)}</strong><span>by ${i(t.owner)}</span></span>`:`<span class="mk-c4-repo"><strong>${i(t.repo||t.name)}</strong></span>`,s||(e+='<span class="mk-c4-nav">',e+='<button type="button" class="mk-c4-btn" data-c4-prev aria-label="Previous architecture map">\u2039</button>',e+=`<span class="mk-c4-count">${this.#t+1} / ${this.#e.length}</span>`,e+='<button type="button" class="mk-c4-btn" data-c4-next aria-label="Next architecture map">\u203A</button>',e+="</span>"),e+="</div>",e+='<div class="mk-c4-frame">',r===void 0?e+='<p class="mk-c4-loading">Loading the architecture map\u2026</p>':r===""?e+=`<p class="mk-c4-loading">Couldn't load this architecture map \u2014 try the next one.</p>`:e+=r,e+="</div>",a&&(e+=`<figcaption class="mk-c4-cap">${c(a)}</figcaption>`),e+="</figure>",n.innerHTML=e;let o=n.querySelector("[data-c4-prev]"),d=n.querySelector("[data-c4-next]");o&&o.addEventListener("click",()=>{this.#a(-1)}),d&&d.addEventListener("click",()=>{this.#a(1)})}});
