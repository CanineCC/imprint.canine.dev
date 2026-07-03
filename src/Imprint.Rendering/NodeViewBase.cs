using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Microsoft.AspNetCore.Components;

namespace Imprint.Rendering;

/// <summary>
/// Shared plumbing for the node views: the cascaded <see cref="RenderContext"/>, the
/// editor-mode selection attributes, locale resolution and size-hint access. Views stay
/// declarative; the mode split lives in one place.
/// </summary>
public abstract class NodeViewBase<TNode> : ComponentBase where TNode : Node
{
    private static readonly IReadOnlyDictionary<string, object> NoAttributes = new Dictionary<string, object>();

    [CascadingParameter] public RenderContext Ctx { get; set; } = default!;

    // Set by BlockInstanceView: selection must route to the whole instance because
    // inner nodes carry the *definition's* ids, which are not addressable on this page.
    [CascadingParameter(Name = "SuppressNodeIds")] public bool SuppressNodeIds { get; set; }

    [CascadingParameter] public SizeHints? Hints { get; set; }

    [Parameter, EditorRequired] public TNode Node { get; set; } = default!;

    protected SizeHints CurrentHints => Hints ?? SizeHints.Root;

    protected bool IsEditor => Ctx.Mode == RenderMode.Editor;

    /// <summary>
    /// The editor-plane selection contract: every node view's root element carries
    /// <c>data-node-id</c>/<c>data-node-type</c> in editor mode and neither in static
    /// mode — published markup stays free of editor residue.
    /// </summary>
    protected IReadOnlyDictionary<string, object> NodeAttributes =>
        IsEditor && !SuppressNodeIds
            ? new Dictionary<string, object>
            {
                ["data-node-id"] = Node.Id.Compact,
                ["data-node-type"] = NodeTypeNames.Of(Node),
            }
            : NoAttributes;

    protected string Resolve(LocalizedText text) => text.Resolve(Ctx.Locale, Ctx.DefaultLocale);

    protected static string Classes(params string?[] parts) =>
        string.Join(' ', parts.Where(part => !string.IsNullOrEmpty(part)));

    protected static string? GapClass(Gap gap) => gap switch
    {
        Gap.Tight => "ip-gap-tight",
        Gap.Loose => "ip-gap-loose",
        _ => null,
    };
}
