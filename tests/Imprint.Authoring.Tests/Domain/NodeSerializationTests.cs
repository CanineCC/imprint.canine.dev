using System.Text.Json;
using Imprint.Authoring;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Tests.Domain;

public sealed class NodeSerializationTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EventSourcing.GuidIdJsonConverterFactory());
        AuthoringJson.Configure(options);
        return options;
    }

    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");

    [Fact]
    public void A_full_tree_round_trips_with_value_equality()
    {
        var tree = new SectionNode
        {
            Id = NodeId.New(),
            Width = SectionWidth.Wide,
            Background = SectionBackground.Surface,
            Children = NodeList.Of(
                new ColumnsNode
                {
                    Id = NodeId.New(),
                    Ratios = [2, 1],
                    CollapseBelow = CollapseBreakpoint.Px640,
                    Children = NodeList.Of(
                        new StackNode
                        {
                            Id = NodeId.New(),
                            Children = NodeList.Of(
                                new HeadingNode { Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(En, "Hello").With(Da, "Hej") },
                                new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, "<p>Body with <strong>bold</strong>.</p>") },
                                new ButtonNode { Id = NodeId.New(), Label = LocalizedText.Of(En, "Go"), LinkTo = new PageLink(PageId.New()) }),
                        },
                        new StackNode
                        {
                            Id = NodeId.New(),
                            Children = NodeList.Of(
                                new ImageNode { Id = NodeId.New(), AssetId = AssetId.New(), Alt = LocalizedText.Of(En, "A pier") },
                                new WidgetNode
                                {
                                    Id = NodeId.New(),
                                    Tag = "x-countdown",
                                    Props = PropBag.Empty.With("until", "2027-01-01").With("label", "Launch"),
                                }),
                        }),
                }),
        };

        var json = JsonSerializer.Serialize<Node>(tree, Options);
        var back = JsonSerializer.Deserialize<Node>(json, Options);

        Assert.Equal<Node>(tree, back);
    }

    [Fact]
    public void Block_instance_overrides_round_trip()
    {
        var definitionNode = NodeId.New();
        var node = new BlockInstanceNode
        {
            Id = NodeId.New(),
            DefinitionId = BlockDefinitionId.New(),
            Overrides = OverrideSet.Empty
                .With(definitionNode, "text", En, "Custom headline")
                .With(definitionNode, "text", Da, "Tilpasset overskrift"),
        };

        var back = JsonSerializer.Deserialize<Node>(JsonSerializer.Serialize<Node>(node, Options), Options);

        Assert.Equal<Node>(node, back);
        Assert.Equal("Custom headline", ((BlockInstanceNode)back!).Overrides.Get(definitionNode, "text", En));
    }

    [Fact]
    public void Discriminators_are_stable_names_not_clr_names()
    {
        var json = JsonSerializer.Serialize<Node>(new DividerNode { Id = NodeId.New() }, Options);
        Assert.Contains("\"$type\":\"divider\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(DividerNode), json, StringComparison.Ordinal);
    }

    [Fact]
    public void Section_appearance_round_trips_as_its_stable_string_name()
    {
        var section = new SectionNode { Id = NodeId.New(), Appearance = SectionAppearance.FeatureGrid };

        var json = JsonSerializer.Serialize<Node>(section, Options);
        Assert.Contains("\"Appearance\":\"FeatureGrid\"", json, StringComparison.Ordinal);

        var back = (SectionNode)JsonSerializer.Deserialize<Node>(json, Options)!;
        Assert.Equal(SectionAppearance.FeatureGrid, back.Appearance);
        Assert.Equal<Node>(section, back);
    }

    [Fact]
    public void Section_without_an_appearance_reads_back_as_plain()
    {
        // Back-compat: a stream written before the field existed carries no "Appearance".
        var legacyJson = JsonSerializer.Serialize<Node>(new SectionNode { Id = NodeId.New() }, Options)
            .Replace(",\"Appearance\":\"Plain\"", "", StringComparison.Ordinal);
        Assert.DoesNotContain("\"Appearance\"", legacyJson, StringComparison.Ordinal);

        var back = (SectionNode)JsonSerializer.Deserialize<Node>(legacyJson, Options)!;
        Assert.Equal(SectionAppearance.Plain, back.Appearance);
    }

    [Fact]
    public void Section_appearance_class_kebab_cases_the_enum_name()
    {
        Assert.Null(SectionAppearanceClass.For(SectionAppearance.Plain));
        Assert.Equal("ip-ap-hero", SectionAppearanceClass.For(SectionAppearance.Hero));
        Assert.Equal("ip-ap-feature-grid", SectionAppearanceClass.For(SectionAppearance.FeatureGrid));
        Assert.Equal("ip-ap-stat-band", SectionAppearanceClass.For(SectionAppearance.StatBand));
        Assert.Equal("ip-ap-table-list", SectionAppearanceClass.For(SectionAppearance.TableList));
        Assert.Equal("ip-ap-live-card", SectionAppearanceClass.For(SectionAppearance.LiveCard));
        Assert.Equal("band-scale", SectionAppearanceClass.Suffix(SectionAppearance.BandScale));
    }
}
