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
    // A mark drawn as a vector is the favicon as it is: one favicon.svg holding both renditions, and the browser's
    // own colour scheme picks one (the dark ink on light tab bars, the light ink on dark), like the header's mark.
    [Fact]
    public async Task A_vector_favicon_with_a_dark_rendition_ships_as_one_svg_that_follows_the_colour_scheme()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        var markId = await host.CreateSvgAsset("<svg viewBox=\"0 0 10 10\" width=\"512\" height=\"512\"><path d=\"M0 0h10v10z\" fill=\"#14171c\"/></svg>");
        await host.AddDarkSvgVariant(markId, "<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\" fill=\"#eceef2\"/></svg>");
        await host.SetFavicon(siteId, markId);

        await host.Publisher.Synchronize();

        var link = Regex.Match(host.ReadText("index.html"), "<link rel=\"icon\" href=\"(/favicon\\.svg\\?v=[0-9a-f]+)\" type=\"image/svg(?:\\+|&#x2B;)xml\" />");
        // The renderer writes the '+' as &#x2B;, which is the same attribute value to a browser.
        Assert.True(link.Success, "the favicon link should point at /favicon.svg with a content version");
        var svg = host.ReadText("favicon.svg");
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-color-scheme: dark)", svg, StringComparison.Ordinal);
        Assert.Contains("fill=\"#14171c\"", svg, StringComparison.Ordinal);
        Assert.Contains("fill=\"#eceef2\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("width=\"512\"", svg, StringComparison.Ordinal);

        // Back to a raster favicon: the svg file is swept and the link follows.
        var rasterId = await host.CreateImageAsset("favicon", 32, 64);
        await host.SetFavicon(siteId, rasterId);
        await host.Publisher.Synchronize();
        Assert.False(host.FileExists("favicon.svg"));
        Assert.Contains($"/assets/{rasterId.Compact}-32.", host.ReadText("index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_vector_favicon_without_a_dark_rendition_ships_as_it_is()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        var markId = await host.CreateSvgAsset("<svg viewBox=\"0 0 10 10\"><path d=\"M0 0h10v10z\" fill=\"#14171c\"/></svg>");
        await host.SetFavicon(siteId, markId);

        await host.Publisher.Synchronize();

        var svg = host.ReadText("favicon.svg");
        Assert.Contains("fill=\"#14171c\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("prefers-color-scheme", svg, StringComparison.Ordinal);
        Assert.Contains("href=\"/favicon.svg?v=", host.ReadText("index.html"), StringComparison.Ordinal);
    }
}

