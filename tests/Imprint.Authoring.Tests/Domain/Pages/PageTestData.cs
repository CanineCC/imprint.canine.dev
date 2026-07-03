using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;

namespace Imprint.Authoring.Tests.Domain.Pages;

// Shared builders for Page tests. Every call mints fresh node ids, so tests never
// couple through shared identity — capture the returned nodes when ids matter.
internal static class PageTestData
{
    public static readonly Locale En = new("en");
    public static readonly Locale Da = new("da");

    public static Slug SlugOf(string value) =>
        Slug.TryCreate(value, out var slug, out var error)
            ? slug
            : throw new InvalidOperationException(error);

    public static PageCreated Created(PageId pageId, SiteId siteId) =>
        new(pageId, siteId, "about", En, "About");

    public static SectionNode Section(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    public static StackNode Stack(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    public static GridNode Grid(params Node[] children) =>
        new() { Id = NodeId.New(), Children = NodeList.Of(children) };

    public static ColumnsNode Columns(params int[] ratios) =>
        Columns(ratios, [.. ratios.Select(_ => (Node)Stack())]);

    public static ColumnsNode Columns(int[] ratios, IReadOnlyList<Node> cells) => new()
    {
        Id = NodeId.New(),
        Ratios = [.. ratios],
        Children = NodeList.Of(cells),
    };

    public static HeadingNode Heading(string text = "Hello") =>
        new() { Id = NodeId.New(), Text = LocalizedText.Of(En, text) };

    public static RichTextNode RichText(string html = "<p>Hi</p>") =>
        new() { Id = NodeId.New(), Html = LocalizedText.Of(En, html) };

    public static ButtonNode Button(string label = "Go") =>
        new() { Id = NodeId.New(), Label = LocalizedText.Of(En, label) };

    public static ImageNode Image() =>
        new() { Id = NodeId.New(), Alt = LocalizedText.Of(En, "A pier") };

    public static SvgNode Svg() =>
        new() { Id = NodeId.New(), Alt = LocalizedText.Of(En, "A logo") };

    public static SpacerNode Spacer() => new() { Id = NodeId.New() };

    public static DividerNode Divider() => new() { Id = NodeId.New() };

    public static BlockInstanceNode BlockInstance() =>
        new() { Id = NodeId.New(), DefinitionId = BlockDefinitionId.New() };

    // A section whose deepest stack sits at depth 1 + stackDepth — used to probe the
    // MaxDepth cap without a wall of nested object initializers in each test.
    public static (SectionNode Root, StackNode Deepest) NestedSection(int stackDepth)
    {
        var deepest = Stack();
        Node current = deepest;
        for (var i = 1; i < stackDepth; i++)
        {
            current = Stack(current);
        }

        return (Section(current), deepest);
    }
}
