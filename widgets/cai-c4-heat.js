var p=`
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
`;function s(n){return String(n??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function f(n){if(n==null||n==="")return"";let t=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",r=0,a;for(;(a=t.exec(n))!==null;){a.index>r&&(e+=s(n.slice(r,a.index)));let o=a[0];if(o.startsWith("**"))e+=`<strong>${s(o.slice(2,-2))}</strong>`;else if(o.startsWith("`"))e+=`<code>${s(o.slice(1,-1))}</code>`;else{let d=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(o);d?e+=`<a href="${s(d[2])}">${s(d[1])}</a>`:e+=s(o)}r=a.index+o.length}return r<n.length&&(e+=s(n.slice(r))),e}var b=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function g(n){let t=n.getAttribute("kicker"),e=n.getAttribute("heading"),r=n.getAttribute("lede");if(!t&&!e&&!r)return"";let a='<div class="mk-section-head">';return t&&(a+=`<span class="mk-kicker">${s(t)}</span>`),e&&(a+=`<h2>${f(e)}</h2>`),r&&(a+=`<p>${f(r)}</p>`),a+="</div>",a}var x=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,u=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let t=this.dataset.theme;this.#t(),this.dataset.theme!==t&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let t=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=t;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(t,e){let r=this.getAttribute(t);if(r==null||r.trim()==="")return e;try{return JSON.parse(r)}catch{return e}}};async function h(n,t,e){if(!n)return e;try{let r=await fetch(n.replace(/\/$/,"")+t);return r.ok?await r.json():e}catch{return e}}async function k(n,t){if(!n)return null;try{let e=await fetch(n.replace(/\/$/,"")+t);return e.ok?await e.text():null}catch{return null}}var v=p+b+x+`
.mk-c4 { max-width: 62rem; margin: 0 auto; }
.mk-c4-frame { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem; overflow: hidden; }
.mk-c4-frame svg { width: 100%; height: auto; display: block; }
.mk-c4-repo { display: flex; align-items: baseline; gap: 0.5rem; margin-bottom: 0.6rem; }
.mk-c4-repo strong { color: var(--heading); font-size: var(--fs-md); }
.mk-c4-repo span { color: var(--muted); font-size: var(--fs-xs); }
.mk-c4-loading { color: var(--muted); font-size: var(--fs-sm); margin: 0; padding: 1.5rem 0; text-align: center; }
.mk-c4-cap { font-size: var(--fs-xs); color: var(--muted); margin-top: 0.9rem; text-align: center; line-height: 1.5; }
`;customElements.define("cai-c4-heat",class extends u{async liveLoad(){let n=this.apiBase();if(!n)return;let t=this.getAttribute("owner")||"",e=this.getAttribute("name")||"",r=await h(n,"/api/public/c4",null),a=r&&Array.isArray(r.items)?r.items:[];if(a.length===0)return;let o=await h(n,"/api/oss",null),d=new Map;if(Array.isArray(o))for(let l of o){let c=l.bestRunId||l.BestRunId;c&&d.set(String(c),l)}let i=null;for(let l of a){let c=d.get(String(l.runId));if(c)if(t&&e){if(c.owner===t&&c.name===e){i=c;break}}else i||(i=c)}if(!i)return;let m=await k(n,"/api/public/oss/"+encodeURIComponent(i.owner)+"/"+encodeURIComponent(i.name)+"/c4.svg");m&&(this._live={owner:i.owner,name:i.name,svg:m},this.render(this.shadowRoot))}render(n){let t=`<style>${v}</style>`;if(t+=g(this),!this._live){t+='<figure class="mk-c4"><div class="mk-c4-frame"><p class="mk-c4-loading">Loading the architecture map\u2026</p></div></figure>',n.innerHTML=t;return}let{owner:e,name:r,svg:a}=this._live,o=this.getAttribute("caption");t+='<figure class="mk-c4">',t+='<div class="mk-c4-frame">',t+=`<div class="mk-c4-repo"><strong>${s(r)}</strong><span>by ${s(e)}</span></div>`,t+=a,t+="</div>",o&&(t+=`<figcaption class="mk-c4-cap">${f(o)}</figcaption>`),t+="</figure>",n.innerHTML=t}});
