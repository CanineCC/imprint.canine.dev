using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;

/// <summary>
/// Attaches an optional dark-mode rendition to an existing image or SVG asset. Like
/// <c>UploadAsset</c> this carries a <see cref="Stream"/>, which is safe because the
/// command bus is in-process (docs/architecture.md): the stream is read exactly once by
/// the handler and never serialized, queued or retried across a wire.
/// </summary>
public sealed record UploadAssetDarkVariant(
    AssetId AssetId,
    string FileName,
    string ContentType,
    long ByteSize,
    Stream Content) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            yield return "A dark-mode file needs a file name.";
        }

        if (string.IsNullOrWhiteSpace(ContentType) || !ContentType.Contains('/'))
        {
            yield return $"'{ContentType}' is not a media type (expected e.g. 'image/png').";
        }

        if (ByteSize <= 0)
        {
            yield return "A dark-mode file cannot be empty.";
        }
    }
}
