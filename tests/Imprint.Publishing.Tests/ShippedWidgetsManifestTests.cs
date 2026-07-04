using Imprint.Rendering;

namespace Imprint.Publishing.Tests;

/// <summary>
/// Guards the widgets/ directory that actually ships with the repo: every built-in
/// manifest entry must load through the real <see cref="WidgetManifest"/> validation
/// (valid custom-element tag, and prop names that clear the reserved-attribute denylist
/// — no on*/style/data-island* that would become live handlers on a visitor's page).
/// Without this, a malformed shipped manifest fails only at editor startup.
/// </summary>
public sealed class ShippedWidgetsManifestTests
{
    private static string ManifestPath()
    {
        // Walk up from the test bin dir to the repo's widgets/manifest.json — the same
        // discovery the editor's ResolveWidgetsDirectory performs at startup.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "widgets", "manifest.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not locate the repo's widgets/manifest.json from the test directory.");
    }

    [Fact]
    public void The_shipped_manifest_loads_and_every_prop_name_is_a_safe_attribute()
    {
        var widgets = WidgetManifest.Load(ManifestPath());

        Assert.NotEmpty(widgets);
        foreach (var widget in widgets)
        {
            Assert.True(WidgetManifest.IsValidTag(widget.Tag), $"invalid tag '{widget.Tag}'");
            foreach (var prop in widget.Props)
            {
                Assert.True(WidgetManifest.IsValidPropName(prop.Name),
                    $"widget '{widget.Tag}' declares a reserved/invalid prop name '{prop.Name}'");
            }
        }
    }

    [Fact]
    public void The_placeable_theme_toggle_widget_is_shipped_with_its_dials()
    {
        var widgets = WidgetManifest.Load(ManifestPath());

        var toggle = Assert.Single(widgets, w => w.Tag == "x-theme-toggle");
        Assert.True(toggle.Eager, "the theme toggle should hydrate eagerly");
        // The four dials the editor exposes as an inspector form.
        Assert.Equal(
            new[] { "variant", "size", "speed", "label" },
            toggle.Props.Select(p => p.Name).ToArray());
        var variant = Assert.Single(toggle.Props, p => p.Name == "variant");
        Assert.Equal(WidgetPropType.Choice, variant.Type);
        Assert.Contains("switch", variant.Options);
    }
}
