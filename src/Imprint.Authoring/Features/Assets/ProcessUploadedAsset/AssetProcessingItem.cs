using Imprint.Authoring.Domain;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>Which pipeline a queued asset id is waiting on — the base rendition or its dark variant.</summary>
public enum AssetProcessingKind
{
    Base,
    DarkVariant,
}

/// <summary>
/// A unit of work on the shared processing queue: an asset id plus which derivative
/// pipeline to run for it. Both pipelines ride one queue so an upload never blocks
/// behind a slow job (see <see cref="AssetProcessingQueue"/>).
/// </summary>
public readonly record struct AssetProcessingItem(AssetId AssetId, AssetProcessingKind Kind);
