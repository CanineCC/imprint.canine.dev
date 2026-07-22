using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Editor.Api;

namespace Imprint.Editor.Tests;

/// <summary>
/// The wire mapping the headless surfaces (authoring API + MCP) use for page nodes. It is the only
/// place a caller's JSON becomes domain nodes, so the rules that matter here are: ids are ALWAYS
/// minted by us (a caller can never name one), a props change is a partial patch over what is
/// already there (so a one-prop edit can't silently reset the rest), and a bad spec fails with a
/// sentence rather than a binder error.
/// </summary>
public sealed class AuthoringNodeJsonTests
{
    private static readonly Locale En = new("en");

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static Node Parse(string raw)
    {
        Assert.True(AuthoringNodeJson.TryParse(Json(raw), En, out var node, out var error), error);
        return node;
    }

    private static string Error(string raw)
    {
        Assert.False(AuthoringNodeJson.TryParse(Json(raw), En, out _, out var error));
        return error;
    }

    // ── parse ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parses_a_section_with_its_whole_subtree_in_one_spec()
    {
        var node = Parse("""
            {
              "type": "section", "appearance": "Hero", "width": "Wide",
              "children": [
                { "type": "heading", "level": 1, "text": "Is the codebase getting better?" },
                { "type": "richtext", "html": "<p>One number, every sprint.</p>" }
              ]
            }
            """);

        var section = Assert.IsType<SectionNode>(node);
        Assert.Equal(SectionAppearance.Hero, section.Appearance);
        Assert.Equal(SectionWidth.Wide, section.Width);
        Assert.Equal(SectionPadding.Normal, section.Padding);   // untouched keys keep their default

        var heading = Assert.IsType<HeadingNode>(section.Children[0]);
        Assert.Equal(1, heading.Level);
        Assert.Equal("Is the codebase getting better?", heading.Text.Get(En));
        Assert.Equal("<p>One number, every sprint.</p>", Assert.IsType<RichTextNode>(section.Children[1]).Html.Get(En));
    }

    [Fact]
    public void Mints_a_fresh_distinct_id_for_every_node_in_the_spec()
    {
        // A caller naming an id could hijack or collide with an existing node, so 'id' in a spec is
        // ignored outright rather than trusted.
        var node = Parse("""
            {
              "type": "section", "id": "ffffffffffffffffffffffffffffffff",
              "children": [ { "type": "heading", "text": "A" }, { "type": "heading", "text": "B" } ]
            }
            """);

        var ids = PageTree.Flatten(node).Select(n => n.Id).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Equal(3, ids.Distinct().Count());
        Assert.DoesNotContain(NodeId.From(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")), ids);
        Assert.DoesNotContain(NodeId.Root, ids);
    }

    [Fact]
    public void Text_takes_either_a_bare_string_or_a_locale_map()
    {
        var bare = Assert.IsType<HeadingNode>(Parse("""{ "type": "heading", "text": "Survey. Fix. Prove it moved." }"""));
        Assert.Equal("Survey. Fix. Prove it moved.", bare.Text.Get(En));

        var mapped = Assert.IsType<HeadingNode>(Parse("""{ "type": "heading", "text": { "en": "The loop", "da": "Sløjfen" } }"""));
        Assert.Equal("The loop", mapped.Text.Get(En));
        Assert.Equal("Sløjfen", mapped.Text.Get(new Locale("da")));
    }

    [Fact]
    public void Columns_get_exactly_one_cell_per_ratio()
    {
        var columns = Assert.IsType<ColumnsNode>(Parse("""{ "type": "columns", "ratios": [2, 1, 1], "collapseBelow": 768 }"""));
        Assert.Equal([2, 1, 1], columns.Ratios);
        Assert.Equal(CollapseBreakpoint.Px768, columns.CollapseBelow);
        Assert.Equal(3, columns.Children.Count);
        Assert.All(columns.Children, cell => Assert.IsType<StackNode>(cell));
    }

    [Fact]
    public void Button_accepts_a_flat_href_and_rejects_a_disallowed_scheme()
    {
        var button = Assert.IsType<ButtonNode>(Parse("""{ "type": "button", "label": "Survey a repo", "href": "https://app.watchdog.canine.dev" }"""));
        Assert.Equal("https://app.watchdog.canine.dev", Assert.IsType<ExternalLink>(button.LinkTo).Url);

        Assert.Contains("https", Error("""{ "type": "button", "label": "x", "href": "javascript:alert(1)" }"""));
    }

    [Fact]
    public void An_unknown_type_or_enum_value_fails_with_the_allowed_values()
    {
        Assert.Contains("richtext", Error("""{ "type": "paragraph" }"""));
        Assert.Contains("Hero", Error("""{ "type": "section", "appearance": "Splashy" }"""));
        Assert.Contains("480", Error("""{ "type": "columns", "collapseBelow": 900 }"""));
    }

    // ── describe ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Describe_reads_back_the_content_an_editor_needs_to_see()
    {
        var heading = new HeadingNode { Id = NodeId.New(), Level = 3, Text = LocalizedText.Of(En, "The loop") };
        var props = AuthoringNodeJson.Describe(heading);

        Assert.Equal(3, props["level"]);
        var text = Assert.IsType<Dictionary<string, string>>(props["text"]);
        Assert.Equal("The loop", text["en"]);
    }

    [Fact]
    public void Describe_round_trips_through_parse()
    {
        var original = Assert.IsType<SectionNode>(Parse("""{ "type": "section", "appearance": "Steps", "background": "Surface", "anchor": "loop" }"""));
        var props = AuthoringNodeJson.Describe(original);

        var reparsed = Assert.IsType<SectionNode>(Parse(
            $$"""{ "type": "section", "appearance": "{{props["appearance"]}}", "background": "{{props["background"]}}", "anchor": "{{props["anchor"]}}" }"""));

        Assert.Equal(original.Appearance, reparsed.Appearance);
        Assert.Equal(original.Background, reparsed.Background);
        Assert.Equal(original.Anchor, reparsed.Anchor);
    }

    // ── patch ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_props_patch_changes_only_the_keys_it_names()
    {
        var section = new SectionNode
        {
            Id = NodeId.New(),
            Appearance = SectionAppearance.Hero,
            Background = SectionBackground.Surface,
            Padding = SectionPadding.Large,
            Anchor = "top",
        };

        Assert.True(AuthoringNodeJson.TryApply(section, Json("""{ "width": "Full" }"""), En, out var patched, out var error), error);
        var result = Assert.IsType<SectionNode>(patched);
        Assert.Equal(SectionWidth.Full, result.Width);
        Assert.Equal(SectionAppearance.Hero, result.Appearance);
        Assert.Equal(SectionBackground.Surface, result.Background);
        Assert.Equal(SectionPadding.Large, result.Padding);
        Assert.Equal("top", result.Anchor);
        Assert.Equal(section.Id, result.Id);
    }

    [Fact]
    public void Patching_text_in_one_locale_keeps_the_others()
    {
        var da = new Locale("da");
        var heading = new HeadingNode
        {
            Id = NodeId.New(),
            Text = LocalizedText.Of(En, "Prove your code").With(da, "Bevis din kode"),
        };

        Assert.True(AuthoringNodeJson.TryApply(heading, Json("""{ "text": "Is the codebase getting better?" }"""), En, out var patched, out var error), error);
        var result = Assert.IsType<HeadingNode>(patched);
        Assert.Equal("Is the codebase getting better?", result.Text.Get(En));
        Assert.Equal("Bevis din kode", result.Text.Get(da));
    }

    [Fact]
    public void A_widget_patch_is_the_whole_prop_bag_both_wrapped_and_bare()
    {
        var widget = new WidgetNode
        {
            Id = NodeId.New(),
            Tag = "cai-score-card",
            Props = PropBag.Of([new("repo", "old"), new("theme", "dark")]),
        };

        Assert.True(AuthoringNodeJson.TryApply(widget, Json("""{ "props": { "repo": "new" } }"""), En, out var wrapped, out var error), error);
        var wrappedProps = Assert.IsType<WidgetNode>(wrapped).Props;
        Assert.Equal("new", wrappedProps.Get("repo"));
        Assert.Null(wrappedProps.Get("theme"));     // whole-bag semantics: an absent key is removed

        Assert.True(AuthoringNodeJson.TryApply(widget, Json("""{ "repo": "bare" }"""), En, out var bare, out error), error);
        Assert.Equal("bare", Assert.IsType<WidgetNode>(bare).Props.Get("repo"));

        Assert.True(AuthoringNodeJson.TryApply(widget, Json("{}"), En, out var cleared, out error), error);
        Assert.Equal(0, Assert.IsType<WidgetNode>(cleared).Props.Count);
    }

    [Fact]
    public void A_patch_never_changes_a_nodes_type()
    {
        // The aggregate rejects a type change outright; the mapper must not even be able to express
        // one, so 'type' in a patch is inert.
        var heading = new HeadingNode { Id = NodeId.New(), Level = 2, Text = LocalizedText.Of(En, "A") };
        Assert.True(AuthoringNodeJson.TryApply(heading, Json("""{ "type": "richtext", "level": 4 }"""), En, out var patched, out var error), error);
        Assert.Equal(4, Assert.IsType<HeadingNode>(patched).Level);
    }
}
