using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring;

public static class AuthoringServiceCollectionExtensions
{
    /// <summary>
    /// The bounded context in one call: event sourcing (store, dispatcher, scanned
    /// handlers and projections) plus the query services that compute over read models.
    /// The upcaster list is where legacy stream shapes are normalized on read — additive
    /// and pure, so a fresh install is unaffected.
    /// </summary>
    public static IServiceCollection AddImprintAuthoring(this IServiceCollection services, string connectionString) =>
        services
            .AddImprintEventSourcing(
                connectionString,
                [typeof(AuthoringJson).Assembly],
                AuthoringJson.Configure,
                upcasters: [new PageCreatedUpcaster()])
            .AddSingleton<ContentUsage>()
            .AddSingleton<TranslationCoverage>();
}
