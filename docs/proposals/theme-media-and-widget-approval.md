# Proposal — theme-aware media variants & the widget request/approval workflow

Two additions, designed to fit the existing grain (event-sourced, static-first,
zero delivered JS beyond islands, aggregate owns its invariants).

---

## Part 1 — Light/dark media variants (images & SVGs)

### The need

A single asset is currently identical in both colour schemes. That is right for a
photo but wrong for a **logo** (a dark wordmark disappears on a dark background) and
often wrong for screenshots, diagrams and multi-colour SVGs. Monochrome SVGs already
adapt (they inherit `currentColor`); everything else does not.

### Model

An asset stays **neutral** by default (one file, both schemes) and may gain **one
optional dark-mode variant**. The original upload is the light/neutral rendition; the
dark variant is a second file rendered only in dark mode. (Video is out of scope —
dual light/dark video is rare and doubles storage + transcode.)

### Events (Asset aggregate, appended to `asset-{id}`)

| Stable name | Payload | Notes |
|---|---|---|
| `asset.dark-variant-uploaded` | `string StorageKey, string ContentType` | The raw dark original. Only valid on `Image`/`Vector` assets whose base is `Ready`/`ReadyDegraded`. Sets a `DarkPending` sub-status. |
| `asset.dark-image-variants-generated` | `IReadOnlyList<ImageVariant> Variants` | WebP widths for the dark image (same widths as the base). |
| `asset.dark-svg-sanitized` | `string StorageKey, int RemovedNodeCount` | Sanitized dark SVG. |
| `asset.dark-variant-failed` | `string Reason` | Processing failed; the dark variant is dropped (asset stays usable, neutral). |
| `asset.dark-variant-removed` | — | Editor removed the dark variant; asset reverts to neutral. |

### Aggregate state additions

`DarkOriginalStorageKey?`, `IReadOnlyList<ImageVariant> DarkVariants`,
`DarkDerivedStorageKey?` (svg), `DarkStatus` (`None | Pending | Ready | ReadyDegraded`).
`HasDarkVariant => DarkStatus is Ready`.

Invariants: a dark variant may only be uploaded on an `Image` or `Vector` asset;
uploading a second dark variant replaces the first (re-enters `Pending`); the dark
content-kind must match the base kind (no dark SVG on a raster image).

### Slices

- `UploadAssetDarkVariant(AssetId, FileName, ContentType, ByteSize, Stream)` — stores
  the dark original via `IMediaStore`, raises `dark-variant-uploaded`, enqueues
  processing.
- `ProcessAssetDarkVariant(AssetId)` — worker-invoked; runs the same
  `IMediaProcessor` path the base used (WebP variants / SVG sanitize) and records the
  outcome. Any exception → `dark-variant-failed` (never crashes the worker).
- `RemoveAssetDarkVariant(AssetId)` — raises `dark-variant-removed`; media bytes
  cleaned after the save.

The processing worker's startup recovery also re-enqueues assets whose `DarkStatus`
is `Pending`.

### Rendering — the zero-JS technique

`AssetRenderInfo` gains dark fields (frozen contract below). When a dark variant
exists, the view emits **both** renditions and lets CSS choose — no JavaScript, and it
honours *both* the OS setting and Imprint's explicit theme toggle:

```html
<img class="ip-img ip-img-light" …light srcset…>
<img class="ip-img ip-img-dark"  …dark srcset… aria-hidden="true">
```

`imprint-base.css` (structural, shipped once):

```css
.ip-img-dark { display: none; }
@media (prefers-color-scheme: dark) {
  .ip-img-light { display: none; }
  .ip-img-dark  { display: revert; }
}
/* the explicit toggle wins over the OS setting, both directions */
:root[data-theme="light"] .ip-img-light { display: revert; }
:root[data-theme="light"] .ip-img-dark  { display: none; }
:root[data-theme="dark"]  .ip-img-light { display: none; }
:root[data-theme="dark"]  .ip-img-dark  { display: revert; }
```

SVG uses the same class pair on the two inline `<svg>` wrappers. The dark `<img>`/svg
carries `aria-hidden` and the light one keeps the alt text, so assistive tech reads it
once. When no dark variant exists, output is unchanged (single neutral image).

### Publishing

The dark variants are copied like base variants
(`assets/{id}-dark-{width}.{hash}.webp`), and the per-page dependency fingerprint
already introduced for staleness gains the dark variant hashes, so re-processing a
dark variant re-renders the referencing pages.

### Editor

The Assets panel asset-detail grows one **"Dark-mode version (optional)"** slot: an
upload control, a live status (processing / ready / failed reason), a thumbnail, and
a remove button. Image/vector assets only.

### Frozen contract (I set this before fan-out)

`AssetRenderInfo` (src/Imprint.Rendering/RenderContext.cs) gains:

```csharp
IReadOnlyList<ImageSource> DarkImageVariants,   // empty when none
int? DarkIntrinsicWidth, int? DarkIntrinsicHeight,
string? DarkInlineSvg,                            // null when none
string? DarkUrl                                   // chosen dark src (video/file: unused)
```

---

## Part 2 — Widget request & approval workflow

### The trust boundary (why this shape)

A widget is **code that runs JavaScript on visitors' pages**. Today widgets are files
a developer drops in the `widgets/` directory — trusted because deploying them is a
privileged act. This feature lets an **editor submit** a widget and a **server owner
approve** it, moving the trust gate into the app *without removing it*: nothing an
editor submits runs anywhere until an admin approves it. Approval is the privileged
act; the app records who did it and preserves the exact approved bytes in the log.

Authorization of *who is the admin* stays the reverse proxy's job (see
architecture.md non-goals) — the `/admin` surface must sit behind your auth. What this
feature owns is the **mechanism** (submit → review → approve/reject) and the **audit
trail** (actor + code recorded as events).

### WidgetSubmission aggregate (stream `widget-{id}`)

The submitted JS bundle is stored **in the event** (widgets are small; cap 512 KB),
so the exact approved code is part of the immutable audit trail.

| Stable name | Payload |
|---|---|
| `widget.submitted` | `WidgetSubmissionId, string Tag, string Name, string Description, string Placeholder, string? AspectRatio, bool Eager, IReadOnlyList<WidgetPropSpec> Props, string BundleSource, long ByteSize, string RequestedBy` |
| `widget.revised` | same manifest fields + `BundleSource` (edit a pending/rejected submission and re-open it for review) |
| `widget.approved` | `string ApprovedBy` |
| `widget.rejected` | `string RejectedBy, string Reason` |
| `widget.withdrawn` | — |

`WidgetPropSpec { string Name, string Label, WidgetPropType Type, string? Default, IReadOnlyList<string> Options }`.

State: `Status (Pending | Approved | Rejected | Withdrawn)`, the manifest fields, the
bundle source, actors.

Submit/revise invariants (in the aggregate + slice): tag is a valid custom-element
name (`WidgetManifest.IsValidTag`); prop names valid (`IsValidPropName`); bundle
non-empty and ≤ cap; `AspectRatio` matches `^[0-9 /.]+$` when present. Cross-aggregate
(slice): the tag must not collide with a built-in widget tag or another **approved**
submission's tag — re-checked at approve time (documented race: two approvals of the
same tag in the same instant → the second approve fails).

### WidgetRegistry projection (read model)

Folds `widget.*` →
- `Approved`: `IReadOnlyList<WidgetDescriptor>` (approved only) + `BundleOf(tag) → source`.
- `Submissions`: every submission with status + metadata + code, for the admin page.
- `ApprovedTags` for collision checks.

### Merged catalog

`EditorWidgetCatalog` becomes the union of **built-in** (filesystem) and **approved**
(registry) widgets. `IWidgetCatalog.Exists/PropNames`, `Descriptors`, `Find` all query
the union. Built-in tags win a collision (they can't be shadowed).

### Publishing

When a used tag is a built-in, the bundle is copied from the widgets directory (as
today). When it is an approved submission, the bundle **source** from the registry is
written to `widgets/{tag}.{hash}.js`. Islands hydrate identically either way.

### Editor — request a widget

The insert picker's *Widgets* group gains a footer action **"Request a new widget…"**
opening a modal: tag, name, description, placeholder, aspect-ratio, eager toggle, a
repeatable prop editor (name/label/type/default/options), and a bundle field (paste
the ES-module source). Submitting dispatches `SubmitWidget`. The editor shows the
submission as *pending* until approved; approved widgets then appear in the group like
built-ins.

### Admin — `/admin/widgets`

A review page (behind your reverse-proxy auth): pending submissions first, each showing
the tag, name, props, requester, and the **full bundle source in a monospace block for
review**, with **Approve** and **Reject (with reason)**. A clear warning states that
approving publishes runnable code to every visitor. Approved and rejected submissions
are listed below with their actor and timestamp. Reachable from a small "Widgets"
link in the editor status bar; the route is gated by `ImprintAdmin` (default on for the
demo) and documented as requiring external protection.

### Security summary

- Submitted JS never executes in the editor (canvas renders widget placeholders).
- Submitted JS reaches a visitor's browser **only after an admin approves** it; the
  admin reviews the exact source shown on the page.
- Submitted **metadata** (tag/props/placeholder) is validated on submit and re-checked
  at approve, and is HTML-attribute-encoded by the existing WidgetView when rendered.
- Tag collisions cannot shadow a built-in or an approved widget.
- The approved bundle bytes are immutable in the event log (audit trail).
