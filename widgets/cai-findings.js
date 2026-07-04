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
`;function s(n){return String(n??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function f(n){if(n==null||n==="")return"";let a=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",r=0,t;for(;(t=a.exec(n))!==null;){t.index>r&&(e+=s(n.slice(r,t.index)));let i=t[0];if(i.startsWith("**"))e+=`<strong>${s(i.slice(2,-2))}</strong>`;else if(i.startsWith("`"))e+=`<code>${s(i.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(i);c?e+=`<a href="${s(c[2])}">${s(c[1])}</a>`:e+=s(i)}r=t.index+i.length}return r<n.length&&(e+=s(n.slice(r))),e}var p=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function g(n){let a=n.getAttribute("kicker"),e=n.getAttribute("heading"),r=n.getAttribute("lede");if(!a&&!e&&!r)return"";let t='<div class="mk-section-head">';return a&&(t+=`<span class="mk-kicker">${s(a)}</span>`),e&&(t+=`<h2>${f(e)}</h2>`),r&&(t+=`<p>${f(r)}</p>`),t+="</div>",t}var b=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,m=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let a=this.dataset.theme;this.#t(),this.dataset.theme!==a&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let a=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=a;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(a,e){let r=this.getAttribute(a);if(r==null||r.trim()==="")return e;try{return JSON.parse(r)}catch{return e}}};async function x(n,a,e){if(!n)return e;try{let r=await fetch(n.replace(/\/$/,"")+a);return r.ok?await r.json():e}catch{return e}}var w=1,y=[{repo:"acme/checkout-service",owner:"acme",name:"checkout-service",reportUrl:"",shown:3,total:11,more:8,findings:[{lensLabel:"Architecture",dim:"D07",title:"Bounded context leak: Orders reaches into Billing's aggregate",file:"src/Orders/OrderService.cs",line:142},{lensLabel:"Domain Modelling",dim:"D22",title:"Anemic aggregate \u2014 invariants enforced in the service, not the entity",file:"src/Billing/Invoice.cs",line:31},{lensLabel:"Event Sourcing",dim:"D31",title:"Event carries a mutable reference type; replay is not deterministic",file:"src/Orders/Events/OrderPlaced.cs",line:18}]}],S=h+p+b+`
.mk-findings { max-width: 62rem; margin: 0 auto; display: grid; gap: 1.1rem; }
.mk-find-repo { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 1rem 1.15rem; }
.mk-find-head { display: flex; align-items: baseline; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 0.75rem; }
.mk-find-repo-name strong { color: var(--heading); font-size: var(--fs-md); }
.mk-find-repo-name span { color: var(--muted); font-size: var(--fs-xs); }
.mk-find-count { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-find-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.7rem; }
.mk-find-item { display: grid; grid-template-columns: 92px 1fr; gap: 0.75rem; align-items: start; }
.mk-find-lens { font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase; color: var(--accent-ink); padding-top: 2px; }
.mk-find-body { min-width: 0; }
.mk-find-title { margin: 0; color: var(--ink); font-size: var(--fs-sm); line-height: 1.45; }
.mk-find-loc { margin: 0.2rem 0 0; font-family: var(--font-mono); font-size: var(--fs-xs); color: var(--muted); overflow: hidden; text-overflow: ellipsis; }
.mk-find-more { margin: 0.75rem 0 0; font-size: var(--fs-xs); }
.mk-find-more a { color: var(--accent-ink); }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.1rem auto 0; max-width: 60ch; text-align: center; }
`;customElements.define("cai-findings",class extends m{#e(){let n=this.getAttribute("count"),a=Number(n);return n!=null&&n!==""&&Number.isFinite(a)&&a>0?Math.floor(a):w}async liveLoad(){let n=this.apiBase();if(!n)return;let a=this.getAttribute("owner")||"",e=this.getAttribute("name")||"",r=await x(n,"/api/public/findings",null),t=r&&Array.isArray(r.items)?r.items:[];if(t.length===0)return;let i;if(a&&e){let c=t.find(l=>l.owner===a&&l.name===e);i=c?[c]:t.slice(0,this.#e())}else i=t.slice(0,this.#e());i.length!==0&&(this._live=i,this.render(this.shadowRoot))}render(n){let a=this._live||y,e=`<style>${S}</style>`;e+=g(this),e+='<div class="mk-findings">';for(let t of a){let i=(t.findings||[]).filter(o=>o&&o.title);if(i.length===0)continue;let c=t.shown!=null?t.shown:i.length,l=t.total!=null?t.total:i.length,u=t.owner||"",k=t.name||t.repo||"";e+='<div class="mk-find-repo">',e+='<div class="mk-find-head">',e+=`<span class="mk-find-repo-name"><strong>${s(k)}</strong>`,u&&(e+=`<span> by ${s(u)}</span>`),e+="</span>",e+=`<span class="mk-find-count">showing ${c} of ${l}</span>`,e+="</div>",e+='<ul class="mk-find-list">';for(let o of i){let v=o.lensLabel||o.lens||"Architecture";e+='<li class="mk-find-item">',e+=`<span class="mk-find-lens">${s(v)}</span>`,e+='<div class="mk-find-body">',e+=`<p class="mk-find-title">${s(o.title||"")}</p>`,o.file&&(e+=`<p class="mk-find-loc">${s(o.file)}${o.line?":"+s(String(o.line)):""}</p>`),e+="</div></li>"}e+="</ul>";let d=t.more!=null?t.more:Math.max(0,l-c);d>0&&t.reportUrl?e+=`<p class="mk-find-more"><a href="${s(t.reportUrl)}" target="_blank" rel="noopener">+ ${d} more in the full report \u2192</a></p>`:d>0&&(e+=`<p class="mk-find-more">+ ${d} more in the full report</p>`),e+="</div>"}e+="</div>";let r=this.getAttribute("footnote");r&&(e+=`<p class="mk-grid-foot">${f(r)}</p>`),n.innerHTML=e}});
