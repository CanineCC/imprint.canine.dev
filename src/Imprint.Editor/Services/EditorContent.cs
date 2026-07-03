using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Projections;
using Imprint.Rendering;

namespace Imprint.Editor.Services;

/// <summary>
/// Widget manifest as loaded once at startup from the repo's widgets directory.
/// Implements the authoring-side port so slices can validate WidgetNodes.
/// </summary>
public sealed class EditorWidgetCatalog : IWidgetCatalog
{
    private readonly Dictionary<string, WidgetDescriptor> _byTag;

    public EditorWidgetCatalog(string widgetsDirectory)
    {
        Descriptors = WidgetManifest.Load(Path.Combine(widgetsDirectory, "manifest.json"));
        _byTag = Descriptors.ToDictionary(d => d.Tag, StringComparer.Ordinal);
    }

    public IReadOnlyList<WidgetDescriptor> Descriptors { get; }

    public WidgetDescriptor? Find(string tag) => _byTag.GetValueOrDefault(tag);

    public bool Exists(string tag) => _byTag.ContainsKey(tag);

    public IReadOnlySet<string> PropNames(string tag) =>
        _byTag.TryGetValue(tag, out var descriptor)
            ? descriptor.Props.Select(p => p.Name).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>();
}

/// <summary>Builds the editor-plane <see cref="RenderContext"/>: same components, editor URLs.</summary>
public sealed class EditorRenderContextFactory(
    AssetLibrary assets,
    BlockLibrary blocks,
    PageList pages,
    SiteOverview site,
    EditorWidgetCatalog widgets,
    Authoring.Features.Assets.IMediaStore media)
{
    public RenderContext For(Locale locale)
    {
        var defaultLocale = site.Current?.DefaultLocale ?? locale;
        return new RenderContext
        {
            Mode = RenderMode.Editor,
            Locale = locale,
            DefaultLocale = defaultLocale,
            ResolveAsset = ResolveAsset,
            // Editor canvas links preview the real path but never navigate (the canvas
            // suppresses clicks); unpublished pages still resolve so editors see them.
            ResolvePagePath = pageId => pages.Get(pageId) is { } summary
                ? summary.IsHome ? "/" : $"/{summary.Slug}/"
                : null,
            ResolveBlock = blockId => blocks.Get(blockId)?.Spec,
            ResolveWidget = widgets.Find,
            ResolveWidgetBundle = _ => null, // islands never hydrate inside the canvas
        };
    }

    private AssetRenderInfo? ResolveAsset(AssetId id)
    {
        if (assets.Get(id) is not { } asset)
        {
            return null;
        }

        string? inlineSvg = null;
        if (asset is { Kind: AssetKind.Vector, Status: AssetStatus.Ready, DerivedStorageKey: { } svgKey })
        {
            // Resolver funcs are synchronous by contract; the sanitized SVG is a tiny
            // local file, so a direct read beats plumbing async through every view.
            inlineSvg = File.ReadAllText(media.PhysicalPathOf(svgKey));
        }

        var variants = asset.Variants
            .Select(v => new ImageSource($"/media/{v.StorageKey}", v.Width, v.Height))
            .ToList();
        var url = asset switch
        {
            { Kind: AssetKind.Image, Variants.Count: > 0 } =>
                variants[Math.Min(1, variants.Count - 1)].Url,
            { Kind: AssetKind.Video, DerivedStorageKey: { } webm } => $"/media/{webm}",
            _ => $"/media/{asset.OriginalStorageKey}",
        };

        var largest = asset.Variants.Count > 0 ? asset.Variants[^1] : null;
        var info = new AssetRenderInfo(
            asset.Kind, asset.Status, url, variants,
            largest?.Width, largest?.Height, inlineSvg, asset.DefaultAlt);

        // Optional dark rendition, resolved through the same media URLs so the canvas
        // previews it exactly as the published site will (pixel-identical preview).
        if (asset.HasDarkVariant)
        {
            if (asset is { Kind: AssetKind.Image } && asset.DarkVariants.Count > 0)
            {
                var darkVariants = asset.DarkVariants
                    .Select(v => new ImageSource($"/media/{v.StorageKey}", v.Width, v.Height))
                    .ToList();
                var darkLargest = asset.DarkVariants[^1];
                info = info with
                {
                    DarkImageVariants = darkVariants,
                    DarkIntrinsicWidth = darkLargest.Width,
                    DarkIntrinsicHeight = darkLargest.Height,
                    DarkUrl = darkVariants[Math.Min(1, darkVariants.Count - 1)].Url,
                };
            }
            else if (asset is { Kind: AssetKind.Vector, DarkDerivedStorageKey: { } darkSvgKey })
            {
                info = info with { DarkInlineSvg = File.ReadAllText(media.PhysicalPathOf(darkSvgKey)) };
            }
        }

        return info;
    }
}

/// <summary>Fresh default nodes for the insert picker — friendly starting values, never empty shells.</summary>
public static class NodeDefaults
{
    public sealed record Insertable(string TypeName, string Name, string Description, Func<Locale, Node> Create);

    public static readonly IReadOnlyList<Insertable> All =
    [
        new("heading", "Heading", "A title line (H1–H4).",
            locale => new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(locale, "New heading") }),
        new("richtext", "Text", "Paragraphs, lists, links.",
            locale => new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(locale, "<p>Write something…</p>") }),
        new("button", "Button", "A call-to-action link.",
            locale => new ButtonNode { Id = NodeId.New(), Label = LocalizedText.Of(locale, "Button") }),
        new("image", "Image", "A picture from your assets.",
            _ => new ImageNode { Id = NodeId.New() }),
        new("video", "Video", "WebM video, ambient or player.",
            _ => new VideoNode { Id = NodeId.New() }),
        new("svg", "Graphic", "An inline SVG that follows the theme.",
            _ => new SvgNode { Id = NodeId.New() }),
        new("stack", "Stack", "A vertical group.",
            _ => new StackNode { Id = NodeId.New() }),
        new("columns", "Columns", "Side-by-side columns that collapse on small screens.",
            _ => new ColumnsNode
            {
                Id = NodeId.New(),
                Ratios = [1, 1],
                Children = NodeList.Of(
                    new StackNode { Id = NodeId.New() },
                    new StackNode { Id = NodeId.New() }),
            }),
        new("grid", "Grid", "A responsive card grid.",
            _ => new GridNode { Id = NodeId.New() }),
        new("divider", "Divider", "A horizontal rule.",
            _ => new DividerNode { Id = NodeId.New() }),
        new("spacer", "Spacer", "Vertical breathing room.",
            _ => new SpacerNode { Id = NodeId.New() }),
        new("section", "Section", "A full-width band of the page.",
            _ => new SectionNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new StackNode { Id = NodeId.New() }),
            }),
    ];

    public static Node CreateWidget(WidgetDescriptor descriptor) => new WidgetNode
    {
        Id = NodeId.New(),
        Tag = descriptor.Tag,
        Props = PropBag.Of(descriptor.Props
            .Where(p => p.Default is not null)
            .Select(p => new KeyValuePair<string, string>(p.Name, p.Default!))),
    };
}
