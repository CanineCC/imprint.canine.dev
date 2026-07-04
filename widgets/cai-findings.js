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
`;function s(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function d(e){if(e==null||e==="")return"";let n=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",r=0,o;for(;(o=n.exec(e))!==null;){o.index>r&&(t+=s(e.slice(r,o.index)));let a=o[0];if(a.startsWith("**"))t+=`<strong>${s(a.slice(2,-2))}</strong>`;else if(a.startsWith("`"))t+=`<code>${s(a.slice(1,-1))}</code>`;else{let i=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(a);i?t+=`<a href="${s(i[2])}">${s(i[1])}</a>`:t+=s(a)}r=o.index+a.length}return r<e.length&&(t+=s(e.slice(r))),t}var u=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function h(e){let n=e.getAttribute("kicker"),t=e.getAttribute("heading"),r=e.getAttribute("lede");if(!n&&!t&&!r)return"";let o='<div class="mk-section-head">';return n&&(o+=`<span class="mk-kicker">${s(n)}</span>`),t&&(o+=`<h2>${d(t)}</h2>`),r&&(o+=`<p>${d(r)}</p>`),o+="</div>",o}var p=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,f=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let n=this.dataset.theme;this.#t(),this.dataset.theme!==n&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let n=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=n;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(n,t){let r=this.getAttribute(n);if(r==null||r.trim()==="")return t;try{return JSON.parse(r)}catch{return t}}};var g=new Map;function v(e,n,t){let r=(e||"").trim().replace(/\/$/,"");if(!r)return Promise.resolve(t);let o=r+" "+n,a=g.get(o);return a||(a=(async()=>{try{let i=await fetch(r+n);return i.ok?await i.json():t}catch{return t}})(),g.set(o,a),a)}async function b(e){let n=await v(e,"/api/public/findings",{items:[]});return n&&Array.isArray(n.items)?n.items:[]}var k=[{repo:"acme/checkout-service",owner:"acme",name:"checkout-service",reportUrl:"",shown:3,total:11,more:8,findings:[{lensLabel:"Architecture",dim:"D07",title:"Bounded context leak: Orders reaches into Billing's aggregate",file:"src/Orders/OrderService.cs",line:142},{lensLabel:"Domain Modelling",dim:"D22",title:"Anemic aggregate \u2014 invariants enforced in the service, not the entity",file:"src/Billing/Invoice.cs",line:31},{lensLabel:"Event Sourcing",dim:"D31",title:"Event carries a mutable reference type; replay is not deterministic",file:"src/Orders/Events/OrderPlaced.cs",line:18}]}],y=m+u+p+`
.mk-findings { max-width: 62rem; margin: 0 auto; }
.mk-find-bar { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; margin-bottom: 0.6rem; }
.mk-find-nav { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
.mk-find-navcount { color: var(--muted); font-size: var(--fs-xs); font-variant-numeric: tabular-nums; white-space: nowrap; }
.mk-find-btn { appearance: none; cursor: pointer; border: 1px solid var(--border-strong); background: var(--surface); color: var(--ink); border-radius: var(--r-full); width: 30px; height: 30px; font-size: var(--fs-md); line-height: 1; display: inline-flex; align-items: center; justify-content: center; }
.mk-find-btn:hover { border-color: var(--accent-ink); color: var(--accent-ink); }
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
`;function w(e,n){return e?/^https?:\/\//i.test(e)?e:(n||"").replace(/\/$/,"")+e:""}customElements.define("cai-findings",class extends f{#e=[];#t=0;async liveLoad(){let e=this.apiBase();if(!e)return;let t=(await b(e)).filter(r=>r&&Array.isArray(r.findings)&&r.findings.length>0).map(r=>({...r,reportUrl:w(r.reportUrl,e)}));t.length!==0&&(this.#e=t,this.#t=0,this._live=!0,this.render(this.shadowRoot))}#n(e){this.#e.length<2||(this.#t=(this.#t+e+this.#e.length)%this.#e.length,this.render(this.shadowRoot))}#r(e){let n=(e.findings||[]).filter(c=>c&&c.title);if(n.length===0)return"";let t=e.shown!=null?e.shown:n.length,r=e.total!=null?e.total:n.length,o=e.owner||"",a=e.name||e.repo||"",i='<div class="mk-find-repo">';i+='<div class="mk-find-head">',i+=`<span class="mk-find-repo-name"><strong>${s(a)}</strong>`,o&&(i+=`<span> by ${s(o)}</span>`),i+="</span>",i+=`<span class="mk-find-count">showing ${s(String(t))} of ${s(String(r))}</span>`,i+="</div>",i+='<ul class="mk-find-list">';for(let c of n){let x=c.lensLabel||c.lens||"Architecture";i+='<li class="mk-find-item">',i+=`<span class="mk-find-lens">${s(x)}</span>`,i+='<div class="mk-find-body">',i+=`<p class="mk-find-title">${s(c.title||"")}</p>`,c.file&&(i+=`<p class="mk-find-loc">${s(c.file)}${c.line!=null?":"+s(String(c.line)):""}</p>`),i+="</div></li>"}i+="</ul>";let l=e.more!=null?e.more:Math.max(0,r-t);return l>0&&e.reportUrl?i+=`<p class="mk-find-more"><a href="${s(e.reportUrl)}" target="_blank" rel="noopener">+ ${s(String(l))} more in the full report \u2192</a></p>`:l>0?i+=`<p class="mk-find-more">+ ${s(String(l))} more in the full report</p>`:e.reportUrl&&(i+=`<p class="mk-find-more"><a href="${s(e.reportUrl)}" target="_blank" rel="noopener">Read the full report \u2192</a></p>`),i+="</div>",i}render(e){let n=this._live?this.#e:k,t=this._live?this.#t:0,r=n.length<2,o=n[t],a=`<style>${y}</style>`;a+=h(this),a+='<div class="mk-findings">',this._live&&!r&&(a+='<div class="mk-find-bar">',a+=`<span class="mk-find-navcount">${t+1} / ${n.length} published repos</span>`,a+='<span class="mk-find-nav">',a+='<button type="button" class="mk-find-btn" data-find-prev aria-label="Previous repo">\u2039</button>',a+='<button type="button" class="mk-find-btn" data-find-next aria-label="Next repo">\u203A</button>',a+="</span>",a+="</div>"),a+=this.#r(o),a+="</div>";let i=this.getAttribute("footnote");i&&(a+=`<p class="mk-grid-foot">${d(i)}</p>`),e.innerHTML=a;let l=e.querySelector("[data-find-prev]"),c=e.querySelector("[data-find-next]");l&&l.addEventListener("click",()=>{this.#n(-1)}),c&&c.addEventListener("click",()=>{this.#n(1)})}});
