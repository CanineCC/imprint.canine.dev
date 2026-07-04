using Imprint.Authoring.Domain;

namespace Imprint.Authoring.Features.Assets;

/// <summary>
/// Port for binary media storage (originals and derivatives). The event store holds
/// the *truth about* assets; this holds the bytes. Disk-backed in
/// <c>Imprint.Media</c>; storage keys are opaque to the domain.
/// </summary>
public interface IMediaStore
{
    /// <summary>Streams an uploaded original to storage; returns its storage key.</summary>
    Task<string> SaveOriginal(AssetId id, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>Saves a derived file (WebP variant, sanitized SVG, WebM); returns its storage key.</summary>
    Task<string> SaveDerived(AssetId id, string derivedName, ReadOnlyMemory<byte> content, CancellationToken ct = default);

    Task<Stream> Open(string storageKey, CancellationToken ct = default);

    Task<string> ReadAllText(string storageKey, CancellationToken ct = default);

    /// <summary>Absolute file path for a key — used by the publisher to copy without buffering.</summary>
    string PhysicalPathOf(string storageKey);

    /// <summary>Removes every stored file of an asset (called after <c>asset.deleted</c>).</summary>
    Task DeleteAll(AssetId id, CancellationToken ct = default);
}

/// <summary>
/// Port for derivative generation, implemented in <c>Imprint.Media</c>. Implementations
/// are infrastructure: they may fail or be unavailable — the Asset aggregate records
/// whichever outcome as an event, and nothing downstream trusts a worker's memory.
/// </summary>
public interface IMediaProcessor
{
    /// <summary>
    /// WebP variants at the spec'd widths (docs/domain-model.md §3), plus intrinsic
    /// dimensions. <paramref name="dark"/> routes the outputs to a distinct derived key
    /// namespace so a dark-mode rendition never overwrites the base (light) rendition —
    /// they share the same asset id but must not share storage keys.
    /// </summary>
    Task<IReadOnlyList<Domain.Assets.ImageVariant>> GenerateImageVariants(AssetId id, string originalKey, bool dark = false, CancellationToken ct = default);

    /// <summary>
    /// Sanitizes an SVG (scripts, handlers, external refs). Returns the new key and how
    /// many nodes were removed. <paramref name="dark"/> routes the output to a distinct
    /// derived key so the dark rendition never overwrites the base sanitized SVG.
    /// </summary>
    Task<(string StorageKey, int RemovedNodes)> SanitizeSvg(AssetId id, string originalKey, bool dark = false, CancellationToken ct = default);

    /// <summary>Null when transcoding is unavailable (no ffmpeg) — the caller records <c>asset.processing-skipped</c>.</summary>
    Task<(string StorageKey, long ByteSize)?> TranscodeToWebM(AssetId id, string originalKey, CancellationToken ct = default);

    /// <summary>Human-readable reason when video transcoding is unavailable, else null.</summary>
    string? VideoUnavailableReason { get; }
}
