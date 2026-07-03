using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring;

public static class AuthoringServiceCollectionExtensions
{
    /// <summary>
    /// The bounded context in one call: event sourcing (store, dispatcher, scanned
    /// handlers and projections) plus the query services that compute over read models.
    /// </summary>
    public static IServiceCollection AddImprintAuthoring(this IServiceCollection services, string connectionString) =>
        services
            .AddImprintEventSourcing(connectionString, [typeof(AuthoringJson).Assembly], AuthoringJson.Configure)
            .AddSingleton<ContentUsage>()
            .AddSingleton<TranslationCoverage>();
}
