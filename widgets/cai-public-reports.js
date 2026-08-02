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
`;function o(e){return String(e??"").replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")}function p(e){if(e==null||e==="")return"";let t=/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g,r="",a=0,n;for(;(n=t.exec(e))!==null;){n.index>a&&(r+=o(e.slice(a,n.index)));let s=n[0];if(s.startsWith("**"))r+=`<strong>${o(s.slice(2,-2))}</strong>`;else if(s.startsWith("`"))r+=`<code>${o(s.slice(1,-1))}</code>`;else{let c=/^\[([^\]]+)\]\(([^)]+)\)$/.exec(s);c?r+=`<a href="${o(c[2])}">${o(c[1])}</a>`:r+=o(s)}a=n.index+s.length}return a<e.length&&(r+=o(e.slice(a))),r}var h=`
:host { display: block; color: var(--ink); font: 400 var(--fs-md)/1.5 var(--font-ui); }
* { box-sizing: border-box; }
a { color: var(--accent-ink); text-decoration: none; }
a:hover { text-decoration: underline; }
code { background: var(--surface-2); padding: 1px 5px; border-radius: var(--r-sm); font: 500 var(--fs-xs) var(--font-mono); }
strong { font-weight: 600; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0; }
`;function b(e){let t=e.getAttribute("kicker"),r=e.getAttribute("heading"),a=e.getAttribute("lede");if(!t&&!r&&!a)return"";let n='<div class="mk-section-head">';return t&&(n+=`<span class="mk-kicker">${o(t)}</span>`),r&&(n+=`<h2>${p(r)}</h2>`),a&&(n+=`<p>${p(a)}</p>`),n+="</div>",n}var y=`
.mk-section-head { margin-bottom: 1.5rem; }
.mk-section-head h2 { font-size: clamp(1.5rem, 1.1rem + 1.4vw, 2.1rem); line-height: 1.2; margin: 0.3rem 0 0; color: var(--heading); font-weight: 600; letter-spacing: -0.01em; }
.mk-section-head p { color: var(--muted); font-size: var(--fs-lg); line-height: 1.6; margin: 0.55rem 0 0; }
.mk-kicker { display: inline-flex; align-items: center; gap: 0.55rem; font-size: var(--fs-2xs); font-weight: 600; letter-spacing: 0.09em; text-transform: uppercase; color: var(--muted); }
`,u=class extends HTMLElement{#e;connectedCallback(){this.shadowRoot||this.attachShadow({mode:"open"}),this.#t(),this.render(this.shadowRoot),typeof this.liveLoad=="function"&&Promise.resolve(this.liveLoad()).catch(()=>{}),this.#e=new MutationObserver(()=>{let t=this.dataset.theme;this.#t(),this.dataset.theme!==t&&this.render(this.shadowRoot)}),this.#e.observe(document.documentElement,{attributes:!0,attributeFilter:["data-theme"]})}apiBase(){return(this.getAttribute("api-base")||"").trim()}disconnectedCallback(){this.#e?.disconnect()}#t(){let t=document.documentElement.dataset.theme||(matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light");this.dataset.theme=t;let r=(this.getAttribute("brand")||"").trim().toLowerCase();r==="assay"||r==="cai"||r==="watchdog"?this.dataset.brand=r:delete this.dataset.brand}json(t,r){let a=this.getAttribute(t);if(a==null||a.trim()==="")return r;try{return JSON.parse(a)}catch{return r}}};var v=new Map;function C(e,t,r){let a=(e||"").trim().replace(/\/$/,"");if(!a)return Promise.resolve(r);let n=a+" "+t,s=v.get(n);return s||(s=(async()=>{try{let c=await fetch(a+t);return c.ok?await c.json():r}catch{return r}})(),v.set(n,s),s)}var x={totals:{repositories:3200,publishedSurveys:3800},facets:{languages:[{language:"C#",count:640},{language:"TypeScript",count:410},{language:"Go",count:300},{language:"Rust",count:240},{language:"Python",count:380},{language:"Java",count:260}],lenses:[{key:"domainModelling",label:"Domain modelling"},{key:"eventSourcing",label:"Event sourcing"},{key:"eventDriven",label:"Event-driven"}]},cap:24,matched:6,capped:!1,curated:!0,reports:[{owner:"ardalis",name:"CleanArchitecture",display:"ardalis/CleanArchitecture",score:64,band:"Adequate",bandHex:"#b0872f",primaryLanguage:"C#",secondaryLanguages:[],lenses:["codeHealth","architecture","domainModelling"],tags:["WebApi","clean-architecture"],sourceUrl:"https://github.com/ardalis/CleanArchitecture",reportPath:"/api/oss/ardalis/CleanArchitecture/report"},{owner:"gin-gonic",name:"gin",display:"gin-gonic/gin",score:72,band:"Strong",bandHex:"#1f7a5a",primaryLanguage:"Go",secondaryLanguages:[],lenses:["codeHealth","architecture","maturity"],tags:["library"],sourceUrl:"https://github.com/gin-gonic/gin",reportPath:"/api/oss/gin-gonic/gin/report"},{owner:"tokio-rs",name:"axum",display:"tokio-rs/axum",score:76,band:"Strong",bandHex:"#1f7a5a",primaryLanguage:"Rust",secondaryLanguages:[],lenses:["codeHealth","architecture"],tags:["library"],sourceUrl:"https://github.com/tokio-rs/axum",reportPath:"/api/oss/tokio-rs/axum/report"},{owner:"oskardudycz",name:"EventSourcing.NetCore",display:"oskardudycz/EventSourcing.NetCore",score:68,band:"Adequate",bandHex:"#b0872f",primaryLanguage:"C#",secondaryLanguages:[],lenses:["codeHealth","architecture","eventSourcing","domainModelling"],tags:["event-sourcing"],sourceUrl:"https://github.com/oskardudycz/EventSourcing.NetCore",reportPath:"/api/oss/oskardudycz/EventSourcing.NetCore/report"},{owner:"tiangolo",name:"fastapi",display:"tiangolo/fastapi",score:74,band:"Strong",bandHex:"#1f7a5a",primaryLanguage:"Python",secondaryLanguages:[],lenses:["codeHealth","architecture","maturity"],tags:["WebApi","library"],sourceUrl:"https://github.com/tiangolo/fastapi",reportPath:"/api/oss/tiangolo/fastapi/report"},{owner:"spring-projects",name:"spring-petclinic",display:"spring-projects/spring-petclinic",score:66,band:"Adequate",bandHex:"#b0872f",primaryLanguage:"Java",secondaryLanguages:["TypeScript"],lenses:["codeHealth","architecture","domainModelling"],tags:["WebApp"],sourceUrl:"https://github.com/spring-projects/spring-petclinic",reportPath:"/api/oss/spring-projects/spring-petclinic/report"}]};async function m(e,t={}){if(!(e||"").trim())return x;let r=new URLSearchParams;t.lang&&r.set("lang",t.lang),t.lens&&r.set("lens",t.lens),t.q&&r.set("q",t.q);let a="/api/public/reports"+(r.toString()?"?"+r.toString():""),n=await C(e,a,null);return n&&Array.isArray(n.reports)?n:x}var $={csharp:"C#",fsharp:"F#",vbnet:"VB.NET",cpp:"C++",typescript:"TypeScript",javascript:"JavaScript"},A=e=>$[String(e||"").toLowerCase()]||e||"",k={domainModelling:"DDD",eventSourcing:"Event sourcing",eventDriven:"Event-driven"},H=f+h+y+`
.mk-rep { max-width: 72rem; margin: 0 auto; }
.mk-rep-scale { display: flex; align-items: baseline; gap: 0.6rem; flex-wrap: wrap; margin: 0 0 1.3rem; }
.mk-rep-big { font-family: var(--font-mono); font-size: clamp(1.8rem, 4vw, 2.6rem); font-weight: 700; color: var(--accent-ink); letter-spacing: -0.02em; font-variant-numeric: tabular-nums; line-height: 1; }
.mk-rep-scale-cap { color: var(--muted); font-size: var(--fs-sm); line-height: 1.5; }
.mk-rep-scale-cap strong { color: var(--ink-soft); font-weight: 600; }
.mk-rep-controls { display: flex; gap: 0.6rem; flex-wrap: wrap; align-items: center; margin: 0 0 1.2rem; }
.mk-rep-search { flex: 1 1 14rem; min-width: 10rem; display: flex; align-items: center; gap: 0.5rem; border: 1px solid var(--border-strong); border-radius: var(--r-full); background: var(--surface); padding: 0.45rem 0.9rem; }
.mk-rep-search input { flex: 1; border: 0; background: transparent; color: var(--ink); font: inherit; font-size: var(--fs-sm); outline: none; min-width: 4rem; }
.mk-rep-search svg { width: 15px; height: 15px; color: var(--muted); flex: none; }
.mk-rep select { appearance: none; border: 1px solid var(--border-strong); border-radius: var(--r-full); background: var(--surface); color: var(--ink); font: inherit; font-size: var(--fs-sm); padding: 0.45rem 2.1rem 0.45rem 0.95rem; cursor: pointer;
  background-image: linear-gradient(45deg, transparent 50%, var(--muted) 50%), linear-gradient(135deg, var(--muted) 50%, transparent 50%); background-position: calc(100% - 15px) 52%, calc(100% - 10px) 52%; background-size: 5px 5px, 5px 5px; background-repeat: no-repeat; }
.mk-rep-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(15.5rem, 1fr)); gap: 0.75rem; }
.mk-rep-card { position: relative; display: flex; flex-direction: column; gap: 0.6rem; border: 1px solid var(--hairline); border-radius: var(--r-lg); background: var(--surface); padding: 0.9rem 1rem; text-decoration: none; color: inherit; overflow: hidden; transition: border-color 0.15s, transform 0.15s; }
.mk-rep-card:hover { border-color: var(--accent); transform: translateY(-1px); }
.mk-rep-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 3px; background: var(--b, var(--accent)); }
.mk-rep-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.6rem; }
.mk-rep-name { font-size: var(--fs-md); font-weight: 600; color: var(--heading); letter-spacing: -0.01em; word-break: break-word; line-height: 1.25; }
.mk-rep-owner { color: var(--muted); font-weight: 400; }
.mk-rep-score { flex: none; display: inline-flex; align-items: baseline; gap: 1px; font-family: var(--font-mono); font-size: var(--fs-lg); font-weight: 700; color: var(--b, var(--accent-ink)); font-variant-numeric: tabular-nums; }
.mk-rep-score small { font-size: 9px; font-weight: 500; color: var(--muted); }
.mk-rep-chips { display: flex; flex-wrap: wrap; gap: 0.35rem; }
.mk-rep-chip { font-family: var(--font-mono); font-size: 9.5px; font-weight: 600; letter-spacing: 0.04em; color: var(--muted); border: 1px solid var(--border); border-radius: var(--r-sm); padding: 2px 6px; white-space: nowrap; }
.mk-rep-chip.lens { color: var(--accent-ink); background: var(--accent-wash); border-color: var(--border-strong); }
.mk-rep-read { margin-top: auto; font-size: var(--fs-xs); font-weight: 600; color: var(--accent-ink); }
.mk-rep-foot { color: var(--muted); font-size: var(--fs-xs); line-height: 1.55; margin: 1.2rem 0 0; text-align: center; }
.mk-rep-empty { color: var(--muted); font-size: var(--fs-sm); text-align: center; padding: 2rem 1rem; border: 1px dashed var(--border-strong); border-radius: var(--r-lg); }
`,E={totals:{repositories:3200,publishedSurveys:3800},facets:{languages:[{language:"C#",count:640},{language:"TypeScript",count:410},{language:"Go",count:300},{language:"Rust",count:240},{language:"Python",count:380},{language:"Java",count:260}],lenses:[{key:"domainModelling",label:"Domain modelling"},{key:"eventSourcing",label:"Event sourcing"},{key:"eventDriven",label:"Event-driven"}]},cap:24,matched:6,capped:!1,curated:!0,reports:[{display:"ardalis/CleanArchitecture",owner:"ardalis",name:"CleanArchitecture",score:64,bandHex:"#b0872f",primaryLanguage:"C#",secondaryLanguages:[],lenses:["domainModelling"],reportPath:"",sourceUrl:"https://github.com/ardalis/CleanArchitecture"},{display:"gin-gonic/gin",owner:"gin-gonic",name:"gin",score:72,bandHex:"#1f7a5a",primaryLanguage:"Go",secondaryLanguages:[],lenses:[],reportPath:"",sourceUrl:"https://github.com/gin-gonic/gin"},{display:"tokio-rs/axum",owner:"tokio-rs",name:"axum",score:76,bandHex:"#1f7a5a",primaryLanguage:"Rust",secondaryLanguages:[],lenses:[],reportPath:"",sourceUrl:"https://github.com/tokio-rs/axum"},{display:"oskardudycz/EventSourcing.NetCore",owner:"oskardudycz",name:"EventSourcing.NetCore",score:68,bandHex:"#b0872f",primaryLanguage:"C#",secondaryLanguages:[],lenses:["eventSourcing","domainModelling"],reportPath:"",sourceUrl:"https://github.com/oskardudycz/EventSourcing.NetCore"},{display:"tiangolo/fastapi",owner:"tiangolo",name:"fastapi",score:74,bandHex:"#1f7a5a",primaryLanguage:"Python",secondaryLanguages:[],lenses:[],reportPath:"",sourceUrl:"https://github.com/tiangolo/fastapi"},{display:"spring-projects/spring-petclinic",owner:"spring-projects",name:"spring-petclinic",score:66,bandHex:"#b0872f",primaryLanguage:"Java",secondaryLanguages:["TypeScript"],lenses:["domainModelling"],reportPath:"",sourceUrl:"https://github.com/spring-projects/spring-petclinic"}]},g=e=>typeof e=="number"?e.toLocaleString("en-US"):e;function w(e,t){let r=t?t.replace(/\/$/,"")+e.reportPath:e.sourceUrl||"#",a=[e.primaryLanguage,...e.secondaryLanguages||[]].filter(Boolean).slice(0,3),n=(e.lenses||[]).filter(i=>k[i]).map(i=>`<span class="mk-rep-chip lens">${o(k[i])}</span>`),s=a.map(i=>`<span class="mk-rep-chip">${o(A(i))}</span>`),[c,l]=e.display.includes("/")?e.display.split("/"):[e.owner,e.name];return`<a class="mk-rep-card" style="--b:${o(e.bandHex||"")}" href="${o(r)}" target="_blank" rel="noopener">
      <div class="mk-rep-top">
        <span class="mk-rep-name"><span class="mk-rep-owner">${o(c)}/</span>${o(l)}</span>
        <span class="mk-rep-score">${o(String(e.score))}<small>/100</small></span>
      </div>
      <div class="mk-rep-chips">${s.join("")}${n.join("")}</div>
      <span class="mk-rep-read">Read the survey \u2192</span>
    </a>`}function S(e){return e.curated?`A curated window on the corpus \u2014 filter or search to reach the rest of the <strong>${g(e.totals.repositories)}+</strong> surveyed.`:e.capped?`Showing the top ${e.reports.length} of <strong>${g(e.matched)}</strong> matches \u2014 refine to narrow it.`:`${g(e.matched)} match${e.matched===1?"":"es"}.`}customElements.define("cai-public-reports",class extends u{#e=null;_state={lang:"",lens:"",q:""};async liveLoad(){let e=this.apiBase();if(!e)return;let t=await m(e,this._state);!t||!Array.isArray(t.reports)||(this.#e=t,this._live=!0,this.render(this.shadowRoot))}render(e){let t=this.apiBase(),a=(this._live&&this.#e?this.#e:null)||this.#r(),n=(a.facets?.languages||[]).map(i=>`<option value="${o(i.language)}">${o(A(i.language))} (${i.count})</option>`).join(""),s=(a.facets?.lenses||[]).map(i=>`<option value="${o(i.key)}">${o(i.label)}</option>`).join(""),c=`<style>${H}</style>`;c+=b(this),c+=`<div class="mk-rep">
        <div class="mk-rep-scale">
          <span class="mk-rep-big">${g(a.totals.repositories)}+</span>
          <span class="mk-rep-scale-cap"><strong>repositories surveyed</strong> across ${(a.facets?.languages||[]).length} languages \u2014 a public, signed record you can open and check.</span>
        </div>
        <div class="mk-rep-controls">
          <label class="mk-rep-search"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>
            <input type="search" data-f="q" placeholder="Search the corpus\u2026" value="${o(this._state.q)}" ${t?"":"disabled"} /></label>
          <select data-f="lang" ${t?"":"disabled"}><option value="">All languages</option>${n}</select>
          <select data-f="lens" ${t?"":"disabled"}><option value="">All lenses</option>${s}</select>
        </div>
        <div class="mk-rep-grid" data-grid>${a.reports.map(i=>w(i,t)).join("")}</div>
        <p class="mk-rep-foot" data-foot>${S(a)}</p>
      </div>`;let l=this.getAttribute("footnote");l&&(c+=`<p class="mk-grid-foot">${p(l)}</p>`),e.innerHTML=c,this.#t(e,t)}#t(e,t){if(!t)return;let r=e.querySelector("[data-grid]"),a=e.querySelector("[data-foot]"),n=e.querySelector('[data-f="q"]'),s=e.querySelector('[data-f="lang"]'),c=e.querySelector('[data-f="lens"]'),l=async()=>{this._state={lang:s.value,lens:c.value,q:n.value.trim()};let d=await m(t,this._state);this.#e=d,r.innerHTML=d.reports.length?d.reports.map(L=>w(L,t)).join(""):"",d.reports.length||(r.innerHTML='<div class="mk-rep-empty">No surveys match \u2014 clear the filters to browse the corpus.</div>'),a.innerHTML=S(d)};s.addEventListener("change",l),c.addEventListener("change",l);let i;n.addEventListener("input",()=>{clearTimeout(i),i=setTimeout(l,250)})}#r(){return E}});
