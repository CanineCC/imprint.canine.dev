# Resource center: worklist (owner GO 2026-10-27)

Owner's brief, 2026-10-27 (translated from Danish):
1. Implement resourcecenter.canine.dev **as designed** (`~/Hentet/Canine Resource Center.html`, v2), created in imprint,
   with the reverse proxy on canine-dgx1, so the site is actually ready.
2. All existing system documentation, guides, blogs and whitepapers are **rewritten to use the resource center as well
   as possible, and COPIED there, not moved**, so it is ready to take over our documentation.
3. Any publish in imprint can become a **scheduled publish (date + time)** with an attached text for an **automatic
   LinkedIn post** (from Canine as the company, or from the product it is about), via LinkedIn's API (option 2).
4. Work in a loop, defer questions to the owner to the end, finish as much as possible before stopping.

Design and plan sources: `docs/proposals/resource-center.md` (branch `proposal/resource-center`, gatekept 2026-10-25)
and the mock above. Built already (imprint main): signed approval by a person, publish hold, markdown tables,
send_post_back, per-post `PublishAt` scheduling.

## Rules for every item
- imprint `main` deploys production (deploy.yml on push). Work on `feat/resource-center` (worktree
  `~/RiderProjects/imprint-rc`); merge to main only with ALL test projects green, the E2E suite run, and the UI looked
  at (Playwright screenshots in `~/Hentet/kennel/resourcecenter/shots/`, axe in both themes).
- Red-green: a failing test first for every behaviour.
- Content is created as **drafts / in review only**. Never approve, sign, publish or schedule an article without the
  owner (approval is a signed act by a person; the API cannot approve). Never `publish_site` on a site holding
  someone else's draft.
- The LinkedIn integration is built and tested against a fake; real posting needs the owner's LinkedIn app
  (question deferred to the end). No post ever reaches LinkedIn from a test or a dry run.
- Owner copy: no em-dashes; byline Jimmy Borch; claims sourced (docs/marketing/ARTICLE-BAR.md in kennel).
- dgx1 edge: `ssh canine@canine-dgx1`; `nginx -t` before every reload; additive vhosts only.
- When the box is busy (`tools/box-load.sh --check` in kennel refuses), work inline.

## Items
Status: [ ] open, [~] in progress, [x] done, [!] blocked (reason in the log).

### Platform (imprint, branch feat/resource-center)
- [ ] **P0. Gatekeeping for the LinkedIn feature**: blind BUILD/DEFER/CUT review of the brief's item 3 (the owner
      chose option 2 explicitly; the review sizes it, it does not re-open the choice).
- [x] **P1. Article kind** (A1): essay | guide | whitepaper | documentation, required at creation; API, MCP, editor.
- [x] **P2. Product tags** (A2 minimal): optional, several allowed (canine, assert, assure, assay, cca, cai); accent only.
- [ ] **P3. "Article" labels + editor classification UI** (A5): UI labels only (events, routes and MCP tools keep `post`); the editor's kind select and product toggles on the new-post box and the post editor; a resource-center site refuses a post without a kind (with P9).
- [x] **P4. Version and sha on publish** (B1): version n, sha256 of the rendered text, date, signer on the publish event;
      shown as "version n · date". No public old versions.
- [x] **P5. Private control notes** (B3): never published; its hash goes into the signature.
- [x] **P6. Callouts** (C2): blockquote with optional NOTE / WARNING label; converter tests extended.
- [ ] **P7. Scheduled publish for every publish** (brief 3): date + time picker on publish for posts AND pages; the
      scheduler publishes at that minute; visible, editable and cancellable while pending; respects the hold.
- [ ] **P8. LinkedIn post per scheduled publish** (brief 3): text (+ optional image = the article's social image)
      authored and approved with the article; sender = Canine company page or a product showcase page; posted by the
      scheduler right after the publish succeeds, with the article URL; outcome recorded (posted id / failure reason,
      retried, never double-posted); LinkedIn Posts API (`/rest/posts`, w_organization_social) behind an interface with
      a fake for tests; OAuth token + refresh stored as a secret, never in the event stream.
- [ ] **P9. Resource center site kind** (D1/D2): ink and paper tokens, product accents; one article template whose header
      depends on kind (method card as table under the title), the grouped index with kind tabs and visible,
      unremembered filters, as in the mock v2.
- [ ] **P10. Redirects table per site** (D4): per-URL 301s, prepared for the old guide/blog URLs; NOT activated until
      the owner says the resource center takes over.
- [ ] **P11. Merge to imprint main** with every test project green + E2E + screenshots looked at; deploy watched.

### Site and edge
- [ ] **S1. Create the site "Resource Center"** in imprint production; Production environment →
      `/home/jimmy/imprint-data/publish-sites/resourcecenter`, BaseUrl `https://resourcecenter.canine.dev`, a free
      wrx1 port in the host nginx (next after 8155, checked free).
- [ ] **S2. dgx1 vhost** `resourcecenter.canine.dev` → wrx1 port; its own certificate via certbot (webroot), renewal
      hook already reloads nginx; `nginx -t` first.
- [ ] **S3. The index answers** at https://resourcecenter.canine.dev with a valid certificate (empty grouped index is
      fine until articles are signed); checked in a browser, screenshot looked at.

### Content (copied, rewritten for the resource center; drafts in review)
- [ ] **C0. Inventory**: every system documentation page, guide, blog post and whitepaper, with its source, kind,
      product and target slug, in `RC-CONTENT.md` beside this file.
- [ ] **C1. Documentation** rewritten as `documentation` articles.
- [ ] **C2. Guides** rewritten as `guide` articles.
- [ ] **C3. Whitepapers** rewritten as `whitepaper` articles.
- [ ] **C4. Blog posts** copied as `essay` articles (the lifted text; their review state is the owner's).
- [ ] **C5. LinkedIn text** drafted for each article (sender chosen by product), stored with the article, unapproved.
- [ ] **C6. A proposed schedule**: date + time per article, in `RC-CONTENT.md`, nothing scheduled until signed.

### Last
- [ ] **Q. Questions for the owner**, collected here as they come up, asked once at the end.

## Questions for the owner (collected)

## Log
- 2026-10-27 P0 waits: the box is busy (9 heavy runs), and a blind review needs a fresh agent; it gates only P8.
- 2026-10-27 P1+P2 done (domain, slice, list, API, MCP): `post.classified(Kind, Products)`; `ArticleClassification` holds the closed lists (essay, guide, whitepaper, documentation; canine, assert, assure, assay, cca, cai), normalizes case, de-duplicates and orders products canonically; reclassifying an approved post lapses the approval; CreatePost takes an optional kind (refused kind leaves no post); `PUT /posts/{id}/classification`, `kind`/`products` on the post view and history; MCP `create_post` kind/products + `classify_post`. Tests red first; Authoring 989/989, Editor 199/199. Editor UI moved into P3.
- 2026-10-27 P4 done: `post.published` carries Version (1 for the first text, +1 only when the words change; kept across unpublish; a pre-version publish counts as 1) and Sha256 = Post.BodySha256, the hash a sign-off records; the event actor is the publisher. PublishedPost snapshots Version, Sha256, Kind and Products as published; history shows version and sha. Four publish expectations updated on purpose. Authoring 995, Publishing 539, Editor 199 green. Rendering 'version n · date' comes with the P9 template.
- 2026-10-27 P5 done: `post.control-notes-changed(Notes?)`, trimmed, max 200,000 chars; the approval event carries ControlNotesSha256 (sha256 of the UTF-8 notes, null when none) and the post-list signature shows it; changing notes on an approved post lapses the approval. Never in PublishedPost. `PUT /posts/{id}/control-notes`, `controlNotes` on the post DETAIL only, history logs the length; MCP `set_post_control_notes`. Authoring 1001, Editor 200 green. Editor field comes with P3.
- 2026-10-27 P6 done: `CalloutNode` (Tone Plain/Note/Warning, container of prose; no section, layout container or callout inside). Markdown `> ...` is a callout, `> [!NOTE]` / `> [!WARNING]` labels it (case-insensitive; other labels refused naming NOTE or WARNING); the inside goes through the same block loop with its source line offset; a nested `>` and an empty callout are refused at their line; a `>` line now interrupts a paragraph (CommonMark). Rendered as `<aside class=ip-callout role=note>` with the label as real text; styles in imprint-marketing.css and editor.css. Four tests that used `>` as their refused example now use indented code. Boyscout: PublisherHostedServiceTests auto-sync read robots.txt after waiting only for index.html (the publisher writes pages first); now waits for both. Authoring 1010, Publishing 541, Editor 200 green.
