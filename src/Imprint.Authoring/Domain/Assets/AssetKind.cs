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

/// <summary>A generated responsive image derivative (WebP).</summary>
public sealed record ImageVariant(int Width, int Height, string StorageKey, long ByteSize);
