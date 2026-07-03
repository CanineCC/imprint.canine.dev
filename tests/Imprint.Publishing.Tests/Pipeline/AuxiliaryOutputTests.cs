using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>Sitemap, robots, 404, precompression, the manifest shape and the performance budget.</summary>
public sealed class AuxiliaryOutputTests
{
    [Fact]
    public async Task Sitemap_lists_every_published_url_with_hreflang_alternates()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var sitemap = host.ReadText("sitemap.xml");

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", sitemap);
        Assert.Contains("xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"", sitemap);

        // 2 pages × 2 locales = 4 URLs; root-relative locs when no BaseUrl is set.
        Assert.Equal(4, Regex.Matches(sitemap, "<url>").Count);
        Assert.Contains("<loc>/</loc>", sitemap);
        Assert.Contains("<loc>/about/</loc>", sitemap);
        Assert.Contains("<loc>/da/</loc>", sitemap);
        Assert.Contains("<loc>/da/about/</loc>", sitemap);

        Assert.Contains("<xhtml:link rel=\"alternate\" hreflang=\"da\" href=\"/da/about/\" />", sitemap);
        Assert.Contains("<xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"/\" />", sitemap);
    }

    [Fact]
    public async Task Robots_allows_everything_and_points_at_the_sitemap()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var robots = host.ReadText("robots.txt");
        Assert.Contains("User-agent: *", robots);
        Assert.Contains("Allow: /", robots);
        Assert.Contains("Sitemap: /sitemap.xml", robots);
    }

    [Fact]
    public async Task NotFound_page_is_themed_in_the_default_locale_and_links_home()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("404.html");

        Assert.StartsWith("<!doctype html>\n<html lang=\"en\">", html);
        Assert.Contains("<title>Page not found · Acme Studio</title>", html);
        Assert.Contains("<h1>Page not found</h1>", html);
        Assert.Contains("<a href=\"/\">Go to the front page</a>", html);

        // Themed: the same single hashed stylesheet and the theme toggle — but never
        // the island loader (no islands on a 404) and no canonical URL.
        var css = Assert.Single(host.FilesMatching("css/site.", ".css"));
        Assert.Contains($"<link rel=\"stylesheet\" href=\"/{css}\"", html);
        Assert.Contains(PublisherScripts.ThemeToggle, html);
        Assert.DoesNotContain(PublisherScripts.IslandLoader, html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
    }

    [Fact]
    public async Task Precompressed_siblings_are_smaller_and_decompress_to_the_original()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var css = Assert.Single(host.FilesMatching("css/site.", ".css"));
        foreach (var file in (string[])["index.html", css])
        {
            var original = host.ReadBytes(file);
            var brotli = host.ReadBytes(file + ".br");
            var gzip = host.ReadBytes(file + ".gz");
            Assert.True(brotli.Length < original.Length, $"{file}.br is not smaller");
            Assert.True(gzip.Length < original.Length, $"{file}.gz is not smaller");

            using var decompressor = new BrotliStream(new MemoryStream(brotli), CompressionMode.Decompress);
            using var roundTrip = new MemoryStream();
            decompressor.CopyTo(roundTrip);
            Assert.Equal(original, roundTrip.ToArray());
        }
    }

    [Fact]
    public async Task Manifest_matches_the_documented_shape()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        using var manifest = host.ReadManifest();
        var root = manifest.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        var siteVersion = await host.SiteVersion(scenario.SiteId);
        Assert.Equal(siteVersion, root.GetProperty("siteVersion").GetInt64());

        var home = root.GetProperty("pages").GetProperty(scenario.HomeId.Compact);
        Assert.True(home.GetProperty("publishedVersion").GetInt64() > 0);
        Assert.Equal(siteVersion, home.GetProperty("renderedAtSiteVersion").GetInt64());
        Assert.Equal(["/", "/da/"], home.GetProperty("paths").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(3, home.GetProperty("assetHashes").GetArrayLength());
        Assert.False(home.TryGetProperty("error", out _));

        var about = root.GetProperty("pages").GetProperty(scenario.AboutId.Compact);
        Assert.Equal(["/about/", "/da/about/"], about.GetProperty("paths").EnumerateArray().Select(p => p.GetString()));
        Assert.Empty(about.GetProperty("assetHashes").EnumerateArray());

        // cssHash and widgetBundles are the very hashes in the published file names.
        var cssHash = root.GetProperty("cssHash").GetString();
        Assert.True(host.FileExists($"css/site.{cssHash}.css"));
        var noteHash = root.GetProperty("widgetBundles").GetProperty("x-note").GetString();
        Assert.True(host.FileExists($"widgets/x-note.{noteHash}.js"));
    }

    [Fact]
    public async Task BaseUrl_makes_canonical_hreflang_and_sitemap_absolute_but_keeps_content_links_relative()
    {
        await using var host = new PublishingTestHost(baseUrl: "https://example.test/");
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");
        Assert.Contains("<link rel=\"canonical\" href=\"https://example.test/\"", html);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"da\" href=\"https://example.test/da/\"", html);

        // Content stays origin-agnostic: nav and assets remain root-relative.
        Assert.Contains("<a href=\"/\" aria-current=\"page\">Home</a>", html);
        Assert.Contains("src=\"/assets/", html);

        Assert.Contains("<loc>https://example.test/about/</loc>", host.ReadText("sitemap.xml"));
        Assert.Contains("Sitemap: https://example.test/sitemap.xml", host.ReadText("robots.txt"));
    }

    [Fact]
    public async Task Slug_collision_errors_both_pages_and_first_in_wins_the_path()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        // No slice-level uniqueness check runs here (we drive aggregates directly),
        // which is exactly the documented race the publisher must absorb.
        var first = await host.CreatePage(siteId, "team", "Team One");
        var second = await host.CreatePage(siteId, "team", "Team Two");
        await host.Publish(first);
        await host.Publish(second);

        var report = await host.Publisher.Synchronize();

        Assert.Equal(2, report.Errors.Count);
        Assert.All(report.Errors, error => Assert.Contains("team", error.Message));

        using var manifest = host.ReadManifest();
        var pages = manifest.RootElement.GetProperty("pages");
        var firstPaths = pages.GetProperty(first.Compact).GetProperty("paths").GetArrayLength();
        var secondPaths = pages.GetProperty(second.Compact).GetProperty("paths").GetArrayLength();
        Assert.NotNull(pages.GetProperty(first.Compact).GetProperty("error").GetString());
        Assert.NotNull(pages.GetProperty(second.Compact).GetProperty("error").GetString());

        // Exactly one page owns /team/, decided deterministically.
        Assert.True(host.FileExists("team/index.html"));
        Assert.True((firstPaths > 0) ^ (secondPaths > 0), "exactly one claimant may own the path");
        var winnerTitle = firstPaths > 0 ? "Team One" : "Team Two";
        Assert.Contains(winnerTitle, host.ReadText("team/index.html"));

        // The outcome is stable: a second pass keeps the same winner and stays quiet on disk.
        var again = await host.Publisher.Synchronize();
        Assert.Equal(0, again.FilesWritten);
        Assert.Contains(winnerTitle, host.ReadText("team/index.html"));
    }

    [Fact]
    public async Task Templated_home_page_meets_the_performance_budget()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        // Over-the-wire = the precompressed brotli siblings the host actually serves.
        var htmlBrotli = host.ReadBytes("index.html.br").Length;
        var css = Assert.Single(host.FilesMatching("css/site.", ".css"));
        var cssBrotli = host.ReadBytes(css + ".br").Length;
        var inlineJsBrotli = BrotliSize(Encoding.UTF8.GetBytes(PublisherScripts.ThemeToggle + PublisherScripts.IslandLoader));

        Assert.True(htmlBrotli <= 15 * 1024, $"home page is {htmlBrotli} B brotli (budget 15 KB)");
        Assert.True(cssBrotli <= 12 * 1024, $"stylesheet is {cssBrotli} B brotli (budget 12 KB)");
        Assert.True(inlineJsBrotli <= 1536, $"inline JS is {inlineJsBrotli} B brotli (budget 1.5 KB)");
    }

    private static int BrotliSize(byte[] content)
    {
        using var buffer = new MemoryStream();
        using (var brotli = new BrotliStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            brotli.Write(content);
        }

        return (int)buffer.Length;
    }
}
