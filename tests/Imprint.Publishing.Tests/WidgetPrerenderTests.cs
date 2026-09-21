using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The bake goes into our own published HTML, so what it drops matters more than what it keeps.
/// </summary>
public sealed class WidgetPrerenderTests
{
    [Fact]
    public void Script_and_style_lose_their_content_not_just_their_tags()
    {
        var reduced = WidgetPrerender.Reduce(
            "<style>.wd-plan{color:red}</style><p>From €245</p><script>alert(1)</script>");

        Assert.Equal("<p>From €245</p>", reduced);
    }

    [Fact]
    public void Attributes_are_dropped_including_class_and_event_handlers()
    {
        var reduced = WidgetPrerender.Reduce(
            "<p class=\"wd-plan-free\" onclick=\"steal()\" style=\"display:none\">Engineering teams</p>");

        Assert.Equal("<p>Engineering teams</p>", reduced);
    }

    [Fact]
    public void An_unknown_wrapper_keeps_its_words_and_loses_its_markup()
    {
        // A <div> carries no meaning a reader needs; its text does.
        var reduced = WidgetPrerender.Reduce("<div><span>XXS</span> <b>€245</b></div>");

        Assert.Equal("XXS <b>€245</b>", reduced);
    }

    [Fact]
    public void A_table_survives_because_that_is_what_a_price_ladder_is()
    {
        var reduced = WidgetPrerender.Reduce(
            "<table><thead><tr><th>Bucket</th></tr></thead><tbody><tr><td>XXS</td></tr></tbody></table>");

        Assert.Equal(
            "<table><thead><tr><th>Bucket</th></tr></thead><tbody><tr><td>XXS</td></tr></tbody></table>",
            reduced);
    }

    [Theory]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "<a>x</a>")]
    [InlineData("<a href=\"/relative\">x</a>", "<a>x</a>")]
    [InlineData("<a href=\"http://insecure.example\">x</a>", "<a>x</a>")]
    public void Only_an_absolute_https_href_crosses(string input, string expected)
    {
        Assert.Equal(expected, WidgetPrerender.Reduce(input));
    }

    [Fact]
    public void An_https_link_keeps_its_href()
    {
        var reduced = WidgetPrerender.Reduce("<a href=\"https://watchdog.canine.dev/pricing\">Pricing</a>");

        Assert.Equal("<a href=\"https://watchdog.canine.dev/pricing\">Pricing</a>", reduced);
    }

    [Fact]
    public void An_iframe_contributes_nothing_at_all()
    {
        // The very thing the bake exists to see past.
        Assert.Null(WidgetPrerender.Reduce("<div><iframe src=\"https://app.example/embed\"></iframe></div>"));
    }

    [Fact]
    public void Markup_with_no_words_bakes_nothing_rather_than_an_empty_shell()
    {
        Assert.Null(WidgetPrerender.Reduce("<div><span></span></div>"));
        Assert.Null(WidgetPrerender.Reduce("   "));
        Assert.Null(WidgetPrerender.Reduce(null));
    }

    [Fact]
    public void Entities_survive_one_decode_and_are_re_encoded_safely()
    {
        // &amp;amp; must not decay to &, and a bare < in text must not become a tag.
        var reduced = WidgetPrerender.Reduce("<p>Rock &amp; Roll &lt;script&gt;</p>");

        Assert.Equal("<p>Rock &amp; Roll &lt;script&gt;</p>", reduced);
    }

    [Fact]
    public void A_fragment_larger_than_the_cap_is_truncated()
    {
        var huge = "<p>" + new string('x', WidgetPrerender.MaxCharacters * 2) + "</p>";

        var reduced = WidgetPrerender.Reduce(huge);

        Assert.NotNull(reduced);
        Assert.Equal(WidgetPrerender.MaxCharacters, reduced.Length);
    }
}
