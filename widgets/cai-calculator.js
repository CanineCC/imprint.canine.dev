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
`;function d(r){return String(r??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function f(r){if(r==null||r==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",n=0,a;for(;(a=e.exec(r))!==null;){a.index>n&&(t+=d(r.slice(n,a.index)));let s=a[0];if(s.startsWith("**"))t+=`<strong>${d(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))t+=`<code>${d(s.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);c?t+=`<a href="${d(c[2])}">${d(c[1])}</a>`:t+=d(s)}n=a.index+s.length}return n<r.length&&(t+=d(r.slice(n))),t}var p=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function u(r){let e=r.getAttribute("kicker"),t=r.getAttribute("heading"),n=r.getAttribute("lede");if(!e&&!t&&!n)return"";let a='<div class="mk-section-head">';return e&&(a+=`<span class="mk-kicker">${d(e)}</span>`),t&&(a+=`<h2>${f(t)}</h2>`),n&&(a+=`<p>${f(n)}</p>`),a+="</div>",a}var g=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,h=class extends HTMLElement{#t;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#t=new MutationObserver(()=>{let e=this.dataset.theme;this.#a(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#t.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#t?.disconnect()}#a(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(e,t){let n=this.getAttribute(e);if(n==null||n.trim()==="")return t;try{return JSON.parse(n)}catch{return t}}};var x=b+p+g+`
.mk-calc { display: grid; gap: 0.9rem; }
.mk-calc textarea {
  width: 100%; min-height: 11rem; padding: 0.7rem 0.8rem; border-radius: 8px;
  border: 1px solid var(--line); background: var(--surface); color: var(--ink);
  font-family: var(--mono, ui-monospace, monospace); font-size: 0.8rem; line-height: 1.5; resize: vertical;
}
.mk-calc textarea:focus { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-actions { display: flex; gap: 0.6rem; align-items: center; flex-wrap: wrap; }
.mk-btn { padding: 0.5rem 1rem; border-radius: 8px; border: 1px solid var(--accent); background: var(--accent);
          color: var(--on-accent, #fff); font: inherit; font-weight: 600; cursor: pointer; }
.mk-btn.ghost { background: transparent; color: var(--ink); border-color: var(--line); }
.mk-btn[disabled] { opacity: 0.6; cursor: progress; }
.mk-hint { color: var(--muted); font-size: 0.85rem; }
.mk-head { display: flex; align-items: baseline; gap: 0.6rem; margin: 0.2rem 0 0.5rem; }
.mk-cai { font-size: 2rem; font-weight: 700; font-variant-numeric: tabular-nums; }
.mk-band { padding: 2px 10px; border-radius: 20px; font-size: 0.78rem; font-weight: 700;
           border: 1px solid var(--line); color: var(--muted); }
.mk-lenses { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.mk-lenses th { text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em;
                color: var(--muted); border-bottom: 1px solid var(--line); padding: 6px 8px; }
.mk-lenses td { padding: 6px 8px; border-bottom: 1px solid var(--line); }
.mk-lenses td.n { text-align: right; font-variant-numeric: tabular-nums; }
.mk-note { border: 1px solid var(--line); border-left-width: 4px; border-radius: 8px; padding: 0.75rem 0.9rem; }
.mk-note.ok { border-left-color: var(--good, #2e9e6b); }
.mk-note.bad { border-left-color: var(--crit, #d05353); }
.mk-note h4 { margin: 0 0 0.3rem; font-size: 0.95rem; }
.mk-note p { margin: 0; color: var(--muted); font-size: 0.88rem; }
`,k=`{
  "rubricVersion": "rubric-2026.08.15",
  "qualityBar": "production",
  "analyzableProjects": 3,
  "productionLoc": 1500,
  "dimensions": [
    {
      "id": "D1",
      "category": "code-quality",
      "score": 7.5,
      "confidence": 0.95
    },
    {
      "id": "D3",
      "category": "code-quality",
      "score": 8.2,
      "confidence": 0.95
    },
    {
      "id": "D8",
      "category": "code-quality",
      "score": 6.4,
      "confidence": 0.9
    },
    {
      "id": "D17",
      "category": "explicit-debt",
      "score": 8.8,
      "confidence": 0.9
    },
    {
      "id": "D5",
      "category": "architecture",
      "score": 7.1,
      "confidence": 0.95
    },
    {
      "id": "D7",
      "category": "architecture",
      "score": 8.4,
      "confidence": 0.9
    }
  ]
}`,o=r=>String(r??"").replace(/[<>&"]/g,e=>({"<":"&lt;",">":"&gt;","&":"&amp;",'"':"&quot;"})[e]),m=class extends h{static get observedAttributes(){return["api-base","kicker","heading","lede","brand"]}#t(){let e=u(this);return e||'<div class="mk-section-head"><h2>Score an evidence bundle</h2><p>Paste an evidence bundle \u2014 the measured dimensions \u2014 and the open scorer folds them into the lens scores and the CAI, worst-first, in the open. If the bundle also states a published headline, it is checked against it.</p></div>'}render(e){e.innerHTML=`
      <style>${x}</style>
      ${this.#t()}
      <div class="mk-calc">
        <label class="mk-hint" for="ev">Evidence bundle (JSON) \u2014 dimensions, each scored 0\u201310</label>
        <textarea id="ev" spellcheck="false" placeholder='{ "rubricVersion": "\u2026", "dimensions": [ { "id": "D1", "lens": "code-quality", "score": 7.5, "confidence": 0.95 } ] }'></textarea>
        <div class="mk-actions">
          <button class="mk-btn" type="button" id="go">Compute the CAI</button>
          <button class="mk-btn ghost" type="button" id="sample">Load a sample bundle</button>
          <span class="mk-hint" id="status"></span>
        </div>
        <div id="out"></div>
      </div>
    `,e.getElementById("go").addEventListener("click",()=>this.#a(e)),e.getElementById("sample").addEventListener("click",()=>{e.getElementById("ev").value=k,e.getElementById("out").innerHTML='<p class="mk-hint">Sample bundle loaded \u2014 a labelled example, not a real survey. Press \u201CCompute the CAI\u201D.</p>'})}async#a(e){let t=this.apiBase().replace(/\/$/,""),n=e.getElementById("ev").value.trim(),a=e.getElementById("out"),s=e.getElementById("status"),c=e.getElementById("go");if(!n){a.innerHTML=this.#e("bad","Nothing to score","Paste an evidence bundle, or load the sample.");return}if(!t){a.innerHTML=this.#e("bad","Not configured","This widget has no API base set, so it cannot score anything.");return}c.disabled=!0,s.textContent="Folding\u2026",a.innerHTML="";try{let i=await fetch(`${t}/api/score`,{method:"POST",headers:{"Content-Type":"application/json"},body:n}),l=await i.json().catch(()=>null);if(i.status===429){a.innerHTML=this.#e("bad","Rate limited","The open API is busy right now \u2014 wait a moment and try again.");return}if(!i.ok||!l){a.innerHTML=this.#e("bad","That bundle could not be scored",l&&(l.error||l.title)||"It could not be read as an evidence bundle.");return}a.innerHTML=this.#n(l)}catch{a.innerHTML=this.#e("bad","Could not reach the scorer","The standard's API did not respond. Nothing about your bundle is implied.")}finally{c.disabled=!1,s.textContent=""}}#n(e){let t=typeof e.headline=="number"?e.headline:e.cai,a=(Array.isArray(e.lenses)?e.lenses:[]).slice().sort((i,l)=>(l.contribution??0)-(i.contribution??0)).map(i=>`<tr>
            <td>${o(i.lens)}</td>
            <td class="n">${o((i.score??0).toFixed(1))}</td>
            <td>${o(i.band??"")}</td>
            <td class="n">${o((i.weight??0).toFixed(3))}</td>
          </tr>`).join(""),s=a?`<table class="mk-lenses">
           <thead><tr><th>Lens</th><th class="n">Score</th><th>Band</th><th class="n">Weight</th></tr></thead>
           <tbody>${a}</tbody>
         </table>`:"",c="";if(e.verification&&typeof e.verification.reproduced=="boolean"){let i=e.verification;c=i.reproduced?this.#e("ok","\u2713 Reproduced",`The published headline ${o((i.claimed??0).toFixed(1))} follows from this evidence.`):this.#e("bad","\u2717 Does not reproduce",`This evidence folds to ${o((i.computed??0).toFixed(2))}, but the bundle claims ${o((i.claimed??0).toFixed(2))}. That is falsifiable proof the published number does not follow from the evidence.`)}return`
      <div class="mk-head">
        <span class="mk-cai">${o((t??0).toFixed(1))}</span>
        <span class="mk-band">${o(e.band??"")}</span>
      </div>
      <p class="mk-hint">The headline is the worst-first ordered-weighted average of the lens scores${e.rubricVersion?` under <code>${o(e.rubricVersion)}</code>`:""}.</p>
      ${s}
      ${c}`}#e(e,t,n){return`<div class="mk-note ${e}"><h4>${o(t)}</h4><p>${o(n)}</p></div>`}};customElements.get("cai-calculator")||customElements.define("cai-calculator",m);
