using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The preview serves a published site's origin-relative output under a
/// <c>/preview/{slug}/</c> prefix. These lock the URL re-homing contract: which
/// root-absolute URLs move (assets, island bundles, internal links, font url()s) and
/// which are deliberately left alone (external, protocol-relative, data:, and the live
/// <c>api-base</c> fetch URL the widgets read).
/// </summary>
public sealed class PreviewRewriteTests
{
    [Fact]
    public void Html_rehomes_root_absolute_asset_and_island_urls_under_the_slug_prefix()
    {
        const string html =
            "<link href=\"/css/site.abc.css\">" +
            "<a href=\"/about/\">About</a>" +
            "<cai-score-card data-island=\"/widgets/cai-score-card.def.js\"></cai-score-card>" +
            "<img src=\"/media/x.webp\">";

        var result = PreviewRewrite.Html(html, "watchdog");

        Assert.Contains("href=\"/preview/watchdog/css/site.abc.css\"", result);
        Assert.Contains("href=\"/preview/watchdog/about/\"", result);
        Assert.Contains("data-island=\"/preview/watchdog/widgets/cai-score-card.def.js\"", result);
        Assert.Contains("src=\"/preview/watchdog/media/x.webp\"", result);
    }

    [Fact]
    public void Html_leaves_external_protocol_relative_and_non_asset_urls_untouched()
    {
        const string html =
            "<a href=\"https://cai.canine.dev/dimensions\">docs</a>" +
            "<a href=\"//cdn.example.com/x.js\">cdn</a>" +
            "<a href=\"#section\">anchor</a>" +
            "<a href=\"mailto:hi@canine.dev\">mail</a>" +
            "<cai-score-card api-base=\"https://api.watchdog.canine.dev\"></cai-score-card>";

        var result = PreviewRewrite.Html(html, "watchdog");

        // Nothing gained a /preview/ prefix — these are all left exactly as-is.
        Assert.DoesNotContain("/preview/", result);
        Assert.Equal(html, result);
    }

    [Fact]
    public void Html_does_not_touch_the_live_api_base_attribute()
    {
        const string html = "<cai-card-gallery api-base=\"https://api.watchdog.canine.dev\" count=\"6\"></cai-card-gallery>";

        var result = PreviewRewrite.Html(html, "cai");

        // The widgets must still fetch the REAL kennel API — the prefix must not corrupt it.
        Assert.Contains("api-base=\"https://api.watchdog.canine.dev\"", result);
    }

    [Fact]
    public void Css_rehomes_root_absolute_font_urls_but_leaves_data_uris_alone()
    {
        const string css =
            "@font-face{src:url(\"/fonts/schibsted-var.woff2\")}" +
            ".x{background:url('/fonts/jetbrains-var.woff2')}" +
            ".y{background:url(\"data:image/svg+xml,%3Csvg/%3E\")}";

        var result = PreviewRewrite.Css(css, "assay");

        Assert.Contains("url(\"/preview/assay/fonts/schibsted-var.woff2\")", result);
        Assert.Contains("url('/preview/assay/fonts/jetbrains-var.woff2')", result);
        // The inlined data: URI must be untouched — it is not a servable path.
        Assert.Contains("url(\"data:image/svg+xml,%3Csvg/%3E\")", result);
        Assert.DoesNotContain("/preview/assay/data:", result);
    }
}
