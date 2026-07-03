using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Assets.Events;

// The Asset aggregate's events — one file because the closed set *is* the media
// lifecycle state machine (docs/domain-model.md §3).

[EventType("asset.uploaded")]
public sealed record AssetUploaded(
    AssetId AssetId,
    string FileName,
    string ContentType,
    AssetKind Kind,
    long ByteSize,
    string StorageKey);

[EventType("asset.image-variants-generated")]
public sealed record ImageVariantsGenerated(IReadOnlyList<ImageVariant> Variants)
{
    // A record holding a list compares by reference, but events must compare by
    // value (specs and round-trip tests rely on it) — same precedent as ColumnsNode.
    public bool Equals(ImageVariantsGenerated? other) =>
        other is not null && Variants.SequenceEqual(other.Variants);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var variant in Variants)
        {
            hash.Add(variant);
        }

        return hash.ToHashCode();
    }
}

[EventType("asset.svg-sanitized")]
public sealed record SvgSanitized(string StorageKey, int RemovedNodeCount);

[EventType("asset.video-transcoded")]
public sealed record VideoTranscoded(string StorageKey, long ByteSize);

[EventType("asset.processing-failed")]
public sealed record ProcessingFailed(string Reason);

[EventType("asset.processing-skipped")]
public sealed record ProcessingSkipped(string Reason);

[EventType("asset.alt-changed")]
public sealed record AssetAltChanged(Locale Locale, string Alt);

[EventType("asset.renamed")]
public sealed record AssetRenamed(string Name);

[EventType("asset.deleted")]
public sealed record AssetDeleted;
