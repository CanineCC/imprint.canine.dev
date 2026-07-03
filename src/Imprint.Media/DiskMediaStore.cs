using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Assets;

namespace Imprint.Media;

/// <summary>
/// Disk-backed <see cref="IMediaStore"/>: originals under <c>originals/{assetId}/</c>,
/// derivatives under <c>derived/{assetId}/</c>. Storage keys are relative paths, but
/// they arrive from events and callers we do not control — every key is re-validated
/// to resolve strictly under the root, and every file name is sanitized, on every call.
/// </summary>
public sealed class DiskMediaStore : IMediaStore
{
    // Uploaded display names keep their meaning at this length while staying far below
    // any file system's component limit (255 bytes on ext4/NTFS, and multi-byte UTF-8
    // can triple the byte count).
    private const int MaxFileNameLength = 100;

    private const string OriginalsDirectory = "originals";
    private const string DerivedDirectory = "derived";

    private readonly string _root;

    public DiskMediaStore(MediaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            throw new ArgumentException("MediaOptions.RootPath must be a non-empty directory path.", nameof(options));
        }

        _root = Path.GetFullPath(options.RootPath);
    }

    public async Task<string> SaveOriginal(AssetId id, string fileName, Stream content, CancellationToken ct = default)
    {
        var key = $"{OriginalsDirectory}/{id.Compact}/{SanitizeFileName(fileName)}";
        var path = ResolveUnderRoot(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Originals can be large (video); stream straight to disk instead of buffering.
        await using var file = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public async Task<string> SaveDerived(AssetId id, string derivedName, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        var key = $"{DerivedDirectory}/{id.Compact}/{SanitizeFileName(derivedName)}";
        var path = ResolveUnderRoot(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, ct);
        return key;
    }

    public Task<Stream> Open(string storageKey, CancellationToken ct = default)
    {
        var path = ResolveUnderRoot(storageKey);
        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task<string> ReadAllText(string storageKey, CancellationToken ct = default) =>
        File.ReadAllTextAsync(ResolveUnderRoot(storageKey), ct);

    public string PhysicalPathOf(string storageKey) => ResolveUnderRoot(storageKey);

    public Task DeleteAll(AssetId id, CancellationToken ct = default)
    {
        string[] directories =
        [
            Path.Combine(_root, OriginalsDirectory, id.Compact),
            Path.Combine(_root, DerivedDirectory, id.Compact),
        ];
        foreach (var directory in directories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    // The single choke point against path traversal: whatever a key claims, the
    // canonical path it resolves to must sit strictly below the root. This also
    // catches absolute keys, because Path.Combine yields them unchanged.
    private string ResolveUnderRoot(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key must not be empty.", nameof(storageKey));
        }

        var full = Path.GetFullPath(Path.Combine(_root, storageKey));
        var rootWithSeparator = Path.EndsInDirectorySeparator(_root)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Storage key '{storageKey}' resolves outside the media root.", nameof(storageKey));
        }

        return full;
    }

    // Uploaded file names are attacker-controlled. Dropping separators and leading
    // dots (rather than rejecting) keeps uploads working while making traversal and
    // hidden-file tricks unrepresentable.
    private static string SanitizeFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var builder = new StringBuilder(fileName.Length);
        foreach (var c in fileName)
        {
            if (char.IsControl(c) || c is '/' or '\\' or ':')
            {
                continue;
            }

            builder.Append(c);
        }

        var name = builder.ToString().Trim().TrimStart('.');
        if (name.Length > MaxFileNameLength)
        {
            name = name[..MaxFileNameLength];
        }

        return name.Length == 0 ? "file" : name;
    }
}
