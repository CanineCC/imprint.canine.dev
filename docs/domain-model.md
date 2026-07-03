# Imprint — Domain Model Specification

This is the authoritative catalog of aggregates, events, node types and invariants.
Code and this document must not drift; when they do, the code review treats it as a bug.

Conventions used below:

- All events are C# `record` types under `Imprint.Authoring.Domain.<Aggregate>.Events`.
- Every event type carries `[EventType("<stable-name>", 1)]` — the stable name is
  serialized, never the CLR name. Stable names are `snake` dot-paths, versioned
  (`page.node-added`, v1). Renaming a CLR type is free; changing a stable name is a
  migration.
- Strongly-typed IDs are Guid-backed `readonly record struct`s: `SiteId`, `PageId`,
  `NodeId`, `AssetId`, `BlockDefinitionId`. Stream ids: `site-{guid:N}`,
  `page-{guid:N}`, `asset-{guid:N}`, `block-{guid:N}`.
- `Locale` is a value object wrapping a normalized IETF tag (`en`, `da`, `de-AT`).
- `LocalizedText` is an immutable map `Locale → string` with helpers
  (`Resolve(locale, defaultLocale)` falls back to default locale, then any).
- Aggregates use the classic `Raise`/`When` pattern on `AggregateRoot<TId>`:
  behaviors validate invariants and `Raise` events; `When` mutates state; `Load`
  replays. Invariant violations throw `DomainException` (caught by the dispatcher and
  returned as a failed `Result` — never a 500, never a crash).

---

## 1. Site aggregate

State: name, locales (ordered, first = created default), default locale, `Theme`,
navigation items, deleted flag. Stream: `site-{id}`.

### Value objects

- `Theme`:
  - `Tokens: IReadOnlyDictionary<string, ThemeToken>` where `ThemeToken { string Light, string Dark }`
    (CSS color values). The **semantic token set is closed** in v1:
    `background, surface, surface-alt, text, text-muted, primary, on-primary, accent, border`.
  - `Typography`: `HeadingStack` + `BodyStack` (curated system stacks, enum
    `FontStack { Sans, Humanist, Geometric, Serif, Slab, Mono }`), `BaseSizePx`
    (14–20), `ScaleRatio` (1.125–1.5, fluid type is computed from these),
    `RadiusPx` (0–24), `SpacingScale` (`Compact | Comfortable | Spacious`).
  - `Theme.Default` is a tasteful, accessible baseline (WCAG AA contrast between
    `text`/`background` and `on-primary`/`primary` in both modes — enforced by test).
- `NavigationItem { PageId PageId, LocalizedText? LabelOverride }` — label falls back
  to the page title.

### Events

| Stable name | Payload | Notes |
|---|---|---|
| `site.created` | `SiteId, string Name, Locale DefaultLocale` | Raised by `Site.Create`. Theme starts at `Theme.Default`. |
| `site.renamed` | `string Name` | |
| `site.locale-added` | `Locale Locale` | Invariant: not already present. |
| `site.locale-removed` | `Locale Locale` | Invariant: not the default locale. Content in that locale is retained in streams (history!) but no longer edited/rendered. |
| `site.default-locale-changed` | `Locale Locale` | Invariant: must be a site locale. |
| `site.theme-token-changed` | `string Token, string Light, string Dark` | Invariant: token ∈ closed set; values are valid CSS colors (validated syntactically). |
| `site.typography-changed` | `Typography Typography` | Whole value object — typography options are chosen together. |
| `site.navigation-changed` | `IReadOnlyList<NavigationItem> Items` | Full list; nav is small and reordered as a unit. |

`Site.Version` (stream version) doubles as the **chrome version** used by the publish
manifest for staleness (nav or theme change ⇒ all published pages stale).

---

## 2. Page aggregate

The heart. State: siteId, slug, per-locale `Title`, per-locale `Meta`
(`MetaTitle`, `MetaDescription`), the **node tree**, `PublishedVersion: long?`
(stream version at last publish; null = never published), deleted flag.
Stream: `page-{id}`.

### The node tree

A page owns an ordered tree of nodes. Every node has a `NodeId` (unique within the
page, stable for its lifetime) and is one of a **closed set** of types, modeled as a
C# abstract record `Node` with sealed subtypes (exhaustive switching everywhere):

**Containers** (have `Children: ImmutableList<Node>`):

| Type | Props | Placement rule |
|---|---|---|
| `SectionNode` | `Width (Normal\|Wide\|Full)`, `Background (None\|Surface\|SurfaceAlt\|Primary)`, `Padding (None\|Normal\|Large)` | Only as a **root child**; contains anything except `SectionNode`. |
| `StackNode` | `Gap (Tight\|Normal\|Loose)`, `Align (Start\|Center\|End)` | Any container except root. |
| `ColumnsNode` | `Ratios: int[]` (2–4 entries, each 1–3, e.g. `[2,1]`), `CollapseBelowPx (480\|640\|768)`, `Gap` | Children are exactly its column cells: implicit `StackNode` per column (the tree stores the stacks; ratios.Length == Children.Count is an invariant). |
| `GridNode` | `MinItemPx (160–480)`, `Gap` | Card grids; children flow. |

**Content** (leaves):

| Type | Props (locale-valued in **bold**) |
|---|---|
| `HeadingNode` | `Level (1–4)`, **`Text`** (plain) |
| `RichTextNode` | **`Html`** (canonical inline subset — see §2.2) |
| `ButtonNode` | **`Label`**, `LinkTo (PageLink(PageId) \| ExternalLink(Url))`, `Variant (Primary\|Secondary\|Ghost)` |
| `ImageNode` | `AssetId?`, **`Alt`**, `Aspect (Natural\|Square\|Wide16x9\|Portrait3x4)`, `Rounded (bool)` |
| `VideoNode` | `AssetId?`, `Mode (Ambient\|Player)` — Ambient = autoplay/muted/loop/playsinline, no controls; Player = controls, no autoplay |
| `SvgNode` | `AssetId?`, **`Alt`**, `MaxWidthPx?` — always rendered inline (sanitized at ingest) so it inherits `currentColor` |
| `DividerNode` | — |
| `SpacerNode` | `Size (Small\|Medium\|Large)` |
| `WidgetNode` | `Tag (string)`, `Props: IReadOnlyDictionary<string,string>` — validated against the widget manifest in the slice, not the aggregate |
| `BlockInstanceNode` | `BlockDefinitionId`, `Overrides: IReadOnlyDictionary<NodeId, FieldOverrides>` where `FieldOverrides = IReadOnlyDictionary<(string Field, Locale), string>` — content-only overrides keyed by the **definition's** node ids |

`NodeSpec` is the serializable description of a node subtree (type, node id, props,
children) used inside events. Events must be self-contained and deterministic: e.g.
duplication generates fresh ids **at decision time** and records them in the event, so
replay never regenerates anything.

### Tree invariants (enforced in the aggregate, tested exhaustively)

1. Root children are `SectionNode`s only; `SectionNode` appears nowhere else.
2. A container's placement rules (table above) hold for every insertion and move.
3. `MoveNode` rejects: unknown ids, moving a node into itself or any descendant,
   target parent that is not a container, index out of range (index is clamped is
   **not** allowed — reject; the editor always sends valid slots).
4. `ColumnsNode` cell stacks cannot be individually removed or moved; changing column
   count via props adds/removes trailing cells (removed cells must be empty, else the
   command fails with a domain error telling the user to move content first).
5. Max tree depth 8; max 500 nodes per page (generous sanity bounds, explicit errors).
6. All `NodeId`s in a page are unique (checked on insert of specs).
7. Text edits target an existing node and a field that exists on that node's type.

### 2.2 The canonical inline HTML subset (`RichTextNode.Html`)

Grammar (strictly validated server-side by `CanonicalHtml.Validate`; the editor's JS
normalizes `contenteditable` output to this form before submitting — the server
**rejects** non-canonical input, it never "fixes" it):

- Block elements: `<p>`, `<ul>`, `<ol>`, `<li>` (li only inside ul/ol; p/ul/ol only at
  top level; no nesting of lists in v1).
- Inline elements: `<strong>`, `<em>`, `<a href="…">`, `<br>`.
- `<a>` href must be `https:`, `http:`, `mailto:` or a page reference
  `page:{guid}` (resolved to the page's URL at render; broken refs render as plain text).
- No attributes anywhere except `href` on `<a>`. Text is HTML-entity-encoded
  (`&lt; &gt; &amp; &quot;`). Anything else ⇒ validation error.

This is the **only** place stored content resembles HTML, and it is a closed grammar
with a strict validator — treated as untrusted input twice (validated at write,
attributes re-emitted from parse at render).

### Events

| Stable name | Payload |
|---|---|
| `page.created` | `PageId, SiteId, string Slug, Locale InitialLocale, string Title` |
| `page.title-changed` | `Locale, string Title` |
| `page.slug-changed` | `string Slug` (normalized kebab-case; uniqueness is checked against the read model in the slice — documented race, acceptable: publisher detects collision and fails that page's render with a visible error) |
| `page.meta-changed` | `Locale, string? MetaTitle, string? MetaDescription` |
| `page.node-added` | `NodeId ParentId, int Index, NodeSpec Spec` (`ParentId == NodeId.Root` sentinel for root) |
| `page.node-moved` | `NodeId NodeId, NodeId NewParentId, int NewIndex` |
| `page.node-removed` | `NodeId NodeId` |
| `page.node-duplicated` | `NodeId SourceId, NodeSpec Copy` (fresh ids inside `Copy`; inserted immediately after source) |
| `page.node-props-changed` | `Node Node` — the complete replacement node with children resolved by the behavior (columns growth mints fresh empty cells at decision time), so replay is a mechanical `PageTree.Replace` |
| `page.text-changed` | `NodeId NodeId, string Field, Locale Locale, string Value` (Field ∈ {`text`,`html`,`label`,`alt`}; `html` values must already be canonical) |
| `page.block-override-set` | `NodeId InstanceId, NodeId DefinitionNodeId, string Field, Locale Locale, string? Value` (null clears the override) |
| `page.block-instance-detached` | `NodeId InstanceId, Node Replacement` — the resolved definition subtree (overrides applied, fresh ids), swapped in place |
| `page.published` | `long Version` — the stream position of the publish event itself. Replaying the stream to this version (inclusive) is the published state; any event after it means the draft has moved on (the `Modified` badge). |
| `page.unpublished` | — |
| `page.deleted` | — (publisher removes output; slice forbids deleting a page referenced by site navigation) |

---

## 3. Asset aggregate

The media lifecycle as an explicit state machine. Stream: `asset-{id}`.
State: name, kind, content type, original storage key, processing status
(`Pending | Ready | Failed | ReadyDegraded`), variants, default alt.

`AssetKind`: `Image` (raster → WebP variants), `Vector` (SVG → sanitized),
`Video` (→ WebM), `File` (passthrough download).

### Events

| Stable name | Payload |
|---|---|
| `asset.uploaded` | `AssetId, string FileName, string ContentType, AssetKind Kind, long ByteSize, string StorageKey` |
| `asset.image-variants-generated` | `IReadOnlyList<ImageVariant> Variants` where `ImageVariant { int Width, int Height, string StorageKey, long ByteSize }` (widths from {480, 960, 1440, 1920} not exceeding source; always includes the largest ≤ source) |
| `asset.svg-sanitized` | `string StorageKey, int RemovedNodeCount` |
| `asset.video-transcoded` | `string StorageKey, long ByteSize` |
| `asset.processing-failed` | `string Reason` → status `Failed` (still downloadable as original in the editor; not publishable) |
| `asset.processing-skipped` | `string Reason` → status `ReadyDegraded` (e.g. ffmpeg absent: original file is published as-is with a visible editor warning) |
| `asset.alt-changed` | `Locale, string Alt` (default alt; `ImageNode.Alt` overrides per placement) |
| `asset.renamed` | `string Name` |
| `asset.deleted` | — (slice forbids deletion while referenced; the `ContentUsage` query service tracks references) |

Processing runs on an in-process `Channel<AssetId>` background worker
(`ProcessUploadedAsset` slice): it reads the original from the media store, produces
derivatives, then issues the corresponding command — the *worker* is untrusted
infrastructure; only the *aggregate* records truth.

---

## 4. BlockDefinition aggregate ("symbols")

Stream: `block-{id}`. State: name, `NodeSpec Spec` (a subtree; same placement grammar
as pages, root of spec must be a single non-Section container or content node),
deleted flag.

| Stable name | Payload |
|---|---|
| `block.defined` | `BlockDefinitionId, string Name, NodeSpec Spec` |
| `block.renamed` | `string Name` |
| `block.spec-changed` | `NodeSpec Spec` (node ids in the spec are stable across changes when possible — the editor sends the edited spec; overrides on removed nodes are simply ignored at render) |
| `block.deleted` | — (slice forbids while instances exist, via the `ContentUsage` query service) |

Rendering a `BlockInstanceNode` = render the definition's spec with the instance's
field overrides overlaid (override lookup: definition node id + field + locale).
"Detach" is a Page command that replaces the instance node with a deep copy
(fresh ids) of the resolved spec.

---

## 5. Read models (projections, in `Imprint.Authoring.Projections`)

All are in-memory, rebuilt by replay at startup, updated live in-process. Each exposes
an immutable snapshot and raises a `Changed` notification (the editor subscribes for
live UI updates).

| Projection | Derived from | Serves |
|---|---|---|
| `SiteOverview` | site.* | settings UI, theme editor, nav editor, locale lists, chrome version |
| `PageList` | page.created/title/slug/published/unpublished/deleted, site.navigation | dashboard, nav picker, slug-uniqueness check, publish badges (`Draft`, `Published`, `Modified` = published but currentVersion > publishedVersion) |
| `PageDrafts` | all page.* | the editor canvas: current tree + all locale values per page |
| `PublishedContent` | all page.* | the publisher's page source: folds each page through its aggregate and **snapshots** the state at every `page.published` — because the global sequence is ordered, the folded state at that moment IS the published state, so no stream re-reading is needed |
| `AssetLibrary` | asset.* | asset panel: status, variants, sizes |
| `ContentUsage` | *(computed over `PageDrafts`/`BlockLibrary`, not folded)* | asset/block reference tracking: delete protection; republish pages when a referenced asset, block or linked page changes |
| `BlockLibrary` | block.*, page.* (instance counts) | blocks panel, delete protection |
| `TranslationCoverage` | page.text/title/meta, site locales | translation panel: per page × locale, missing/total field counts and the field list |

---

## 6. Slice catalog (`Imprint.Authoring.Features`)

Folder = use case = command + validator (data-shape checks) + handler. Handlers load
aggregates via `IAggregateStore`, call one behavior, save. Cross-aggregate checks go
through read models (each such check carries a comment naming the accepted race).

- **Sites/**: `CreateSite`, `RenameSite`, `AddLocale`, `RemoveLocale`,
  `ChangeDefaultLocale`, `ChangeThemeToken`, `ChangeTypography`, `ChangeNavigation`,
  `CreateSiteFromTemplate` (orchestrates: creates site, theme, assets?, pages, nav from
  a `SiteTemplate` description — the seed-stream pattern).
- **Pages/**: `CreatePage`, `ChangePageTitle`, `ChangeSlug`, `ChangePageMeta`,
  `DeletePage`, `AddNode`, `AddPreset` (inserts a section preset's spec), `MoveNode`,
  `RemoveNode`, `DuplicateNode`, `ChangeNodeProps`, `EditText`, `SetBlockOverride`,
  `DetachBlockInstance`, `PublishPage`, `UnpublishPage`.
- **Assets/**: `UploadAsset` (streams to media store, then command), `ProcessUploadedAsset`
  (internal, worker-invoked), `SetAssetAlt`, `RenameAsset`, `DeleteAsset`.
- **Blocks/**: `DefineBlockFromNode` (creates definition from a page node's subtree and
  replaces the node with an instance — one logical operation, two aggregates, **two
  transactions**: definition first, then page; the handler compensates by deleting the
  definition if the page append fails), `RenameBlock`, `UpdateBlockFromInstance`
  (pushes an instance's resolved subtree back to the definition), `DeleteBlock`.
- **Publishing/**: `PublishAllStale` (fan-out of `PublishPage` over stale pages).

---

## 7. Testing contract

- Aggregate tests use the Given/When/Then kit from `Imprint.TestKit`:
  `Given(events…).When(a => a.Behavior(…)).ThenRaised(expected…)` /
  `.ThenFails(messagePart)`. Every invariant in this document
  has at least one negative test.
- Slice tests run against a real SQLite in-memory store + real dispatcher + real
  projections: dispatch command(s), assert on events **and** read-model state.
- `CanonicalHtml` has an adversarial test battery (script/style/iframe/event handlers/
  `javascript:`/data URIs/nested lists/malformed nesting/entity tricks) — all rejected.
- Serialization round-trip tests cover **every** event type via the registry
  (reflection-driven: no event may be added without a round-trip test passing).
