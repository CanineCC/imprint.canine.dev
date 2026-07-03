namespace Imprint.Publishing.Tests.Pipeline;

public sealed class FullPublishTests
{
    [Fact]
    public async Task Full_publish_produces_the_complete_output_layout()
    {
        await using var host = new PublishingTestHost();
        var scenario = await TemplatedSiteScenario.Build(host);

        var report = await host.Publisher.Synchronize();

        Assert.Equal(2, report.PagesRendered);
        Assert.Empty(report.Errors);

        // Pages: default locale at / and /{slug}/, Danish under /da/.
        Assert.True(host.FileExists("index.html"));
        Assert.True(host.FileExists("about/index.html"));
        Assert.True(host.FileExists("da/index.html"));
        Assert.True(host.FileExists("da/about/index.html"));

        // Site-wide outputs.
        Assert.True(host.FileExists("404.html"));
        Assert.True(host.FileExists("robots.txt"));
        Assert.True(host.FileExists("sitemap.xml"));
        Assert.True(host.FileExists("publish-manifest.json"));
        Assert.Single(host.FilesMatching("css/site.", ".css"));

        // One hashed webp per generated variant of the referenced image.
        var imagePrefix = $"assets/{scenario.ImageId.Compact}-";
        Assert.Equal(3, host.FilesMatching(imagePrefix, ".webp").Count);

        // The used widget's bundle is copied once, hashed; the unused one is not.
        Assert.Single(host.FilesMatching("widgets/x-note.", ".js"));
        Assert.Empty(host.FilesMatching("widgets/x-extra."));

        // Every text output has precompressed siblings.
        foreach (var file in host.AllFiles().Where(f =>
                     f.EndsWith(".html", StringComparison.Ordinal) ||
                     f.EndsWith(".css", StringComparison.Ordinal) ||
                     f.EndsWith(".js", StringComparison.Ordinal) ||
                     f.EndsWith(".xml", StringComparison.Ordinal) ||
                     f.EndsWith(".txt", StringComparison.Ordinal)))
        {
            Assert.True(host.FileExists(file + ".br"), $"{file}.br missing");
            Assert.True(host.FileExists(file + ".gz"), $"{file}.gz missing");
        }

        // The status singleton carries the report for the editor's status bar.
        Assert.NotNull(host.Status.Last);
        Assert.Equal(report, host.Status.Last);
    }

    [Fact]
    public async Task Deleting_the_output_directory_and_synchronizing_is_a_full_republish()
    {
        await using var host = new PublishingTestHost();
        await TemplatedSiteScenario.Build(host);
        await host.Publisher.Synchronize();
        var before = host.AllFiles();

        Directory.Delete(host.OutputPath, recursive: true);
        var report = await host.Publisher.Synchronize();

        Assert.Equal(2, report.PagesRendered);
        Assert.Equal(before, host.AllFiles());
    }

    [Fact]
    public async Task Synchronize_with_no_site_publishes_nothing_and_touches_nothing()
    {
        await using var host = new PublishingTestHost();

        var report = await host.Publisher.Synchronize();

        Assert.Equal(0, report.PagesRendered);
        Assert.Equal(0, report.FilesWritten);
        Assert.Empty(host.AllFiles());
    }
}
