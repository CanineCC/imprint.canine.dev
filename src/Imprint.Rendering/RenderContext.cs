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
}

/// <summary>Render-ready facts about an asset (URLs are already correct for the current plane).</summary>
public sealed record AssetRenderInfo(
    AssetKind Kind,
    AssetStatus Status,
    string Url,
    IReadOnlyList<ImageSource> ImageVariants,
    int? IntrinsicWidth,
    int? IntrinsicHeight,
    string? InlineSvg,
    LocalizedText DefaultAlt);

public sealed record ImageSource(string Url, int Width, int Height);
