using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class RichTextViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    [Fact]
    public async Task RichText_renders_canonical_html_inside_prose_wrapper()
    {
        var node = SampleNodes.RichText("<p>Plain <strong>bold</strong> and <em>italic</em></p>");

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.Contains("<div class=\"ip-prose\"><p>Plain <strong>bold</strong> and <em>italic</em></p></div>", html);
    }

    [Fact]
    public async Task Page_links_resolve_to_public_paths()
    {
        var node = SampleNodes.RichText($"<p>Read <a href=\"page:{SampleNodes.LinkedPageId.Value}\">the guide</a></p>");
        var ctx = Static with { ResolvePagePath = id => id == SampleNodes.LinkedPageId ? "/guides/intro/" : null };

        var html = await RenderHarness.RenderNode(ctx, node);

        Assert.Contains("<a href=\"/guides/intro/\">the guide</a>", html);
        Assert.DoesNotContain("page:", html);
    }

    [Fact]
    public async Task Broken_page_links_unwrap_keeping_inner_formatting()
    {
        var node = SampleNodes.RichText($"<p>Read <a href=\"page:{Guid.NewGuid()}\">the <strong>lost</strong> guide</a> now</p>");

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.Contains("<p>Read the <strong>lost</strong> guide now</p>", html);
        Assert.DoesNotContain("<a", html);
        Assert.DoesNotContain("</a>", html);
    }

    [Fact]
    public async Task External_links_keep_encoded_href_and_gain_noopener()
    {
        var node = SampleNodes.RichText("<p><a href=\"https://example.com/?a=1&amp;b=2\">out</a></p>");

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.Contains("<a href=\"https://example.com/?a=1&amp;b=2\" rel=\"noopener\">out</a>", html);
    }

    [Fact]
    public async Task Mailto_links_gain_noopener_and_stay_verbatim()
    {
        var node = SampleNodes.RichText("<p><a href=\"mailto:hi@example.com\">mail</a></p>");

        var html = await RenderHarness.RenderNode(Static, node);

        Assert.Contains("<a href=\"mailto:hi@example.com\" rel=\"noopener\">mail</a>", html);
    }

    [Fact]
    public async Task Mixed_resolved_and_broken_links_are_each_handled()
    {
        var node = SampleNodes.RichText(
            $"<p><a href=\"page:{SampleNodes.LinkedPageId.Value}\">ok</a> and <a href=\"page:{Guid.NewGuid()}\">gone</a></p>" +
            "<ul><li><a href=\"https://example.com/\">ext</a></li></ul>");
        var ctx = Static with { ResolvePagePath = id => id == SampleNodes.LinkedPageId ? "/ok/" : null };

        var html = await RenderHarness.RenderNode(ctx, node);

        Assert.Contains("<a href=\"/ok/\">ok</a> and gone", html);
        Assert.Contains("<li><a href=\"https://example.com/\" rel=\"noopener\">ext</a></li>", html);
    }

    [Fact]
    public async Task Locale_falls_back_to_default_locale_html()
    {
        var node = SampleNodes.RichText("<p>English body</p>");
        var ctx = Static with { Locale = RenderHarness.Da };

        var html = await RenderHarness.RenderNode(ctx, node);

        Assert.Contains("<p>English body</p>", html);
    }
}
