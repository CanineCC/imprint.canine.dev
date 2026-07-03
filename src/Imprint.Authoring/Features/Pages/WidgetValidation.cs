using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages;

/// <summary>
/// Handler-side widget manifest checks, shared by the slices that let node specs onto
/// a page (<c>AddNode</c>, <c>ChangeNodeProps</c>). The Page aggregate is deliberately
/// manifest-blind (see <see cref="IWidgetCatalog"/>): "is this tag real, are these
/// prop names declared" is a slice concern, answered here with human messages.
/// </summary>
internal static class WidgetValidation
{
    /// <summary>Validates every <see cref="WidgetNode"/> inside <paramref name="spec"/> against the catalog.</summary>
    public static Result CheckWidgets(this IWidgetCatalog catalog, Node spec)
    {
        foreach (var node in PageTree.Flatten(spec))
        {
            if (node is not WidgetNode widget)
            {
                continue;
            }

            if (!catalog.Exists(widget.Tag))
            {
                return Result.Fail($"There is no '{widget.Tag}' widget installed.");
            }

            var declared = catalog.PropNames(widget.Tag);
            foreach (var (name, _) in widget.Props)
            {
                if (!declared.Contains(name))
                {
                    return Result.Fail($"The '{widget.Tag}' widget has no '{name}' setting.");
                }
            }
        }

        return Result.Ok();
    }
}
