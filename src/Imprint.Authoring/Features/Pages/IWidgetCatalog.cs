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
}
