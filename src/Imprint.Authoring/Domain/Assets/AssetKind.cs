using System.Text.Json.Serialization;

namespace Imprint.Authoring.Domain.Assets;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetKind
{
    /// <summary>Raster image → WebP responsive variants.</summary>
    Image,

    /// <summary>SVG → sanitized, inlined at render.</summary>
    Vector,

    /// <summary>Video → WebM (external ffmpeg; degrades gracefully when absent).</summary>
    Video,

    /// <summary>Anything else: published as a plain download.</summary>
    File,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetStatus
{
    /// <summary>Uploaded, derivative processing not finished.</summary>
    Pending,

    /// <summary>Derivatives ready; publishable.</summary>
    Ready,

    /// <summary>Publishable, but with the original file (e.g. ffmpeg unavailable). Editor shows why.</summary>
    ReadyDegraded,

    /// <summary>Processing failed; not publishable. Editor shows why.</summary>
    Failed,
}

/// <summary>
/// The optional dark-mode variant's own lifecycle, parallel to <see cref="AssetStatus"/>
/// but distinct: an asset is <see cref="None"/> (neutral, one file for both schemes) until
/// a dark variant is uploaded. A failed dark processing run drops the variant back to
/// <see cref="None"/> rather than to a Failed state — the base asset stays usable and
/// neutral, so there is nothing to surface as broken.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DarkVariantStatus
{
    /// <summary>No dark variant — the asset renders identically in both colour schemes.</summary>
    None,

    /// <summary>A dark original was uploaded; its derivatives are not finished.</summary>
    Pending,

    /// <summary>Dark derivatives ready; the view can emit the second rendition.</summary>
    Ready,

    /// <summary>Reserved for parity with the base pipeline; no image/vector dark path degrades today (video is out of scope).</summary>
    ReadyDegraded,
}

/// <summary>A generated responsive image derivative (WebP).</summary>
public sealed record ImageVariant(int Width, int Height, string StorageKey, long ByteSize);
