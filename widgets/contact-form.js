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
`;function s(a){return String(a??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function h(a){if(a==null||a==="")return"";let r=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,e="",n=0,o;for(;(o=r.exec(a))!==null;){o.index>n&&(e+=s(a.slice(n,o.index)));let i=o[0];if(i.startsWith("**"))e+=`<strong>${s(i.slice(2,-2))}</strong>`;else if(i.startsWith("`"))e+=`<code>${s(i.slice(1,-1))}</code>`;else{let t=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(i);t?e+=`<a href="${s(t[2])}">${s(t[1])}</a>`:e+=s(i)}n=o.index+i.length}return n<a.length&&(e+=s(a.slice(n))),e}var x=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function k(a){let r=a.getAttribute("kicker"),e=a.getAttribute("heading"),n=a.getAttribute("lede");if(!r&&!e&&!n)return"";let o='<div class="mk-section-head">';return r&&(o+=`<span class="mk-kicker">${s(r)}</span>`),e&&(o+=`<h2>${h(e)}</h2>`),n&&(o+=`<p>${h(n)}</p>`),o+="</div>",o}var v=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,u=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let r=this.dataset.theme;this.#t(),this.dataset.theme!==r&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let r=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=r;let e=(this.getAttribute("brand")||"").trim().toLowerCase();e==="assay"||e==="cai"||e==="watchdog"?this.dataset.brand=e:delete this.dataset.brand}json(r,e){let n=this.getAttribute(r);if(n==null||n.trim()==="")return e;try{return JSON.parse(n)}catch{return e}}};var w={sales:["sales"],demo:["demo"],onprem:["on-prem","self-hosted"],compliance:["compliance"],general:["something else"],security:["security"],appraisal:["appraisal"],attestation:["attestation"],consequences:["consequences"],dd:["due-diligence","due diligence"],portfolio:["portfolio"],tender:["tender"]};function y(a,r){if(!a)return null;let e=String(a).trim().toLowerCase();if(!e)return null;let n=r.find(o=>o.trim().toLowerCase()===e);if(n)return n;for(let o of w[e]||[]){let i=r.find(t=>t.toLowerCase().includes(o));if(i)return i}return null}var S=g+x+v+`
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
.mk-form-status.is-error { color: var(--band-poor-text); }
.mk-form-status.is-ok { color: var(--band-exemplary-text); }
.mk-cta-row { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: flex-start; }
.btn { display: inline-flex; align-items: center; justify-content: center; gap: 8px; font: 500 var(--fs-md)/1.2 var(--font-ui); color: var(--ink); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--r-sm); padding: 8px 14px; cursor: pointer; text-decoration: none; }
.btn:hover { background: var(--surface-2); }
.btn-primary { background: var(--accent-strong); border-color: var(--accent-strong); color: var(--on-accent); font-weight: 600; }
.btn-primary:hover { opacity: 0.92; }
.btn-lg { padding: 12px 22px; font-size: var(--fs-lg); font-weight: 600; border-radius: var(--r-md); }
`;customElements.define("contact-form",class extends u{render(a){let r=(this.json("topics",[])||[]).map(String),e=(this.getAttribute("action")||"").trim()||"https://app.imprint.canine.dev/api/contact",n=this.getAttribute("privacy-note"),o=r[0]||"";try{let c=new URLSearchParams(window.location.search).get("topic"),l=y(c,r);l&&(o=l)}catch{}let i=r.length>0?'<label>What&rsquo;s this about?<select name="topic">'+r.map(c=>`<option value="${s(c)}"${c===o?" selected":""}>${s(c)}</option>`).join("")+"</select></label>":"",t=`<style>${S}</style>`;t+=k(this),t+=`<form class="mk-form" method="post" action="${s(e)}">`,t+=i,t+='<label>Your name<input name="name" autocomplete="name" required></label>',t+='<label>Work email<input name="email" type="email" autocomplete="email" required></label>',t+='<label>Organisation <span style="font-weight:400">(optional)</span><input name="org" autocomplete="organization"></label>',t+='<label>How can we help?<textarea name="message" required></textarea></label>',t+='<label class="mk-form-hp" aria-hidden="true">Website<input name="website" tabindex="-1" autocomplete="off"></label>';{let c="";try{c=window.location.hostname}catch{}t+=`<input type="hidden" name="site" value="${s(c)}">`}t+='<div class="mk-cta-row"><button class="btn btn-primary btn-lg" type="submit">Send message</button></div>',t+='<p class="mk-form-status" aria-live="polite"></p>',n&&(t+=`<p class="mk-form-status">${h(n)}</p>`),t+="</form>",a.innerHTML=t;let d=a.querySelector("form");if(!d)return;let f=d.querySelector(".mk-form-status"),m=d.querySelector('button[type="submit"]'),p=(c,l)=>{f&&(f.textContent=c,f.classList.toggle("is-ok",l===!0),f.classList.toggle("is-error",l===!1))};d.addEventListener("submit",async c=>{let l=d.querySelector('input[name="website"]');if(c.preventDefault(),l&&l.value.trim()!==""){p("Thanks \u2014 we'll get back to you.",!0);return}m&&(m.disabled=!0),p("Sending\u2026",null);try{let b=await fetch(e,{method:"POST",body:new URLSearchParams(new FormData(d))});if(!b.ok)throw new Error(String(b.status));d.reset(),p("Thanks \u2014 we'll get back to you.",!0)}catch{p("Something went wrong sending your message \u2014 please try again in a minute.",!1)}finally{m&&(m.disabled=!1)}})}});
