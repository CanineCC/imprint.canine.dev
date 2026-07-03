# Imprint — Publishing & Delivery Specification

The publisher (`Imprint.Publishing`) is a projection whose store is a directory.
It subscribes to domain events and keeps `<output>/` equal to "the published state of
the site, rendered". Deleting the directory and replaying produces the same bytes
(modulo content hashes, which are content-derived and therefore also identical).

## Output layout

```
<output>/
├── index.html                      # default locale, nav-first page (or /{slug}/)
├── {slug}/index.html               # default locale pages
├── {locale}/{slug}/index.html      # other locales (locale root: /{locale}/index.html)
├── 404.html
├── css/site.{hash}.css             # tokens + structural styles, one file, hashed
├── js/theme-toggle.js              # ~15 lines, only if… no: always inline instead — see §JS
├── assets/{assetId}-{width}.{hash}.webp
├── assets/{assetId}.{hash}.webm|svg|<original ext>
├── widgets/{tag}.{hash}.js         # only widgets actually used on published pages
├── sitemap.xml, robots.txt
├── publish-manifest.json           # the projection's durable checkpoint
└── *.br / *.gz                     # precompressed siblings for text files
```

## The HTML contract (verified by tests)

Every published page:

- Valid HTML5, `<html lang="{locale}">`, UTF-8, viewport meta,
  `<title>` + meta description from page meta (fallback: title), canonical URL,
  `hreflang` alternates for every locale the page has content in (plus
  `x-default` → default locale).
- One stylesheet link (`css/site.{hash}.css`, immutable-cacheable).
- **Inline scripts only, and only two, both tiny and optional**:
  1. Theme toggle (~15 lines): reads `localStorage.imprintTheme`, sets
     `data-theme` on `<html>` before first paint (placed in `<head>`, blocking by
     design to avoid flash), wires the toggle button if the page has one.
  2. Island loader (~1 KB, only when the page contains widgets): finds
     `[data-island]`, `IntersectionObserver` with 200px rootMargin, injects
     `<script type="module" src>` once per bundle. `data-island-eager` skips the
     observer.
  No other JavaScript. No framework. No external requests of any kind (fonts are
  system stacks; analytics is your problem, deliberately).
- Widgets render as
  `<x-tag prop-a="…" data-island="/widgets/x-tag.{hash}.js">…fallback content…</x-tag>`
  (server-rendered fallback/placeholder content comes from the widget manifest's
  `placeholder` setting; custom elements upgrade in place — no layout shift for
  fixed-aspect widgets).
- Images: `<img src="…960.webp" srcset="480w, 960w, 1440w, 1920w (as available)"
  sizes="…computed from the column context…" width height loading="lazy"
  decoding="async" alt="…">`. First image in the first section gets
  `loading="eager" fetchpriority="high"` (LCP care). `width`/`height` always present
  (from variant metadata) — zero CLS.
- Videos: `<video>` WebM source; Ambient mode = `autoplay muted loop playsinline`
  (+`disableremoteplayback`), Player mode = `controls preload="metadata"`.
- SVGs: inlined (sanitized at ingest, re-checked at publish), `role="img"` +
  `aria-label` from alt, or `aria-hidden="true"` when alt empty.
- Navigation: `<nav>` with `aria-current="page"`; skip-link; exactly one `<h1>` per
  page is the editor's job (the inspector warns), landmarks: `header/main/footer`.

## Theme → CSS

`ThemeCss.Emit(theme)` produces:

```css
:root { color-scheme: light dark;
  --ip-background: light-dark(#fff, #111); …one custom prop per token… ;
  --ip-font-heading: …stack…; --ip-text-base: clamp(…fluid from BaseSizePx/Scale…); … }
:root[data-theme=light] { color-scheme: light } /* explicit override via toggle */
:root[data-theme=dark]  { color-scheme: dark }
```

plus the structural styles for layout primitives (authored once in
`Imprint.Rendering/styles/imprint-base.css`, appended verbatim):
`.ip-section` (container-type: inline-size; width variants map to max-width tracks),
`.ip-stack` (flex column, gap variants), `.ip-columns` (grid, `--cols-template` from
ratios, collapses to single column under its `CollapseBelowPx` via container query
classes `.ip-collapse-480/640/768`), `.ip-grid`
(`repeat(auto-fill, minmax(min(var(--min-item), 100%), 1fr))`), typography scale,
buttons, prose styles for richtext. The same file styles the editor canvas —
**pixel-identical preview by construction**.

`light-dark()` requires the modern baseline (2024+ browsers) — accepted and documented;
the graceful floor is "light theme everywhere" because the light values are the
function's first argument evaluated under `color-scheme: light`.

## Publish manifest (the checkpoint)

```json
{ "schemaVersion": 1,
  "siteVersion": 42,
  "pages": { "<pageId>": { "publishedVersion": 17, "renderedAtSiteVersion": 42,
                            "paths": ["/", "/da/"], "assetHashes": ["…"] } },
  "cssHash": "…", "widgetBundles": { "x-countdown": "…hash…" } }
```

Staleness rules (drive both auto-republish and the editor's badges):

- page stale ⇔ `publishedVersion < page.PublishedVersion` (a newer publish decision)
  — republish page.
- chrome stale ⇔ `renderedAtSiteVersion < site.Version` — republish all published
  pages (debounced 2 s so a theme-editing session doesn't re-render per keystroke).
- asset stale ⇔ a used asset re-processed — republish referencing pages
  (via `AssetUsage`).
- `page.unpublished`/`page.deleted` — remove files (and locale variants) + manifest
  entry; stale-file sweep removes anything on disk not reachable from the manifest
  (hash rotation cleanup).

The publisher never throws at the editor: a page render failure (e.g. slug collision)
lands in the manifest as `"error": "…"` and surfaces in the editor status bar.

## Imprint.Site (the host)

Kestrel static file host over `<output>/`: content-hash pattern ⇒
`Cache-Control: public, max-age=31536000, immutable`; HTML/manifest ⇒
`no-cache` + ETag; serves precompressed `.br`/`.gz` when acceptable;
maps `/{path}/` → `index.html`; custom 404 → `404.html`; correct MIME for
webp/webm/svg/js modules. ~150 lines including comments — the point is to show the
whole delivery story fits in one file you can read.

## Performance budget (enforced in E2E)

Published page with images and one widget, over the wire (brotli): HTML ≤ 15 KB,
CSS ≤ 12 KB, inline JS ≤ 1.5 KB, zero external requests, zero CLS from images,
no JS parse cost until a widget approaches the viewport.
