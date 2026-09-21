using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The delivery plane in one call: the publisher itself, the editor-facing status
    /// singleton, and the hosted service that keeps the output directory in sync with
    /// the event stream. Requires <c>AddImprintAuthoring</c> (read models) and an
    /// <c>IMediaStore</c> to be registered by the host.
    /// </summary>
    public static IServiceCollection AddImprintPublishing(this IServiceCollection services, PublishingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WidgetsDirectory);

        services.AddSingleton(options);
        services.AddSingleton<PublisherStatus>();
        services.AddSingleton<PublishGate>();
        // The publish-time widget bake. Registered here so a production publish has it and a test
        // host does not unless it asks — SitePublisher takes it as an optional dependency, and a
        // publisher without one behaves exactly as it did before the feature existed.
        //
        // ★ A SHORT TIMEOUT IS THE POINT. This runs inside a publish, once per distinct fragment
        // URL, and a publish that hangs is worse than a page without its bake — so the client gives
        // up quickly and the fetcher turns that into "no bake" rather than an error.
        services.AddHttpClient<IWidgetPrerenderSource, HttpWidgetPrerenderSource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("imprint-publisher/1.0 (+prerender)");
        });
        services.AddSingleton<SitePublisher>();
        services.AddSingleton<DeployPathResolver>();
        services.AddSingleton<SiteDeployService>();
        services.AddHostedService<PublisherHostedService>();
        return services;
    }
}
