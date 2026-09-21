using Imprint.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The production registration must actually hand the publisher a fetcher. Without this the feature
/// fails in the only way that leaves no trace: every page publishes its fallback, every test passes,
/// and the output looks exactly like a fetch that returned nothing.
/// </summary>
public sealed class WidgetPrerenderWiringTests
{
    [Fact]
    public void AddImprintPublishing_registers_a_prerender_source()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImprintPublishing(new PublishingOptions
        {
            OutputPath = Path.Combine(Path.GetTempPath(), "probe-out"),
            WidgetsDirectory = Path.Combine(Path.GetTempPath(), "probe-widgets"),
        });

        using var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IWidgetPrerenderSource>());
    }
}
