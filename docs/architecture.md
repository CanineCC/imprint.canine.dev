# Imprint — Architecture

Imprint is an open-source (Apache-2.0) content management system built as a **reference
implementation** of three architectural styles working together:

- **Domain-Driven Design** — the content model is a rich domain, not rows in a table.
- **Event Sourcing** — every change is an immutable domain event; all state is derived.
- **Vertical Slice Architecture** — features are organized by use case, not by layer.

It is deliberately dependency-light. The delivered website is static HTML/CSS with zero
framework JavaScript (islands opt in per widget). The toolchain is pure .NET — no Node,
no bundler.

## The one-sentence architecture

> **The published website is a projection.** Editors issue commands; commands append
> events; projections derive state from events — and one of those projections writes
> HTML files to disk instead of objects to memory.

Everything else in this document is elaboration of that sentence.

## Two planes

```
┌────────────────────────────── EDITING PLANE ──────────────────────────────┐
│                                                                           │
│  Imprint.Editor (Blazor Server)                                           │
│      │  commands (CreatePage, MoveNode, EditText, PublishPage, …)         │
│      ▼                                                                    │
│  Imprint.Authoring ── vertical slices ──► Domain aggregates               │
│      │                                    (Page, Site, Asset, Block)      │
│      ▼                                                                    │
│  Imprint.EventSourcing ── SQLite event store (append-only, the truth)     │
│      │                                                                    │
│      ├──► in-memory read models (page tree, asset library, coverage, …)   │
│      │         ▲ consumed by the editor UI                                │
│      │                                                                    │
└──────┼────────────────────────────────────────────────────────────────────┘
       │  events (PagePublished, ThemeTokenChanged, ImageVariantsGenerated…)
┌──────▼─────────────────────── DELIVERY PLANE ─────────────────────────────┐
│                                                                           │
│  Imprint.Publishing ── the file-system projection                         │
│      renders published content with the same Razor components the         │
│      editor canvas uses (Imprint.Rendering), writes static HTML/CSS,      │
│      content-hashed assets, and a ~1 KB island loader to an output dir    │
│      ▼                                                                    │
│  Imprint.Site ── any static host (this one adds ETags, immutable          │
│      cache headers and precompression; a CDN or nginx works equally)      │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

The split is what makes "lightweight, cacheable, fast" a structural property instead of
an optimization effort. Visitors never touch the editor, the event store, or any
server-side rendering path. Editors get a fully interactive Blazor Server app with
direct, in-process access to the domain.

## Projects

| Project | Role |
|---|---|
| `Imprint.EventSourcing` | Generic machinery: event store (SQLite), aggregate base, command dispatch, projection engine. Could be extracted as a NuGet package; contains **no CMS concepts**. |
| `Imprint.Authoring` | The bounded context. `Domain/` (aggregates, events, value objects), `Features/` (one folder per use case), `Projections/` (read models). |
| `Imprint.Media` | Supporting infrastructure: WebP variant generation (SkiaSharp), SVG sanitization, WebM transcoding (external ffmpeg, optional), disk-backed media store. |
| `Imprint.Rendering` | Razor class library: the block components. Rendered by **both** the editor canvas and the static publisher — write components once. |
| `Imprint.Publishing` | The file-system projection: static site generation, theme CSS emission, island loader, sitemap, precompression, publish manifest. |
| `Imprint.Editor` | Blazor Server admin app. UI organized by the same feature folders as the slices it invokes. |
| `Imprint.Site` | Minimal production-grade static host for the published output. |
| `tests/*` | Mirror of the above; plus `Imprint.E2E` (Playwright) driving the real editor in a real browser. |
| `widgets/` | Example web-component widgets (vanilla ES modules) + `manifest.json`. |

## Why these three styles fit a CMS

**Event sourcing is unusually natural here.** Versioning, drafts, undo/redo, audit
history, translation review, "what did this page look like last Tuesday" — in a
state-stored CMS each of these is a feature you build; in an event-sourced CMS each is
a query over data you already have. Publishing illustrates it best: `PagePublished`
carries nothing but the stream version to publish. The published site is a replay of
the page's stream *up to that version* — drafts after it simply don't exist in the
delivery plane.

**DDD keeps the tree honest.** The editor never manipulates HTML. It manipulates a
domain tree — `Section → Stack/Columns/Grid → Heading/RichText/Image/…` — where every
node is a named concept with typed properties and every mutation is a domain behavior
with invariants (`sections live only at the root`, `a node cannot move into its own
descendant`, `RichText accepts only the canonical inline subset`). HTML is a *render*
of that tree, produced identically by the editor canvas and the static publisher.

**Vertical slices keep features findable.** Every use case —
`Features/Pages/MoveNode/`, `Features/Assets/UploadAsset/`,
`Features/Publishing/PublishPage/` — is one folder containing its command, validation,
and handler. Adding a feature touches one new folder plus (rarely) the domain. Nothing
registers itself in central files: handlers and projections are discovered by assembly
scanning.

### The tension between VSA and DDD — and how Imprint resolves it

Purist vertical slices own everything, including their data access. Purist DDD wants
one aggregate implementation with all its invariants in one place. These conflict: many
slices (`AddNode`, `MoveNode`, `EditText`, …) operate on the *same* Page aggregate.

Imprint resolves it the way most mature systems do:

- **Slices own the use case**: command shape, validation, orchestration, and the
  decision *which* domain behavior to invoke.
- **The domain owns the invariants**: aggregates in `Domain/` are shared by all slices
  of the bounded context. A slice never appends events directly; it calls an aggregate
  method that decides which events to raise.
- **Read models own the queries**: the editor UI reads projections, never aggregates.

This is a deliberate, documented trade-off, not an accident. The slice folder is the
unit of *change*; the aggregate is the unit of *consistency*.

## Consistency model

- **Within an aggregate**: strong. Appends use optimistic concurrency
  (`UNIQUE(stream_id, version)`); the dispatcher retries a conflicted command up to
  three times by reloading and re-deciding. Two editors moving nodes on the same page
  at the same time both succeed or the later one is re-decided against fresh state.
- **Across aggregates**: eventual, via read models. Example: `EditText` validates its
  locale against the *Site read model*, not the Site aggregate. A locale removed in the
  same instant might slip through — the render simply ignores unknown locales. The doc
  comment on each such validation names the race and why it is acceptable. This is the
  textbook discussion point, made concrete.
- **Read models**: in-memory, rebuilt by full replay at startup, kept live by
  in-process dispatch after each commit. For a CMS, whole-history replay is milliseconds
  to seconds; the payoff is the strongest possible demonstration that *derived state is
  disposable*. (A durable checkpointed projection is a mechanical extension; the
  projection engine already tracks positions.)
- **The publisher**: also a subscriber, but its checkpoint is durable — the
  `publish-manifest.json` in the output directory records, per page, the page version
  and site (chrome) version it rendered. Staleness = manifest vs. current read models.
  Deleting the output directory and replaying is a full republish, by construction.

## The domain, briefly

Four aggregates (detail in [domain-model.md](domain-model.md)):

- **Site** — identity, locales (with a default), theme (design tokens with light/dark
  values, typography), navigation. One editor installation manages one site by default;
  the domain supports many.
- **Page** — slug, per-locale titles and metadata, the node tree, and the published
  version pointer. The richest aggregate; nearly all editor interactions land here.
- **Asset** — the media lifecycle as an explicit event-sourced state machine:
  `AssetUploaded → ImageVariantsGenerated | SvgSanitized | VideoTranscoded/Failed/Skipped`.
- **BlockDefinition** — reusable "symbols". A page places a `BlockInstance` node that
  references a definition and carries content-only overrides; editing the definition
  updates every instance.

Locale-valued text is a value object (`LocalizedText`), not a parallel table — which is
why side-by-side translation editing is a projection (`TranslationCoverage`) rather
than a feature.

## The delivery contract

What the publisher guarantees about its output (detail in
[publishing.md](publishing.md)):

- Pure static HTML + one CSS file per site (design tokens + structural styles),
  content-hashed for immutable caching.
- **No JavaScript** except: a ~1 KB inline island loader (only on pages that contain
  widgets) and a ~15-line theme-toggle snippet. No framework, no hydration of content.
- Widgets are **web components**: the page carries the custom element tag and its
  attributes; the loader lazy-loads the widget's ES module when it scrolls near the
  viewport. A widget's cost is paid only by pages that use it, only when visible.
- Images ship as WebP with automatic `srcset`/`sizes`; videos as WebM; SVGs sanitized
  at ingest.
- Dark/light mode is CSS: tokens emit `light-dark()` values driven by
  `color-scheme` — honoring `prefers-color-scheme` by default, with an optional
  explicit override attribute set by the tiny toggle.
- Responsiveness is structural: layout primitives are intrinsically responsive
  (container queries, fluid type); the editor offers no absolute positioning, so a
  broken mobile layout is unrepresentable.

## Non-goals (v1) — deliberate, not forgotten

- **Authentication/authorization.** Orthogonal to what this project teaches. The editor
  should sit behind your reverse-proxy auth; command metadata already carries an
  `actor`. For the **multi-site SaaS** shape (many owners, one editor), the recommended
  wiring — an OIDC auth proxy in front (e.g. oauth2-proxy + Google), per-circuit identity
  capture, and site ownership filtering — is written up in
  [multi-site-saas.md](multi-site-saas.md), not built in. Sites already record their
  owner from the `site.created` envelope, so the remaining work is reading a forwarded
  identity, not reshaping the domain.
- **Scheduled publishing, approval workflows.** Both are natural event-sourced
  extensions (`PublishScheduled` + a clock; `ApprovalRequested/Granted`) and would make
  good community contributions.
- **Custom font upload.** Published sites use curated system font stacks — zero
  requests, zero FOUT, zero third parties. `@font-face` from an uploaded WOFF2 asset is
  a straightforward extension.
- **Multi-node hosting of the editor.** Read models are in-process; the event store is
  a file. Swapping SQLite for Postgres and in-memory projections for durable ones is
  the documented path to scale-out. The *published site* scales infinitely — it's
  static files.

Multi-site itself is **not** a non-goal — the domain has always been one `site-{id}`
stream per site, and the editor, dashboard and per-site deploy pipeline are built on
that (see [multi-site-saas.md](multi-site-saas.md)). What stays out of v1 is the *auth
layer* that turns "many sites" into "many isolated tenants".

## Extension guide (where to start reading)

- **New node type**: add the record in `Domain/Pages/Nodes/`, a view component in
  `Imprint.Rendering`, an inspector section in the editor, and (if it carries text) its
  fields to the translation projection. The compiler walks you through each site — node
  dispatch is exhaustive switches over a closed union.
- **New widget**: drop an ES module + manifest entry in `widgets/`. No C# required.
- **New feature**: new folder under `Features/`. Command + handler + (optionally) new
  domain behavior + events. Discovery is automatic.
- **New projection**: implement `IProjection`, subscribe to event types, register by
  existing. Replay happens on next startup.
