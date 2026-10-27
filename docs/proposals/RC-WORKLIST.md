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
- [x] **P0. Gatekeeping for the LinkedIn feature**: blind BUILD/DEFER/CUT review of the brief's item 3 (the owner
      chose option 2 explicitly; the review sizes it, it does not re-open the choice).
- [x] **P1. Article kind** (A1): essay | guide | whitepaper | documentation, required at creation; API, MCP, editor.
- [x] **P2. Product tags** (A2 minimal): optional, several allowed (canine, assert, assure, assay, cca, cai); accent only.
- [x] **P3. "Article" labels + editor classification UI** (A5): UI labels only (events, routes and MCP tools keep `post`); the editor's kind select and product toggles on the new-post box and the post editor; a resource-center site refuses a post without a kind (with P9).
- [x] **P4. Version and sha on publish** (B1): version n, sha256 of the rendered text, date, signer on the publish event;
      shown as "version n · date". No public old versions.
- [x] **P5. Private control notes** (B3): never published; its hash goes into the signature.
- [x] **P6. Callouts** (C2): blockquote with optional NOTE / WARNING label; converter tests extended.
- [x] **P7. Scheduled publish for every publish** (brief 3): date + time picker on publish for posts AND pages; the
      scheduler publishes at that minute; visible, editable and cancellable while pending; respects the hold.
- [ ] **P8. LinkedIn post per scheduled publish** (brief 3): text (+ optional image = the article's social image)
      authored and approved with the article; sender = Canine company page or a product showcase page; posted by the
      scheduler right after the publish succeeds, with the article URL; outcome recorded (posted id / failure reason,
      retried, never double-posted); LinkedIn Posts API (`/rest/posts`, w_organization_social) behind an interface with
      a fake for tests; OAuth token + refresh stored as a secret, never in the event stream.
- [x] **P9. Resource center site kind** (D1/D2): ink and paper tokens, product accents; one article template whose header
      depends on kind (method card as table under the title), the grouped index with kind tabs and visible,
      unremembered filters, as in the mock v2.
- [x] **P10. Redirects table per site** (D4): per-URL 301s, prepared for the old guide/blog URLs; NOT activated until
      the owner says the resource center takes over.
- [x] **P11. Merge to imprint main** with every test project green + E2E + screenshots looked at; deploy watched.

### Site and edge
- [x] **S1. Create the site "Resource Center"** in imprint production; Production environment →
      `/home/jimmy/imprint-data/publish-sites/resourcecenter`, BaseUrl `https://resourcecenter.canine.dev`, a free
      wrx1 port in the host nginx (next after 8155, checked free).
- [x] **S2. dgx1 vhost** `resourcecenter.canine.dev` → wrx1 port; its own certificate via certbot (webroot), renewal
      hook already reloads nginx; `nginx -t` first.
- [!] **S3. The index answers** at https://resourcecenter.canine.dev with a valid certificate (empty grouped index is
      fine until articles are signed); checked in a browser, screenshot looked at.

### Content (copied, rewritten for the resource center; drafts in review)
- [x] **C0. Inventory**: every system documentation page, guide, blog post and whitepaper, with its source, kind,
      product and target slug, in `RC-CONTENT.md` beside this file.
- [x] **C1. Documentation** rewritten as `documentation` articles.
- [x] **C2. Guides** rewritten as `guide` articles.
- [x] **C3. Whitepapers** rewritten as `whitepaper` articles.
- [x] **C4. Blog posts** copied as `essay` articles (the lifted text; their review state is the owner's).
- [!] **C5. LinkedIn text** drafted for each article (sender chosen by product), stored with the article, unapproved.
- [x] **C6. A proposed schedule**: date + time per article, in `RC-CONTENT.md`, nothing scheduled until signed.

### Last
- [ ] **Q. Questions for the owner**, collected here as they come up, asked once at the end.

## Questions for the owner (collected)

6. LinkedIn sender: your reach is on your personal profile (the GTM plan says founder-led posting beats the company page), and option 2 as built posts as the Canine page or a product page. Posting as you needs a different permission (w_member_social) and is a later addition, not a rebuild. Which should the first posts come from?
7. Scheduled PAGE publishes do not get a LinkedIn post (the review cut it: no page in the plan is announced). You wrote "any publish"; say so if pages should post too, and it is a small addition.

5. LinkedIn (needed before anything can post, whatever P0 decides about the build): a LinkedIn developer app tied to
   the Canine company page with the Community Management API product approved (LinkedIn reviews it; the company must be
   verified), and a person with an admin or content-admin role on the Canine page and on each product page to authorize
   it once. Then the organization ids of the Canine page and the product pages. Details in RC-LINKEDIN-NOTES.md.

3. wrx1 needs the resource center's port: a host nginx server block listening on 8156 with root /home/jimmy/imprint-data/publish-sites/resourcecenter, the same shape as the blog's 8155 block (copy it, change the port and the folder), then `sudo nginx -t && sudo systemctl reload nginx`, and the firewall must let dgx1 (192.168.1.159) reach 8156 as it reaches 8155. Either do it, or allow `ssh canine-wrx1` for this session (a permission rule) and I will.
4. The resource center's reviewer is set to Jimmy Borch with the account's email. If your imprint login is a different address, the record will say 'a named person' instead of your name; tell me the login and I will change it.

1. The design has a search palette (⌘K), a 'Go deeper' related panel, a bento of featured cards, guide step ticks, view-transition animation and a version-history list. The gatekept plan you approved on 2026-10-25 deferred or cut these (public old versions are against ARTICLE-BAR §1). Build any of them now?
2. The reading serif in the design is Source Serif 4, which imprint does not ship; a Georgia-like fallback is used. Add the font (OFL, about 60 KB woff2)?

## Log
- 2026-10-27 P0 waits: the box is busy (9 heavy runs), and a blind review needs a fresh agent; it gates only P8.
- 2026-10-27 P1+P2 done (domain, slice, list, API, MCP): `post.classified(Kind, Products)`; `ArticleClassification` holds the closed lists (essay, guide, whitepaper, documentation; canine, assert, assure, assay, cca, cai), normalizes case, de-duplicates and orders products canonically; reclassifying an approved post lapses the approval; CreatePost takes an optional kind (refused kind leaves no post); `PUT /posts/{id}/classification`, `kind`/`products` on the post view and history; MCP `create_post` kind/products + `classify_post`. Tests red first; Authoring 989/989, Editor 199/199. Editor UI moved into P3.
- 2026-10-27 P4 done: `post.published` carries Version (1 for the first text, +1 only when the words change; kept across unpublish; a pre-version publish counts as 1) and Sha256 = Post.BodySha256, the hash a sign-off records; the event actor is the publisher. PublishedPost snapshots Version, Sha256, Kind and Products as published; history shows version and sha. Four publish expectations updated on purpose. Authoring 995, Publishing 539, Editor 199 green. Rendering 'version n · date' comes with the P9 template.
- 2026-10-27 P5 done: `post.control-notes-changed(Notes?)`, trimmed, max 200,000 chars; the approval event carries ControlNotesSha256 (sha256 of the UTF-8 notes, null when none) and the post-list signature shows it; changing notes on an approved post lapses the approval. Never in PublishedPost. `PUT /posts/{id}/control-notes`, `controlNotes` on the post DETAIL only, history logs the length; MCP `set_post_control_notes`. Authoring 1001, Editor 200 green. Editor field comes with P3.
- 2026-10-27 P6 done: `CalloutNode` (Tone Plain/Note/Warning, container of prose; no section, layout container or callout inside). Markdown `> ...` is a callout, `> [!NOTE]` / `> [!WARNING]` labels it (case-insensitive; other labels refused naming NOTE or WARNING); the inside goes through the same block loop with its source line offset; a nested `>` and an empty callout are refused at their line; a `>` line now interrupts a paragraph (CommonMark). Rendered as `<aside class=ip-callout role=note>` with the label as real text; styles in imprint-marketing.css and editor.css. Four tests that used `>` as their refused example now use indented code. Boyscout: PublisherHostedServiceTests auto-sync read robots.txt after waiting only for index.html (the publisher writes pages first); now waits for both. Authoring 1010, Publishing 541, Editor 200 green.
- 2026-10-27 P7 done: posts already had date+time (`PublishAt`, datetime-local in the editorial zone, hold respected by DuePostPublisher). Pages now too: `PageSchedule` aggregate in its own stream `schedule-page-{id}` (so scheduling is not an edit and never sets 'Modified'), events set/cancelled/fulfilled/failed; `SchedulePagePublish(PageId, At?)` (null cancels), `CompletePageSchedule`; `ScheduledPages` projection; `DuePagePublisher` (30 s poll, publishes through PublishPage, records Published or Failed with the command's reason, never retries a refusal) registered in Program.cs. API `PUT/GET /pages/{id}/schedule`, MCP `schedule_page_publish`. Publish bar: Schedule… → datetime-local → Schedule; shows 'Scheduled <time>' + Cancel schedule; a failed run shows a badge with the reason. Tests: aggregate 10, slice 6 (fixed clock), API 1, E2E 1 (screenshot ~/Hentet/kennel/resourcecenter/shots/page-scheduled.png looked at). Authoring 1026, Editor 201 green.
- 2026-10-27 P9 done: `SiteKind.ResourceCenter` (post stream: articles at the root, index always rendered, refuses an article without a kind; `CreateBlog(Kind)`, MCP `create_blog`). The publisher renders the index as `rc-hub` (server-side prop template: hero, tabs + product chips, groups Thought leadership / Whitepapers / Guides / Documentation, cards per kind incl. the whitepaper sheet with sha256 and the documentation path) plus `rc-filter` (bundle-only island: tabs and product filter over the rendered cards, never remembered; a widget is template XOR bundle, so the filter is its own element). An article is one `Article` section: `rc-toc` (h2 contents with HeadingView's exact ids) | `rc-article-head` (kind, products, status, title, dek, version · date, Signed by NAME) + body (leading # title dropped) | `rc-record` (version, published, signed by, signed with a login · date, sha256). The signer is the site reviewer's NAME when the approving login is the reviewer's email, else 'A named person'; a login never reaches the HTML (tested). PublishedPost carries SignedBy/SignedAt from the approval's metadata. Styles in their own sheet imprint-resource-center.css, appended only on resource-center sites (the shared sheet stays under its 12 KB brotli budget). Verified: 4 publishing tests (incl. a 6-article center), screenshots light/dark/390 px looked at (~/Hentet/kennel/resourcecenter/shots/rc-*.png), axe wcag2a/aa/21aa clean on index + 3 kinds in both themes, no horizontal scroll, filter 6 -> 1 on Assert. Not built (gatekept/deferred, see Questions): search palette, Go-deeper panel, bento, step ticks, view transitions, version history list, Source Serif 4 font (serif fallback used).
- 2026-10-27 P3 done: the posts page on a resource center reads Articles / New article and its create needs a kind (select), the list shows each article's kind; the post editor's right pane gets a Details view (kind select, product checkboxes, Save kind and products; private control notes textarea, Save notes). E2E Kind_products_and_control_notes... green, survives a reload; screenshot post-details.png looked at. Boyscout: the markdown placeholder rendered '&#10;' literally (Blazor writes attribute text as-is); now a C# string.
- 2026-10-27 P10 done: `site.redirects-changed` (SiteRedirect From/To); `Site.SetRedirects` normalizes the source to /a/b/ and refuses the root, non-plain paths, non-https/non-site-path targets (script sink), duplicates and self-redirects (max 500). The publisher writes a redirect page (meta refresh 0, canonical, noindex, a plain link) at each old address where NO published page lives, keeps it in the sweep, and removes it when the entry goes; not in sitemap or llms. MCP `set_site_redirects`. Nothing is activated: the table is empty until the owner says the resource center takes over. Authoring 1044, Publishing 549, Editor green.
- 2026-10-27 P11 done (first landing, without P8): all seven test projects green (Authoring 1044, Publishing 549, Editor 201, Media 98+3 skipped, EventSourcing 7, E2E 46 after the refused-body example moved to indented code); fast-forwarded imprint main to 3f4828c; deploy run 40571262428 green incl. blue-green cutover. P8 will land separately.
- 2026-10-27 S1 done: site 'Resource Center' 707963ae892b40f99c5bbb3bbee9dcda (kind ResourceCenter) created on production imprint via MCP create_blog, Production environment /home/jimmy/imprint-data/publish-sites/resourcecenter, BaseUrl https://resourcecenter.canine.dev; reviewer Jimmy Borch <owner's account email>; theme tokens set to the design's ink and paper (background, surface, surface-alt, text, text-muted, primary ramp, border, on-primary).
- 2026-10-27 S2 done: dgx1 /etc/nginx/sites-available/resourcecenter.canine.dev (80: acme + 301; 443: proxy to 192.168.1.10:8156, the blog vhost's headers and snippets), its own Let's Encrypt certificate (webroot; renewal reloads nginx via the deploy hook), nginx -t before each reload. TLS verifies; http 301s to https.
- 2026-10-27 S3 BLOCKED on wrx1: https://resourcecenter.canine.dev/ answers 504 because nothing serves port 8156 on wrx1 yet. ssh canine-wrx1 is refused to this session by the permission layer (production). See Questions.
- 2026-10-27 C0 done: RC-CONTENT.md lists 16 documentation pages (Watchdog 9, Assay 7), 10 guides, 10 whitepapers (repo manuscripts W1 to W10; the CAI site's own papers stay there, CAI decision 0002) and 20 essays (merged-away posts left out), each with kind, product and slug; no slug collides.
- 2026-10-27 Inline code (found by the new MarkdownFolderLintTests over the 20 essay copies): imprint refused `code`, so B5, B6 and B15, IN REVIEW on the blog, and B22, B23, B28 could not have been published as written. Built per the proposal's order (C1/C2/inline code): CommonMark backtick spans to <code> (text only, escaped; an unmatched backtick is literal), the canonical grammar admits <code> with text and entities only, the editor's rich-text normalizer keeps code as text, a mono chip style. Four tests that used code as their refused example moved to raw HTML / emphasis. Authoring 1053, Publishing 549, Editor 201, E2E 46 (SiteChromeSettingsTests.Navigation_entries_round_trip failed once in the full run, passes 2/2 alone: a load flake, detail not captured; watch it at the next full run).
- 2026-10-27 C1-C4 done: 56 drafts on the resource center (16 documentation, 10 guides, 10 whitepapers, 20 essays), all Draft, none submitted; rewrites and verification in RC-CONTENT.md 'Copy log'. Real-content render checked locally (The_real_articles_publish_and_can_be_previewed with RC_CONTENT_JSON): 56 cards, one h1, contents lists populated, no console errors, no horizontal scroll; screenshots real-*.png looked at.
- 2026-10-27 C5 BLOCKED on P8 (the LinkedIn field does not exist yet; P8 waits on P0's blind review, which needs a fresh agent and the box has been too busy all day).
- 2026-10-27 C6 done: a proposed schedule in RC-CONTENT.md (launch 3 Nov 09:00 docs + guides; essays weekly Tuesdays from 10 Nov; whitepapers fortnightly Thursdays from 12 Nov once each control closes). Nothing scheduled.
- 2026-10-27 P0 done (blind review, fresh agent): BUILD a (text signed with the article), b (sender as one setting: a per-site list of senders, default Canine), c (auto-post after the publish, ONLY when an approved text exists, so the 3 Nov batch cannot flood the page), e minimal (record Attempting before the call; Posted / Failed / Unknown; never retry from Unknown, retry only 429/5xx that provably did not land), f minimal (owner authorize + callback, tokens in a secret file, refresh on 401, expiry shown), h (Details field, API, MCP); a per-site rate cap. DEFER d (thumbnail upload: title and description first). CUT g (page publishes): no page in the content plan is announced. The review also found the owner's LinkedIn reach is on his personal account (GTM: 'founder-led posting > company page'), so the sender stays a URN and a personal sender can be added later (w_member_social). P8 is built to this scope.
