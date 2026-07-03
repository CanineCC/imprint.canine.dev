namespace Imprint.Authoring.Features.Pages;

/// <summary>
/// Port for widget manifest lookups. The manifest itself (widgets/manifest.json and
/// its schema) belongs to the delivery side; slices only need to answer "is this tag
/// real and are these prop names declared" before letting a WidgetNode onto a page.
/// The aggregate stays manifest-blind — a page whose widget later disappears from the
/// manifest still folds fine and simply renders nothing in the static output.
/// </summary>
public interface IWidgetCatalog
{
    bool Exists(string tag);

    /// <summary>Declared prop names for a tag; empty when the tag is unknown.</summary>
    IReadOnlySet<string> PropNames(string tag);

    /// <summary>
    /// True when the tag belongs to a built-in (filesystem) widget rather than an
    /// approved submission. The widget-submission slice uses it to reject a submitted
    /// tag that would shadow a built-in (a built-in can never be shadowed). The default
    /// treats every known tag as built-in — correct for a manifest-only catalog and for
    /// test fakes; the merged editor catalog overrides it to separate the two sources.
    /// </summary>
    bool IsBuiltInTag(string tag) => Exists(tag);
}
