using Imprint.Authoring.Features.Assets;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Media;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImprintMedia(this IServiceCollection services, MediaOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IMediaStore, DiskMediaStore>();
        // Singleton on purpose: the processor caches its one-time ffmpeg probe.
        services.AddSingleton<IMediaProcessor, SkiaMediaProcessor>();
        return services;
    }
}
