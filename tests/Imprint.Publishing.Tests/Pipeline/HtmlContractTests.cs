using System.Text.RegularExpressions;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>Every guarantee of docs/publishing.md §"The HTML contract", asserted on real output.</summary>
public sealed class HtmlContractTests
{
    [Fact]
    public async Task Published_page_fulfills_the_document_contract()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        // Valid HTML5 shell: doctype, lang, charset, viewport.
        Assert.StartsWith("<!doctype html>\n<html lang=\"en\">", html);
        Assert.Contains("<meta charset=\"utf-8\"", html);
        Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"", html);

        // Title is page title · site name; description falls back to the title.
        Assert.Contains("<title>Home · Acme Studio</title>", html);
        Assert.Contains("<meta name=\"description\" content=\"Home\"", html);

        // Canonical + hreflang alternates for every locale, plus x-default.
        Assert.Contains("<link rel=\"canonical\" href=\"/\"", html);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"en\" href=\"/\"", html);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"da\" href=\"/da/\"", html);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"x-default\" href=\"/\"", html);

        // Exactly ONE stylesheet link, hashed.
        var stylesheets = Regex.Matches(html, "<link rel=\"stylesheet\"");
        Assert.Single(stylesheets);
        Assert.Contains("<link rel=\"stylesheet\" href=\"/css/site.", html);

        // Inline scripts: exactly the two sanctioned ones, byte-identical to the
        // frozen assets; theme toggle sits in <head> BEFORE the stylesheet.
        Assert.Equal(2, Regex.Matches(html, "<script>").Count);
        Assert.Contains(PublisherScripts.ThemeToggle, html);
        Assert.Contains(PublisherScripts.IslandLoader, html);
        Assert.True(
            html.IndexOf(PublisherScripts.ThemeToggle, StringComparison.Ordinal) <
            html.IndexOf("<link rel=\"stylesheet\"", StringComparison.Ordinal),
            "theme toggle must precede the stylesheet");

        // Landmarks, skip link, marketing chrome shell, nav with aria-current on the
        // active item. The header/footer carry the ip-* chrome classes the marketing
        // theme styles against; the skip link, landmarks and aria-current stay intact.
        Assert.Contains("<a href=\"#main\">Skip to content</a>", html);
        Assert.Contains("<header class=\"ip-site-header\">", html);
        Assert.Contains("<main id=\"main\">", html);
        Assert.Contains("<footer class=\"ip-site-footer\">", html);
        Assert.Contains("<nav class=\"ip-nav\" aria-label=\"Main\">", html);
        Assert.Contains("<a href=\"/\" aria-current=\"page\">Home</a>", html);
        Assert.Contains("<a href=\"/about/\">About</a>", html);
        Assert.DoesNotContain("aria-current=\"page\">About", html);

        // The brand mark links home; the footer carries the static Canine byline.
        Assert.Contains("<a class=\"ip-brand\" href=\"/\">", html);
        Assert.Contains("by <a href=\"https://canine.dev\">Canine Development</a>", html);

        // Block instance content renders from its definition.
        Assert.Contains("Reusable promo block", html);

        // No editor residue. The only external URL is the fixed Canine byline in the
        // footer chrome; the page CONTENT and nav still carry no external links (this
        // site sets none), so there is no other https:// and no http:// at all.
        Assert.DoesNotContain("data-node-id", html);
        Assert.DoesNotContain("data-node-type", html);
        Assert.DoesNotContain("http://", html);
        Assert.Single(Regex.Matches(html, "https://"));
        Assert.Contains("https://canine.dev", html);
    }

    [Fact]
    public async Task Images_render_srcset_sizes_dimensions_and_loading_attributes()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        var prefix = $"/assets/{scenario.ImageId.Compact}-";

        // srcset lists every variant with width descriptors; src is the middle variant.
        Assert.Contains($"src=\"{prefix}960.", html);
        var srcset = Regex.Match(html, "srcset=\"([^\"]+)\"");
        Assert.True(srcset.Success);
        Assert.Contains("480w", srcset.Groups[1].Value);
        Assert.Contains("960w", srcset.Groups[1].Value);
        Assert.Contains("1440w", srcset.Groups[1].Value);

        // sizes computed from the layout context; intrinsic dimensions → zero CLS.
        Assert.Contains("sizes=\"", html);
        Assert.Contains("width=\"1440\" height=\"960\"", html);
        Assert.Contains("alt=\"A studio photo\"", html);

        // LCP care: exactly one eager/high-priority image, the rest lazy.
        Assert.Single(Regex.Matches(html, "loading=\"eager\" fetchpriority=\"high\""));
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("decoding=\"async\"", html);
    }

    [Fact]
    public async Task Widgets_render_islands_and_the_loader_ships_only_with_them()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var home = host.ReadText("index.html");
        // Every island carries the ip-widget marker class, so stylesheets can target "a widget"
        // without enumerating tags (the hero two-up layout used to name each card tag).
        Assert.Contains("<x-note class=\"ip-widget\" text=\"hello\" data-island=\"/widgets/x-note.", home);
        Assert.Contains(">x-note placeholder</x-note>", home);
        Assert.Contains(PublisherScripts.IslandLoader, home);

        // The island loader is emitted at the END of <body>, after the islands it queries.
        Assert.True(
            home.IndexOf("<x-note", StringComparison.Ordinal) <
            home.IndexOf(PublisherScripts.IslandLoader, StringComparison.Ordinal));

        // A page without widgets carries no loader — and only one inline script.
        var about = host.ReadText("about/index.html");
        Assert.DoesNotContain(PublisherScripts.IslandLoader, about);
        Assert.Single(Regex.Matches(about, "<script>"));
    }

    [Fact]
    public async Task Localized_variant_renders_locale_lang_paths_and_navigation()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("da/index.html");

        Assert.StartsWith("<!doctype html>\n<html lang=\"da\">", html);
        Assert.Contains("<title>Hjem · Acme Studio</title>", html);
        Assert.Contains("<link rel=\"canonical\" href=\"/da/\"", html);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"x-default\" href=\"/\"", html);

        // Nav links stay inside the locale; labels resolve per locale.
        Assert.Contains("<a href=\"/da/\" aria-current=\"page\">Hjem</a>", html);
        Assert.Contains("<a href=\"/da/about/\">Om os</a>", html);

        // Untranslated content falls back to the default locale rather than vanishing.
        Assert.Contains("Make something people remember", html);
    }

    [Fact]
    public async Task Sanitized_svg_is_inlined_with_accessible_naming_and_never_copied_as_a_file()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var svgId = await host.CreateSvgAsset();
        var pageId = await host.CreatePage(siteId, "graphics", "Graphics");
        await host.AddSection(pageId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new SvgNode { Id = NodeId.New(), AssetId = svgId, Alt = LocalizedText.Of(PublishingTestHost.En, "Logo mark") }),
        });
        await host.SetNavigation(siteId, pageId);
        await host.Publish(pageId);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.Contains("<svg viewBox=\"0 0 10 10\">", html);
        Assert.Contains("role=\"img\"", html);
        Assert.Contains("aria-label=\"Logo mark\"", html);

        // Inline only: no .svg file lands in assets/.
        Assert.Empty(host.FilesMatching("assets/", ".svg"));
    }

    [Fact]
    public async Task Links_to_unpublished_pages_unwrap_to_inert_text()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var draftId = await host.CreatePage(siteId, "draft", "Draft");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new ButtonNode
            {
                Id = NodeId.New(),
                Label = LocalizedText.Of(PublishingTestHost.En, "Read the draft"),
                LinkTo = new PageLink(draftId),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.Contains(">Read the draft</span>", html);
        Assert.DoesNotContain(">Read the draft</a>", html);
        Assert.False(host.FileExists("draft/index.html"));
    }
}
