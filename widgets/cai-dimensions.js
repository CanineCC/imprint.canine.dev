var k=`
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
`;function o(t){return String(t??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function w(t){if(t==null||t==="")return"";let s=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,n="",r=0,c;for(;(c=s.exec(t))!==null;){c.index>r&&(n+=o(t.slice(r,c.index)));let d=c[0];if(d.startsWith("**"))n+=`<strong>${o(d.slice(2,-2))}</strong>`;else if(d.startsWith("`"))n+=`<code>${o(d.slice(1,-1))}</code>`;else{let p=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(d);p?n+=`<a href="${o(p[2])}">${o(p[1])}</a>`:n+=o(d)}r=c.index+d.length}return r<t.length&&(n+=o(t.slice(r))),n}var $=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function S(t){let s=t.getAttribute("kicker"),n=t.getAttribute("heading"),r=t.getAttribute("lede");if(!s&&!n&&!r)return"";let c='<div class="mk-section-head">';return s&&(c+=`<span class="mk-kicker">${o(s)}</span>`),n&&(c+=`<h2>${w(n)}</h2>`),r&&(c+=`<p>${w(r)}</p>`),c+="</div>",c}var C=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,b=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let s=this.dataset.theme;this.#t(),this.dataset.theme!==s&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let s=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=s;let n=(this.getAttribute("brand")||"").trim().toLowerCase();n==="assay"||n==="cai"||n==="watchdog"?this.dataset.brand=n:delete this.dataset.brand}json(s,n){let r=this.getAttribute(s);if(r==null||r.trim()==="")return n;try{return JSON.parse(r)}catch{return n}}};var D=`
.dx { max-width: 72rem; margin: 0 auto; }
.dx-bar { display: flex; align-items: center; gap: 0.7rem; flex-wrap: wrap; margin: 0 0 1.4rem; }
.dx-pick { display: inline-flex; align-items: center; gap: 0.45rem; font-size: var(--fs-xs); color: var(--muted); }
.dx-pick select {
  font: 600 var(--fs-xs) var(--font-mono); color: var(--ink); background: var(--surface);
  border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 5px 8px;
}
.dx-count { font-family: var(--font-mono); font-size: var(--fs-2xs); color: var(--muted); font-variant-numeric: tabular-nums; }
.dx-badge {
  display: inline-flex; align-items: center; gap: 0.4rem; font-family: var(--font-mono);
  font-size: 10px; font-weight: 600; letter-spacing: 0.07em; text-transform: uppercase;
  color: var(--accent-ink); background: var(--accent-wash); border: 1px solid var(--border-strong);
  border-radius: var(--r-full); padding: 4px 9px;
}
.dx-badge.sample { color: var(--muted); background: transparent; border-style: dashed; }
.dx-lenses { display: grid; grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr)); gap: 0.7rem; margin: 0 0 2rem; }
.dx-lens { border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 0.85rem 0.95rem; }
.dx-lens-name { font-size: var(--fs-md); font-weight: 600; color: var(--heading); }
.dx-lens-meta { font-family: var(--font-mono); font-size: 9.5px; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); margin-top: 3px; }
.dx-lens-count { font-family: var(--font-mono); font-size: var(--fs-2xs); color: var(--accent-ink); margin-top: 0.5rem; }
.dx-tablewrap { overflow-x: auto; border: 1px solid var(--hairline); border-radius: var(--r-lg); }
table.dx-t { width: 100%; border-collapse: collapse; font-size: var(--fs-sm); }
table.dx-t th, table.dx-t td { text-align: left; padding: 0.55rem 0.75rem; border-bottom: 1px solid var(--hairline); vertical-align: top; }
table.dx-t thead th {
  font-family: var(--font-mono); font-size: 9.5px; font-weight: 600; letter-spacing: 0.08em;
  text-transform: uppercase; color: var(--muted); background: var(--surface-2); white-space: nowrap;
}
table.dx-t tbody tr:last-child td { border-bottom: 0; }
td.dx-code { font-family: var(--font-mono); font-size: var(--fs-xs); font-weight: 600; color: var(--accent-ink); white-space: nowrap; }
td.dx-name { color: var(--heading); font-weight: 500; white-space: nowrap; }
td.dx-what { color: var(--ink-soft); line-height: 1.5; min-width: 20rem; }
td.dx-lensc { color: var(--muted); white-space: nowrap; }
span.dx-ev { font-family: var(--font-mono); font-size: 9.5px; letter-spacing: 0.05em; text-transform: uppercase; border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 2px 6px; color: var(--muted); }
span.dx-ev.tool { color: var(--accent-ink); }
.dx-find {
  flex: 1 1 16rem; min-width: 12rem; font: inherit; font-size: var(--fs-xs); color: var(--ink);
  background: var(--surface); border: 1px solid var(--border-strong); border-radius: var(--r-sm);
  padding: 6px 10px;
}
.dx-find::placeholder { color: var(--muted); }
span.dx-fam {
  font-family: var(--font-mono); font-size: 9.5px; letter-spacing: 0.05em; text-transform: uppercase;
  border: 1px dashed var(--border-strong); border-radius: var(--r-sm); padding: 2px 6px; color: var(--muted);
  white-space: nowrap;
}
span.dx-fam.coded { border-style: solid; color: var(--accent-ink); }
.dx-none { font-size: var(--fs-sm); color: var(--muted); margin: 1rem 0 0; }
.dx-none code { font-family: var(--font-mono); font-size: var(--fs-xs); }
.dx-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1rem 0 0; line-height: 1.6; }
.dx-foot a { color: var(--accent-ink); }
`,H={rubricVersion:"sample",lenses:[{key:"codeHealth",name:"Code health",alwaysOn:!0},{key:"architecture",name:"Architecture",alwaysOn:!0},{key:"maturity",name:"Maturity",alwaysOn:!0},{key:"productionReadiness",name:"Readiness",alwaysOn:!0},{key:"securityCompliance",name:"Security & Compliance",alwaysOn:!0}],dimensions:[{id:"D1",name:"Cyclomatic Complexity",lens:"codeHealth",evaluator:"tool",family:"dimension",whatItMeasures:"How tangled the control flow is."},{id:"D13",name:"Secret Scanning",lens:"productionReadiness",evaluator:"tool",family:"dimension",whatItMeasures:"Whether secrets have leaked into the code."},{id:"D28",name:"Secrets (history)",lens:"securityCompliance",evaluator:"tool",family:"dimension",whatItMeasures:"Secrets reachable in git history."}]},M={codeHealth:"Code health",architecture:"Architecture",maturity:"Maturity",productionReadiness:"Readiness",securityCompliance:"Security & Compliance",domainModelling:"Domain Modelling",eventDriven:"Event-Driven",eventSourcing:"Event Sourcing",accessibility:"Accessibility",performance:"Performance"};function E(t,s){return(s||[]).find(r=>r.key===t||r.id===t)?.name||M[t]||t||"\u2014"}async function A(t,s){let n=(t||"").trim().replace(/\/$/,"");if(!n)return null;try{let r=await fetch(n+s);return r.ok?await r.json():null}catch{return null}}customElements.define("cai-dimensions",class extends b{#e=null;#t=null;#n=null;#a=!1;#r="";async liveLoad(){let t=this.apiBase();if(!t){this.#a=!0;return}let s=await A(t,"/api/rubrics");if(!s||!Array.isArray(s.versions)||s.versions.length===0){this.#a=!0,this.render(this.shadowRoot);return}let n=this.#n||s.latest||s.versions[0],r=await A(t,`/api/rubrics/${encodeURIComponent(n)}/catalog`);if(!r||!Array.isArray(r.dimensions)){this.#a=!0,this.render(this.shadowRoot);return}this.#e=s,this.#t=r,this._live=!0,this.render(this.shadowRoot)}#s(t){this.#n=t,this.liveLoad().catch(()=>{})}render(t){let s=this._live&&this.#t,n=s?this.#t:H,r=n.dimensions||[],c=n.lenses||[],d=e=>(e.family||"dimension")==="dimension",p=r.filter(d),z=r.length-p.length,L=[...r].sort((e,i)=>{let l=d(e)?0:1,f=d(i)?0:1;if(l!==f)return l-f;let h=String(e.id).replace(/[0-9]/g,""),m=String(i.id).replace(/[0-9]/g,"");if(h!==m)return h.localeCompare(m);let x=parseInt(String(e.id).replace(/\D/g,""),10)||0,g=parseInt(String(i.id).replace(/\D/g,""),10)||0;return x-g}),a=`<style>${k}${$}${C}${D}</style>`;if(a+=S(this),a+='<div class="dx">',a+='<div class="dx-bar">',s&&this.#e){let e=this.#e.versions.map(i=>`<option value="${o(i)}"${i===n.rubricVersion?" selected":""}>${o(i)}</option>`).join("");a+=`<label class="dx-pick">Rubric version <select part="version">${e}</select></label>`,a+='<span class="dx-badge">live from the archive</span>'}else this.#a?a+='<span class="dx-badge sample">sample \u2014 the archive was not reachable</span>':a+='<span class="dx-badge sample">sample \u2014 the live catalogue loads from the archive</span>';if(a+=`<span class="dx-count">${r.length} dimensions \xB7 ${p.length} with a citable code \xB7 ${z} meta \xB7 ${c.length} lenses</span>`,a+='<input class="dx-find" type="search" part="filter" placeholder="Filter by code, name or description \u2014 e.g. ED4" aria-label="Filter dimensions">',a+="</div>",c.length){a+='<div class="dx-lenses">';for(let e of c){let i=e.key||e.id,l=r.filter(g=>g.lens===i),f=l.length,h=l.filter(d).length,m=e.alwaysOn===!0||e.always===!0||e.alwaysOn===!1||e.always===!1,x=e.alwaysOn===!0||e.always===!0;a+='<div class="dx-lens">',a+=`<div class="dx-lens-name">${o(e.name||E(i,c))}</div>`,m&&(a+=`<div class="dx-lens-meta">${x?"always on":"conditional"}</div>`),a+=`<div class="dx-lens-count">${f} dimension${f===1?"":"s"}${h?` \xB7 ${h} coded`:""}</div>`,a+="</div>"}a+="</div>"}a+='<div class="dx-tablewrap"><table class="dx-t">',a+="<thead><tr><th>Code</th><th>Dimension</th><th>What it measures</th><th>Lens</th><th>Cited</th><th>Evaluator</th></tr></thead><tbody>";for(let e of L){let i=(e.evaluator||"").toLowerCase(),l=d(e),f=`${e.id} ${e.name||""} ${e.whatItMeasures||""}`.toLowerCase();a+=`<tr data-code="${o(String(e.id).toLowerCase())}" data-find="${o(f)}">`,a+=`<td class="dx-code">${o(e.id)}</td>`,a+=`<td class="dx-name">${o(e.name||"")}</td>`,a+=`<td class="dx-what">${o(e.whatItMeasures||"")}</td>`,a+=`<td class="dx-lensc">${o(E(e.lens,c))}</td>`,a+=`<td><span class="dx-fam${l?" coded":""}">${l?"on findings":"lens only"}</span></td>`,a+=`<td><span class="dx-ev ${i==="tool"?"tool":""}">${o(i||"\u2014")}</span></td>`,a+="</tr>"}if(a+="</tbody></table></div>",a+='<p class="dx-none" hidden>No dimension matches that. Codes look like <code>D4</code>, <code>ED4</code> or <code>AX9</code>.</p>',s){let e=o(n.rubricVersion),i=o(this.apiBase().replace(/\/$/,""));a+=`<p class="dx-foot">Read verbatim from the published catalogue <code>${e}</code>. Rubrics are immutable, so a score always names the one it was computed under \u2014 pick that version above to read the definitions it was scored against. This catalogue as JSON: <a href="${i}/api/rubrics/${e}/catalog">${e}/catalog</a>.</p>`}a+="</div>",t.innerHTML=a;let v=t.querySelector("select");v&&v.addEventListener("change",e=>this.#s(e.target.value));let u=t.querySelector(".dx-find"),y=t.querySelector(".dx-none");if(u){let e=[...t.querySelectorAll("tbody tr")];u.value=this.#r||"";let i=()=>{let l=u.value.trim().toLowerCase();this.#r=u.value;let f=/^[a-z]{1,3}\d*$/.test(l),h=0;for(let m of e){let x=!l||(f?m.dataset.code.startsWith(l):m.dataset.find.includes(l));m.hidden=!x,x&&h++}y&&(y.hidden=h>0)};u.addEventListener("input",i),u.value&&i()}}});
