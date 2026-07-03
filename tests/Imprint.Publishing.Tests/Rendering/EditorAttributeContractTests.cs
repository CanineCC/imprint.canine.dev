using System.Reflection;
using System.Text.Json.Serialization;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The editor/static markup contract: in editor mode every node view's root element
/// carries <c>data-node-id</c>/<c>data-node-type</c>; in static mode published markup
/// carries neither. One sweep over every node type so a new view cannot forget either
/// half of the contract.
/// </summary>
public sealed class EditorAttributeContractTests
{
    private static RenderContext WithResolvers(RenderMode mode) => RenderHarness.Context(mode) with
    {
        ResolveAsset = id =>
            id == SampleNodes.ImageAssetId ? RenderHarness.ImageAsset()
            : id == SampleNodes.VideoAssetId ? RenderHarness.VideoAsset()
            : id == SampleNodes.SvgAssetId ? RenderHarness.SvgAsset()
            : null,
        ResolveBlock = id => id == SampleNodes.DefinitionId ? SampleNodes.BlockDefinition() : null,
        ResolveWidget = tag => tag == "x-countdown" ? SampleNodes.CountdownDescriptor() : null,
        ResolveWidgetBundle = tag => $"/widgets/{tag}.abc123.js",
        ResolvePagePath = id => id == SampleNodes.LinkedPageId ? "/linked/" : null,
    };

    [Fact]
    public async Task Editor_mode_every_node_type_carries_id_and_type_attributes()
    {
        foreach (var node in SampleNodes.OneOfEachType())
        {
            var html = await RenderHarness.RenderNode(WithResolvers(RenderMode.Editor), node);

            Assert.True(
                html.Contains($"data-node-id=\"{node.Id.Compact}\"", StringComparison.Ordinal),
                $"{node.GetType().Name} did not emit its data-node-id in editor mode. Html: {html}");
            Assert.True(
                html.Contains($"data-node-type=\"{NodeTypeNames.Of(node)}\"", StringComparison.Ordinal),
                $"{node.GetType().Name} did not emit its data-node-type in editor mode. Html: {html}");
        }
    }

    [Fact]
    public async Task Static_mode_no_node_type_leaks_editor_attributes()
    {
        foreach (var node in SampleNodes.OneOfEachType())
        {
            var html = await RenderHarness.RenderNode(WithResolvers(RenderMode.Static), node);

            Assert.True(
                !html.Contains("data-node-", StringComparison.Ordinal),
                $"{node.GetType().Name} leaked editor attributes into static markup. Html: {html}");
        }
    }

    [Fact]
    public void Type_names_match_the_serialization_discriminators_for_every_node_type()
    {
        var discriminators = typeof(Node)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToDictionary(a => a.DerivedType, a => (string)a.TypeDiscriminator!);
        var samples = SampleNodes.OneOfEachType().ToDictionary(n => n.GetType());

        Assert.Equal(discriminators.Count, samples.Count);
        foreach (var (type, discriminator) in discriminators)
        {
            Assert.True(samples.ContainsKey(type), $"No sample node for {type.Name} — extend SampleNodes.OneOfEachType.");
            Assert.Equal(discriminator, NodeTypeNames.Of(samples[type]));
        }
    }
}
