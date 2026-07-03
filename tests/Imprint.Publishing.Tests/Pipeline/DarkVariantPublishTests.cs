using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The publishing plane of the light/dark media feature: dark WebP variants are copied
/// under a <c>-dark-</c> name, dark SVGs are inlined (never a file), and a re-processed
/// dark variant re-renders the pages that show it — all without breaking the zero-rewrite
/// determinism guarantee (docs/proposals/theme-media-and-widget-approval.md §Part 1).
/// </summary>
public sealed class DarkVariantPublishTests
{
    private static readonly Locale En = PublishingTestHost.En;

    [Fact]
    public async Task Dark_image_variants_are_copied_and_both_renditions_ship()
    {
        await using var host = new PublishingTestHost();
        var (_, imageId) = await BuildSiteWithImage(host);
        await host.AddDarkImageVariant(imageId, 480, 960);

        await host.Publisher.Synchronize();

        var webp = host.FilesMatching("assets/", ".webp");
        var darkFiles = webp.Where(f => f.Contains("-dark-", StringComparison.Ordinal)).ToList();
        Assert.Equal(4, webp.Count); // 2 base + 2 dark
        Assert.Equal(2, darkFiles.Count);
        Assert.Contains(darkFiles, f => f.Contains("-dark-480.", StringComparison.Ordinal));
        Assert.Contains(darkFiles, f => f.Contains("-dark-960.", StringComparison.Ordinal));

        var html = host.ReadText("index.html");
        Assert.Contains("class=\"ip-img ip-img-light\"", html);
        Assert.Contains("class=\"ip-img ip-img-dark\"", html);
        // The dark <img> points at the copied dark files.
        foreach (var file in darkFiles)
        {
            Assert.Contains($"/{file}", html);
        }
    }

    [Fact]
    public async Task Dark_svg_is_inlined_and_never_written_as_a_file()
    {
        await using var host = new PublishingTestHost();
        var (_, svgId) = await BuildSiteWithSvg(host);
        await host.AddDarkSvgVariant(svgId);

        await host.Publisher.Synchronize();

        // Inlined SVGs produce no assets/ files at all — light or dark.
        Assert.Empty(host.FilesMatching("assets/"));

        var html = host.ReadText("index.html");
        Assert.Contains("class=\"ip-svg ip-img-light\"", html);
        Assert.Contains("class=\"ip-svg ip-img-dark\"", html);
        Assert.Contains("<path d=\"M0 0h10v10z\"/>", html);        // the light rendition
        Assert.Contains("<circle cx=\"5\" cy=\"5\" r=\"4\"/>", html); // the dark rendition
    }

    [Fact]
    public async Task Reprocessing_a_dark_variant_re_renders_the_referencing_page()
    {
        await using var host = new PublishingTestHost();
        var (_, imageId) = await BuildSiteWithImage(host);
        await host.AddDarkImageVariant(imageId, 480, 960);
        await host.Publisher.Synchronize();

        var homeBefore = host.ReadText("index.html");
        var darkBefore = DarkAssets(host);
        Assert.Equal(2, darkBefore.Count);

        host.MutateDarkImageVariants(imageId, "reprocessed");
        var report = await host.Publisher.Synchronize();

        // The dark hashes join the asset-staleness key, so the page that shows the image
        // re-renders and the dark files rotate to fresh hashes (old ones swept).
        Assert.Equal(1, report.PagesRendered);
        var darkAfter = DarkAssets(host);
        Assert.Equal(2, darkAfter.Count);
        Assert.Empty(darkBefore.Intersect(darkAfter, StringComparer.Ordinal));
        Assert.NotEqual(homeBefore, host.ReadText("index.html"));
    }

    [Fact]
    public async Task Second_synchronize_with_a_dark_variant_is_a_zero_rewrite()
    {
        await using var host = new PublishingTestHost();
        var (_, imageId) = await BuildSiteWithImage(host);
        await host.AddDarkImageVariant(imageId, 480, 960);
        await host.Publisher.Synchronize();
        var before = host.SnapshotWriteTimes();

        var report = await host.Publisher.Synchronize();

        Assert.Equal(0, report.PagesRendered);
        Assert.Equal(0, report.FilesWritten);
        Assert.Equal(0, report.BytesWritten);
        Assert.Empty(report.Errors);
        var after = host.SnapshotWriteTimes();
        Assert.Equal(before.Keys.Order(StringComparer.Ordinal), after.Keys.Order(StringComparer.Ordinal));
        foreach (var (file, time) in before)
        {
            Assert.Equal(time, after[file]);
        }
    }

    [Fact]
    public async Task Removing_a_dark_variant_reverts_to_a_single_rendition_and_sweeps_the_dark_files()
    {
        await using var host = new PublishingTestHost();
        var (_, imageId) = await BuildSiteWithImage(host);
        await host.AddDarkImageVariant(imageId, 480, 960);
        await host.Publisher.Synchronize();
        Assert.Equal(2, DarkAssets(host).Count);

        await host.RemoveDarkVariant(imageId);
        var report = await host.Publisher.Synchronize();

        Assert.Equal(1, report.PagesRendered);
        Assert.Empty(DarkAssets(host));
        var html = host.ReadText("index.html");
        Assert.DoesNotContain("ip-img-dark", html);
        Assert.DoesNotContain("ip-img-light", html);
    }

    private static IReadOnlyList<string> DarkAssets(PublishingTestHost host) =>
        [.. host.FilesMatching("assets/").Where(f => f.Contains("-dark-", StringComparison.Ordinal))];

    private static async Task<(PageId Home, AssetId Image)> BuildSiteWithImage(PublishingTestHost host)
    {
        var siteId = await host.CreateSite("Dark", "en");
        var imageId = await host.CreateImageAsset("logo", 480, 960);
        var home = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(home, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new ImageNode
            {
                Id = NodeId.New(),
                AssetId = imageId,
                Alt = LocalizedText.Of(En, "Our logo"),
            }),
        });
        await host.SetNavigation(siteId, home);
        await host.Publish(home);
        return (home, imageId);
    }

    private static async Task<(PageId Home, AssetId Svg)> BuildSiteWithSvg(PublishingTestHost host)
    {
        var siteId = await host.CreateSite("Dark", "en");
        var svgId = await host.CreateSvgAsset();
        var home = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(home, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new SvgNode
            {
                Id = NodeId.New(),
                AssetId = svgId,
                Alt = LocalizedText.Of(En, "Logo"),
            }),
        });
        await host.SetNavigation(siteId, home);
        await host.Publish(home);
        return (home, svgId);
    }
}
