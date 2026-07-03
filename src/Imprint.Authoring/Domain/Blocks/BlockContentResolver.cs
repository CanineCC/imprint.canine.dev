using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Domain.Blocks;

/// <summary>
/// Resolves a block definition's subtree with a placed instance's content overrides
/// applied. Shared by rendering (drawing instances), detach (materializing the
/// resolved tree onto the page) and update-definition-from-instance (pushing the
/// resolved tree back to the definition).
/// </summary>
public static class BlockContentResolver
{
    /// <summary>Applies overrides in place, keeping the definition's node ids.</summary>
    public static Node Resolve(Node definitionSpec, OverrideSet overrides)
    {
        if (overrides.Count == 0)
        {
            return definitionSpec;
        }

        return Walk(definitionSpec);

        Node Walk(Node node)
        {
            var updated = ApplyFields(node);
            if (updated is IContainerNode container)
            {
                var children = container.Children;
                for (var i = 0; i < children.Count; i++)
                {
                    children = children.SetItem(i, Walk(children[i]));
                }

                updated = container.WithChildren(children);
            }

            return updated;
        }

        Node ApplyFields(Node node) => node switch
        {
            HeadingNode heading => heading with { Text = Overlay(heading.Text, node.Id, "text") },
            RichTextNode richText => richText with { Html = Overlay(richText.Html, node.Id, "html") },
            ButtonNode button => button with { Label = Overlay(button.Label, node.Id, "label") },
            ImageNode image => image with { Alt = Overlay(image.Alt, node.Id, "alt") },
            SvgNode svg => svg with { Alt = Overlay(svg.Alt, node.Id, "alt") },
            _ => node,
        };

        LocalizedText Overlay(LocalizedText text, NodeId definitionNodeId, string field)
        {
            foreach (var entry in overrides.Entries)
            {
                if (entry.DefinitionNodeId == definitionNodeId && entry.Field == field)
                {
                    text = text.With(entry.Locale, entry.Value);
                }
            }

            return text;
        }
    }

    /// <summary>A deep copy with fresh node ids — for detaching an instance into plain page content.</summary>
    public static Node WithFreshIds(Node node)
    {
        var copy = node switch
        {
            IContainerNode container => (Node)container.WithChildren(
                RemapChildren(container.Children)),
            _ => node,
        };
        return copy with { Id = NodeId.New() };

        static NodeList RemapChildren(NodeList children)
        {
            var result = children;
            for (var i = 0; i < children.Count; i++)
            {
                result = result.SetItem(i, WithFreshIds(children[i]));
            }

            return result;
        }
    }
}
