using System.Text.RegularExpressions;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The favicon + header-logo brand imagery must ship like page images: their bytes land
/// under <c>assets/</c> and the published HTML references those <c>/assets/…</c> URLs (never
/// the editor-only <c>/media/…</c> route, which 404s in the deploy output and /preview).
/// </summary>
public sealed class BrandImagePublishTests
{
    [Fact]
    public async Task Raster_favicon_and_logo_publish_to_assets_and_the_files_exist()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // A small square favicon and a wider header logo, each raster with two variants.
        var faviconId = await host.CreateImageAsset("favicon", 32, 64);
        var logoId = await host.CreateImageAsset("logo", 200, 400);
        await host.SetFavicon(siteId, faviconId);
        await host.SetHeaderLogo(siteId, logoId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        // No editor-only /media/ URL survives into the published output.
        Assert.DoesNotContain("/media/", html);

        // The favicon <link rel="icon"> points at a published /assets URL — the smallest variant.
        var favicon = Regex.Match(html, "<link rel=\"icon\" href=\"(/assets/[^\"]+)\"");
        Assert.True(favicon.Success, "favicon link should reference an /assets URL");
        Assert.Contains($"/assets/{faviconId.Compact}-32.", favicon.Groups[1].Value);

        // The brand logo <img> points at a published /assets URL — a modest (second) variant.
        var logo = Regex.Match(html, "<img class=\"ip-brand-logo\" src=\"(/assets/[^\"]+)\"");
        Assert.True(logo.Success, "brand logo should reference an /assets URL");
        Assert.Contains($"/assets/{logoId.Compact}-400.", logo.Groups[1].Value);

        // Every referenced brand asset URL resolves to a file that actually exists on disk.
        foreach (Match m in Regex.Matches(html, "/assets/[A-Za-z0-9.\\-]+\\.webp"))
        {
            var relative = m.Value.TrimStart('/');
            Assert.True(host.FileExists(relative), $"published asset {relative} should exist on disk");
        }

        // Concretely: both the favicon and the logo variant files are present under assets/.
        Assert.True(host.FilesMatching($"assets/{faviconId.Compact}-32.", ".webp").Count > 0);
        Assert.True(host.FilesMatching($"assets/{logoId.Compact}-400.", ".webp").Count > 0);
    }

    [Fact]
    public async Task Vector_brand_image_falls_back_gracefully_pending_svg_file_support()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // An SVG logo INLINES into the brand mark — that is what lets a currentColor mark
        // take the header's ink and follow the theme with no second rendition. The markup
        // is the catalog's sanitized SVG; no /media URL and no <img> are emitted.
        var svgId = await host.CreateSvgAsset(
            "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\" stroke=\"currentColor\"/></svg>");
        await host.SetHeaderLogo(siteId, svgId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.DoesNotContain("/media/", html);
        Assert.Contains("<span class=\"ip-brand-logo\" aria-hidden=\"true\"><svg", html);
        Assert.Contains("stroke=\"currentColor\"", html);
        Assert.DoesNotContain("<img class=\"ip-brand-logo\"", html);
        Assert.DoesNotContain("ip-brand-dot", html);
    }

    [Fact]
    public async Task An_SVG_logo_with_a_dark_rendition_ships_both_and_the_theme_reveals_one()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // A two-colour mark cannot follow the theme through currentColor alone: its second colour needs its own
        // dark rendition, exactly like a page image's. Dropping it put the light mark on a dark header.
        var svgId = await host.CreateSvgAsset("<svg viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"4\" fill=\"#2f5d85\"/></svg>");
        await host.AddDarkSvgVariant(svgId, "<svg viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"4\" fill=\"#9bbedb\"/></svg>");
        await host.SetHeaderLogo(siteId, svgId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        var header = html[html.IndexOf("<header", StringComparison.Ordinal)..html.IndexOf("</header>", StringComparison.Ordinal)];
        Assert.Contains("<span class=\"ip-brand-logo ip-img-light\" aria-hidden=\"true\"><svg", header);
        Assert.Contains("<span class=\"ip-brand-logo ip-img-dark\" aria-hidden=\"true\"><svg", header);
        Assert.Contains("#2f5d85", header);
        Assert.Contains("#9bbedb", header);
        var footer = html[html.IndexOf("ip-footer-brand", StringComparison.Ordinal)..];
        Assert.Contains("ip-brand-logo ip-img-dark", footer);
    }

    [Fact]
    public async Task An_SVG_favicon_publishes_as_a_file_and_the_page_links_it()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // A browser tab takes an SVG icon; a vector favicon used to resolve to no URL at all, so the site had none.
        const string svg = "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\" fill=\"#0b7a5e\"/></svg>";
        var svgId = await host.CreateSvgAsset(svg);
        await host.SetFavicon(siteId, svgId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        // The renderer writes the + of the media type as &#x2B;; a browser decodes the attribute, so the test does too.
        var favicon = Regex.Match(System.Net.WebUtility.HtmlDecode(html), "<link rel=\"icon\" href=\"(/assets/[^\"]+\\.svg)\" type=\"image/svg\\+xml\"");
        Assert.True(favicon.Success, "an SVG favicon should be linked as image/svg+xml from /assets, got: "
            + Regex.Match(html, "<link rel=\"icon\"[^>]*>").Value);
        var relative = favicon.Groups[1].Value.TrimStart('/');
        Assert.True(host.FileExists(relative), $"the favicon file {relative} should exist on disk");
        Assert.Equal(svg, host.ReadText(relative));

        // A later publish keeps it: the sweep must not delete the file the page links.
        await host.Publisher.Synchronize();
        Assert.True(host.FileExists(relative), "the sweep removed the favicon the page still links");
    }
}
