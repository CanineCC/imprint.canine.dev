using System.Collections.Concurrent;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;

namespace Imprint.TestKit;

/// <summary>In-memory media store fake: keys behave like the disk store's.</summary>
public sealed class InMemoryMediaStore : IMediaStore
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public void Seed(string storageKey, byte[] content) => _files[storageKey] = content;

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
