using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAsset;

/// <summary>
/// The one command that carries a <see cref="Stream"/>. That is safe because the
/// command bus is in-process by design (docs/architecture.md — editors have direct,
/// in-process access to the domain): the stream is read exactly once by the handler
/// and never serialized, queued or retried across a wire.
/// </summary>
public sealed record UploadAsset(
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
            yield return "An uploaded file needs a file name.";
        }

        if (string.IsNullOrWhiteSpace(ContentType) || !ContentType.Contains('/'))
        {
            yield return $"'{ContentType}' is not a media type (expected e.g. 'image/png').";
        }

        if (ByteSize <= 0)
        {
            yield return "An uploaded file cannot be empty.";
        }
    }
}
