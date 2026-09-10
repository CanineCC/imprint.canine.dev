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

    /// <summary>
    /// The corpus sheet's section rail is authored, not hard-coded: an island renders it because
    /// the page passed <c>kicker</c> (and, for the (i), <c>tip</c>). A widget whose bundle grew
    /// the attribute but whose descriptor never declared it renders the rail on a syndicated page
    /// and NOWHERE in the editor, because the inspector only offers the props listed here — the
    /// same silent half-shipped state the bundle guard below exists for.
    /// </summary>
    [Theory]
    [InlineData("cai-trend", "kicker", "tip", "figures")]
    [InlineData("cai-link-cards", "kicker", "tip")]
    [InlineData("cai-figure-band", "kicker")]
    [InlineData("cai-share-bars", "kicker")]
    public void The_sheet_widgets_declare_the_props_their_section_layout_reads(
        string tag,
        params string[] required)
    {
        var widget = Assert.Single(WidgetManifest.Load(ManifestPath()), w => w.Tag == tag);

        var declared = widget.Props.Select(p => p.Name).ToArray();
        var missing = required.Where(r => !declared.Contains(r)).ToArray();
        Assert.True(
            missing.Length == 0,
            $"widget '{tag}' renders its section layout from {string.Join(", ", required)} but its "
            + $"descriptor declares only {string.Join(", ", declared)} — the editor cannot author "
            + $"{string.Join(", ", missing)}.");
    }

    /// <summary>
    /// The four-things-by-hand guard. Adding a widget means a source file, a tag in
    /// <c>widgets/_src/build.sh</c>, a BUILT bundle in <c>widgets/</c>, and a descriptor here —
    /// and until this test, nothing checked the middle two. A descriptor whose bundle was never
    /// built, or a bundle that defines a different tag than the descriptor claims, fails in the
    /// worst possible way: a syndicated page does not validate its tags against the manifest, so
    /// the element is stored happily and renders NOTHING in the static output. No error, no
    /// placeholder, no 404 in the log the author will ever see — just an empty space where the
    /// data was. Assert the bundle is on disk and that it actually registers the tag it is
    /// filed under.
    /// </summary>
    [Fact]
    public void Every_descriptor_has_a_built_bundle_that_registers_its_own_tag()
    {
        var manifestPath = ManifestPath();
        var widgetsDir = Path.GetDirectoryName(manifestPath)!;
        var widgets = WidgetManifest.Load(manifestPath);

        var missing = new List<string>();
        var unregistered = new List<string>();

        foreach (var widget in widgets)
        {
            var bundlePath = Path.Combine(widgetsDir, widget.Bundle);
            if (!File.Exists(bundlePath))
            {
                missing.Add($"{widget.Tag} -> {widget.Bundle}");
                continue;
            }

            // esbuild keeps the tag as a string literal and may quote it either way.
            var text = File.ReadAllText(bundlePath);
            var registersTag =
                text.Contains($"customElements.define(\"{widget.Tag}\"", StringComparison.Ordinal) ||
                text.Contains($"customElements.define('{widget.Tag}'", StringComparison.Ordinal);
            if (!registersTag)
            {
                unregistered.Add($"{widget.Tag} -> {widget.Bundle}");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"widgets/manifest.json names {missing.Count} bundle(s) that are not on disk — run widgets/_src/build.sh for each tag: {string.Join(", ", missing)}");
        Assert.True(
            unregistered.Count == 0,
            $"{unregistered.Count} shipped bundle(s) never call customElements.define for the tag they are filed under, so the element renders nothing: {string.Join(", ", unregistered)}");
    }
}
