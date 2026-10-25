using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A page that renders data from another service (a price list) is kept current by two things: the producer's "look again"
/// (<c>/api/authoring/notify</c>, near-instant) and, as the safety net when that signal is lost or blocked, a timed refresh that
/// re-fetches every bake with no event at all. Prices must never go stale because one HTTP call did not arrive.
/// </summary>
public sealed class PublisherRefreshTests
{
    private sealed class ChangingSource : IWidgetPrerenderSource
    {
        public volatile string Html = "<p>€10 a month</p>";

        public Task<string?> FetchAsync(Uri url, CancellationToken ct) => Task.FromResult<string?>(Html);
    }

    [Fact]
    public async Task A_price_that_changes_upstream_reaches_the_page_with_no_event_and_no_signal()
    {
        var source = new ChangingSource();
        await using var host = new PublishingTestHost(refreshMilliseconds: 200, configure: services =>
            services.AddSingleton<IWidgetPrerenderSource>(source));
        File.WriteAllText(Path.Combine(host.WidgetsDirectory, "manifest.json"), JsonSerializer.Serialize(new[]
        {
            new
            {
                tag = "wd-embed", name = "Embed", bundle = "wd-embed.js", placeholder = "Prices are published here.",
                prerender = "https://app.example.test/embed/{view}", props = new[] { new { name = "view", label = "View" } },
            },
        }));
        File.WriteAllText(Path.Combine(host.WidgetsDirectory, "wd-embed.js"), "// island\n");
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode
            {
                Id = NodeId.New(), Tag = "wd-embed", Props = PropBag.Of([new KeyValuePair<string, string>("view", "pricing")]),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        var service = host.Services.GetServices<IHostedService>().OfType<PublisherHostedService>().Single();
        await service.StartAsync(CancellationToken.None);
        try
        {
            await WaitFor(() => host.FileExists("index.html") && host.ReadText("index.html").Contains("€10 a month", StringComparison.Ordinal), "first publish");
            source.Html = "<p>€12 a month</p>";   // the price changed upstream; no event, no notify
            await WaitFor(() => host.ReadText("index.html").Contains("€12 a month", StringComparison.Ordinal), "the timed refresh");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitFor(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, $"timed out waiting for {what}");
            await Task.Delay(50);
        }
    }
}
