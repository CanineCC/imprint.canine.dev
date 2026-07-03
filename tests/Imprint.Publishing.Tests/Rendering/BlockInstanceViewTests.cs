using System.Text.RegularExpressions;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class BlockInstanceViewTests
{
    private static RenderContext WithDefinition(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveBlock = id => id == SampleNodes.DefinitionId ? SampleNodes.BlockDefinition() : null,
    };

    [Fact]
    public async Task Instance_renders_the_definition_subtree()
    {
        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), SampleNodes.BlockInstance());

        Assert.Contains("Definition heading", html);
        Assert.Contains("<p>Definition body</p>", html);
    }

    [Fact]
    public async Task Text_override_replaces_the_definition_value()
    {
        var overrides = OverrideSet.Empty.With(SampleNodes.DefinitionHeadingId, "text", RenderHarness.En, "Instance heading");

        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), SampleNodes.BlockInstance(overrides));

        Assert.Contains("Instance heading", html);
        Assert.DoesNotContain("Definition heading", html);
        // Untouched siblings keep their definition content.
        Assert.Contains("<p>Definition body</p>", html);
    }

    [Fact]
    public async Task Override_for_a_node_the_definition_no_longer_has_is_ignored()
    {
        var overrides = OverrideSet.Empty.With(NodeId.New(), "text", RenderHarness.En, "Orphaned");

        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), SampleNodes.BlockInstance(overrides));

        Assert.DoesNotContain("Orphaned", html);
        Assert.Contains("Definition heading", html);
    }

    [Fact]
    public async Task Override_for_a_field_the_node_type_lacks_is_ignored()
    {
        var overrides = OverrideSet.Empty.With(SampleNodes.DefinitionHeadingId, "label", RenderHarness.En, "Wrong field");

        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), SampleNodes.BlockInstance(overrides));

        Assert.DoesNotContain("Wrong field", html);
        Assert.Contains("Definition heading", html);
    }

    [Fact]
    public async Task Editor_mode_wrapper_carries_the_instance_id_and_inner_ids_are_suppressed()
    {
        var instance = SampleNodes.BlockInstance();

        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Editor), instance);

        Assert.Contains($"data-node-id=\"{instance.Id.Compact}\"", html);
        Assert.Contains("data-node-type=\"block-instance\"", html);
        // Exactly one id in the whole subtree: selection routes to the instance.
        Assert.Single(Regex.Matches(html, "data-node-id"));
        Assert.DoesNotContain("data-node-type=\"heading\"", html);
    }

    [Fact]
    public async Task Static_mode_renders_no_wrapper_attributes()
    {
        var html = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), SampleNodes.BlockInstance());

        Assert.DoesNotContain("data-node-", html);
    }

    [Fact]
    public async Task Missing_definition_warns_in_editor_and_renders_nothing_in_static()
    {
        var orphan = SampleNodes.BlockInstance() with { DefinitionId = BlockDefinitionId.New() };

        var editor = await RenderHarness.RenderNode(WithDefinition(RenderMode.Editor), orphan);
        var published = await RenderHarness.RenderNode(WithDefinition(RenderMode.Static), orphan);

        Assert.Contains("Missing block definition", editor);
        Assert.Contains("ip-placeholder-warn", editor);
        Assert.Equal(string.Empty, published.Trim());
    }
}
