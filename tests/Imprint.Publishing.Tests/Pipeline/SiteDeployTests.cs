using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The SaaS deploy plane: publishing a site into a named environment folder, promoting
/// the exact rendered bytes up the pipeline, per-environment status, and the sandbox
/// that keeps a folder from escaping the deploy root.
/// </summary>
public sealed class SiteDeployTests
{
    private static async Task<(SiteId Site, PageId Home)> OneSite(PublishingTestHost host, string name = "Acme")
    {
        var siteId = await host.CreateSite(name, "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new HeadingNode
            {
                Id = NodeId.New(),
                Level = 1,
                Text = LocalizedText.Of(PublishingTestHost.En, $"Welcome to {name}"),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        return (siteId, homeId);
    }

    [Fact]
    public async Task PublishToEnvironment_renders_the_site_into_the_environment_folder()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        var testFolder = Path.Combine(host.Root, "test-env");
        await host.SetEnvironments(siteId, ("Test", testFolder), ("Production", Path.Combine(host.Root, "prod-env")));

        var report = await host.Deploy.PublishToEnvironment(siteId, "Test");

        Assert.Equal(1, report.PagesRendered);
        var index = Path.Combine(testFolder, "index.html");
        Assert.True(File.Exists(index));
        Assert.Contains("Welcome to Acme", await File.ReadAllTextAsync(index));
        Assert.True(File.Exists(Path.Combine(testFolder, PublishManifest.FileName)));
        // The other environment was not touched by a publish-to-Test.
        Assert.False(Directory.Exists(Path.Combine(host.Root, "prod-env")));
    }

    [Fact]
    public async Task PublishToEnvironment_is_case_insensitive_on_the_environment_name()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        var testFolder = Path.Combine(host.Root, "test-env");
        await host.SetEnvironments(siteId, ("Test", testFolder));

        await host.Deploy.PublishToEnvironment(siteId, "tEsT");

        Assert.True(File.Exists(Path.Combine(testFolder, "index.html")));
    }

    [Fact]
    public async Task PublishToEnvironment_with_an_unknown_environment_throws()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        await host.SetEnvironments(siteId, ("Test", Path.Combine(host.Root, "test-env")));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.Deploy.PublishToEnvironment(siteId, "Staging"));

        Assert.Contains("no environment named 'Staging'", error.Message);
    }

    [Fact]
    public async Task Promote_mirrors_the_rendered_bytes_from_one_environment_to_the_next()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        var testFolder = Path.Combine(host.Root, "test-env");
        var prodFolder = Path.Combine(host.Root, "prod-env");
        await host.SetEnvironments(siteId, ("Test", testFolder), ("Production", prodFolder));
        await host.Deploy.PublishToEnvironment(siteId, "Test");

        await host.Deploy.Promote(siteId, "Test", "Production");

        var testFiles = PublishingTestHost.FilesUnder(testFolder);
        var prodFiles = PublishingTestHost.FilesUnder(prodFolder);
        Assert.NotEmpty(prodFiles);
        Assert.Equal(testFiles, prodFiles);
        // Byte-for-byte: promotion is a copy, not a re-render.
        foreach (var relative in testFiles)
        {
            Assert.Equal(
                await File.ReadAllBytesAsync(Path.Combine(testFolder, relative)),
                await File.ReadAllBytesAsync(Path.Combine(prodFolder, relative)));
        }
    }

    [Fact]
    public async Task Promote_removes_files_that_left_the_source_since_the_last_promotion()
    {
        await using var host = new PublishingTestHost();
        var (siteId, homeId) = await OneSite(host);
        var aboutId = await host.CreatePage(siteId, "about", "About");
        await host.AddSection(aboutId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new HeadingNode { Id = NodeId.New(), Level = 1, Text = LocalizedText.Of(PublishingTestHost.En, "About") }),
        });
        await host.SetNavigation(siteId, homeId, aboutId);
        await host.Publish(aboutId);
        var testFolder = Path.Combine(host.Root, "test-env");
        var prodFolder = Path.Combine(host.Root, "prod-env");
        await host.SetEnvironments(siteId, ("Test", testFolder), ("Production", prodFolder));

        // First promotion carries About/ to production.
        await host.Deploy.PublishToEnvironment(siteId, "Test");
        await host.Deploy.Promote(siteId, "Test", "Production");
        Assert.True(File.Exists(Path.Combine(prodFolder, "about", "index.html")));

        // About is unpublished, Test re-rendered without it, then promoted again.
        await host.Unpublish(aboutId);
        await host.Deploy.PublishToEnvironment(siteId, "Test");
        await host.Deploy.Promote(siteId, "Test", "Production");

        Assert.False(File.Exists(Path.Combine(prodFolder, "about", "index.html")));
        Assert.Equal(PublishingTestHost.FilesUnder(testFolder), PublishingTestHost.FilesUnder(prodFolder));
    }

    [Fact]
    public async Task Promote_before_the_source_is_published_throws()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        await host.SetEnvironments(
            siteId, ("Test", Path.Combine(host.Root, "test-env")), ("Production", Path.Combine(host.Root, "prod-env")));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.Deploy.Promote(siteId, "Test", "Production"));

        Assert.Contains("not been published yet", error.Message);
    }

    [Fact]
    public async Task Promote_between_environments_sharing_a_folder_throws()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        var shared = Path.Combine(host.Root, "shared");
        await host.SetEnvironments(siteId, ("Test", shared), ("Production", shared));
        await host.Deploy.PublishToEnvironment(siteId, "Test");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.Deploy.Promote(siteId, "Test", "Production"));

        Assert.Contains("same folder", error.Message);
    }

    [Fact]
    public async Task StatusOf_reports_deployed_and_undeployed_environments()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);
        var testFolder = Path.Combine(host.Root, "test-env");
        await host.SetEnvironments(siteId, ("Test", testFolder), ("Production", Path.Combine(host.Root, "prod-env")));
        await host.Deploy.PublishToEnvironment(siteId, "Test");

        var statuses = host.Deploy.StatusOf(siteId);

        Assert.Equal(["Test", "Production"], statuses.Select(s => s.Name).ToArray());
        var test = statuses[0];
        Assert.True(test.Deployed);
        Assert.Equal(1, test.PageCount);
        Assert.True(test.SiteVersion > 0);
        Assert.NotNull(test.DeployedAt);
        var prod = statuses[1];
        Assert.False(prod.Deployed);
        Assert.Equal(0, prod.PageCount);
        Assert.Null(prod.DeployedAt);
    }

    [Fact]
    public async Task StatusOf_for_a_site_without_environments_is_empty()
    {
        await using var host = new PublishingTestHost();
        var (siteId, _) = await OneSite(host);

        Assert.Empty(host.Deploy.StatusOf(siteId));
    }

    [Fact]
    public async Task Two_sites_publish_to_their_own_folders_without_cross_contamination()
    {
        await using var host = new PublishingTestHost();
        var (first, _) = await OneSite(host, "First");
        var (second, _) = await OneSite(host, "Second");
        var firstFolder = Path.Combine(host.Root, "first-env");
        var secondFolder = Path.Combine(host.Root, "second-env");
        await host.SetEnvironments(first, ("Test", firstFolder));
        await host.SetEnvironments(second, ("Test", secondFolder));

        await host.Deploy.PublishToEnvironment(first, "Test");
        await host.Deploy.PublishToEnvironment(second, "Test");

        Assert.Contains("Welcome to First", await File.ReadAllTextAsync(Path.Combine(firstFolder, "index.html")));
        Assert.Contains("Welcome to Second", await File.ReadAllTextAsync(Path.Combine(secondFolder, "index.html")));
        Assert.DoesNotContain("Second", await File.ReadAllTextAsync(Path.Combine(firstFolder, "index.html")));
    }

    // ------------------------------------------------------------------- sandbox

    [Fact]
    public async Task Sandboxed_relative_environment_folder_resolves_under_the_deploy_root()
    {
        await using var host = new PublishingTestHost(sandboxDeploys: true);
        var (siteId, _) = await OneSite(host);
        await host.SetEnvironments(siteId, ("Test", "acme/test"));

        await host.Deploy.PublishToEnvironment(siteId, "Test");

        var expected = Path.Combine(host.DeployRoot!, "acme", "test", "index.html");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public async Task Sandboxed_environment_folder_escaping_the_root_is_rejected()
    {
        await using var host = new PublishingTestHost(sandboxDeploys: true);
        var (siteId, _) = await OneSite(host);
        await host.SetEnvironments(siteId, ("Test", "../escape"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.Deploy.PublishToEnvironment(siteId, "Test"));

        Assert.Contains("outside the configured deploy root", error.Message);
    }

    [Fact]
    public async Task Sandboxed_absolute_environment_folder_is_confined_to_the_root()
    {
        // An absolute-looking value must not escape: it is treated as relative to the root.
        await using var host = new PublishingTestHost(sandboxDeploys: true);
        var (siteId, _) = await OneSite(host);
        await host.SetEnvironments(siteId, ("Test", "/etc/imprint"));

        await host.Deploy.PublishToEnvironment(siteId, "Test");

        Assert.True(File.Exists(Path.Combine(host.DeployRoot!, "etc", "imprint", "index.html")));
    }
}
