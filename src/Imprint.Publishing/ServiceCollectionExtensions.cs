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
        services.AddSingleton<SitePublisher>();
        services.AddSingleton<DeployPathResolver>();
        services.AddSingleton<SiteDeployService>();
        services.AddHostedService<PublisherHostedService>();
        return services;
    }
}
