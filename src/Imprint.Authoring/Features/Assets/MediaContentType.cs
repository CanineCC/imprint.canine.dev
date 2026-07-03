using Imprint.Authoring.Domain.Assets;

namespace Imprint.Authoring.Features.Assets;

/// <summary>
/// Maps an upload's declared media type to its <see cref="AssetKind"/>. The rule lives
/// in exactly one place so a base upload and its dark variant can never classify the
/// same bytes differently — a divergence would let a dark SVG land on a raster base and
/// slip past the kind-match invariant. SVG is tested before the <c>image/*</c> family
/// because it matches both.
/// </summary>
internal static class MediaContentType
{
    public static AssetKind KindOf(string contentType)
    {
        var type = contentType.Trim().ToLowerInvariant();
        return type switch
        {
            "image/svg+xml" => AssetKind.Vector,
            _ when type.StartsWith("image/", StringComparison.Ordinal) => AssetKind.Image,
            _ when type.StartsWith("video/", StringComparison.Ordinal) => AssetKind.Video,
            _ => AssetKind.File,
        };
    }
}
