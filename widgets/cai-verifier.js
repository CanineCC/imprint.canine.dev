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
`;function c(r){return String(r??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function f(r){if(r==null||r==="")return"";let e=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,t="",i=0,a;for(;(a=e.exec(r))!==null;){a.index>i&&(t+=c(r.slice(i,a.index)));let n=a[0];if(n.startsWith("**"))t+=`<strong>${c(n.slice(2,-2))}</strong>`;else if(n.startsWith("`"))t+=`<code>${c(n.slice(1,-1))}</code>`;else{let s=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(n);s?t+=`<a href="${c(s[2])}">${c(s[1])}</a>`:t+=c(n)}i=a.index+n.length}return i<r.length&&(t+=c(r.slice(i))),t}var b=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function u(r){let e=r.getAttribute("kicker"),t=r.getAttribute("heading"),i=r.getAttribute("lede");if(!e&&!t&&!i)return"";let a='<div class="mk-section-head">';return e&&(a+=`<span class="mk-kicker">${c(e)}</span>`),t&&(a+=`<h2>${f(t)}</h2>`),i&&(a+=`<p>${f(i)}</p>`),a+="</div>",a}var p=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,l=class extends HTMLElement{#t;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#a(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#t=new MutationObserver(()=>{let e=this.dataset.theme;this.#a(),this.dataset.theme!==e&&this.render(this.shadowRoot)}),this.#t.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#t?.disconnect()}#a(){let e=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=e;let t=(this.getAttribute("brand")||"").trim().toLowerCase();t==="assay"||t==="cai"||t==="watchdog"?this.dataset.brand=t:delete this.dataset.brand}json(e,t){let i=this.getAttribute(e);if(i==null||i.trim()==="")return t;try{return JSON.parse(i)}catch{return t}}};var g=m+b+p+`
.mk-verify { display: grid; gap: 0.9rem; }
.mk-verify textarea {
  width: 100%; min-height: 11rem; padding: 0.7rem 0.8rem; border-radius: 8px;
  border: 1px solid var(--line); background: var(--surface); color: var(--ink);
  font-family: var(--mono, ui-monospace, monospace); font-size: 0.8rem; line-height: 1.5;
  resize: vertical;
}
.mk-verify textarea:focus { outline: 2px solid var(--accent); outline-offset: 1px; }
.mk-actions { display: flex; gap: 0.6rem; align-items: center; flex-wrap: wrap; }
.mk-btn {
  padding: 0.5rem 1rem; border-radius: 8px; border: 1px solid var(--accent);
  background: var(--accent); color: var(--on-accent, #fff); font: inherit; font-weight: 600;
  cursor: pointer;
}
.mk-btn[disabled] { opacity: 0.6; cursor: progress; }
.mk-hint { color: var(--muted); font-size: 0.85rem; }
.mk-result { border: 1px solid var(--line); border-left-width: 4px; border-radius: 8px; padding: 0.85rem 1rem; }
.mk-result.ok { border-left-color: var(--good, #2e9e6b); }
.mk-result.bad { border-left-color: var(--crit, #d05353); }
.mk-result h4 { margin: 0 0 0.35rem; font-size: 1rem; }
.mk-result p { margin: 0; color: var(--muted); font-size: 0.9rem; }
.mk-facts { margin: 0.7rem 0 0; padding: 0; list-style: none; display: grid; gap: 0.25rem; font-size: 0.85rem; }
.mk-facts b { color: var(--ink); font-weight: 600; }
.mk-facts code { font-family: var(--mono, ui-monospace, monospace); font-size: 0.82em; }
`,h=class extends l{static get observedAttributes(){return["api-base","kicker","heading","lede","brand"]}#t(){let e=u(this);return e||'<div class="mk-section-head"><h2>Verify a signed survey</h2><p>Paste a signed CAI delivery package. We check two things separately: that it is authentically ours and unedited, and that the number it states follows from the evidence it carries.</p></div>'}render(e){e.innerHTML=`
      <style>${g}</style>
      ${this.#t()}
      <div class="mk-verify">
        <label class="mk-hint" for="pkg">Signed CAI delivery package (JSON)</label>
        <textarea id="pkg" spellcheck="false" placeholder='{ "payload": { "schemaVersion": "1.0", \u2026 }, "signature": { "alg": "Ed25519", \u2026 } }'></textarea>
        <div class="mk-actions">
          <button class="mk-btn" type="button" id="go">Verify the signature</button>
          <span class="mk-hint" id="status"></span>
        </div>
        <div id="out"></div>
      </div>
    `,e.getElementById("go").addEventListener("click",()=>this.#a(e))}async#a(e){let t=this.apiBase().replace(/\/$/,""),i=e.getElementById("pkg").value.trim(),a=e.getElementById("out"),n=e.getElementById("status"),s=e.getElementById("go");if(!i){a.innerHTML=this.#e("bad","Nothing to check","Paste a signed delivery package first.");return}if(!t){a.innerHTML=this.#e("bad","Not configured","This widget has no API base set, so it cannot verify anything.");return}s.disabled=!0,n.textContent="Checking\u2026",a.innerHTML="";try{let o=await fetch(`${t}/api/verify-delivery`,{method:"POST",headers:{"Content-Type":"application/json"},body:i}),d=await o.json().catch(()=>null);if(o.status===429){a.innerHTML=this.#e("bad","Rate limited","The open API is busy right now \u2014 wait a moment and try again.");return}if(!o.ok||!d){a.innerHTML=this.#e("bad","That is not a delivery package",d&&d.error||"It could not be read as a signed CAI delivery package.");return}a.innerHTML=this.#i(d)}catch{a.innerHTML=this.#e("bad","Could not reach the verifier","The standard's API did not respond. Nothing about your package is implied.")}finally{s.disabled=!1,n.textContent=""}}#i(e){let t=s=>String(s??"").replace(/[<>&"]/g,o=>({"<":"&lt;",">":"&gt;","&":"&amp;",'"':"&quot;"})[o]),i;e.trustworthy?i=this.#e("ok","\u2713 Authentic and reproducing","This package was signed by a published CAI key and has not been altered since \u2014 and the CAI it states follows from the evidence it carries."):e.signatureValid?i=this.#e("bad","\u2717 Signed, but the number does not reproduce",`The signature is genuine, so the document really was issued this way \u2014 but folding its own evidence gives ${t((e.computedCai??0).toFixed(2))}, not the ${t((e.claimedCai??0).toFixed(2))} it claims. A valid signature attests the author, never the arithmetic.`):i=this.#e("bad","\u2717 Not authentic",`${t(e.reason||"the signature did not verify")} \u2014 treat this document as unattributed. Either it was not issued by cai.canine.dev, or it has been edited since it was signed.`);let a=e.subject||{},n=`
      <p class="mk-hint" style="margin:.7rem 0 0">A signature checks the document, not that it describes the code you were shown \u2014 confirm the subject:</p>
      <ul class="mk-facts">
        <li><b>Repository:</b> <code>${t(a.repository)}</code>${a.commit?` at commit <code>${t(a.commit)}</code>`:""}</li>
        <li><b>Rubric:</b> <code>${t(e.rubricVersion)}</code></li>
        <li><b>Issued:</b> ${t(e.issuedAt)} \xB7 <b>key</b> <code>${t(e.keyId)}</code></li>
        <li><b>Produced by:</b> ${t(e.producer&&e.producer.name||"\u2014")}</li>
        <li><b>Stated verdict:</b> CAI ${t((e.verdict&&e.verdict.cai)??"\u2014")} (${t(e.verdict&&e.verdict.band||"\u2014")})</li>
      </ul>`;return i+n}#e(e,t,i){return`<div class="mk-result ${e}"><h4>${(n=>String(n??"").replace(/[<>&]/g,s=>({"<":"&lt;",">":"&gt;","&":"&amp;"})[s]))(t)}</h4><p>${i}</p></div>`}};customElements.get("cai-verifier")||customElements.define("cai-verifier",h);
