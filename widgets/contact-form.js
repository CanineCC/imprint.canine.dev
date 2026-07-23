var y=`
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
`;function s(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function b(a){if(a==null||a==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",n=0,i;for(;(i=r.exec(a))!==null;){i.index>n&&(e+=s(a.slice(n,i.index)));let c=i[0];if(c.startsWith("**"))e+=`<strong>${s(c.slice(2,-2))}</strong>`;else if(c.startsWith("`"))e+=`<code>${s(c.slice(1,-1))}</code>`;else{let t=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(c);t?e+=`<a href="${s(t[2])}">${s(t[1])}</a>`:e+=s(c)}n=i.index+c.length}return n<a.length&&(e+=s(a.slice(n))),e}var w=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function S(a){let r=a.getAttribute("kicker"),e=a.getAttribute("heading"),n=a.getAttribute("lede");if(!r&&!e&&!n)return"";let i='<div class="mk-section-head">';return r&&(i+=`<span class="mk-kicker">${s(r)}</span>`),e&&(i+=`<h2>${b(e)}</h2>`),n&&(i+=`<p>${b(n)}</p>`),i+="</div>",i}var E=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,g=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#t(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let n=this.getAttribute(r);if(n==null||n.trim()==="")return e;try{return JSON.parse(n)}catch{return e}}};var C={sales:["sales"],demo:["demo"],onprem:["on-prem","self-hosted"],compliance:["compliance"],general:["something else"],security:["security"],appraisal:["appraisal"],attestation:["attestation"],consequences:["consequences"],dd:["due-diligence","due diligence"],portfolio:["portfolio"],tender:["tender"]};function A(a,r){if(!a)return null;let e=String(a).trim().toLowerCase();if(!e)return null;let n=r.find(i=>i.trim().toLowerCase()===e);if(n)return n;for(let i of C[e]||[]){let c=r.find(t=>t.toLowerCase().includes(i));if(c)return c}return null}var L=y+w+E+`
.mk-form { display: grid; gap: 1rem; max-width: 40rem; margin: 0 auto; }
.mk-form label { display: grid; gap: 0.35rem; font-size: var(--fs-sm); font-weight: 500; color: var(--heading); }
.mk-form input, .mk-form select, .mk-form textarea {
  font: 400 var(--fs-md)/1.4 var(--font-ui);
  color: var(--ink); background: var(--surface);
  border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 9px 11px;
}
.mk-form textarea { min-height: 130px; resize: vertical; }
.mk-form-hp { position: absolute; left: -9999px; height: 0; overflow: hidden; }
.mk-form-status { font-size: var(--fs-sm); color: var(--muted); }
/* Success gets a dialog, not a line of text. The form can sit well below the fold on a long page,
   so an inline "Thanks" is easy to miss entirely \u2014 the sender is left unsure whether it sent. A
   modal cannot be scrolled past and has to be dismissed, which is the acknowledgement they came for. */
.mk-modal { position: fixed; inset: 0; display: grid; place-items: center; z-index: 9999;
  background: rgba(0,0,0,.55); padding: 24px; }
.mk-modal[hidden] { display: none; }
.mk-modal-card { background: var(--surface); color: var(--ink); border: 1px solid var(--border);
  border-radius: var(--r-lg); padding: 22px 24px; max-width: 24rem; width: 100%;
  box-shadow: 0 12px 40px rgb(0 0 0 / .45); text-align: center; }
.mk-modal-card h3 { margin: 0 0 6px; font-size: var(--fs-lg); color: var(--heading); }
.mk-modal-card p { margin: 0 0 16px; font-size: var(--fs-sm); color: var(--muted); }
.mk-form-status.is-error { color: var(--band-poor-text); }
.mk-form-status.is-ok { color: var(--band-exemplary-text); }
.mk-cta-row { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: flex-start; }
.btn { display: inline-flex; align-items: center; justify-content: center; gap: 8px; font: 500 var(--fs-md)/1.2 var(--font-ui); color: var(--ink); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 8px 14px; cursor: pointer; text-decoration: none; }
.btn:hover { background: var(--surface-2); }
.btn-primary { background: var(--accent-strong); border-color: var(--accent-strong); color: var(--on-accent); font-weight: 600; }
.btn-primary:hover { opacity: 0.92; }
.btn-lg { padding: 12px 22px; font-size: var(--fs-lg); font-weight: 600; border-radius: var(--r-md); }
`;customElements.define("contact-form",class extends g{render(a){let r=(this.json("topics",[])||[]).map(String),e=(this.getAttribute("action")||"").trim()||"https://app.imprint.canine.dev/api/contact",n=this.getAttribute("privacy-note"),i=r[0]||"";try{let o=new URLSearchParams(window.location.search).get("topic"),m=A(o,r);m&&(i=m)}catch{}let c=r.length>0?'<label>What&rsquo;s this about?<select name="topic">'+r.map(o=>`<option value="${s(o)}"${o===i?" selected":""}>${s(o)}</option>`).join("")+"</select></label>":"",t=`<style>${L}</style>`;t+=S(this),t+=`<form class="mk-form" method="post" action="${s(e)}">`,t+=c,t+='<label>Your name<input name="name" autocomplete="name" required></label>',t+='<label>Work email<input name="email" type="email" autocomplete="email" required></label>',t+='<label>Organisation <span style="font-weight:400">(optional)</span><input name="org" autocomplete="organization"></label>',t+='<label>How can we help?<textarea name="message" required></textarea></label>',t+='<label class="mk-form-hp" aria-hidden="true">Website<input name="website" tabindex="-1" autocomplete="off"></label>';{let o="";try{o=window.location.hostname}catch{}t+=`<input type="hidden" name="site" value="${s(o)}">`}t+='<div class="mk-cta-row"><button class="btn btn-primary btn-lg" type="submit">Send message</button></div>',t+='<p class="mk-form-status" aria-live="polite"></p>',t+=`<div class="mk-modal" role="dialog" aria-modal="true" aria-labelledby="mk-sent-h" hidden>
        <div class="mk-modal-card">
          <h3 id="mk-sent-h">Thanks \u2014 message sent.</h3>
          <p>We'll get back to you.</p>
          <button type="button" class="mk-btn mk-btn-primary" data-mk-ok>OK</button>
        </div>
      </div>`,n&&(t+=`<p class="mk-form-status">${b(n)}</p>`),t+="</form>",a.innerHTML=t;let l=a.querySelector("form");if(!l)return;let f=l.querySelector(".mk-form-status"),p=l.querySelector('button[type="submit"]'),h=(o,m)=>{f&&(f.textContent=o,f.classList.toggle("is-ok",m===!0),f.classList.toggle("is-error",m===!1))},d=a.querySelector(".mk-modal"),u=d&&d.querySelector("[data-mk-ok]"),x=()=>{if(!d)return;d.hidden=!0;let o=l.querySelector("input, textarea");o&&o.focus()},k=()=>{if(!d){h("Thanks \u2014 we'll get back to you.",!0);return}h("",null),d.hidden=!1,u&&u.focus()};u&&u.addEventListener("click",x),d&&(d.addEventListener("click",o=>{o.target===d&&x()}),d.addEventListener("keydown",o=>{o.key==="Escape"&&x()})),l.addEventListener("submit",async o=>{let m=l.querySelector('input[name="website"]');if(o.preventDefault(),m&&m.value.trim()!==""){k();return}p&&(p.disabled=!0),h("Sending\u2026",null);try{let v=await fetch(e,{method:"POST",body:new URLSearchParams(new FormData(l))});if(!v.ok)throw new Error(String(v.status));l.reset(),k()}catch{h("Something went wrong sending your message \u2014 please try again in a minute.",!1)}finally{p&&(p.disabled=!1)}})}});
