# Proposal: articles, and resourcecenter.canine.dev

**Status:** draft for the owner, 2026-10-25. Not built. Gatekeeping review pending (BUILD / DEFER / CUT per item).
**Design:** `~/Hentet/Canine Resource Center.html` (artifact https://claude.ai/artifact/8i4hmdnCyVBrT5wtSUSAV4).

## The owner's decisions this rests on (2026-10-25)

1. Every article is **authored and published in imprint**: essays, whitepapers, guides and documentation alike. None
   lives in a repository. Whitepapers are not special; every kind meets the same bar.
2. Imprint's **post becomes an article**, and every article carries **publication control**, a **signature stamped by
   the signer's login**, and **versioning**.
3. **resourcecenter.canine.dev** is one new site. Everything moves in, with **redirects** from the old pages once ready.
4. **Markdown gains tables and callouts.**

## What imprint already has (origin/main 82abb1c)

| Need | Already there |
|---|---|
| Markdown authoring | `Post` stores markdown (`PostBodyChanged(Locale, Markdown)`); `MarkdownToNodes` converts on save and reports problems by line; the editor has a split markdown/preview pane (`PostEditor.razor`, `post-split.js`) and a media shelf |
| Review before publish | `PostReview`, a per-site reviewer, `ReviewNote`, `ReviewMailer`; states Draft / InReview / ChangesRequested / Approved / Scheduled / Published / Modified |
| Who did it | Keycloak/OIDC login; `EditorActor` resolves the e-mail; every command is an event with its actor |
| History | Event-sourced aggregates: every change is already recorded |
| Embeds | `::: widget <tag> key=value :::` directives in markdown; widget islands with manifest-allowlisted props |
| Publishing | Static site per site, blue/green deploy, `llms.txt`, theme tokens, print stylesheet, authoring API and MCP |

## What is missing, item by item

Each item names the need it answers. "Owner decision" items are decided; the review may still cut or defer parts.

### A. Article, not post (owner decision 1–2)

- **A1. Kind.** `essay | whitepaper | guide | documentation` on the article. Drives the template (B) and the hub's
  groups and tabs. *Need: the four kinds read differently (design v2).*
- **A2. Product.** `assert | assure | assay | cca | company`. Drives the scoped accent colour and the product filter.
  *Need: approved product family brand (2026-10-24); the owner asked for product identity inside one site identity.*
- **A3. Related articles.** An ordered list of article ids with a one-line reason each, authored, never inferred.
  *Need: the reader's "Go deeper" panel; today every cross-link is hand-written prose.*
- **A4. Kind-specific fields.** Whitepaper: paper code (W7), abstract, subtitle. Guide: prerequisites and steps (each
  step: what to do, what you will see, how to check). Documentation: group and order in the documentation tree, and
  the engine/rubric version it applies to.
- **A5. Rename in the editor and the MCP** (post → article), keeping the existing post ids so nothing re-mints a URL.

### B. Versioning, signature and publication control (owner decision 2)

- **B1. Named versions.** Publishing a signed article freezes an immutable **version** (v1, v2 …): the markdown, the
  rendered text's sha256, the date, and a one-line "what changed" the author writes. Older versions stay readable at
  a stable URL. *Need: "built in, and if not, it must come". Events give history; they do not give a citable version.*
- **B2. Signature by login.** A new `ArticleSigned(Version, Actor, At, Sha256)` event, issued only by a signed-in
  person who holds the signer role on that site, never by the authoring API or the MCP (an agent may draft and submit;
  it may never sign). Publishing requires a signature on the exact version. *Need: owner decision; today a post can
  be approved but the approval is not bound to a hash of what was approved.*
- **B3. Publication control.** A claim register per article: each claim (a sentence or a number) with its source, the
  evidence class (Measured / Modelled / Opinion) and a state. Publishing is refused while a claim has no source.
  *Need: every whitepaper (10 of 10) keeps a hand-written `PUBLICATION-CONTROL.md` in the kennel repo, as the guides did, which
  drifts from the published page (the reason guides moved to imprint, 2026-09-06).*
- **B4. Unpublish is a version, not a deletion.** A withdrawn article says so at its URL, with the reason and date.

### C. Markdown vocabulary (owner decision 4)

- **C1. Tables** (GitHub syntax), rendered as a real `<table>` in an overflow container. *Need: the system
  documentation lost every table when it was moved (converted to lists, 2026-08-09); W7 alone has ten.*
- **C2. Callouts**: `> [!NOTE]`, `> [!WARNING]`, `> [!CLAIM]` (GitHub-style admonitions), so no new syntax is invented.
- **C3. The method card** as a widget directive (`::: widget method-card :::` with eleven named fields), because it is
  a fixed structure, not prose. Refused at publish when a field is empty on an article whose claims include a
  Measured one.
- Still refused, deliberately: nested lists, raw HTML, footnote syntax (the `[n]` reference style stays plain text).

### D. The site (owner decision 3)

- **D1. A new site, resourcecenter.canine.dev**, with its own theme tokens: ink and paper, the family braces holding
  a pilcrow, and the four product accents as scoped tokens.
- **D2. Templates:** the hub (bento of featured articles, grouped index of everything, kind tabs, product chips, a
  visible filter state) and one article template with four layouts (essay, whitepaper, guide, documentation).
- **D3. Search.** The publisher writes a `search-index.json` (title, kind, product, dek, headings); a small island
  answers ⌘K over it in the browser. No server search.
- **D4. Redirects.** A redirect table on the old sites (watchdog, assay, blog) that the publisher emits as static
  redirect pages, and a sitemap. Switched on per page when its article is live.
- **D5. The hand-off animation** (card title becomes the article title) with the View Transitions API, as progressive
  enhancement.
- **D6. Guide steps ticked off in the reader's own browser** (localStorage).

### E. Moving the content in

| From | Pages | Becomes |
|---|---|---|
| watchdog.canine.dev documentation | 8 + index | documentation articles, product CCA or Assert |
| watchdog.canine.dev guides | 10 + index | guide articles, product Assert (and Assay for the CRA/EAA guides) |
| watchdog.canine.dev whitepapers | 1 published + index | whitepaper articles |
| assay.canine.dev documentation | 6 + index | documentation articles, product Assay |
| Canine Blog posts | 19 (all InReview) | essay articles |
| kennel repo whitepaper manuscripts W1–W10, WP-40 | 10 + 1 drafts | whitepaper articles, entered as drafts |
| kennel repo blog drafts B21–B27 | 7 drafts | essay articles |

Each move is: create the article from the page's text (markdown, now with tables), run publication control, sign,
publish, then switch the old URL to a redirect. Page by page, never a big switch. The kennel repo keeps only evidence
files that a published article links to.

## Order of work

1. **A + B on the existing blog site** (article model, versions, signature, publication control). Smallest useful
   step: the 19 stalled posts can be signed and published under the new rules.
2. **C** (tables, callouts, method card), with the converter's existing refusal tests extended.
3. **D1–D3** (site, templates, search) on preprod, measured: Playwright smoke, axe gate in both themes, a screenshot
   of every template looked at.
4. **E** in batches: documentation first (most cross-links), then guides, whitepapers, essays. **D4** per batch.
5. **D5, D6** last.

## Constraints to respect

- `imprint-base.css` has about 100 bytes left of its 8 KB budget: the resource center gets its own stylesheet.
- Templates are monolingual today; the resource center starts English-only.
- Site chrome has no draft state (a navigation or theme write is live in about two seconds).
- Deploy order: a new widget's manifest ships before any article uses it.

## Open questions for the owner

1. Who may sign? Proposal: a signer role per site, held by named people (owner, Lasse); the author may not sign their
   own article.
2. Does a correction to a signed article need a new signature (a new version), or may a typo fix ride on the old one?
   Proposal: every change is a new version; a version can be marked "editorial only".
3. Danish: in scope now, or after launch?

---

## Gatekeeping review, 2026-10-25, and what changes

A blind review (no conversation context) gave BUILD / DEFER / CUT per item, from the repos. Accepted unless noted.

| Item | Verdict | What changes |
|---|---|---|
| A1 Kind | BUILD | One required field, set at creation |
| A2 Product | DEFER → **BUILD minimal** *(disagree)* | The owner asked for product identity inside one site identity and approved the design with it (2026-10-25). Minimal: an OPTIONAL tag that allows several products (the charter: company material serves several at once); accent only, no product-only pages |
| A3 Related | DEFER | Prose links stay. Instead a publish check: a guide or whitepaper may not link to an essay (CHARTER §1) |
| A4 Kind fields | CUT, docs order DEFER | **No paper codes** (owner decision 2026-08-07: published papers carry no series numbers; the design's "W7" goes). Steps stay a numbered list |
| A5 Rename | BUILD, labels only | Event names, API routes and MCP tools keep `post`; add aliases |
| B1 Versions | BUILD, reduced | Version n, sha256 of the rendered text, date and signer on the publish event. Shown as "version n · date". **No public old versions or change lines** (PUBLICATION-BAR §1); essays keep appended, dated corrections (EDITORIAL-BAR §4) |
| B2 Signature | BUILD, much cheaper | **First fix a gap that exists today:** the authoring API approves as `service:authoring-api` and the approve handler checks no reviewer (`AuthoringApi.cs` ~l.1027, `ApprovePostReviewHandler.cs`; verified). So: refuse approval from the API; the handler checks the signed-in actor is the site reviewer; add sha256 to the existing approval event. Edit-cancels-approval already exists |
| B3 Publication control | BUILD, reduced | A private, never-published control-notes field per article (today's PUBLICATION-CONTROL.md / appendix bN.md pasted in); its hash goes into the signature. Structured register deferred until one whitepaper has been signed with notes alone |
| B4 Tombstone | DEFER | Plain unpublish |
| C1 Tables | BUILD | Header row + inline-only cells; 45 tables across 9 manuscripts |
| C2 Callouts | BUILD, reduced | Plain blockquote, optional NOTE / WARNING label; no `[!CLAIM]` |
| C3 Method card widget | CUT | It is a two-column table under the title (C1) |
| D1 Site | BUILD | Ink and paper tokens; product accents via A2 |
| D2 Templates | BUILD, reduced | One article template whose header depends on kind; one index grouped by kind, with kind tabs (the owner's brief asked for them; client-side, cheap). Bento: after the move |
| D3 Search | DEFER | Until the corpus outgrows the grouped index; `llms.txt` exists |
| D4 Redirects | BUILD | Per-site table, per URL; sitemaps already exist |
| D5 Animation, D6 Step ticks | CUT | |
| E Move | BUILD, corrected | Needs a **page-to-markdown converter** and a render-equivalence check. Pre-registrations (WP-40) stay in git as evidence; the paper itself becomes an article when signed. *(Review claimed B21–B27 are not drafts: wrong, it read a stale local checkout; five are on origin/main, B23/B25 to come.)* |

**Added by the review:** a **publish hold** (a blocker field that refuses publish while set: B4 in imprint is past its go-live and "held on figures"; B22/B23/B25 wait on WP-40); **inline code** in the node vocabulary (~274 spans in the manuscripts); a defined path for **SVG figures**; settle **which bar applies** (EDITORIAL-BAR is deliberately a different contract from PUBLICATION-BAR, against decision 1's "same bar"); update **CHARTER §6** ("guides stay on the product site").

**Revised order:** 0) the 19 InReview posts need the reviewer, not code (untouched since 2026-08-17, four past their date); 1) B2 gap fix + publish hold; 2) A1, A5, B1, B3 notes; 3) C1, C2, inline code, SVG path; 4) D1, D2, D4 on preprod; 5) E in batches with the converter.
