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

        // An SVG logo: the catalog only inlines SVGs (no assets/ file), so there is no file
        // URL to reference. It must degrade to the brand dot rather than emit a dead /media URL.
        var svgId = await host.CreateSvgAsset();
        await host.SetHeaderLogo(siteId, svgId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.DoesNotContain("/media/", html);
        Assert.DoesNotContain("ip-brand-logo", html); // no logo <img>
        Assert.Contains("ip-brand-dot", html);         // graceful fallback
    }
}
