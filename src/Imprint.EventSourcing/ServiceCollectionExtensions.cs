using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Imprint.EventSourcing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires the whole write path: store, aggregate store, dispatcher, projections.
    /// <paramref name="domainAssemblies"/> are scanned twice (the two sanctioned scans):
    /// once by <see cref="EventRegistry"/> for [EventType] records, once here for
    /// <see cref="ICommandHandler{T}"/> and <see cref="IProjection"/> implementations —
    /// which is why adding a feature slice never touches a central registration file.
    /// </summary>
    public static IServiceCollection AddImprintEventSourcing(
        this IServiceCollection services,
        string connectionString,
        IReadOnlyList<Assembly> domainAssemblies,
        Action<JsonSerializerOptions>? configureJson = null)
    {
        services.AddSingleton(new EventRegistry(domainAssemblies, configureJson));
        services.AddSingleton<IEventStore>(provider =>
            new SqliteEventStore(connectionString, provider.GetRequiredService<EventRegistry>()));
        services.AddSingleton<EventMetadataProvider>();
        services.AddSingleton<IAggregateStore, AggregateStore>();
        services.AddSingleton<ProjectionEngine>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

        foreach (var assembly in domainAssemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
            {
                foreach (var handlerInterface in type.GetInterfaces().Where(i =>
                             i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
                {
                    services.TryAddScoped(handlerInterface, type);
                }

                if (typeof(IProjection).IsAssignableFrom(type))
                {
                    // Registered as self *and* as IProjection so UI code injects the
                    // concrete read model while the engine sees the collection.
                    services.TryAddSingleton(type);
                    services.AddSingleton(provider => (IProjection)provider.GetRequiredService(type));
                }
            }
        }

        return services;
    }

    /// <summary>Creates the schema and replays projections. Call once at startup, before serving.</summary>
    public static async Task InitializeImprintEventSourcing(
        this IServiceProvider provider,
        CancellationToken ct = default)
    {
        if (provider.GetRequiredService<IEventStore>() is SqliteEventStore sqlite)
        {
            await sqlite.EnsureSchema(ct);
        }

        await provider.GetRequiredService<ProjectionEngine>().Rebuild(ct);
    }
}
