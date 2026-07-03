using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The closed union of everything that can appear on a page. There is deliberately no
/// "anonymous div": every node is a named concept a user can select, drag and edit —
/// the whole editor UX rests on that (docs/editor-ux.md §Kill the anonymous div).
/// Serialization uses built-in STJ polymorphism; the discriminators are stable names.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SectionNode), "section")]
[JsonDerivedType(typeof(StackNode), "stack")]
[JsonDerivedType(typeof(ColumnsNode), "columns")]
[JsonDerivedType(typeof(GridNode), "grid")]
[JsonDerivedType(typeof(HeadingNode), "heading")]
[JsonDerivedType(typeof(RichTextNode), "richtext")]
[JsonDerivedType(typeof(ButtonNode), "button")]
[JsonDerivedType(typeof(ImageNode), "image")]
[JsonDerivedType(typeof(VideoNode), "video")]
[JsonDerivedType(typeof(SvgNode), "svg")]
[JsonDerivedType(typeof(DividerNode), "divider")]
[JsonDerivedType(typeof(SpacerNode), "spacer")]
[JsonDerivedType(typeof(WidgetNode), "widget")]
[JsonDerivedType(typeof(BlockInstanceNode), "block-instance")]
public abstract record Node
{
    public required NodeId Id { get; init; }

    /// <summary>Human name shown in breadcrumbs, layer panel and drag chips.</summary>
    public abstract string DisplayName { get; }
}

/// <summary>A node that holds children. Placement rules live in <see cref="Placement"/>.</summary>
public interface IContainerNode
{
    NodeList Children { get; }
    Node WithChildren(NodeList children);
}

// ---------------------------------------------------------------------------- enums

[JsonConverter(typeof(JsonStringEnumConverter))] public enum SectionWidth { Normal, Wide, Full }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum SectionBackground { None, Surface, SurfaceAlt, Primary }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum SectionPadding { None, Normal, Large }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum Gap { Tight, Normal, Loose }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum StackAlign { Start, Center, End }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum CollapseBreakpoint { Px480 = 480, Px640 = 640, Px768 = 768 }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum SpacerSize { Small, Medium, Large }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ImageAspect { Natural, Square, Wide16x9, Portrait3x4 }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum VideoMode { Ambient, Player }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ButtonVariant { Primary, Secondary, Ghost }

// ----------------------------------------------------------------------- containers

public sealed record SectionNode : Node, IContainerNode
{
    public SectionWidth Width { get; init; } = SectionWidth.Normal;
    public SectionBackground Background { get; init; } = SectionBackground.None;
    public SectionPadding Padding { get; init; } = SectionPadding.Normal;
    public NodeList Children { get; init; } = NodeList.Empty;
    public override string DisplayName => "Section";
    public Node WithChildren(NodeList children) => this with { Children = children };
}

public sealed record StackNode : Node, IContainerNode
{
    public Gap Gap { get; init; } = Gap.Normal;
    public StackAlign Align { get; init; } = StackAlign.Start;
    public NodeList Children { get; init; } = NodeList.Empty;
    public override string DisplayName => "Stack";
    public Node WithChildren(NodeList children) => this with { Children = children };
}

/// <summary>
/// Columns hold exactly one implicit <see cref="StackNode"/> per column; ratios and
/// cell count always match. The collapse breakpoint drives a container query — this is
/// where "unbreakable on mobile" is enforced structurally.
/// </summary>
public sealed record ColumnsNode : Node, IContainerNode
{
    public required ImmutableArray<int> Ratios { get; init; }
    public CollapseBreakpoint CollapseBelow { get; init; } = CollapseBreakpoint.Px640;
    public Gap Gap { get; init; } = Gap.Normal;
    public NodeList Children { get; init; } = NodeList.Empty;
    public override string DisplayName => $"Columns ({Ratios.Length})";
    public Node WithChildren(NodeList children) => this with { Children = children };

    public bool Equals(ColumnsNode? other) =>
        other is not null && Id == other.Id && Ratios.SequenceEqual(other.Ratios) &&
        CollapseBelow == other.CollapseBelow && Gap == other.Gap && Children.Equals(other.Children);

    public override int GetHashCode() => HashCode.Combine(Id, Ratios.Length, CollapseBelow, Gap, Children);
}

public sealed record GridNode : Node, IContainerNode
{
    public int MinItemPx { get; init; } = 280;
    public Gap Gap { get; init; } = Gap.Normal;
    public NodeList Children { get; init; } = NodeList.Empty;
    public override string DisplayName => "Grid";
    public Node WithChildren(NodeList children) => this with { Children = children };
}

// -------------------------------------------------------------------- content nodes

public sealed record HeadingNode : Node
{
    public int Level { get; init; } = 2;
    public LocalizedText Text { get; init; } = LocalizedText.Empty;
    public override string DisplayName => $"Heading {Level}";
}

/// <summary>Body copy in the canonical inline subset — see <see cref="CanonicalHtml"/>.</summary>
public sealed record RichTextNode : Node
{
    public LocalizedText Html { get; init; } = LocalizedText.Empty;
    public override string DisplayName => "Text";
}

public sealed record ButtonNode : Node
{
    public LocalizedText Label { get; init; } = LocalizedText.Empty;
    public Link? LinkTo { get; init; }
    public ButtonVariant Variant { get; init; } = ButtonVariant.Primary;
    public override string DisplayName => "Button";
}

public sealed record ImageNode : Node
{
    public AssetId? AssetId { get; init; }
    public LocalizedText Alt { get; init; } = LocalizedText.Empty;
    public ImageAspect Aspect { get; init; } = ImageAspect.Natural;
    public bool Rounded { get; init; }
    public override string DisplayName => "Image";
}

public sealed record VideoNode : Node
{
    public AssetId? AssetId { get; init; }
    public VideoMode Mode { get; init; } = VideoMode.Player;
    public override string DisplayName => "Video";
}

/// <summary>Inline-embedded (sanitized) SVG, so it inherits <c>currentColor</c> from the theme.</summary>
public sealed record SvgNode : Node
{
    public AssetId? AssetId { get; init; }
    public int? MaxWidthPx { get; init; }
    public LocalizedText Alt { get; init; } = LocalizedText.Empty;
    public override string DisplayName => "Graphic";
}

public sealed record DividerNode : Node
{
    public override string DisplayName => "Divider";
}

public sealed record SpacerNode : Node
{
    public SpacerSize Size { get; init; } = SpacerSize.Medium;
    public override string DisplayName => "Spacer";
}

/// <summary>
/// An island: a web component whose tag and props are validated against the widget
/// manifest in the slice (the aggregate cannot see the manifest — accepted split).
/// </summary>
public sealed record WidgetNode : Node
{
    public required string Tag { get; init; }
    public PropBag Props { get; init; } = PropBag.Empty;
    public override string DisplayName => $"Widget <{Tag}>";
}

/// <summary>
/// A linked instance of a <c>BlockDefinition</c> ("symbol"): renders the definition's
/// subtree with per-instance, content-only overrides keyed by definition node ids.
/// </summary>
public sealed record BlockInstanceNode : Node
{
    public required BlockDefinitionId DefinitionId { get; init; }
    public OverrideSet Overrides { get; init; } = OverrideSet.Empty;
    public override string DisplayName => "Block";
}

// ------------------------------------------------------------------------- links

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PageLink), "page")]
[JsonDerivedType(typeof(ExternalLink), "external")]
public abstract record Link;

public sealed record PageLink(PageId PageId) : Link;

public sealed record ExternalLink(string Url) : Link;
