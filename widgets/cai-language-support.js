var f=`
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
`;function s(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function c(e){if(e==null||e==="")return"";let a=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,n="",t=0,i;for(;(i=a.exec(e))!==null;){i.index>t&&(n+=s(e.slice(t,i.index)));let r=i[0];if(r.startsWith("**"))n+=`<strong>${s(r.slice(2,-2))}</strong>`;else if(r.startsWith("`"))n+=`<code>${s(r.slice(1,-1))}</code>`;else{let o=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(r);o?n+=`<a href="${s(o[2])}">${s(o[1])}</a>`:n+=s(r)}t=i.index+r.length}return t<e.length&&(n+=s(e.slice(t))),n}var p=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function u(e){let a=e.getAttribute("kicker"),n=e.getAttribute("heading"),t=e.getAttribute("lede");if(!a&&!n&&!t)return"";let i='<div class="mk-section-head">';return a&&(i+=`<span class="mk-kicker">${s(a)}</span>`),n&&(i+=`<h2>${c(n)}</h2>`),t&&(i+=`<p>${c(t)}</p>`),i+="</div>",i}var m=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,d=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let a=this.dataset.theme;this.#t(),this.dataset.theme!==a&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let a=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=a;let n=(this.getAttribute("brand")||"").trim().toLowerCase();n==="assay"||n==="cai"||n==="watchdog"?this.dataset.brand=n:delete this.dataset.brand}json(a,n){let t=this.getAttribute(a);if(t==null||t.trim()==="")return n;try{return JSON.parse(t)}catch{return n}}};var h=new Map;function b(e,a,n){let t=(e||"").trim().replace(/\/$/,"");if(!t)return Promise.resolve(n);let i=t+" "+a,r=h.get(i);return r||(r=(async()=>{try{let o=await fetch(t+a);return o.ok?await o.json():n}catch{return n}})(),h.set(i,r),r)}async function g(e){let a=await b(e,"/api/public/language-support",{languages:[],bands:[]});return a&&Array.isArray(a.languages)?a:{languages:[],bands:[]}}var v={note:"FIT is survey clarity \u2014 how clearly a language leads to a complete architecture survey. It is NOT a measure of language or code quality.",languages:[{code:"csharp",displayName:"C#",applicability:10,supportKind:"Deep",band:"PERFECT",summary:"Native symbols resolve every lens with full fidelity.",coveredLenses:["DDD","Event sourcing","Event-driven","Vertical slice"],notApplicableLenses:[]},{code:"java",displayName:"Java",applicability:9,supportKind:"Deep",band:"PERFECT",summary:"Source-resolution is strong; the full domain survey fires.",coveredLenses:["DDD","Event sourcing","Event-driven","Vertical slice"],notApplicableLenses:["strongly-typed-id (field-based)"]},{code:"python",displayName:"Python",applicability:7,supportKind:"Deep",band:"HIGH",summary:"Marker lenses are strong; call-owner lenses partial on untyped code \u2014 declined, never guessed.",coveredLenses:["DDD","Event sourcing","Event-driven"],notApplicableLenses:["Sealed/DU lens"]},{code:"ruby",displayName:"Ruby",applicability:6,supportKind:"Deep",band:"MEDIUM",summary:"The hardest static target; markers and block-fold DSLs still resolve.",coveredLenses:["DDD","Event sourcing","Event-driven"],notApplicableLenses:["god-class/LCOM"]},{code:"javascript",displayName:"JavaScript",applicability:4,supportKind:"Structural",band:"LOW",summary:"Structural suite only, without types \u2014 the least architecture signal.",coveredLenses:["Structural","Module graph"],notApplicableLenses:["DDD","Event sourcing"]}],bands:[{band:"PERFECT",label:"PERFECT",why:"Every lens applies and resolves cleanly \u2014 the clearest surveys we produce. About survey clarity, not language quality."},{band:"HIGH",label:"HIGH",why:"Most lenses apply and resolve \u2014 a clear, well-populated survey. About survey clarity, not language quality."},{band:"MEDIUM",label:"MEDIUM",why:"A meaningful slice of the survey is populated; some lenses are N/A or best-effort. About survey clarity, not language quality."},{band:"LOW",label:"LOW",why:"The structural lenses apply but not the deep domain catalogue. What we report is real; there is simply less of it. About survey clarity, not language quality."}]},y=["PERFECT","HIGH","MEDIUM","LOW"],x={PERFECT:4,HIGH:3,MEDIUM:2,LOW:1},k=f+p+m+`
.mk-fit { max-width: 66rem; margin: 0 auto; }
.mk-fit-note { color: var(--muted); font-size: var(--fs-sm); line-height: 1.6; margin: 0 0 1.6rem; max-width: 60ch; }
.mk-fit-note strong { color: var(--ink-soft); }
.mk-fit-band { margin-top: 1.8rem; }
.mk-fit-band:first-of-type { margin-top: 0; }
.mk-fit-bandhead { display: flex; align-items: baseline; gap: 0.7rem; flex-wrap: wrap; }
.mk-fit-bandname { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.11em; text-transform: uppercase; color: var(--ink-soft); }
.mk-fit-dots { display: inline-flex; gap: 3px; }
.mk-fit-dots i { width: 6px; height: 6px; border-radius: var(--r-full); background: var(--border-strong); display: block; }
.mk-fit-dots i.on { background: var(--accent); box-shadow: 0 0 6px var(--accent-wash); }
.mk-fit-bandcount { color: var(--muted); font-size: var(--fs-2xs); font-variant-numeric: tabular-nums; }
.mk-fit-why { color: var(--muted); font-size: var(--fs-xs); line-height: 1.55; margin: 0.4rem 0 1rem; max-width: 76ch; }
.mk-fit-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr)); gap: 0.75rem; }
.mk-fit-card { position: relative; border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 0.95rem 1rem 0.85rem; display: flex; flex-direction: column; gap: 0.6rem; overflow: hidden; }
.mk-fit-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 3px; background: var(--accent); opacity: var(--edge, 0.85); }
.mk-fit-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.6rem; }
.mk-fit-name { font-size: var(--fs-lg); font-weight: 600; color: var(--heading); letter-spacing: -0.01em; }
.mk-fit-code { font-family: var(--font-mono); font-size: var(--fs-2xs); color: var(--muted); margin-top: 2px; }
.mk-fit-kind { font-family: var(--font-mono); font-size: 9.5px; font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 3px 6px; white-space: nowrap; }
.mk-fit-gauge { display: flex; align-items: center; gap: 0.6rem; }
.mk-fit-track { flex: 1; height: 7px; border-radius: var(--r-full); background: var(--surface-2); border: 1px solid var(--border); overflow: hidden; }
.mk-fit-fill { height: 100%; border-radius: var(--r-full); background: linear-gradient(90deg, var(--accent), var(--accent-strong)); box-shadow: 0 0 8px var(--accent-wash); }
.mk-fit-num { font-family: var(--font-mono); font-size: var(--fs-sm); font-weight: 600; color: var(--accent-ink); min-width: 2.6rem; text-align: right; font-variant-numeric: tabular-nums; }
.mk-fit-num small { color: var(--muted); font-weight: 400; }
.mk-fit-pill { align-self: flex-start; display: inline-flex; align-items: center; gap: 0.4rem; font-family: var(--font-mono); font-size: 10px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; color: var(--accent-ink); background: var(--accent-wash); border: 1px solid var(--border-strong); border-radius: var(--r-full); padding: 4px 9px; }
.mk-fit-pill i { width: 5px; height: 5px; border-radius: var(--r-full); background: var(--accent); box-shadow: 0 0 6px var(--accent-wash); }
.mk-fit-summary { margin: 0; color: var(--ink-soft); font-size: var(--fs-xs); line-height: 1.5; }
.mk-fit-lenses { display: flex; flex-direction: column; gap: 0.4rem; }
.mk-fit-lensrow { display: flex; gap: 0.5rem; align-items: baseline; }
.mk-fit-lenslabel { font-family: var(--font-mono); font-size: 9px; font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); min-width: 3.1rem; padding-top: 3px; }
.mk-fit-chips { display: flex; flex-wrap: wrap; gap: 4px; }
.mk-fit-chip { font-size: var(--fs-2xs); padding: 3px 7px; border-radius: var(--r-sm); background: var(--surface-2); border: 1px solid var(--border); color: var(--ink-soft); }
.mk-fit-chip.na { color: var(--muted); border-style: dashed; }
.mk-fit-foot { display: flex; justify-content: flex-end; margin-top: 0.1rem; padding-top: 0.5rem; border-top: 1px solid var(--hairline); font-family: var(--font-mono); font-size: 10px; color: var(--muted); }
.mk-grid-foot { font-size: var(--fs-xs); color: var(--muted); margin: 1.4rem auto 0; max-width: 60ch; text-align: center; }
`;function w(e){let a=x[e]||0,n='<span class="mk-fit-dots" aria-hidden="true">';for(let t=1;t<=4;t++)n+=`<i class="${t<=a?"on":""}"></i>`;return n+"</span>"}function S(e){let a={PERFECT:1,HIGH:.7,MEDIUM:.45,LOW:.22}[e.band]??.5,n=(e.coveredLenses||[]).map(l=>`<span class="mk-fit-chip">${s(l)}</span>`).join(""),t=(e.notApplicableLenses||[]).map(l=>`<span class="mk-fit-chip na">${s(l)}</span>`).join(""),i=t?`<div class="mk-fit-lensrow"><span class="mk-fit-lenslabel">N/A</span><div class="mk-fit-chips">${t}</div></div>`:"",r=e.signedOffOn?`signed off ${s(e.signedOffOn)}`:"baseline coverage",o=Math.max(0,Math.min(100,(Number(e.applicability)||0)*10));return`<article class="mk-fit-card" style="--edge:${a}"><div class="mk-fit-top"><div><div class="mk-fit-name">${s(e.displayName)}</div><div class="mk-fit-code">${s(e.code)}</div></div><span class="mk-fit-kind">${s(e.supportKind||"")}</span></div><div class="mk-fit-gauge"><div class="mk-fit-track"><div class="mk-fit-fill" style="width:${o}%"></div></div><span class="mk-fit-num">${s(String(e.applicability))}<small>/10</small></span></div><span class="mk-fit-pill"><i></i>${s(e.bandLabel||e.band)} fit</span><p class="mk-fit-summary">${s(e.summary||"")}</p><div class="mk-fit-lenses"><div class="mk-fit-lensrow"><span class="mk-fit-lenslabel">Lenses</span><div class="mk-fit-chips">${n}</div></div>${i}</div><div class="mk-fit-foot">${r}</div></article>`}customElements.define("cai-language-support",class extends d{#e=null;async liveLoad(){let e=this.apiBase();if(!e)return;let a=await g(e);!a||!Array.isArray(a.languages)||a.languages.length===0||(this.#e=a,this._live=!0,this.render(this.shadowRoot))}render(e){let a=this._live&&this.#e?this.#e:v,n={};for(let r of a.bands||[])n[r.band]=r.why;let t=`<style>${k}</style>`;if(t+=u(this),t+='<div class="mk-fit">',a.note&&!this.getAttribute("lede")){let r=a.note.split("\u2014"),o=s(r[0].trim()),l=s(r.slice(1).join("\u2014").trim());t+=`<p class="mk-fit-note"><strong>${o}</strong>${l?" \u2014 "+l:""}</p>`}for(let r of y){let o=a.languages.filter(l=>l.band===r);o.length!==0&&(t+='<section class="mk-fit-band"><div class="mk-fit-bandhead">',t+=`<span class="mk-fit-bandname">${w(r)} ${s(r)} fit</span>`,t+=`<span class="mk-fit-bandcount">${o.length} language${o.length>1?"s":""}</span>`,t+="</div>",n[r]&&(t+=`<p class="mk-fit-why">${s(n[r])}</p>`),t+=`<div class="mk-fit-grid">${o.map(S).join("")}</div>`,t+="</section>")}t+="</div>";let i=this.getAttribute("footnote");i&&(t+=`<p class="mk-grid-foot">${c(i)}</p>`),e.innerHTML=t}});
