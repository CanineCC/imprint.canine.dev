using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.SetAssetAlt;

public sealed record SetAssetAlt(Domain.AssetId AssetId, string Locale, string Alt) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
