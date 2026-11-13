var g=`
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
:host([data-brand="assert"]) {
  --accent: #b6b6d6;
  --accent-ink: #cbcbe5;
  --accent-wash: #262635;
  --accent-strong: #dfdff1;
  --on-accent: #15191e;
}
:host([data-brand="assert"][data-theme="light"]) {
  --accent: #56557a;
  --accent-ink: #444367;
  --accent-wash: #efeff9;
  --accent-strong: #3d3b5d;
  --on-accent: #ffffff;
}
`;function l(t){return String(t??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function m(t){if(t==null||t==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,n="",r=0,a;for(;(a=e.exec(t))!==null;){a.index>r&&(n+=l(t.slice(r,a.index)));let s=a[0];if(s.startsWith("**"))n+=`<strong>${l(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))n+=`<code>${l(s.slice(1,-1))}</code>`;else{let o=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);o?n+=`<a href="${l(o[2])}">${l(o[1])}</a>`:n+=l(s)}r=a.index+s.length}return r<t.length&&(n+=l(t.slice(r))),n}var b=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function y(t){let e=t.getAttribute("kicker"),n=t.getAttribute("heading"),r=t.getAttribute("lede");if(!e&&!n&&!r)return"";let a='<div class="mk-section-head">';return e&&(a+=`<span class="mk-kicker">${l(e)}</span>`),n&&(a+=`<h2>${m(n)}</h2>`),r&&(a+=`<p>${m(r)}</p>`),a+="</div>",a}var x=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,p=class extends HTMLElement{#a;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#e(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#a=new MutationObserver(()=>{let e=this.dataset.theme;this.#e(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#a.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#a?.disconnect()}#e(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let n=(this.getAttribute("brand")||"").trim().toLowerCase();n==="assay"||n==="cai"||n==="watchdog"||n==="assert"?this.dataset.brand=n:delete this.dataset.brand}json(e,n){let r=this.getAttribute(e);if(r==null||r.trim()==="")return n;try{return JSON.parse(r)}catch{return n}}};function v(t,e){if(!t)return"";let n=t.bestRunId||t.BestRunId,r=t.owner||t.Owner,a=t.name||t.Name;return!n||!r||!a?"":(e||"").trim().replace(/\/$/,"")+"/api/oss/"+encodeURIComponent(r)+"/"+encodeURIComponent(a)+"/report?run="+encodeURIComponent(n)}var k=new Map;function E(t,e,n){let r=(t||"").trim().replace(/\/$/,"");if(!r)return Promise.resolve(n);let a=r+" "+e,s=k.get(a);return s||(s=(async()=>{try{let o=await fetch(r+e);return o.ok?await o.json():n}catch{return n}})(),k.set(a,s),s)}async function w(t){let e=await E(t,"/api/oss",[]);return Array.isArray(e)?e:[]}function S(t){let e=Number(t);return Number.isFinite(e)?N(M(e).toFixed(1)):"\u2014"}function H(){return typeof document<"u"&&/^da(-|$)/i.test(document.documentElement?.lang??"")}function N(t){return H()?t.replace(".",","):t}function M(t){let e=Number(t)*10,n=Math.floor(e),r=(e-n===.5?n%2===0?n:n+1:Math.round(e))/10;return r===0?0:r}var R=g+b+x+`
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
`,L=[["codeHealth","Code Health"],["architecture","Architecture"],["maturity","Maturity"],["productionReadiness","Production Readiness"],["securityCompliance","Security & Compliance"],["domainModelling","Domain Modelling"],["eventDriven","Event-Driven"],["eventSourcing","Event Sourcing"],["accessibility","Accessibility"],["performance","Performance"]],P={csharp:"C#",vbnet:"VB.NET",fsharp:"F#",javascript:"JavaScript",typescript:"TypeScript",php:"PHP"},A=t=>P[t]||t&&t[0].toUpperCase()+t.slice(1),c=t=>String(t??"").replace(/[<>&"]/g,e=>({"<":"&lt;",">":"&gt;","&":"&amp;",'"':"&quot;"})[e]);function C(t){return L.map(([e])=>e).filter(e=>t[e]!==void 0&&t[e]!==null)}function h(t){let e=t.primaryLanguage||null,n=Array.isArray(t.secondaryLanguages)?t.secondaryLanguages:[];return e?[e,...n]:[]}var f=class extends p{static get observedAttributes(){return["api-base","kicker","heading","lede","brand"]}#a(){let e=y(this);return e||'<div class="mk-section-head"><h2>Public surveys</h2><p>Every survey whose owner chose to publish it, with the score exactly as it was measured. The number is reproducible from the evidence and the rubric it was scored under.</p></div>'}#e=[];#t=null;#n=null;render(e){e.innerHTML=`<style>${R}</style>${this.#a()}<div id="body"><p class="mk-empty">Loading the published record\u2026</p></div>`,this.#r(e)}async liveLoad(){let e=await w(this.apiBase());this.#e=Array.isArray(e)?e:[],this.shadowRoot&&this.#r(this.shadowRoot)}#r(e){let n=e.getElementById("body");if(!n)return;if(this.#e.length===0){n.innerHTML='<p class="mk-empty">No published surveys are available right now.</p>';return}let r=this.#e.filter(a=>(!this.#t||h(a).includes(this.#t))&&(!this.#n||C(a).includes(this.#n)));n.innerHTML=this.#s()+this.#o(r)+this.#i(r,e),this.#c(e)}#s(){let e=new Map;for(let o of this.#e)for(let i of h(o))e.set(i,(e.get(i)||0)+1);let n=L.map(([o,i])=>[o,i,this.#e.filter(d=>C(d).includes(o)).length]).filter(([,,o])=>o>0),r=[...e.entries()].sort((o,i)=>i[1]-o[1]||o[0].localeCompare(i[0]));if(r.length===0&&n.length===0)return"";let a=(o,i,d,u,$)=>`<button class="mk-chip" type="button" data-kind="${o}" data-value="${c(i??"")}" aria-pressed="${$}">${c(d)}${u===null?"":` <span class="n">${u}</span>`}</button>`,s='<div class="mk-filters">';if(r.length){s+=`<div class="mk-frow"><span class="mk-flabel">Language</span>${a("lang","","All",null,this.#t===null)}`;for(let[o,i]of r)s+=a("lang",o,A(o),i,this.#t===o);s+="</div>"}if(n.length){s+=`<div class="mk-frow"><span class="mk-flabel">Lens</span>${a("lens","","All",null,this.#n===null)}`;for(let[o,i,d]of n)s+=a("lens",o,i,d,this.#n===o);s+="</div>"}return s+"</div>"}#o(e){return`<p class="mk-count">${this.#t||this.#n?`${e.length} of ${this.#e.length} published surveys`:`${this.#e.length} published surveys`}</p>`}#i(e){if(e.length===0)return'<p class="mk-empty">No published survey matches that combination.</p>';let n=this.apiBase();return`<table class="mk-table">
        <thead><tr><th>Repository</th><th>Languages</th><th>CAI</th><th>Band</th><th></th></tr></thead>
        <tbody>${e.map(a=>{let s=h(a),o=s.length?s.map((d,u)=>`<span class="mk-lang${u===0?" primary":""}">${c(A(d))}</span>`).join(""):'<span class="mk-lang">\u2014</span>',i=v(a,n);return`<tr>
            <td><span class="mk-repo">${c(a.display||a.name)}</span><br><span class="mk-owner">${c(a.owner)}/${c(a.name)}</span></td>
            <td>${o}</td>
            <td class="mk-cai">${c(S(a.headlineScore??0))}</td>
            <td>${c(a.band??"")}</td>
            <td>${i?`<a href="${c(i)}" target="_blank" rel="noopener">Read the survey \u2192</a>`:""}</td>
          </tr>`}).join("")}</tbody>
      </table>`}#c(e){for(let n of e.querySelectorAll(".mk-chip"))n.addEventListener("click",()=>{let{kind:r,value:a}=n.dataset,s=a===""?null:a;r==="lang"?this.#t=this.#t===s?null:s:this.#n=this.#n===s?null:s,this.#r(e)})}};customElements.get("cai-report-index")||customElements.define("cai-report-index",f);
