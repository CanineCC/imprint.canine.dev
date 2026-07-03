using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Features.Assets;

public static class AssetServiceCollectionExtensions
{
    /// <summary>
    /// The asset processing pipeline: the shared queue plus the background worker
    /// that drains it. Called by the app composition root next to
    /// <c>AddImprintAuthoring</c>; handlers themselves are registered by the
    /// existing assembly scan.
    /// </summary>
    public static IServiceCollection AddImprintAssetProcessing(this IServiceCollection services) =>
        services
            .AddSingleton<AssetProcessingQueue>()
            .AddHostedService<AssetProcessingWorker>();
}
