using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// An asset LINK (a button or a prose anchor to <c>asset:{guid}</c> — a whitepaper PDF) must
/// make the published site carry the file: the link is the only thing that tells the publisher
/// to ship it, and a link into <c>/media/…</c> or to a missing file is exactly the bug this
/// feature exists to close (a download that works in the editor and 404s in production).
/// </summary>
public sealed class AssetDownloadPublishTests
{
    [Fact]
    public async Task A_linked_file_lands_in_assets_and_both_link_forms_point_at_it()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);

        var paperId = await host.CreateFileAsset("whitepaper");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new ButtonNode
                {
                    Id = NodeId.New(),
                    Label = LocalizedText.Of(PublishingTestHost.En, "Download the whitepaper"),
                    LinkTo = new AssetLink(paperId),
                },
                new RichTextNode
                {
                    Id = NodeId.New(),
                    Html = LocalizedText.Of(
                        PublishingTestHost.En,
                        $"<p>Read <a href=\"asset:{paperId.Compact}\">the paper</a> in full.</p>"),
                }),
        });
        await host.Publish(homeId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        // Both link forms resolve to the same published /assets URL...
        var hrefs = System.Text.RegularExpressions.Regex
            .Matches(html, $"href=\"(/assets/{paperId.Compact}\\.[0-9a-f]+\\.pdf)\"")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        var href = Assert.Single(hrefs);

        // ...no editor-plane URL and no unresolved reference survive...
        Assert.DoesNotContain("/media/", html);
        Assert.DoesNotContain("asset:", html);

        // ...and the file itself exists in the deploy output.
        Assert.True(File.Exists(host.FullPath(href.TrimStart('/'))), $"{href} should exist in the output");
    }

    [Fact]
    public async Task A_link_to_a_deleted_file_degrades_to_prose_and_ships_nothing()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite("Acme Studio", "en");
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, homeId);

        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new ButtonNode
                {
                    Id = NodeId.New(),
                    Label = LocalizedText.Of(PublishingTestHost.En, "Download"),
                    LinkTo = new AssetLink(AssetId.New()),
                },
                new RichTextNode
                {
                    Id = NodeId.New(),
                    Html = LocalizedText.Of(
                        PublishingTestHost.En,
                        $"<p>Read <a href=\"asset:{Guid.NewGuid():N}\">the paper</a> here.</p>"),
                }),
        });
        await host.Publish(homeId);

        await host.Publisher.Synchronize();

        var html = host.ReadText("index.html");

        // Button degrades to the inert span; the prose anchor unwraps to plain text.
        Assert.Contains("<span class=\"ip-btn ip-btn-primary\">Download</span>", html);
        Assert.Contains("Read the paper here.", html);
        Assert.DoesNotContain("asset:", html);
        Assert.DoesNotContain(".pdf", html);
    }
}
