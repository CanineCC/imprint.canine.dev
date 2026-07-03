using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Imprint.Publishing.Tests.Pipeline;

public sealed class PublisherHostedServiceTests
{
    [Fact]
    public async Task Hosted_service_publishes_at_startup_and_after_debounced_changes()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var pageId = await host.CreatePage(siteId, "home", "Home");
        await host.SetNavigation(siteId, pageId);
        await host.Publish(pageId);

        var service = host.Services.GetServices<IHostedService>().OfType<PublisherHostedService>().Single();
        await service.StartAsync(CancellationToken.None);
        try
        {
            // Initial synchronize at startup.
            await WaitFor(() => host.FileExists("index.html"), "initial publish");
            Assert.Contains("<title>Home · Acme Studio</title>", host.ReadText("index.html"));

            // A change → CaughtUp → debounced republish, without anyone calling Synchronize.
            await host.SetTitle(pageId, "en", "Welcome");
            await host.Publish(pageId);
            await WaitFor(
                () => host.ReadText("index.html").Contains("<title>Welcome · Acme Studio</title>", StringComparison.Ordinal),
                "debounced republish");

            Assert.NotNull(host.Status.Last);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitFor(Func<bool> condition, string what)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail($"Timed out waiting for {what}.");
    }
}
