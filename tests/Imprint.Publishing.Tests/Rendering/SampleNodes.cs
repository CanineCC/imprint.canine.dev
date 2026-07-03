using System.Collections.Immutable;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// One fully-populated node per type plus the fixed asset/definition ids their
/// resolvers key on. Deterministic ids keep failure output readable.
/// </summary>
internal static class SampleNodes
{
    public static readonly AssetId ImageAssetId = AssetId.From(new Guid("00000000-0000-0000-0000-0000000000a1"));
    public static readonly AssetId VideoAssetId = AssetId.From(new Guid("00000000-0000-0000-0000-0000000000a2"));
    public static readonly AssetId SvgAssetId = AssetId.From(new Guid("00000000-0000-0000-0000-0000000000a3"));
    public static readonly BlockDefinitionId DefinitionId = BlockDefinitionId.From(new Guid("00000000-0000-0000-0000-0000000000b1"));
    public static readonly NodeId DefinitionHeadingId = NodeId.From(new Guid("00000000-0000-0000-0000-0000000000c1"));
    public static readonly PageId LinkedPageId = PageId.From(new Guid("00000000-0000-0000-0000-0000000000d1"));

    public static SectionNode Section(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    public static StackNode Stack(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    public static ColumnsNode Columns(int[] ratios, params Node[] cells) =>
        new() { Id = NodeId.New(), Ratios = [.. ratios], Children = NodeList.Of(cells) };

    public static GridNode Grid(int minItemPx, params Node[] children) =>
        new() { Id = NodeId.New(), MinItemPx = minItemPx, Children = NodeList.Of(children) };

    public static HeadingNode Heading(string text = "Welcome", int level = 2) =>
        new() { Id = NodeId.New(), Level = level, Text = LocalizedText.Of(RenderHarness.En, text) };

    public static RichTextNode RichText(string canonicalHtml = "<p>Hello</p>") =>
        new() { Id = NodeId.New(), Html = LocalizedText.Of(RenderHarness.En, canonicalHtml) };

    public static ButtonNode Button(Link? linkTo, ButtonVariant variant = ButtonVariant.Primary, string label = "Go") =>
        new() { Id = NodeId.New(), Label = LocalizedText.Of(RenderHarness.En, label), LinkTo = linkTo, Variant = variant };

    public static ImageNode Image(string? alt = "A dog") => new()
    {
        Id = NodeId.New(),
        AssetId = ImageAssetId,
        Alt = alt is null ? LocalizedText.Empty : LocalizedText.Of(RenderHarness.En, alt),
    };

    public static VideoNode Video(VideoMode mode = VideoMode.Player) =>
        new() { Id = NodeId.New(), AssetId = VideoAssetId, Mode = mode };

    public static SvgNode Svg(string? alt = null, int? maxWidthPx = null) => new()
    {
        Id = NodeId.New(),
        AssetId = SvgAssetId,
        MaxWidthPx = maxWidthPx,
        Alt = alt is null ? LocalizedText.Empty : LocalizedText.Of(RenderHarness.En, alt),
    };

    public static WidgetNode Widget(params KeyValuePair<string, string>[] props) =>
        new() { Id = NodeId.New(), Tag = "x-countdown", Props = PropBag.Of(props) };

    public static BlockInstanceNode BlockInstance(OverrideSet? overrides = null) =>
        new() { Id = NodeId.New(), DefinitionId = DefinitionId, Overrides = overrides ?? OverrideSet.Empty };

    /// <summary>A block definition whose heading id is stable so overrides can target it.</summary>
    public static Node BlockDefinition() => new StackNode
    {
        Id = NodeId.From(new Guid("00000000-0000-0000-0000-0000000000c0")),
        Children = NodeList.Of(
            new HeadingNode { Id = DefinitionHeadingId, Level = 2, Text = LocalizedText.Of(RenderHarness.En, "Definition heading") },
            new RichTextNode { Id = NodeId.From(new Guid("00000000-0000-0000-0000-0000000000c2")), Html = LocalizedText.Of(RenderHarness.En, "<p>Definition body</p>") }),
    };

    public static WidgetDescriptor CountdownDescriptor(bool eager = false, string? aspectRatio = null) => new()
    {
        Tag = "x-countdown",
        Name = "Countdown",
        Bundle = "x-countdown.js",
        Placeholder = "Counting down…",
        Eager = eager,
        AspectRatio = aspectRatio,
        Props =
        [
            new WidgetProp { Name = "until", Label = "Until" },
            new WidgetProp { Name = "title", Label = "Title" },
        ],
    };

    /// <summary>One instance of every concrete node type, for the attribute-contract sweep.</summary>
    public static IReadOnlyList<Node> OneOfEachType() =>
    [
        Section(Heading()),
        Stack(Heading()),
        Columns([2, 1], Stack(Heading()), Stack()),
        Grid(280, Image()),
        Heading(),
        RichText(),
        Button(new ExternalLink("https://example.com/")),
        Image(),
        Video(),
        Svg(alt: "Logo"),
        new DividerNode { Id = NodeId.New() },
        new SpacerNode { Id = NodeId.New(), Size = SpacerSize.Large },
        Widget(new KeyValuePair<string, string>("until", "2027-01-01")),
        BlockInstance(),
    ];
}
