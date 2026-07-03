using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class ButtonViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    [Fact]
    public async Task Resolved_page_link_renders_anchor_without_noopener()
    {
        var button = SampleNodes.Button(new PageLink(SampleNodes.LinkedPageId), label: "About us");
        var ctx = Static with { ResolvePagePath = id => id == SampleNodes.LinkedPageId ? "/about/" : null };

        var html = await RenderHarness.RenderNode(ctx, button);

        Assert.Contains("<a class=\"ip-btn ip-btn-primary\" href=\"/about/\">About us</a>", html);
        Assert.DoesNotContain("noopener", html);
    }

    [Fact]
    public async Task Unresolvable_page_link_renders_labelled_span()
    {
        var button = SampleNodes.Button(new PageLink(PageId.New()), label: "Gone");

        var html = await RenderHarness.RenderNode(Static, button);

        Assert.Contains("<span class=\"ip-btn ip-btn-primary\">Gone</span>", html);
        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public async Task External_link_renders_verbatim_href_with_noopener()
    {
        var button = SampleNodes.Button(new ExternalLink("https://example.com/signup?plan=pro"));

        var html = await RenderHarness.RenderNode(Static, button);

        Assert.Contains("href=\"https://example.com/signup?plan=pro\"", html);
        Assert.Contains("rel=\"noopener\"", html);
    }

    [Fact]
    public async Task External_link_with_disallowed_scheme_renders_span()
    {
        var button = SampleNodes.Button(new ExternalLink("javascript:alert(1)"));

        var html = await RenderHarness.RenderNode(Static, button);

        Assert.Contains("<span class=\"ip-btn ip-btn-primary\">Go</span>", html);
        Assert.DoesNotContain("javascript:", html);
        Assert.DoesNotContain("<a", html);
    }

    [Fact]
    public async Task Missing_link_renders_span()
    {
        var button = SampleNodes.Button(linkTo: null);

        var html = await RenderHarness.RenderNode(Static, button);

        Assert.Contains("<span class=\"ip-btn ip-btn-primary\">Go</span>", html);
    }

    [Fact]
    public async Task Variant_classes_follow_the_node()
    {
        var secondary = await RenderHarness.RenderNode(Static, SampleNodes.Button(null, ButtonVariant.Secondary));
        var ghost = await RenderHarness.RenderNode(Static, SampleNodes.Button(null, ButtonVariant.Ghost));

        Assert.Contains("ip-btn ip-btn-secondary", secondary);
        Assert.Contains("ip-btn ip-btn-ghost", ghost);
    }

    [Fact]
    public async Task Label_is_html_encoded()
    {
        var button = SampleNodes.Button(null, label: "Save & <exit>");

        var html = await RenderHarness.RenderNode(Static, button);

        Assert.Contains("Save &amp; &lt;exit&gt;", html);
    }
}
