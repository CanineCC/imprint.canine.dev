using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;

/// <summary>
/// Attaches an optional dark-mode variant to an existing image/vector asset. Carries a
/// <see cref="Stream"/> for the same reason <c>UploadAsset</c> does: the bus is
/// in-process and the handler reads it exactly once.
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
