namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>The diff-driven guarantees: staleness rules, determinism, and the sweep.</summary>
public sealed class IncrementalPublishTests
{
    [Fact]
    public async Task Second_synchronize_is_a_zero_rewrite()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();
        var before = host.SnapshotWriteTimes();

        var report = await host.Publisher.Synchronize();

        Assert.Equal(0, report.PagesRendered);
        Assert.Equal(0, report.PagesRemoved);
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
    public async Task Unpublish_removes_files_locale_variants_manifest_entry_and_nav_links()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        await host.Unpublish(scenario.AboutId);
        var report = await host.Publisher.Synchronize();

        Assert.Equal(1, report.PagesRemoved);
        Assert.False(host.FileExists("about/index.html"));
        Assert.False(host.FileExists("about/index.html.br"));
        Assert.False(host.FileExists("about/index.html.gz"));
        Assert.False(host.FileExists("da/about/index.html"));
        Assert.False(Directory.Exists(host.FullPath("about")));

        using var manifest = host.ReadManifest();
        Assert.False(manifest.RootElement.GetProperty("pages").TryGetProperty(scenario.AboutId.Compact, out _));
        Assert.DoesNotContain("/about/", host.ReadText("sitemap.xml"));

        // The nav is shared chrome: remaining pages re-render without the dead link.
        Assert.Equal(1, report.PagesRendered);
        Assert.DoesNotContain("/about/", host.ReadText("index.html"));
    }

    [Fact]
    public async Task Theme_token_change_rerenders_every_page_but_copies_no_assets()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var cssBefore = Assert.Single(host.FilesMatching("css/site.", ".css"));
        var assetTimesBefore = host.SnapshotWriteTimes()
            .Where(pair => pair.Key.StartsWith("assets/", StringComparison.Ordinal))
            .ToDictionary();
        Assert.NotEmpty(assetTimesBefore);

        await host.SetThemeToken(scenario.SiteId, "primary", "#112233", "#445566");
        var report = await host.Publisher.Synchronize();

        // Chrome version moved → every published page re-renders against the new css.
        Assert.Equal(2, report.PagesRendered);
        var cssAfter = Assert.Single(host.FilesMatching("css/site.", ".css"));
        Assert.NotEqual(cssBefore, cssAfter);
        Assert.False(host.FileExists(cssBefore));
        Assert.Contains(cssAfter, host.ReadText("index.html"));
        Assert.Contains(cssAfter, host.ReadText("da/about/index.html"));
        Assert.Contains("#112233", host.ReadText(cssAfter));

        // ... but no asset was copied again: hashed names, untouched files.
        foreach (var (file, time) in assetTimesBefore)
        {
            Assert.True(host.FileExists(file), $"{file} disappeared");
            Assert.Equal(time, File.GetLastWriteTimeUtc(host.FullPath(file)));
        }
    }

    [Fact]
    public async Task Asset_reprocess_rerenders_only_the_referencing_pages()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var assetsBefore = host.FilesMatching("assets/");
        var homeBefore = host.ReadText("index.html");
        var aboutTime = File.GetLastWriteTimeUtc(host.FullPath("about/index.html"));

        host.MutateImageVariants(scenario.ImageId, "reprocessed");
        var report = await host.Publisher.Synchronize();

        // Only the page that shows the image re-renders.
        Assert.Equal(1, report.PagesRendered);
        Assert.NotEqual(homeBefore, host.ReadText("index.html"));
        Assert.Equal(aboutTime, File.GetLastWriteTimeUtc(host.FullPath("about/index.html")));

        // Hash rotation: three new variant files, the old ones swept.
        var assetsAfter = host.FilesMatching("assets/");
        Assert.Equal(3, assetsAfter.Count);
        Assert.Empty(assetsBefore.Intersect(assetsAfter, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Widget_bundle_change_rotates_the_hashed_copy_and_rerenders_only_its_users()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        var bundleBefore = Assert.Single(host.FilesMatching("widgets/x-note.", ".js"));
        var aboutTime = File.GetLastWriteTimeUtc(host.FullPath("about/index.html"));

        host.WriteWidgets(
            ("x-note", "export default class XNote extends HTMLElement { connectedCallback() { this.textContent = 'v2'; } }\n"),
            ("x-extra", "export default class XExtra extends HTMLElement {}\n"));
        var report = await host.Publisher.Synchronize();

        Assert.Equal(1, report.PagesRendered);
        var bundleAfter = Assert.Single(host.FilesMatching("widgets/x-note.", ".js"));
        Assert.NotEqual(bundleBefore, bundleAfter);
        Assert.False(host.FileExists(bundleBefore));
        Assert.Contains($"data-island=\"/{bundleAfter}\"", host.ReadText("index.html"));
        Assert.Equal(aboutTime, File.GetLastWriteTimeUtc(host.FullPath("about/index.html")));
    }

    [Fact]
    public async Task Slug_change_takes_effect_on_republish_and_sweeps_the_old_paths()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();

        // A slug edit alone changes only the draft — the published output stays put.
        await host.ChangeSlug(scenario.AboutId, "info");
        var untouched = await host.Publisher.Synchronize();
        Assert.Equal(0, untouched.PagesRendered);
        Assert.True(host.FileExists("about/index.html"));

        await host.Publish(scenario.AboutId);
        var report = await host.Publisher.Synchronize();

        // The page moved AND the shared nav points at it → both pages re-render.
        Assert.Equal(2, report.PagesRendered);
        Assert.True(host.FileExists("info/index.html"));
        Assert.True(host.FileExists("da/info/index.html"));
        Assert.False(host.FileExists("about/index.html"));
        Assert.False(Directory.Exists(host.FullPath("about")));
        Assert.Contains("<a href=\"/info/\">About</a>", host.ReadText("index.html"));
        Assert.Contains("<loc>/info/</loc>", host.ReadText("sitemap.xml"));
    }
}
