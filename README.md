# Imprint

**An event-sourced, domain-driven CMS that publishes static sites — built to be read.**

Imprint is an open-source (Apache-2.0) reference implementation of three architectural
styles working together in a real, useful product:

- **Domain-Driven Design** — pages are a rich domain tree of named nodes, never blobs
  of HTML. Every mutation is a domain behavior with invariants.
- **Event Sourcing** — every change is an immutable event in an append-only SQLite
  store. Versioning, drafts, undo, audit history and publishing all *derive* from the
  log instead of being features.
- **Vertical Slice Architecture** — every use case is one folder: command, validation,
  handler. No layers to spelunk.

And one idea that ties the whole system together:

> **The published website is a projection.** Editors issue commands; commands append
> events; projections fold events into state — and one of those projections writes
> HTML files to disk instead of objects to memory.

## What you get

- **A structural editor** (Blazor Server): true drag-and-drop over the domain tree with
  slot-based drop targets (invalid drops are unrepresentable), Figma-style selection
  (click / Esc / double-click, breadcrumb, layers panel), inline text editing with a
  strict canonical rich-text subset, keyboard-first operation, undo via compensating
  commands.
- **Multilingual by construction**: every text field is locale-valued. Side-by-side
  translation editing and coverage tracking are projections, not features.
- **Design tokens, not themes**: semantic colors with light/dark values (dark mode is
  pure CSS via `light-dark()`), curated system font stacks, fluid type. Layout
  primitives (Stack, Columns, Grid) are intrinsically responsive via container
  queries — there is no absolute positioning, so broken mobile layouts cannot be
  authored.
- **Reusable blocks ("symbols")**: define once, place linked instances with per-field
  content overrides, push instance edits back to the definition, or detach.
- **A media pipeline**: uploads become WebP variant sets (`srcset` automatic,
  zero CLS), SVGs are sanitized and inlined, videos transcode to WebM when ffmpeg is
  available (and degrade honestly when it isn't).
- **Static output that respects your visitors**: plain HTML + one CSS file,
  content-hashed for immutable caching, precompressed, **zero framework JavaScript**.
  Interactive widgets are web components that hydrate lazily as islands — pages
  without widgets ship no JS at all (beyond a 15-line inline theme toggle).
- **A delivery host in one readable file** — or point any static host / CDN at the
  output directory.

## Quickstart

```bash
# the editor (creates ./data on first run — event store, media, published output)
dotnet run --project src/Imprint.Editor
# → http://localhost:5000 — the onboarding screen offers a starter template

# the published site (after you hit Publish in the editor)
dotnet run --project src/Imprint.Site
```

Optional: install `ffmpeg` for WebM video transcoding. Everything else is .NET only —
no Node, no bundler, no external services.

## Reading guide

| Read this | To understand |
|---|---|
| [docs/architecture.md](docs/architecture.md) | The two planes, the projection insight, the VSA↔DDD tension and its resolution, consistency model, non-goals |
| [docs/domain-model.md](docs/domain-model.md) | Aggregates, the full event catalog, the node tree and its invariants, the canonical HTML grammar |
| [docs/editor-ux.md](docs/editor-ux.md) | The selection model, the drag-and-drop slot protocol, inline editing, the JS interop contract |
| [docs/publishing.md](docs/publishing.md) | The static output contract, islands, theme→CSS, the publish manifest, the performance budget |
| [docs/conventions.md](docs/conventions.md) | How the code is written and tested |
| `src/Imprint.EventSourcing/` | A complete event store + aggregate + projection machinery in ~15 files with no CMS concepts |

Suggested first trace: follow one drag-and-drop through the system —
`editor canvas (pointer events) → DragPlan slots → MoveNode command → Page.MoveNode
(invariants) → page.node-moved event → PageDraft projection → canvas re-render`, then
hit Publish and watch the same event log become files on disk.

## Non-goals (v1, deliberate)

Authentication (bring your own reverse proxy; `actor` metadata is already threaded),
approval workflows, scheduled publishing, custom font uploads, multi-node editor
hosting. Each is documented in [architecture.md](docs/architecture.md) with its natural
extension path — several would make excellent first contributions.

## License

[Apache-2.0](LICENSE). Dependencies are deliberately minimal: `Microsoft.Data.Sqlite`
and `SkiaSharp` (MIT) at runtime — everything else is the .NET platform.
