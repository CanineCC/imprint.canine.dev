// <cai-dimensions api-base="https://codeassuranceindex.info" kicker="…" heading="…" lede="…">
//
// The rubric catalogue, read LIVE from the archive, with a version picker.
//
// WHY THIS EXISTS. The catalogue used to be copy-pasted into CMS rich text. It froze there: the
// page went on saying "rubric-2026.08.15" and "a finding carries a code in D1-D39" for a month
// while the archive moved to rubric-2026.08.19 with 127 entries and the full D1-D42 family. A
// reader who saw D40 on a report and came here to look it up found nothing, and anyone checking
// our dimension count was told a number four versions old.
//
// The catalogue is versioned data that changes when a rubric is PUBLISHED, which is not when the
// site is deployed. Anything that snapshots it is stale by the next publication, so this widget
// does not snapshot it: it reads the archive at view time and renders whatever is actually there.
//
//   /api/rubrics                        → { latest, versions[], catalogs[{version, contentHash}] }
//   /api/rubrics/{version}/catalog      → { rubricVersion, lenses[], dimensions[] }
//
// The picker is the point as much as the freshness: a rubric is immutable and a score always names
// the one it was computed under, so a reader holding an older survey needs to read THAT catalogue,
// not today's. Choosing a version re-reads the archive; it never re-renders a cached copy.
//
// EVERY ENTRY, NOT JUST THE D-FAMILY. This widget used to render only entries whose family was
// "dimension" — the D-codes — and print "…· 85 meta-dimensions" in the header bar beside them. So it
// announced that 85 definitions existed and then declined to show any of them, which is the same
// hole the copy-pasted table had, one family over: a reader who saw ED4 or DM2 or AX9 on a report
// came here to look it up and found nothing. On a typical survey MOST findings carry one of those
// codes, so the missing 85 were not an appendix — they were the common case.
//
// A meta-dimension differs from a coded one in exactly one way that matters to a reader: it folds
// straight into its lens instead of through a category, and so carries no citable D-number. That is
// a column, not a reason to omit the row.
//
// With 127 rows the filter stops being a nicety: someone arrives holding one code and wants that
// one line, so the box is focused-first and matches code, name and description.
//
// Offline, or with no api-base, it renders the labelled sample below and says so. It never shows a
// sample dressed as live.

import {
  CaiIsland,
  TOKENS_CSS,
  BASE_CSS,
  SECTION_HEAD_CSS,
  sectionHeadHtml,
  escapeHtml as esc,
} from "./tokens.js";

const CSS = `
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
`;

// The labelled fallback. Deliberately TINY and obviously partial: it exists so the page renders
// without the archive, not so it can impersonate it. Whenever it is used the badge says SAMPLE.
const SAMPLE = {
  rubricVersion: "sample",
  lenses: [
    { key: "codeHealth", name: "Code health", alwaysOn: true },
    { key: "architecture", name: "Architecture", alwaysOn: true },
    { key: "maturity", name: "Maturity", alwaysOn: true },
    { key: "productionReadiness", name: "Readiness", alwaysOn: true },
    { key: "securityCompliance", name: "Security & Compliance", alwaysOn: true },
  ],
  dimensions: [
    { id: "D1", name: "Cyclomatic Complexity", lens: "codeHealth", evaluator: "tool", family: "dimension", whatItMeasures: "How tangled the control flow is." },
    { id: "D13", name: "Secret Scanning", lens: "productionReadiness", evaluator: "tool", family: "dimension", whatItMeasures: "Whether secrets have leaked into the code." },
    { id: "D28", name: "Secrets (history)", lens: "securityCompliance", evaluator: "tool", family: "dimension", whatItMeasures: "Secrets reachable in git history." },
  ],
};

const LENS_LABEL = {
  codeHealth: "Code health",
  architecture: "Architecture",
  maturity: "Maturity",
  productionReadiness: "Readiness",
  securityCompliance: "Security & Compliance",
  domainModelling: "Domain Modelling",
  eventDriven: "Event-Driven",
  eventSourcing: "Event Sourcing",
  accessibility: "Accessibility",
  performance: "Performance",
};

function lensLabel(key, lenses) {
  const hit = (lenses || []).find((l) => l.key === key || l.id === key);
  return hit?.name || LENS_LABEL[key] || key || "—";
}

async function getJson(base, path) {
  const root = (base || "").trim().replace(/\/$/, "");
  if (!root) return null;
  try {
    const r = await fetch(root + path);
    return r.ok ? await r.json() : null;
  } catch {
    return null;
  }
}

customElements.define(
  "cai-dimensions",
  class extends CaiIsland {
    #index = null; // { latest, versions[] }
    #catalog = null; // the catalog currently shown
    #chosen = null; // the version the reader picked, if any
    #tried = false; // has a live read been ATTEMPTED and finished?
    #filter = ""; // the reader's filter text, kept across re-renders

    async liveLoad() {
      const base = this.apiBase();
      if (!base) {
        this.#tried = true;
        return;
      }

      const index = await getJson(base, "/api/rubrics");
      if (!index || !Array.isArray(index.versions) || index.versions.length === 0) {
        this.#tried = true;
        this.render(this.shadowRoot);
        return;
      }

      const want = this.#chosen || index.latest || index.versions[0];
      const catalog = await getJson(base, `/api/rubrics/${encodeURIComponent(want)}/catalog`);
      if (!catalog || !Array.isArray(catalog.dimensions)) {
        this.#tried = true;
        this.render(this.shadowRoot);
        return;
      }

      this.#index = index;
      this.#catalog = catalog;
      this._live = true;
      this.render(this.shadowRoot);
    }

    #onPick(version) {
      this.#chosen = version;
      // Re-read the archive rather than re-rendering a cached copy: a picked version must be the
      // published document, not whatever this page happened to load first.
      this.liveLoad().catch(() => {});
    }

    render(root) {
      const live = this._live && this.#catalog;
      const cat = live ? this.#catalog : SAMPLE;
      const dims = cat.dimensions || [];
      const lenses = cat.lenses || [];
      const isCoded = (d) => (d.family || "dimension") === "dimension";
      const coded = dims.filter(isCoded);
      const meta = dims.length - coded.length;

      // Every entry is rendered. Coded ones first (they are what a D-number on a report resolves
      // to), then the meta families in code order — AC1…, AX1…, DM1… — so a reader scanning for the
      // code in front of them lands in a predictable place.
      const ordered = [...dims].sort((a, b) => {
        const ca = isCoded(a) ? 0 : 1;
        const cb = isCoded(b) ? 0 : 1;
        if (ca !== cb) return ca - cb;
        const pa = String(a.id).replace(/[0-9]/g, "");
        const pb = String(b.id).replace(/[0-9]/g, "");
        if (pa !== pb) return pa.localeCompare(pb);
        const na = parseInt(String(a.id).replace(/\D/g, ""), 10) || 0;
        const nb = parseInt(String(b.id).replace(/\D/g, ""), 10) || 0;
        return na - nb;
      });

      let h = `<style>${TOKENS_CSS}${BASE_CSS}${SECTION_HEAD_CSS}${CSS}</style>`;
      h += sectionHeadHtml(this);
      h += '<div class="dx">';

      // Bar: what you are looking at, and the choice of which version.
      h += '<div class="dx-bar">';
      if (live && this.#index) {
        const opts = this.#index.versions
          .map((v) => `<option value="${esc(v)}"${v === cat.rubricVersion ? " selected" : ""}>${esc(v)}</option>`)
          .join("");
        h += `<label class="dx-pick">Rubric version <select part="version">${opts}</select></label>`;
        h += `<span class="dx-badge">live from the archive</span>`;
      } else if (this.#tried) {
        h += `<span class="dx-badge sample">sample — the archive was not reachable</span>`;
      } else {
        h += `<span class="dx-badge sample">sample — the live catalogue loads from the archive</span>`;
      }
      h += `<span class="dx-count">${dims.length} dimensions · ${coded.length} with a citable code · ${meta} meta · ${lenses.length} lenses</span>`;
      h += `<input class="dx-find" type="search" part="filter" placeholder="Filter by code, name or description — e.g. ED4" aria-label="Filter dimensions">`;
      h += "</div>";

      // Lenses.
      if (lenses.length) {
        h += '<div class="dx-lenses">';
        for (const l of lenses) {
          const key = l.key || l.id;
          // Count everything that folds into this lens. Counting only the coded ones reported "0
          // coded dimensions" for Accessibility, Performance, Domain Modelling, Event-Driven and
          // Event Sourcing — five of the ten lenses looked empty when each has a full set of
          // meta-dimensions behind it.
          const all = dims.filter((d) => d.lens === key);
          const n = all.length;
          const nc = all.filter(isCoded).length;
          // Say "conditional" only when the catalogue actually says so. The published catalogue's
          // lenses carry {key, label} and nothing else, so defaulting the missing flag to false
          // labelled all ten conditional — including the five that are always on. A rendering
          // default is not a fact about the standard; when the archive is silent, so is this.
          const declared = l.alwaysOn === true || l.always === true || l.alwaysOn === false || l.always === false;
          const always = l.alwaysOn === true || l.always === true;
          h += '<div class="dx-lens">';
          h += `<div class="dx-lens-name">${esc(l.name || lensLabel(key, lenses))}</div>`;
          if (declared) {
            h += `<div class="dx-lens-meta">${always ? "always on" : "conditional"}</div>`;
          }
          h += `<div class="dx-lens-count">${n} dimension${n === 1 ? "" : "s"}${nc ? ` · ${nc} coded` : ""}</div>`;
          h += "</div>";
        }
        h += "</div>";
      }

      h += '<div class="dx-tablewrap"><table class="dx-t">';
      h += "<thead><tr><th>Code</th><th>Dimension</th><th>What it measures</th><th>Lens</th><th>Cited</th><th>Evaluator</th></tr></thead><tbody>";
      for (const d of ordered) {
        const ev = (d.evaluator || "").toLowerCase();
        const codedRow = isCoded(d);
        // data-find is what the filter matches on, lower-cased once here rather than per keystroke.
        const hay = `${d.id} ${d.name || ""} ${d.whatItMeasures || ""}`.toLowerCase();
        h += `<tr data-code="${esc(String(d.id).toLowerCase())}" data-find="${esc(hay)}">`;
        h += `<td class="dx-code">${esc(d.id)}</td>`;
        h += `<td class="dx-name">${esc(d.name || "")}</td>`;
        h += `<td class="dx-what">${esc(d.whatItMeasures || "")}</td>`;
        h += `<td class="dx-lensc">${esc(lensLabel(d.lens, lenses))}</td>`;
        h += `<td><span class="dx-fam${codedRow ? " coded" : ""}">${codedRow ? "on findings" : "lens only"}</span></td>`;
        h += `<td><span class="dx-ev ${ev === "tool" ? "tool" : ""}">${esc(ev || "—")}</span></td>`;
        h += "</tr>";
      }
      h += "</tbody></table></div>";
      h += '<p class="dx-none" hidden>No dimension matches that. Codes look like <code>D4</code>, <code>ED4</code> or <code>AX9</code>.</p>';

      if (live) {
        const v = esc(cat.rubricVersion);
        const base = esc(this.apiBase().replace(/\/$/, ""));
        h += `<p class="dx-foot">Read verbatim from the published catalogue <code>${v}</code>. Rubrics are immutable, so a score always names the one it was computed under — pick that version above to read the definitions it was scored against. This catalogue as JSON: <a href="${base}/api/rubrics/${v}/catalog">${v}/catalog</a>.</p>`;
      }

      h += "</div>";
      root.innerHTML = h;

      const sel = root.querySelector("select");
      if (sel) sel.addEventListener("change", (e) => this.#onPick(e.target.value));

      const find = root.querySelector(".dx-find");
      const none = root.querySelector(".dx-none");
      if (find) {
        const rows = [...root.querySelectorAll("tbody tr")];
        find.value = this.#filter || "";
        const apply = () => {
          const q = find.value.trim().toLowerCase();
          this.#filter = find.value; // survives a re-render when the version picker re-reads
          // A code-shaped query searches CODES, by prefix. The reader arriving with "ED4" or "AX"
          // in hand is looking up a code they saw on a report, and a plain substring search over
          // every description answers that with rows matching "tangl(ed)" and "shar(ed)".
          const asCode = /^[a-z]{1,3}\d*$/.test(q);
          let shown = 0;
          for (const tr of rows) {
            const hit = !q || (asCode ? tr.dataset.code.startsWith(q) : tr.dataset.find.includes(q));
            tr.hidden = !hit;
            if (hit) shown++;
          }
          if (none) none.hidden = shown > 0;
        };
        find.addEventListener("input", apply);
        if (find.value) apply();
      }
    }
  },
);
