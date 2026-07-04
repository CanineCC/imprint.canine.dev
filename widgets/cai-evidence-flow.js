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
`;function i(o){return String(o??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function c(o){if(o==null||o==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",a=0,t;for(;(t=n.exec(o))!==null;){t.index>a&&(e+=i(o.slice(a,t.index)));let r=t[0];if(r.startsWith("**"))e+=`<strong>${i(r.slice(2,-2))}</strong>`;else if(r.startsWith("`"))e+=`<code>${i(r.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(r);s?e+=`<a href="${i(s[2])}">${i(s[1])}</a>`:e+=i(r)}a=t.index+r.length}return a<o.length&&(e+=i(o.slice(a))),e}var f=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function h(o){let n=o.getAttribute("kicker"),e=o.getAttribute("heading"),a=o.getAttribute("lede");if(!n&&!e&&!a)return"";let t='<div class="mk-section-head">';return n&&(t+=`<span class="mk-kicker">${i(n)}</span>`),e&&(t+=`<h2>${c(e)}</h2>`),a&&(t+=`<p>${c(a)}</p>`),t+="</div>",t}var m=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,d=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let n=this.dataset.theme;this.#t(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(n,e){let a=this.getAttribute(n);if(a==null||a.trim()==="")return e;try{return JSON.parse(a)}catch{return e}}};var g='<svg viewBox="0 0 24 24" focusable="false"><path d="M3 12h16m0 0-5.5-5.5M19 12l-5.5 5.5" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"></path></svg>',x=l+f+m+`
.mk-flow { max-width: 62rem; margin: 0 auto; }
.mk-flow-nodes { list-style: none; display: flex; align-items: stretch; gap: 10px; margin: 0; padding: 0; }
.mk-flow-node { flex: 1 1 0; border: 1px solid var(--hairline); border-radius: var(--r-lg); padding: 0.9rem 1rem; background: var(--surface); display: flex; flex-direction: column; gap: 0.25rem; min-width: 0; }
.mk-flow-node strong { color: var(--heading); font-size: var(--fs-md); }
.mk-flow-node span { color: var(--muted); font-size: var(--fs-xs); line-height: 1.45; }
.mk-flow-node.tone-on { border-color: var(--accent); box-shadow: inset 0 0 0 1px var(--accent); }
.mk-flow-node.tone-muted { border-style: dashed; }
.mk-flow-arrow { flex: 0 0 26px; display: flex; align-items: center; justify-content: center; color: var(--muted); }
.mk-flow-arrow svg { width: 22px; height: 22px; }
.mk-flow-return { position: relative; height: 34px; margin: 0 11%; border: 1px dashed var(--border-strong); border-top: 0; border-radius: 0 0 14px 14px; }
.mk-flow-return::before { content: ""; position: absolute; top: -8px; left: -5.5px; border: 5px solid transparent; border-bottom-color: var(--muted); }
.mk-flow-return-label { position: absolute; left: 50%; top: 100%; transform: translate(-50%, -50%); background: var(--bg); padding: 0 10px; font-size: var(--fs-xs); color: var(--muted); white-space: nowrap; }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
@media (max-width: 760px) {
  .mk-flow-nodes { flex-direction: column; }
  .mk-flow-arrow { flex-basis: auto; height: 24px; }
  .mk-flow-arrow svg { transform: rotate(90deg); }
  .mk-flow-return { height: auto; border: 0; margin: 0; }
  .mk-flow-return::before { display: none; }
  .mk-flow-return-label { position: static; transform: none; display: block; text-align: center; white-space: normal; padding-top: 0.6rem; }
}
`;customElements.define("cai-evidence-flow",class extends d{render(o){let n=(this.json("nodes",[])||[]).filter(s=>s&&s.title),e=this.getAttribute("loop-label"),a=this.getAttribute("footnote"),t="";n.forEach((s,p)=>{p>0&&(t+=`<li class="mk-flow-arrow" aria-hidden="true">${g}</li>`);let b=s.tone&&s.tone!=="default"?` tone-${s.tone}`:"";t+=`<li class="mk-flow-node${b}"><strong>${i(s.title)}</strong>`,s.body&&(t+=`<span>${c(s.body)}</span>`),t+="</li>"});let r=`<style>${x}</style>`;r+=h(this),r+=`<div class="mk-flow${e?" has-loop":""}">`,r+=`<ol class="mk-flow-nodes">${t}</ol>`,e&&(r+=`<div class="mk-flow-return"><span class="mk-flow-return-label">${i(e)}</span></div>`),r+="</div>",a&&(r+=`<p class="mk-grid-foot">${c(a)}</p>`),o.innerHTML=r}});
