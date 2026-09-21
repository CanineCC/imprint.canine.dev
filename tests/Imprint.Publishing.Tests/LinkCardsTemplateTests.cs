using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// Further-reading cards, rendered from the node's own props — the first template that takes no
/// fetched body at all.
/// </summary>
public sealed class LinkCardsTemplateTests
{
    private const string Links = """
        [{"icon":"watchdog","label":"Real examples, without an account","note":"watchdog.canine.dev/publicreports","href":"https://watchdog.canine.dev/publicreports"},
         {"icon":"cai","label":"The dimensions behind the view","note":"codeassuranceindex.info/dimensions","href":"https://codeassuranceindex.info/dimensions"},
         {"label":"A page on this site","note":"/methodology/","href":"/methodology/"}]
        """;

    private static Func<string, string?> Props(string? links) => name => name == "links" ? links : null;

    [Fact]
    public void Every_link_becomes_a_real_anchor_a_crawler_can_follow()
    {
        // ★ This is the entire point. Eight guide pages ended with a card of further reading whose
        //   every href lived in a shadow root, so the pages they point at lost the one internal link
        //   that named what they were for.
        var html = LinkCardsTemplate.Render(Props(Links))!;

        Assert.Contains("href=\"https://watchdog.canine.dev/publicreports\"", html, StringComparison.Ordinal);
        Assert.Contains("Real examples, without an account", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/methodology/\"", html, StringComparison.Ordinal);
        Assert.Equal(3, html.Split("ip-linkcard\"").Length - 1);
    }

    [Fact]
    public void A_javascript_url_never_becomes_a_link_but_its_label_still_prints()
    {
        // ★ An href is a script sink: `javascript:` in one runs on click, and HTML-escaping does not
        //   touch it. The LABEL is the reference, so a card whose destination failed the check is
        //   still printed — as text. A dead link would be worse.
        var html = LinkCardsTemplate.Render(Props(
            """[{"label":"Looks innocent","href":"javascript:alert(1)"}]"""))!;

        Assert.DoesNotContain("javascript:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<a", html, StringComparison.Ordinal);
        Assert.Contains("Looks innocent", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_protocol_relative_url_is_refused_because_it_is_not_a_site_path()
    {
        var html = LinkCardsTemplate.Render(Props("""[{"label":"x","href":"//evil.example/x"}]"""))!;

        Assert.DoesNotContain("evil.example", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Label_and_note_from_the_props_are_escaped()
    {
        var html = LinkCardsTemplate.Render(Props(
            """[{"label":"<script>x</script>","note":"<img src=x>","href":"/a"}]"""))!;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"links\":[]}")]
    [InlineData("[{\"href\":\"/a\"}]")]
    public void Nothing_to_show_renders_nothing(string? links) =>
        Assert.Null(LinkCardsTemplate.Render(Props(links)));

    [Fact]
    public void An_unknown_prop_template_renders_nothing_rather_than_something_unexpected() =>
        Assert.Null(PrerenderTemplates.RenderFromProps("no-such-template", Props(Links)));

    [Fact]
    public void The_dispatcher_routes_the_name_to_the_template()
    {
        var html = PrerenderTemplates.RenderFromProps(LinkCardsTemplate.Name, Props(Links))!;

        Assert.Contains("ip-linkcards", html, StringComparison.Ordinal);
    }
}
