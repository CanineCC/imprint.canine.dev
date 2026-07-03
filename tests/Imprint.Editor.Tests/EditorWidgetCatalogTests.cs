using System.Collections.Immutable;
using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.Editor.Services;
using Imprint.Rendering;

namespace Imprint.Editor.Tests;

/// <summary>
/// The merged widget catalog: built-in (filesystem manifest) ∪ approved (registry)
/// widgets, with built-in tags winning collisions, and the WidgetPropSpec → WidgetProp
/// mapping the editor and publisher share. (The submit → approve write path and the
/// registry folding are integration/E2E concerns owned elsewhere.)
/// </summary>
public sealed class EditorWidgetCatalogTests : IDisposable
{
    private readonly string _widgetsDir;

    public EditorWidgetCatalogTests()
    {
        _widgetsDir = Directory.CreateTempSubdirectory("imprint-catalog-").FullName;
        // Two built-ins: "x-built" and "x-shared" (the collision partner below).
        File.WriteAllText(Path.Combine(_widgetsDir, "manifest.json"), """
        [
          {
            "tag": "x-built",
            "name": "Built widget",
            "bundle": "x-built.js",
            "props": [ { "name": "caption", "label": "Caption", "type": "text", "default": "hi" } ]
          },
          {
            "tag": "x-shared",
            "name": "Built shared",
            "bundle": "x-shared.js",
            "props": [ { "name": "only-builtin", "label": "Only built-in", "type": "text" } ]
          }
        ]
        """);
    }

    public void Dispose() => Directory.Delete(_widgetsDir, recursive: true);

    private EditorWidgetCatalog Catalog(WidgetRegistry registry) => new(_widgetsDir, registry);

    private static WidgetRegistry RegistryWith(params WidgetSubmissionView[] submissions)
    {
        var registry = new WidgetRegistry();
        registry.Seed(submissions);
        return registry;
    }

    private static WidgetSubmissionView Submission(
        string tag,
        WidgetStatus status,
        params WidgetPropSpec[] props) => new()
    {
        Id = WidgetSubmissionId.New(),
        Tag = tag,
        Name = $"{tag} name",
        RequestedBy = "alice",
        Status = status,
        Props = props,
        BundleSource = $"/* {tag} */",
    };

    [Fact]
    public void Approved_tag_Exists_and_reports_its_prop_names()
    {
        var registry = RegistryWith(Submission(
            "x-approved", WidgetStatus.Approved,
            new WidgetPropSpec("mode", "Mode", "choice", "a", ["a", "b"]),
            new WidgetPropSpec("count", "Count", "number", null, [])));
        var catalog = Catalog(registry);

        Assert.True(catalog.Exists("x-approved"));
        Assert.False(catalog.IsBuiltInTag("x-approved"));
        Assert.Equal(new HashSet<string> { "mode", "count" }, catalog.PropNames("x-approved"));
    }

    [Fact]
    public void Pending_and_unknown_tags_do_not_exist()
    {
        var registry = RegistryWith(Submission("x-pending", WidgetStatus.Pending));
        var catalog = Catalog(registry);

        Assert.False(catalog.Exists("x-pending"));   // only APPROVED widgets join the catalog
        Assert.False(catalog.Exists("x-missing"));
        Assert.Null(catalog.Find("x-pending"));
    }

    [Fact]
    public void Built_in_wins_a_tag_collision_with_an_approved_submission()
    {
        // An approved submission tries to reuse the built-in tag "x-shared".
        var registry = RegistryWith(Submission(
            "x-shared", WidgetStatus.Approved,
            new WidgetPropSpec("should-be-ignored", "Nope", "text", null, [])));
        var catalog = Catalog(registry);

        Assert.True(catalog.IsBuiltInTag("x-shared"));
        // The built-in descriptor is the one served — its props, not the submission's.
        Assert.Equal("Built shared", catalog.Find("x-shared")!.Name);
        Assert.Equal(new HashSet<string> { "only-builtin" }, catalog.PropNames("x-shared"));
        // And the union lists x-shared exactly once.
        Assert.Equal(1, catalog.Descriptors.Count(descriptor => descriptor.Tag == "x-shared"));
    }

    [Fact]
    public void Descriptors_are_the_union_of_built_in_and_approved()
    {
        var registry = RegistryWith(
            Submission("x-approved", WidgetStatus.Approved),
            Submission("x-shared", WidgetStatus.Approved),   // collides, dropped
            Submission("x-pending", WidgetStatus.Pending));  // not approved, excluded
        var catalog = Catalog(registry);

        var tags = catalog.Descriptors.Select(descriptor => descriptor.Tag).Order().ToList();
        Assert.Equal(new[] { "x-approved", "x-built", "x-shared" }, tags);
    }

    [Fact]
    public void Approved_prop_spec_maps_to_the_render_prop_type_enum()
    {
        var registry = RegistryWith(Submission(
            "x-approved", WidgetStatus.Approved,
            new WidgetPropSpec("mode", "Mode", "choice", "a", ["a", "b"]),
            new WidgetPropSpec("count", "Count", "number", "3", []),
            new WidgetPropSpec("tint", "Tint", "color", null, []),
            new WidgetPropSpec("href", "Link", "url", null, []),
            new WidgetPropSpec("on", "On", "toggle", null, []),
            new WidgetPropSpec("note", "Note", "text", null, [])));
        var descriptor = Catalog(registry).Find("x-approved")!;

        WidgetProp Prop(string name) => descriptor.Props.Single(prop => prop.Name == name);

        Assert.Equal(WidgetPropType.Choice, Prop("mode").Type);
        Assert.Equal(new[] { "a", "b" }, Prop("mode").Options);
        Assert.Equal("a", Prop("mode").Default);
        Assert.Equal(WidgetPropType.Number, Prop("count").Type);
        Assert.Equal(WidgetPropType.Color, Prop("tint").Type);
        Assert.Equal(WidgetPropType.Url, Prop("href").Type);
        Assert.Equal(WidgetPropType.Toggle, Prop("on").Type);
        Assert.Equal(WidgetPropType.Text, Prop("note").Type);
    }

    [Fact]
    public void Built_in_props_are_unaffected_by_the_registry()
    {
        var catalog = Catalog(RegistryWith());

        Assert.True(catalog.IsBuiltInTag("x-built"));
        Assert.Equal(new HashSet<string> { "caption" }, catalog.PropNames("x-built"));
        Assert.Equal("hi", catalog.Find("x-built")!.Props.Single().Default);
    }
}
