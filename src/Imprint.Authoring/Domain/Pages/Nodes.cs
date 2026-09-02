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
[JsonDerivedType(typeof(CodeNode), "code")]
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

/// <summary>
/// The visual role a section plays — the one dimension that lets a Section reproduce a
/// named marketing "block appearance" (a CMS <c>_template</c>) without inventing a new
/// node type per block. <see cref="Plain"/> is the structural default (emits no extra
/// class); every other value emits <c>ip-ap-{kebab-name}</c>, which the marketing theme
/// keys its chrome CSS off. The set is the shared contract between the block seeder, the
/// renderer and the marketing stylesheet — one value per CMS block template.
/// A value the reader does not know reads back as <see cref="Plain"/> (forward-compatible).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SectionAppearance
{
    Plain,
    Hero,
    Boundary,
    FeatureGrid,
    StatBand,
    Personas,
    Steps,
    Panels,
    Pricing,
    Composition,
    TableList,
    Docmock,
    Note,
    Cta,
    Flow,
    BandScale,
    Gallery,
    LiveCard,
    Contact,
    C4Heat,
    Findings,
    // Long-form marketing prose (canine's manifesto/doctrine sections): a measure-width
    // reading column with kicker-eyebrow, hairline-list and pull-quote chrome.
    Prose,
    // Date/body rows (canine's "How we got here"): a mono accent date column beside each
    // item's body, hairline-separated.
    Timeline,
    // Long-form legal / prose document (canine's .mk-doc): a measure-width, centered
    // reading column. Not a CMS block template — it's the marketing look for a whole
    // markdown page — but part of the same appearance vocabulary so any page reproduces.
    Doc,
}
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

    /// <summary>
    /// The named appearance this section plays (marketing block role). Defaults to
    /// <see cref="SectionAppearance.Plain"/>; a value missing from persisted JSON reads
    /// back as Plain, so older streams stay renderable.
    /// </summary>
    public SectionAppearance Appearance { get; init; } = SectionAppearance.Plain;

    /// <summary>
    /// Optional in-page anchor: the <c>id</c> the published section carries so header
    /// links like <c>/#products</c> land on it. Additive on the node contract — absent
    /// in persisted JSON reads back as null (no id emitted). The renderer sanitizes via
    /// <see cref="SectionAnchor"/> before emitting, so a stored value can never produce
    /// a broken or unsafe attribute.
    /// </summary>
    public string? Anchor { get; init; }

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

/// <summary>
/// A block of literal code, rendered verbatim in a monospace block.
/// <para>
/// The canonical inline subset (<see cref="CanonicalHtml"/>) has no <c>&lt;pre&gt;</c> or
/// <c>&lt;code&gt;</c> and deliberately keeps none: it is a grammar for PROSE, where every
/// character is markup or text. Code is the opposite — whitespace is significant, and
/// <c>&lt;</c> and <c>&amp;</c> are ordinary characters a reader must see. Squeezing it into
/// rich text would mean entity-encoding at write time and hoping every later consumer
/// decodes identically. A node of its own stores the code as it was written.
/// </para>
/// <para>
/// <b>Not localized, on purpose.</b> Every other text field on a page is
/// <see cref="LocalizedText"/> because prose is written per language; a code sample is the
/// same characters in every language, and giving it per-locale values would invite
/// translations that drift from the program they claim to be. The surrounding prose
/// explains it; the code itself is one artefact.
/// </para>
/// </summary>
public sealed record CodeNode : Node
{
    /// <summary>The code exactly as authored: no trimming beyond the fence, no re-indentation.</summary>
    public required string Text { get; init; }

    /// <summary>
    /// An informational language tag (<c>csharp</c>, <c>bash</c>, …) or null. It becomes a
    /// <c>language-*</c> class so a future highlighting island can find it, and nothing more —
    /// the delivery contract is zero framework JavaScript, so the published page never
    /// highlights on its own. Constrained to <see cref="IsValidLanguage"/> because it is
    /// written into a class attribute.
    /// </summary>
    public string? Language { get; init; }

    public override string DisplayName => Language is { Length: > 0 } lang ? $"Code ({lang})" : "Code";

    /// <summary>A conservative tag shape — letters, digits, <c>+</c>, <c>#</c>, <c>-</c> — so the
    /// value can be emitted into <c>class="language-…"</c> without escaping questions.</summary>
    public static bool IsValidLanguage(string? language) =>
        language is null ||
        (language.Length is > 0 and <= 24 &&
         language.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '#' or '-'));
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
[JsonDerivedType(typeof(AssetLink), "asset")]
public abstract record Link;

/// <summary>
/// A same-site page, resolved to the reader's own locale at render time. <see cref="Fragment"/>
/// optionally narrows it to one section of that page — the only correct way to put "Independence"
/// in a header when the section lives on the front page: an absolute URL to <c>/#independence</c>
/// would send a Danish reader to the English front page, and a bare <c>#independence</c> only
/// works while you are already standing on it.
/// <para>The value is sanitized by <see cref="SectionAnchor"/>, the same gate a section's own
/// anchor passes, so a link can only ever name an id a section could actually carry — and an
/// unusable one reads back as no fragment at all rather than as a broken href.</para>
/// </summary>
public sealed record PageLink(PageId PageId, string? Fragment = null) : Link
{
    private readonly string? _fragment = SectionAnchor.Sanitize(Fragment);

    // Sanitizing in the accessor as well as at construction covers `with { Fragment = … }`
    // too — an object initializer writes the property directly, so a constructor-only gate
    // would let the editor's one-field edit slip an unusable anchor past it.
    public string? Fragment
    {
        get => _fragment;
        init => _fragment = SectionAnchor.Sanitize(value);
    }

    /// <summary>
    /// The link's href, given the page's path in the reader's locale: the path itself, with the
    /// section appended when there is one. Null in, null out — an unresolvable page stays
    /// unresolvable, which is what tells every caller to drop the link rather than emit a dead one.
    /// </summary>
    public string? Href(string? pagePath) =>
        pagePath is null || Fragment is null ? pagePath : $"{pagePath}#{Fragment}";
}

public sealed record ExternalLink(string Url) : Link;

/// <summary>
/// An uploaded file offered to the reader — a whitepaper PDF, a press kit. Stored as the
/// asset reference (never a media path) because the editor and the published site serve
/// the same bytes from different URLs; each plane resolves the reference through its own
/// <c>ResolveAsset</c>, and the publisher ships the file precisely because a page links it
/// (see <see cref="AssetHref"/>). An unresolvable asset degrades exactly like a deleted
/// page: the link renders as plain text, never as a dead href.
/// </summary>
public sealed record AssetLink(AssetId AssetId) : Link;
