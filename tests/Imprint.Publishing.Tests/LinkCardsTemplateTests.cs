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

    /// <summary>
    /// ★★ THE MARK FOLLOWS WHERE THE CARD GOES. Every survey page carries two cards generated with
    /// icon "doc" that point at the corpus on codeassuranceindex.info — so they wore a generic page
    /// glyph beside a card for the same site wearing its badge. And "Verify this score yourself",
    /// which points at the standard, wore the STUDIO's badge tinted green rather than the standard's
    /// own mark. The owner found both by reading the markup.
    /// </summary>
    [Fact]
    public void A_generic_icon_takes_the_mark_of_the_site_it_points_at()
    {
        var html = LinkCardsTemplate.Render(Props("""
            [{"icon":"doc","label":"How this project compares","href":"https://codeassuranceindex.info/state-of-the-corpus/"},
             {"icon":"doc","label":"A real document","href":"https://example.com/whitepaper"},
             {"icon":"","label":"The repository","href":"https://github.com/acme/api"}]
            """))!;

        Assert.Contains("/brand/cai-mark.svg", html, StringComparison.Ordinal);
        // The one that points nowhere in particular keeps the document glyph.
        Assert.Contains("M9.2 1.4H4a1.6", html, StringComparison.Ordinal);
        Assert.Contains("M8 0C3.58 0 0 3.58 0 8", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_standards_own_mark_is_drawn_not_the_studio_badge_tinted()
    {
        var html = LinkCardsTemplate.Render(Props("""
            [{"icon":"cai","label":"Verify this score yourself","href":"https://codeassuranceindex.info/verify/"}]
            """))!;

        Assert.Contains("/brand/cai-mark.svg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("canine-badge.svg", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_explicit_icon_still_wins_over_the_destination()
    {
        // The caller's word is honoured when it says something. Only "doc" and "" defer.
        var html = LinkCardsTemplate.Render(Props("""
            [{"icon":"github","label":"Read it on the standard's site","href":"https://codeassuranceindex.info/spec/"}]
            """))!;

        Assert.Contains("M8 0C3.58 0 0 3.58 0 8", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cai-mark.svg", html, StringComparison.Ordinal);
    }

    [Fact]
    public void A_preprod_or_local_host_resolves_the_same_as_the_live_one()
    {
        // ★ Matched on the host SUFFIX: a mark that is right on prod and wrong everywhere it is
        //   reviewed is a mark nobody trusts.
        var html = LinkCardsTemplate.Render(Props("""
            [{"icon":"doc","label":"The corpus","href":"https://preprod.codeassuranceindex.info/state-of-the-corpus/"}]
            """))!;

        Assert.Contains("/brand/cai-mark.svg", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ A MARK THIS TEMPLATE REFERENCES MUST BE A FILE SOME SITE ACTUALLY WRITES. The markup was
    /// correct and the card rendered a BROKEN IMAGE, because adding the svg to wwwroot is not what
    /// puts it under a published site root — <see cref="Imprint.Rendering.FontAssets"/> is, and it
    /// had not been told. Caught by looking at the render; invisible in a diff.
    /// </summary>
    [Fact]
    public void Every_mark_this_template_references_is_shipped_with_the_site()
    {
        var shipped = Imprint.Rendering.FontAssets.All.Select(f => "/" + f.RelativePath).ToHashSet(StringComparer.Ordinal);

        var html = LinkCardsTemplate.Render(Props("""
            [{"icon":"cai","label":"The standard","href":"https://codeassuranceindex.info/"},
             {"icon":"watchdog","label":"The surveyor","href":"https://watchdog.canine.dev/"},
             {"icon":"assay","label":"The buyer's side","href":"https://assay.canine.dev/"}]
            """))!;

        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(html, @"(/brand/[A-Za-z0-9._-]+)"))
        {
            Assert.True(shipped.Contains(m.Groups[1].Value),
                $"{m.Groups[1].Value} is referenced by a card but no site writes it");
        }
    }
}
