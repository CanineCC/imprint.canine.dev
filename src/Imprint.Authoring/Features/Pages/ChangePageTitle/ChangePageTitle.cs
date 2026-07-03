using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangePageTitle;

public sealed record ChangePageTitle(PageId PageId, string Locale, string Title) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        // An empty title is allowed here on purpose: writing "" in a non-default
        // locale clears that translation (LocalizedText drops empty values).
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
