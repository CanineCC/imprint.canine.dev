var b=`
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
`;function l(n){return String(n??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function m(n){if(n==null||n==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",a=0,r;for(;(r=e.exec(n))!==null;){r.index>a&&(t+=l(n.slice(a,r.index)));let s=r[0];if(s.startsWith("**"))t+=`<strong>${l(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))t+=`<code>${l(s.slice(1,-1))}</code>`;else{let i=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);i?t+=`<a href="${l(i[2])}">${l(i[1])}</a>`:t+=l(s)}a=r.index+s.length}return a<n.length&&(t+=l(n.slice(a))),t}var g=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function x(n){let e=n.getAttribute("kicker"),t=n.getAttribute("heading"),a=n.getAttribute("lede");if(!e&&!t&&!a)return"";let r='<div class="mk-section-head">';return e&&(r+=`<span class="mk-kicker">${l(e)}</span>`),t&&(r+=`<h2>${m(t)}</h2>`),a&&(r+=`<p>${m(a)}</p>`),r+="</div>",r}var y=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,h=class extends HTMLElement{#r;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#r=new MutationObserver(()=>{let e=this.dataset.theme;this.#e(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#r.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#r?.disconnect()}#e(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(e,t){let a=this.getAttribute(e);if(a==null||a.trim()==="")return t;try{return JSON.parse(a)}catch{return t}}};function v(n,e){if(!n)return"";let t=n.bestRunId||n.BestRunId,a=n.owner||n.Owner,r=n.name||n.Name;return!t||!a||!r?"":(e||"").trim().replace(/\/$/,"")+"/api/oss/"+encodeURIComponent(a)+"/"+encodeURIComponent(r)+"/report?run="+encodeURIComponent(t)}var k=new Map;function L(n,e,t){let a=(n||"").trim().replace(/\/$/,"");if(!a)return Promise.resolve(t);let r=a+" "+e,s=k.get(r);return s||(s=(async()=>{try{let i=await fetch(a+e);return i.ok?await i.json():t}catch{return t}})(),k.set(r,s),s)}async function w(n){let e=await L(n,"/api/oss",[]);return Array.isArray(e)?e:[]}var E=b+g+y+`
.mk-filters { display: grid; gap: 0.45rem; margin: 0 0 0.9rem; }
.mk-frow { display: flex; flex-wrap: wrap; gap: 0.35rem; align-items: center; }
.mk-flabel { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); min-width: 4.6rem; }
.mk-chip { padding: 2px 10px; border: 1px solid var(--line); border-radius: 20px; font-size: 0.8rem;
           color: var(--muted); background: transparent; cursor: pointer; font: inherit; font-size: 0.8rem; }
.mk-chip:hover { border-color: var(--accent); color: var(--accent); }
.mk-chip[aria-pressed="true"] { background: var(--accent); border-color: var(--accent); color: var(--on-accent, #fff); }
.mk-chip .n { opacity: 0.65; font-variant-numeric: tabular-nums; }
.mk-count { color: var(--muted); font-size: 0.85rem; margin: 0 0 0.5rem; }
.mk-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.mk-table th { text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em;
               color: var(--muted); border-bottom: 1px solid var(--line); padding: 8px; }
.mk-table td { padding: 10px 8px; border-bottom: 1px solid var(--line); vertical-align: middle; }
.mk-repo { font-weight: 600; }
.mk-owner { color: var(--muted); font-weight: 400; font-size: 0.85em; }
.mk-cai { font-variant-numeric: tabular-nums; font-weight: 700; }
.mk-lang { display: inline-block; margin: 0 3px 2px 0; padding: 1px 7px; border-radius: 4px; font-size: 0.75rem;
           border: 1px solid var(--line); color: var(--muted); }
.mk-lang.primary { color: var(--ink); font-weight: 600; }
.mk-empty { color: var(--muted); padding: 1.5rem 0; }
`,$=[["codeHealth","Code Health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Production Readiness"],["securityCompliance","Security & Compliance"],["domainModelling","Domain Modelling"],["eventDriven","Event-Driven"],["eventSourcing","Event Sourcing"],["accessibility","Accessibility"],["performance","Performance"]],N={csharp:"C#",vbnet:"VB.NET",fsharp:"F#",javascript:"JavaScript",typescript:"TypeScript",php:"PHP"},S=n=>N[n]||n&&n[0].toUpperCase()+n.slice(1),c=n=>String(n??"").replace(/[<>&"]/g,e=>({"<":"&lt;",">":"&gt;","&":"&amp;",'"':"&quot;"})[e]);function A(n){return $.map(([e])=>e).filter(e=>n[e]!==void 0&&n[e]!==null)}function p(n){let e=n.primaryLanguage||null,t=Array.isArray(n.secondaryLanguages)?n.secondaryLanguages:[];return e?[e,...t]:[]}var f=class extends h{static get observedAttributes(){return["api-base","kicker","heading","lede","brand"]}#r(){let e=x(this);return e||'<div class="mk-section-head"><h2>Public surveys</h2><p>Every survey whose owner chose to publish it, with the score exactly as it was measured. The number is reproducible from the evidence and the rubric it was scored under.</p></div>'}#e=[];#t=null;#n=null;render(e){e.innerHTML=`<style>${E}</style>${this.#r()}<div id="body"><p class="mk-empty">Loading the published record\u2026</p></div>`,this.#a(e)}async liveLoad(){let e=await w(this.apiBase());this.#e=Array.isArray(e)?e:[],this.shadowRoot&&this.#a(this.shadowRoot)}#a(e){let t=e.getElementById("body");if(!t)return;if(this.#e.length===0){t.innerHTML='<p class="mk-empty">No published surveys are available right now.</p>';return}let a=this.#e.filter(r=>(!this.#t||p(r).includes(this.#t))&&(!this.#n||A(r).includes(this.#n)));t.innerHTML=this.#s()+this.#i(a)+this.#o(a,e),this.#c(e)}#s(){let e=new Map;for(let i of this.#e)for(let o of p(i))e.set(o,(e.get(o)||0)+1);let t=$.map(([i,o])=>[i,o,this.#e.filter(d=>A(d).includes(i)).length]).filter(([,,i])=>i>0),a=[...e.entries()].sort((i,o)=>o[1]-i[1]||i[0].localeCompare(o[0]));if(a.length===0&&t.length===0)return"";let r=(i,o,d,u,C)=>`<button class="mk-chip" type="button" data-kind="${i}" data-value="${c(o??"")}" aria-pressed="${C}">${c(d)}${u===null?"":` <span class="n">${u}</span>`}</button>`,s='<div class="mk-filters">';if(a.length){s+=`<div class="mk-frow"><span class="mk-flabel">Language</span>${r("lang","","All",null,this.#t===null)}`;for(let[i,o]of a)s+=r("lang",i,S(i),o,this.#t===i);s+="</div>"}if(t.length){s+=`<div class="mk-frow"><span class="mk-flabel">Lens</span>${r("lens","","All",null,this.#n===null)}`;for(let[i,o,d]of t)s+=r("lens",i,o,d,this.#n===i);s+="</div>"}return s+"</div>"}#i(e){return`<p class="mk-count">${this.#t||this.#n?`${e.length} of ${this.#e.length} published surveys`:`${this.#e.length} published surveys`}</p>`}#o(e){if(e.length===0)return'<p class="mk-empty">No published survey matches that combination.</p>';let t=this.apiBase();return`<table class="mk-table">
        <thead><tr><th>Repository</th><th>Languages</th><th>CAI</th><th>Band</th><th></th></tr></thead>
        <tbody>${e.map(r=>{let s=p(r),i=s.length?s.map((d,u)=>`<span class="mk-lang${u===0?" primary":""}">${c(S(d))}</span>`).join(""):'<span class="mk-lang">\u2014</span>',o=v(r,t);return`<tr>
            <td><span class="mk-repo">${c(r.display||r.name)}</span><br><span class="mk-owner">${c(r.owner)}/${c(r.name)}</span></td>
            <td>${i}</td>
            <td class="mk-cai">${c((r.headlineScore??0).toFixed(1))}</td>
            <td>${c(r.band??"")}</td>
            <td>${o?`<a href="${c(o)}" target="_blank" rel="noopener">Read the survey \u2192</a>`:""}</td>
          </tr>`}).join("")}</tbody>
      </table>`}#c(e){for(let t of e.querySelectorAll(".mk-chip"))t.addEventListener("click",()=>{let{kind:a,value:r}=t.dataset,s=r===""?null:r;a==="lang"?this.#t=this.#t===s?null:s:this.#n=this.#n===s?null:s,this.#a(e)})}};customElements.get("cai-report-index")||customElements.define("cai-report-index",f);
