var l=`
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
`;function o(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function c(a){if(a==null||a==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",n=0,r;for(;(r=e.exec(a))!==null;){r.index>n&&(t+=o(a.slice(n,r.index)));let s=r[0];if(s.startsWith("**"))t+=`<strong>${o(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))t+=`<code>${o(s.slice(1,-1))}</code>`;else{let i=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);i?t+=`<a href="${o(i[2])}">${o(i[1])}</a>`:t+=o(s)}n=r.index+s.length}return n<a.length&&(t+=o(a.slice(n))),t}var f=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function h(a){let e=a.getAttribute("kicker"),t=a.getAttribute("heading"),n=a.getAttribute("lede");if(!e&&!t&&!n)return"";let r='<div class="mk-section-head">';return e&&(r+=`<span class="mk-kicker">${o(e)}</span>`),t&&(r+=`<h2>${c(t)}</h2>`),n&&(r+=`<p>${c(n)}</p>`),r+="</div>",r}var u=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,d=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let e=this.dataset.theme;this.#t(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(e,t){let n=this.getAttribute(e);if(n==null||n.trim()==="")return t;try{return JSON.parse(n)}catch{return t}}};var m=new Map;function g(a,e){return a+" "+(e||"")}function p(a,e){let t=(a||"").trim().replace(/\/$/,"");if(!t)return Promise.resolve(null);let n=g(t,e),r=m.get(n);if(r)return r;let s=e?"?cohort="+encodeURIComponent(e):"";return r=(async()=>{try{let i=await fetch(t+"/api/public/showcase"+s);return i.ok?await i.json():null}catch{return null}})(),m.set(n,r),r}async function b(a,e){let t=(a||"").trim().replace(/\/$/,"");if(!t)return null;try{let n=await fetch(t+e);return n.ok?await n.text():null}catch{return null}}var x=l+f+u+`
.mk-c4 { max-width: 62rem; margin: 0 auto; }
.mk-c4-frame { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem; overflow: hidden; }
.mk-c4-frame svg { width: 100%; height: auto; display: block; }
.mk-c4-repo { display: flex; align-items: baseline; gap: 0.5rem; margin-bottom: 0.6rem; }
.mk-c4-repo strong { color: var(--heading); font-size: var(--fs-md); }
.mk-c4-repo span { color: var(--muted); font-size: var(--fs-xs); }
.mk-c4-loading { color: var(--muted); font-size: var(--fs-sm); margin: 0; padding: 1.5rem 0; text-align: center; }
.mk-c4-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;customElements.define("cai-c4-heat",class extends d{async liveLoad(){let a=this.apiBase();if(a){this._pending=!0,this.render(this.shadowRoot);try{let e=await p(a),t=e&&e.c4,n=t&&t.owner,r=t&&t.name;if(!n||!r)return;let s=await b(a,"/api/public/oss/"+encodeURIComponent(n)+"/"+encodeURIComponent(r)+"/c4.svg");if(!s)return;this._live={owner:n,name:r,svg:s}}finally{this._pending=!1,this.render(this.shadowRoot)}}}render(a){let e=`<style>${x}</style>`;if(e+=h(this),!this._live){this._pending&&(e+='<figure class="mk-c4"><div class="mk-c4-frame"><p class="mk-c4-loading">Loading the architecture map\u2026</p></div></figure>'),a.innerHTML=e;return}let{owner:t,name:n,svg:r}=this._live,s=this.getAttribute("caption");e+='<figure class="mk-c4">',e+='<div class="mk-c4-frame">',e+=`<div class="mk-c4-repo"><strong>${o(n)}</strong><span>by ${o(t)}</span></div>`,e+=r,e+="</div>",s&&(e+=`<figcaption class="mk-c4-cap">${c(s)}</figcaption>`),e+="</figure>",a.innerHTML=e}});
