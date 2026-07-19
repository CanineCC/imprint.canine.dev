# Imprint — Editor UX Specification

The editor is a Blazor Server app. Its job: make structural editing feel like direct
manipulation while every mutation remains a domain command. This document is the
contract between the Blazor components, the JS interop modules, and the slices.

Design stance: **Figma's selection model, Notion's block affordances, zero mystery.**
Users only ever see and manipulate *named domain nodes* — there is no anonymous markup
anywhere in the UI.

## 1. Layout

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Top bar: site name · page switcher · viewport toggle (⌀ 390 / 768 / full)    │
│          locale switcher · canvas light/dark toggle · Publish (badge)        │
├────────┬──────────────────────────────────────────────────┬─────────────────┤
│ Rail   │  Canvas                                          │ Inspector       │
│ Pages  │   ┌─ breadcrumb: Home › Hero › Columns › Heading │  (contextual:   │
│ Layers │   │                                              │   props of the  │
│ Blocks │   │  [rendered page tree, editor mode,           │   selected      │
│ Assets │   │   width-constrained by viewport toggle,      │   node; page    │
│ Theme  │   │   overlay draws selection/hover/drag UI]     │   settings when │
│ Trans- │   │                                              │   nothing is    │
│ lations│   │                                              │   selected)     │
├────────┴───┴──────────────────────────────────────────────┴─────────────────┤
│ Status bar: save state ("All changes saved" — always true, commands are     │
│ synchronous), publish staleness, undo hint                                   │
└──────────────────────────────────────────────────────────────────────────────┘
```

- **Rail** (56 px icon rail) switches the **left panel** (280 px, collapsible):
  Pages (tree/list + create), Layers (domain tree of current page, with
  select-sync and per-row move/duplicate/delete affordances),
  Blocks (symbols library), Assets (library + upload), Theme (token editor),
  Translations (side-by-side editor).
- The **canvas** renders the current page via `Imprint.Rendering` components in Editor
  mode inside a width-constrained, horizontally centered container
  (`.canvas-viewport`). The viewport toggle sets its width (390 / 768 / 100%); because
  all published responsiveness is container-query-driven, this preview is *exactly*
  what mobile rendering does — no iframe needed. Theme tokens are applied to the canvas
  root (not `:root`), so editor chrome and site theme never bleed into each other. The
  canvas light/dark toggle switches the canvas root's `data-theme`, previewing the
  site's dark mode independent of the editor's own chrome theme.
- Editor chrome has its own light/dark following `prefers-color-scheme` (with toggle in
  the user menu), styled exclusively with editor CSS custom properties (`--ed-*`).
  System font stack. No icon font — inline SVG icons (a single `Icon.razor` with a
  curated path set).

## 2. Selection model (the contract)

State lives in C# (`EditorSession.Selection: NodeId?`); JS reports intents, C# decides,
then instructs the overlay.

- **Click** on canvas → JS resolves the *deepest* element with `data-node-id` under the
  pointer → `ReportClick(nodeId)` → C# selects it.
- **Esc** → select parent (root child → clear selection).
- **Double-click** on a text-bearing node (`heading, richtext, button`) → enter inline
  edit (§4). Double-click elsewhere = click.
- **Breadcrumb** above the canvas always shows the ancestor path of the selection
  (`Home › Hero › Columns › Stack › Heading`); every crumb is clickable. Node display
  names: type display name, plus a truncated text preview for text nodes
  (`Heading "Welcome to…"`).
- **Hover**: outline (1 px, `--ed-accent` at 40%) + type chip. Selection: 2 px solid
  `--ed-accent` + chip + the node toolbar.
- **Node toolbar** (overlay, anchored top-start of selection, flips if clipped):
  drag handle `⋮⋮`, type label, then icon buttons: *duplicate*, *make block /
  detach block* (contextual), *delete*. Section selections additionally show
  *move up / move down* arrows (coarse reorder without drag).
- **Layers panel** mirrors selection bidirectionally (select in canvas highlights row
  and scrolls it into view; click row selects in canvas and scrolls canvas node into
  view).

## 3. Drag and drop (the protocol)

Slot-based, computed in C#, executed in JS. HTML5 DnD is not used anywhere;
pointer events only.

1. **Lift**: pointerdown on the drag handle (mouse) or 350 ms long-press on the node
   (touch; movement > 8 px before threshold cancels lift and scrolls naturally) → JS
   calls `BeginDrag(nodeId)`.
2. **Plan**: C# computes the full `DragPlan` from the domain tree and placement rules
   (`Imprint.Authoring.Features.Pages.MoveNode.SlotPlanner` — the same rules the
   aggregate enforces, so an invalid drop is *unrepresentable in the UI*):
   ```json
   { "dragNodeId": "…",
     "slots": [ { "slotId": 0, "parentId": "…", "index": 2,
                  "anchorId": "…", "edge": "before|after|into",
                  "orientation": "h|v" } ] }
   ```
   `anchorId` is the sibling whose rect the indicator snaps to (`edge`
   before/after), or the empty container itself (`into`). Slots exclude: the dragged
   node's own subtree, its current position (dropping where it already is is a no-op,
   not an error), and anything violating placement rules.
3. **Track** (pure JS, 60 fps, no server roundtrips): ghost chip follows the pointer
   (node type + name, slight tilt); candidate slot = nearest slot by geometry
   (distance to the indicator line the slot would draw, orientation-aware); indicator =
   2 px accent line with rounded caps (vertical or horizontal per `orientation`), or a
   dashed inset rect for `into` empty containers; the *target container* gets a subtle
   tint. Auto-scroll when pointer is within 48 px of the canvas' scroll edges
   (speed ∝ proximity). `Esc` cancels.
4. **Drop**: JS calls `CompleteDrag(slotId)` → C# dispatches `MoveNode` → projection
   updates → Blazor re-renders → overlay re-measures. JS never mutates the DOM tree.

The **Layers panel** offers the same *moves* through row affordances rather than a
second drag surface: selecting a row mirrors the canvas selection, and per-row
up/down (Alt-arrow equivalents), duplicate and delete drive the same commands. Full
row-rect drag in the panel is a natural extension left to contributors.

## 4. Inline text editing

- Double-click (or Enter with a text node selected) → the node's text element gets
  `contenteditable` (`plaintext-only` for heading/button; full for richtext) and
  focus; the overlay shows a thin "editing" ring instead of selection chrome.
- **RichText**: a floating mini-toolbar (Bold, Italic, Link, and list toggles) appears
  above the text selection inside the node. Marks are applied with
  `document.execCommand` equivalents implemented manually via Range/Selection APIs
  (no deprecated APIs): wrap/unwrap `strong`/`em`, prompt-less link editing via a small
  popover (URL field + "link to page" picker fed by C#).
- **Normalization**: on commit, JS walks the edited DOM and rebuilds canonical subset
  markup (§2.2 of the domain spec) — unknown tags unwrapped, disallowed attributes
  dropped, empty inlines removed, `<div>`→`<p>`, `&nbsp;` collapsed. The server
  validates and **rejects** non-canonical input; JS normalization is UX, the validator
  is the guarantee.
- **Commit**: on blur, Esc (reverts instead), or debounce (800 ms of no input) →
  `CommitText(nodeId, field, locale, value)` → `EditText` command. Blazor must not
  re-render the node *while it is being edited* (the canvas skips patching the active
  edit node; overlay ring tracks it).
- Enter in heading/button commits (single-line); Enter in richtext makes a new `<p>`,
  Shift+Enter a `<br>`.

## 5. Insertion

- **Gap affordance**: hovering the boundary between siblings (or an empty container)
  reveals a `+` pill on the boundary line. Click → **block picker** popover anchored at
  the gap: a searchable grid of node types (with small visual glyphs), section presets
  (with thumbnails) when the gap is at root level, and blocks (symbols). Choosing
  dispatches `AddNode`/`AddPreset` at exactly that slot.
- **Slash command**: with a selection, `/` opens the same picker; insertion lands after
  the selected node (or inside, if an empty container is selected).
- **Empty states matter**: an empty page shows a friendly hero-sized picker ("Add your
  first section — or start from a preset"); an empty container shows a centered ghost
  `+`.

## 6. Keyboard map (canvas focused)

| Key | Action |
|---|---|
| `Esc` | Parent / clear; cancels drag or inline edit |
| `Enter` | Edit text of selection (if text-bearing) |
| `Delete`/`Backspace` | Remove selection (with 5 s undo toast, no confirm dialog) |
| `Ctrl/⌘+D` | Duplicate selection |
| `Ctrl/⌘+Z` / `Ctrl/⌘+Shift+Z` | Undo / redo (session-scoped, compensating commands: move-back, re-add-removed-spec, restore-previous-text/props — an event-sourcing showcase, implemented in `EditorSession.UndoStack`) |
| `Alt+↑`/`Alt+↓` | Move selection within its parent |
| `/` | Open block picker |
| `↑`/`↓` | Select previous/next sibling; `←` parent, `→` first child |

All drag-and-drop operations are therefore fully keyboard-accessible
(select → Alt+arrows / cut-free move via duplicate+delete is not needed: Alt+arrows +
`Esc`-parent navigation reaches every valid slot).

## 7. Panels

- **Inspector** (right, 300 px): header with select-parent, the node's name, and a
  delete button (disabled for managed column cells); typed prop editors per node type — segmented controls
  for enums, steppers for bounded ints, color-role pickers (theme roles, not raw
  colors), asset pickers (opens asset panel in select mode), link editor
  (page picker + external URL tab), the widget prop form (from manifest), block
  instance panel (override list + *Update definition from this instance* + *Detach*).
  Nothing selected → page settings (title, slug with live URL preview, meta per
  locale, publish/unpublish, delete).
- **Translations**: locale pair selectors (source | target), then every text field of
  the current page in a two-column grid — field label with node breadcrumb, source
  read-only, target editable in place (same `EditText` slice). Untranslated fields
  highlighted; coverage bar per page from `TranslationCoverage`; "next untranslated"
  jump. Page title + meta included.
- **Assets**: grid with kind filter + upload (drag-file-anywhere onto panel or button);
  cards show processing state live (Pending spinner → variants ready / degraded
  warning with reason / failed). Detail: rename, default alt per locale, variant list
  with sizes, usage list ("used on 3 pages"), delete (disabled while used, with the
  reason shown).
- **Theme**: token list grouped (Background/Text/Brand/Border) — each row: name, light
  swatch, dark swatch (popover pickers with contrast hints against paired tokens);
  typography section (stack pickers with live specimen, base size, scale, radius,
  spacing); every change is a command, the canvas updates live.
- **Blocks**: library cards (name, small render thumbnail, usage count), rename,
  delete (guarded). Creation happens from the canvas (*make block* on the node
  toolbar).
- **Pages**: list ordered as site nav first then rest alphabetically; status badges
  (Draft / Published / Modified); create (title → slug suggested live); nav editor
  (checkbox "in navigation" + drag-reorder of nav items).

## 8. Publish UX

Top-bar button shows scope: `Publish` (current page, when draft/modified), with a
split-menu for `Publish all (N stale)`. After publish: toast with the public URL.
Staleness comes from the publish manifest read model — theme/nav changes flip all
published pages to stale with a status-bar hint ("Site chrome changed — 4 pages need
republish").

## 9. JS interop architecture

Vanilla ES modules under `Imprint.Editor/wwwroot/js/`, no build step, JSDoc-typed:

- `canvas-interop.js` — the single entry: wires pointer/keyboard listeners, owns the
  **overlay layer** (`position:absolute` sibling of the canvas, `pointer-events:none`
  except its own controls), draws hover/selection/toolbar/indicator/ghost, re-measures
  on scroll + `ResizeObserver` + `MutationObserver` (rAF-batched), and exports exactly:
  `init(canvasEl, overlayEl, scrollerEl, dotnetRef)`, `setSelection(nodeId|null)`,
  `enterInlineEdit(nodeId, mode)`, `dispose()`. (Dragging starts from the overlay
  handle inside the module — there is no separate `beginDrag` export; the drag plan is
  fetched from C# via `BeginDrag` and tracked entirely client-side.)
  Inbound to C# (`[JSInvokable]` on `CanvasBridge`): `ReportClick`,
  `ReportDoubleClick`, `ReportSelectionRect`, `BeginDrag`, `CompleteDrag`,
  `CancelDrag`, `CommitText`, `EndInlineEdit`, `ReportKey` (only keys C# owns),
  `ReportGapClick`.
- `rich-toolbar.js` — selection-anchored mini toolbar + canonical normalizer
  (`normalize(rootEl): string` — pure, unit-testable in isolation).
- `file-drop.js` — drag-file-onto-panel upload plumbing (streams via Blazor's
  `IBrowserFile`? No — panel uses `InputFile`; this module only handles drop targeting
  and forwards the FileList to the hidden input).
- Everything is idempotent and disposable; Blazor circuits reconnecting must not leak
  listeners (register once per `init`, full teardown in `dispose`).

## 10. Feel & polish bar

- Every interactive element: visible focus ring (`:focus-visible`), hover state,
  `aria-label`s on icon buttons; overlay chips/toolbars don't obstruct the text being
  edited; animations 120–160 ms ease-out (indicator fade/slide, panel transitions),
  no springy excess; `prefers-reduced-motion` respected (transitions off).
- Empty states, error toasts (command failures surface the domain error text — they
  are written for humans), and optimistic-feel latency: commands are local in-process,
  so round-trips are single-digit ms; nothing needs optimistic UI trickery.
- The editor must be honestly usable on a tablet: pointer events + long-press lift
  already cover touch; panels collapse. Tap targets are generous — the rail and
  primary actions at 40 px, toolbar buttons at 32 px. The insertion gap pill stays
  small (a transient hover affordance surrounded by whitespace); it is deliberately
  *not* padded out to a 40 px hit area, because an invisible expanded region over the
  canvas would steal clicks from the content beneath it.
