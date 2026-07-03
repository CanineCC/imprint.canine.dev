using System.Collections.Concurrent;
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
        services.AddImprintEventSourcing(
            _database.ConnectionString,
            [typeof(AuthoringJson).Assembly],
            AuthoringJson.Configure);
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

/// <summary>In-memory media store fake: keys behave like the disk store's.</summary>
public sealed class InMemoryMediaStore : IMediaStore
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public async Task<string> SaveOriginal(AssetId id, string fileName, Stream content, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var key = $"originals/{id.Compact}/{fileName}";
        _files[key] = buffer.ToArray();
        return key;
    }

    public Task<string> SaveDerived(AssetId id, string derivedName, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        var key = $"derived/{id.Compact}/{derivedName}";
        _files[key] = content.ToArray();
        return Task.FromResult(key);
    }

    public Task<Stream> Open(string storageKey, CancellationToken ct = default) =>
        _files.TryGetValue(storageKey, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes))
            : throw new FileNotFoundException(storageKey);

    public Task<string> ReadAllText(string storageKey, CancellationToken ct = default) =>
        Task.FromResult(System.Text.Encoding.UTF8.GetString(_files[storageKey]));

    public string PhysicalPathOf(string storageKey) => $"/fake/{storageKey}";

    public Task DeleteAll(AssetId id, CancellationToken ct = default)
    {
        foreach (var key in _files.Keys.Where(k => k.Contains(id.Compact, StringComparison.Ordinal)))
        {
            _files.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Deterministic media processor fake; failure modes are switchable per test.</summary>
public sealed class FakeMediaProcessor : IMediaProcessor
{
    public bool FailNext { get; set; }
    public bool VideoAvailable { get; set; } = true;

    public string? VideoUnavailableReason => VideoAvailable ? null : "ffmpeg is not installed (fake)";

    public Task<IReadOnlyList<ImageVariant>> GenerateImageVariants(AssetId id, string originalKey, CancellationToken ct = default)
    {
        ThrowIfFailing();
        return Task.FromResult<IReadOnlyList<ImageVariant>>(
        [
            new ImageVariant(480, 320, $"derived/{id.Compact}/480.webp", 10_000),
            new ImageVariant(960, 640, $"derived/{id.Compact}/960.webp", 30_000),
        ]);
    }

    public Task<(string StorageKey, int RemovedNodes)> SanitizeSvg(AssetId id, string originalKey, CancellationToken ct = default)
    {
        ThrowIfFailing();
        return Task.FromResult(($"derived/{id.Compact}/clean.svg", 1));
    }

    public Task<(string StorageKey, long ByteSize)?> TranscodeToWebM(AssetId id, string originalKey, CancellationToken ct = default)
    {
        ThrowIfFailing();
        return Task.FromResult<(string, long)?>(
            VideoAvailable ? ($"derived/{id.Compact}/video.webm", 500_000) : null);
    }

    private void ThrowIfFailing()
    {
        if (FailNext)
        {
            FailNext = false;
            throw new InvalidOperationException("Simulated processing failure");
        }
    }
}
