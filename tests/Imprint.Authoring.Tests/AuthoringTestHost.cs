using System.Collections.Concurrent;
using Imprint.Authoring;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using Imprint.EventSourcing;
using Imprint.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Tests;

/// <summary>
/// The slice-test host: a REAL SQLite (in-memory) event store, the real dispatcher,
/// real projections — only the media ports are fakes. Slice tests dispatch commands
/// and assert on events and read models, exactly the path production takes.
/// </summary>
public sealed class AuthoringTestHost : IAsyncDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public AuthoringTestHost(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddImprintAuthoring(_database.ConnectionString);
        services.AddSingleton<IMediaStore, InMemoryMediaStore>();
        services.AddSingleton<IMediaProcessor, FakeMediaProcessor>();
        configure?.Invoke(services);
        Services = services.BuildServiceProvider();

        Services.InitializeImprintEventSourcing().GetAwaiter().GetResult();
    }

    public ServiceProvider Services { get; }

    public ICommandDispatcher Dispatcher => Services.GetRequiredService<ICommandDispatcher>();
    public IEventStore Store => Services.GetRequiredService<IEventStore>();
    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    /// <summary>Dispatches and asserts success — the default expectation in slice tests.</summary>
    public async Task<Result> Ok(ICommand command)
    {
        var result = await Dispatcher.Dispatch(command);
        Assert.True(result.Succeeded, $"{command.GetType().Name} failed: {result.ErrorMessage}");
        return result;
    }

    /// <summary>Dispatches and asserts failure, returning the message for content checks.</summary>
    public async Task<string> Fails(ICommand command)
    {
        var result = await Dispatcher.Dispatch(command);
        Assert.False(result.Succeeded, $"{command.GetType().Name} unexpectedly succeeded.");
        return result.ErrorMessage;
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        _database.Dispose();
    }
}
