using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Rendering;

public enum RenderMode
{
    /// <summary>Published output: no editor attributes, hashed asset URLs, island markup.</summary>
    Static,

    /// <summary>Editor canvas: every node carries <c>data-node-id</c>/<c>data-node-type</c>; widgets render as placeholders.</summary>
    Editor,
}

/// <summary>
/// Everything a node view needs to render, supplied as a cascading value. The editor
/// and the publisher construct different contexts; the components cannot tell — that is
/// the pixel-identical-preview guarantee.
/// </summary>
public sealed record RenderContext
{
    public required RenderMode Mode { get; init; }
    public required Locale Locale { get; init; }
    public required Locale DefaultLocale { get; init; }

    /// <summary>Null when the asset is unknown or not publishable — views render an editor placeholder / skip in static.</summary>
    public required Func<AssetId, AssetRenderInfo?> ResolveAsset { get; init; }

    /// <summary>Public path of a published page for links (<c>/</c>, <c>/about/</c>, <c>/da/om/</c>). Null → link renders as plain text.</summary>
    public required Func<PageId, string?> ResolvePagePath { get; init; }

    /// <summary>Resolves a block definition's subtree for instance rendering. Null → instance renders nothing (definition deleted).</summary>
    public required Func<BlockDefinitionId, Node?> ResolveBlock { get; init; }

    /// <summary>Widget manifest lookup. Null → unknown widget: placeholder in editor, omitted in static.</summary>
    public required Func<string, WidgetDescriptor?> ResolveWidget { get; init; }

    /// <summary>Public URL of a widget's ES-module bundle (hashed); static mode only.</summary>
    public Func<string, string?>? ResolveWidgetBundle { get; init; }

    /// <summary>
    /// The publish-time bake for a widget instance, keyed by the URL its
    /// <see cref="WidgetDescriptor.Prerender"/> template resolved to. Null (or a null result) means
    /// nothing was baked — the view renders exactly as it did before. Static mode only: the editor
    /// canvas shows the live island, so a bake there would be a second copy of the same data.
    /// </summary>
    public Func<string, string?>? ResolvePrerendered { get; init; }

    /// <summary>
    /// Renders a widget from its OWN props, for a descriptor that names a
    /// <see cref="WidgetDescriptor.PropTemplate"/>. Given the template name and a lookup over the
    /// instance's declared props, returns this site's markup — or null, which leaves the widget
    /// exactly as it was.
    /// </summary>
    /// <remarks>
    /// ★ A SEPARATE SEAM FROM <see cref="ResolvePrerendered"/> BECAUSE THE TIMING IS DIFFERENT, not
    /// because the output is. A bake is fetched and cached by (url, template) at publish time, which
    /// is why that cache cannot see props — one fragment serves every instance reading that URL. A
    /// prop-driven widget has nothing to share and no fetch to amortise, so it renders here, per
    /// instance, with the props in hand. Static mode only, for the same reason: the editor canvas
    /// shows the live island.
    /// </remarks>
    public Func<string, Func<string, string?>, string?>? RenderFromProps { get; init; }

    /// <summary>
    /// Every in-page id already emitted on this page, so a heading can tell whether its
    /// slug is taken. Deliberately mutable inside an otherwise immutable context: ids are
    /// unique per rendered document, and a context is built per page per locale, so this
    /// set has exactly the right lifetime. Sections register their anchors as they render,
    /// which is what stops a heading from stealing an anchor someone already links to.
    /// </summary>
    public HashSet<string> UsedAnchors { get; init; } = [];
}

/// <summary>
/// Render-ready facts about an asset (URLs are already correct for the current plane).
/// The <c>Dark*</c> fields are populated only when the asset has an optional dark-mode
/// variant; when present, image/svg views emit both renditions and let CSS pick by
/// colour scheme (docs/proposals/theme-media-and-widget-approval.md). They are empty/
/// null for a neutral asset, so the single-rendition path is unchanged.
/// </summary>
public sealed record AssetRenderInfo(
    AssetKind Kind,
    AssetStatus Status,
    string Url,
    IReadOnlyList<ImageSource> ImageVariants,
    int? IntrinsicWidth,
    int? IntrinsicHeight,
    string? InlineSvg,
    LocalizedText DefaultAlt)
{
    /// <summary>
    /// The uploaded file itself, published unconverted — set only for assets that asked
    /// for it. Derived variants are WebP, which browsers all accept but several link
    /// scrapers do not, so anything consumed by a third-party fetcher rather than a
    /// browser needs the boring original. Null when it was not requested.
    /// </summary>
    public string? OriginalUrl { get; init; }

    /// <summary>Dark-mode image sources; empty when the asset is neutral.</summary>
    public IReadOnlyList<ImageSource> DarkImageVariants { get; init; } = [];

    public int? DarkIntrinsicWidth { get; init; }
    public int? DarkIntrinsicHeight { get; init; }

    /// <summary>Sanitized dark-mode inline SVG; null when neutral.</summary>
    public string? DarkInlineSvg { get; init; }

    /// <summary>The chosen dark <c>src</c> (middle image variant / video / file); null when neutral.</summary>
    public string? DarkUrl { get; init; }

    /// <summary>True when a usable dark-mode rendition exists for this asset.</summary>
    public bool HasDark => DarkImageVariants.Count > 0 || DarkInlineSvg is not null || DarkUrl is not null;
}

public sealed record ImageSource(string Url, int Width, int Height);
