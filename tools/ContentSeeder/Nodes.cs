using System.Collections.Immutable;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace ContentSeeder;

/// <summary>
/// Small, deterministic builders for Imprint page nodes. Every node gets a fresh
/// <see cref="NodeId"/>. Text goes in as the "en" locale (the corpus is EN-only).
/// These keep the block→node mapper readable and guarantee well-formed subtrees
/// (Sections only at root, Stacks/Grids/Columns inside, leaves at the edges).
/// </summary>
public static class Nodes
{
    public static readonly Locale En = new("en");
    public static readonly Locale Da = new("da");

    private const int HeadingMax = 500; // Page.MaxTextLength / NodeContentRules
    private const int PlainMax = 500;   // headings + button labels are plain, capped at 500

    public static LocalizedText Text(string value) => LocalizedText.Of(En, value);

    public static SectionNode Section(params Node[] children) =>
        Section(SectionBackground.None, SectionAppearance.Plain, children);

    public static SectionNode Section(SectionBackground background, params Node[] children) =>
        Section(background, SectionAppearance.Plain, children);

    /// <summary>A root section carrying its marketing block appearance (the ip-ap-* hook).</summary>
    public static SectionNode Section(SectionBackground background, SectionAppearance appearance, params Node[] children) => new()
    {
        Id = NodeId.New(),
        Width = SectionWidth.Normal,
        Background = background,
        Padding = SectionPadding.Normal,
        Appearance = appearance,
        Children = NodeList.Of(children),
    };

    public static StackNode Stack(params Node[] children) => new()
    {
        Id = NodeId.New(),
        Gap = Gap.Normal,
        Align = StackAlign.Start,
        Children = NodeList.Of(children),
    };

    public static GridNode Grid(int minItemPx, params Node[] children) => new()
    {
        Id = NodeId.New(),
        MinItemPx = Math.Clamp(minItemPx, 160, 480),
        Gap = Gap.Normal,
        Children = NodeList.Of(children),
    };

    /// <summary>Columns with N equal cells; each cell is a Stack holding that column's content.</summary>
    public static ColumnsNode Columns(IReadOnlyList<StackNode> cells)
    {
        var count = Math.Clamp(cells.Count, 2, 4);
        var ratios = Enumerable.Repeat(1, count).ToImmutableArray();
        return new ColumnsNode
        {
            Id = NodeId.New(),
            Ratios = ratios,
            CollapseBelow = CollapseBreakpoint.Px768,
            Gap = Gap.Normal,
            Children = NodeList.Of([.. cells.Take(count)]),
        };
    }

    public static HeadingNode Heading(int level, string text) => new()
    {
        Id = NodeId.New(),
        Level = Math.Clamp(level, 1, 4),
        Text = Text(Clamp(text, HeadingMax)),
    };

    /// <summary>A RichText node from already-canonical HTML (must be valid canonical grammar).</summary>
    public static RichTextNode RichHtml(string canonicalHtml) => new()
    {
        Id = NodeId.New(),
        Html = Text(canonicalHtml),
    };

    /// <summary>A single-paragraph RichText from CMS inline markup (resolved to canonical HTML).</summary>
    public static RichTextNode Paragraph(string? cmsInline, string origin) =>
        RichHtml(Inline.ToParagraph(cmsInline, origin));

    public static ButtonNode Button(string label, string? cmsHref, string origin, ButtonVariant variant)
    {
        Link? link = null;
        if (!string.IsNullOrWhiteSpace(cmsHref))
        {
            var resolved = Inline.ResolveHref(cmsHref!.Trim(), origin);
            if (resolved is not null)
            {
                link = new ExternalLink(resolved);
            }
        }

        return new ButtonNode
        {
            Id = NodeId.New(),
            Label = Text(Clamp(label, PlainMax)),
            LinkTo = link,
            Variant = variant,
        };
    }

    public static WidgetNode Widget(string tag, IReadOnlyDictionary<string, string> props) => new()
    {
        Id = NodeId.New(),
        Tag = tag,
        Props = PropBag.Of(props),
    };

    /// <summary>Plain text is capped at 500 chars for headings/labels; copy is never otherwise altered.</summary>
    public static string Clamp(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
