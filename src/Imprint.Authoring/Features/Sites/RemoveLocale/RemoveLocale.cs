using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RemoveLocale;

public sealed record RemoveLocale(Domain.SiteId SiteId, string Locale) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
