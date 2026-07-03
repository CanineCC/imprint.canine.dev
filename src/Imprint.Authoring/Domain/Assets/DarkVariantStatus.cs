using System.Text.Json.Serialization;

namespace Imprint.Authoring.Domain.Assets;

/// <summary>
/// The lifecycle of an asset's optional dark-mode variant, independent of the base
/// asset's <see cref="AssetStatus"/>: an asset is publishable and usable regardless of
/// whether its dark variant is present, pending, ready or failed.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DarkVariantStatus
{
    /// <summary>No dark variant — the asset is neutral (one rendition in both schemes).</summary>
    None,

    /// <summary>A dark original was uploaded; its derivatives are not finished.</summary>
    Pending,

    /// <summary>Dark derivatives ready; the dark rendition is emitted alongside the light one.</summary>
    Ready,

    /// <summary>Publishable dark rendition, produced on a degraded path (reserved for parity with the base pipeline).</summary>
    ReadyDegraded,
}
