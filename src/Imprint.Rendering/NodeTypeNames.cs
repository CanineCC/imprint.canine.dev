using Imprint.Authoring.Domain.Pages;

namespace Imprint.Rendering;

/// <summary>
/// Stable node type names for <c>data-node-type</c> attributes. These must equal the
/// JsonDerivedType discriminators on <see cref="Node"/> — the editor's JS, the
/// serialized tree and the DOM all speak the same dialect, so selection routing never
/// needs a translation table. A test asserts the two lists cannot drift.
/// </summary>
public static class NodeTypeNames
{
    public static string Of(Node node) => node switch
    {
        SectionNode => "section",
        StackNode => "stack",
        ColumnsNode => "columns",
        GridNode => "grid",
        HeadingNode => "heading",
        RichTextNode => "richtext",
        ButtonNode => "button",
        ImageNode => "image",
        VideoNode => "video",
        SvgNode => "svg",
        DividerNode => "divider",
        SpacerNode => "spacer",
        WidgetNode => "widget",
        BlockInstanceNode => "block-instance",
        // Node is a closed union; reaching this means a new node type was added
        // without a view — fail loudly rather than render silently wrong.
        _ => throw new InvalidOperationException($"No stable type name for node type {node.GetType().Name}."),
    };
}
