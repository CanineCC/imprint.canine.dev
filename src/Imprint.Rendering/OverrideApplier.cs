using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Rendering;

/// <summary>
/// Overlays a block instance's content overrides onto its definition subtree at render
/// time. Only the four locale-valued fields are overridable — structure always comes
/// from the definition, which is the entire point of a symbol. Overrides addressing
/// nodes the definition no longer has (or fields the node type lacks) are ignored, per
/// docs/domain-model.md §4: definitions evolve independently of their instances.
/// </summary>
public static class OverrideApplier
{
    public static Node Apply(Node definitionRoot, OverrideSet overrides)
    {
        if (overrides.Count == 0)
        {
            return definitionRoot;
        }

        var byNode = overrides.Entries.ToLookup(e => e.DefinitionNodeId);
        return Walk(definitionRoot, byNode);
    }

    private static Node Walk(Node node, ILookup<NodeId, OverrideSet.Entry> overrides)
    {
        foreach (var entry in overrides[node.Id])
        {
            node = ApplyField(node, entry);
        }

        if (node is IContainerNode container && container.Children.Count > 0)
        {
            var children = container.Children;
            for (var i = 0; i < children.Count; i++)
            {
                var updated = Walk(children[i], overrides);
                if (!ReferenceEquals(updated, children[i]))
                {
                    children = children.SetItem(i, updated);
                }
            }

            if (!ReferenceEquals(children, container.Children))
            {
                node = container.WithChildren(children);
            }
        }

        return node;
    }

    private static Node ApplyField(Node node, OverrideSet.Entry entry) => (node, entry.Field) switch
    {
        (HeadingNode heading, "text") => heading with { Text = heading.Text.With(entry.Locale, entry.Value) },
        // html override values were validated against the canonical grammar by the
        // Page aggregate when the override was set — safe to splice in unchecked here.
        (RichTextNode richText, "html") => richText with { Html = richText.Html.With(entry.Locale, entry.Value) },
        (ButtonNode button, "label") => button with { Label = button.Label.With(entry.Locale, entry.Value) },
        (ImageNode image, "alt") => image with { Alt = image.Alt.With(entry.Locale, entry.Value) },
        (SvgNode svg, "alt") => svg with { Alt = svg.Alt.With(entry.Locale, entry.Value) },
        _ => node,
    };
}
