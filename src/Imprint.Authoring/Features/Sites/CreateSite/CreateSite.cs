using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateSite;

public sealed record CreateSite(SiteId SiteId, string Name, string DefaultLocale) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return "The site name cannot be empty.";
        }
        else if (Name.Trim().Length > Site.MaxNameLength)
        {
            yield return $"The site name must be {Site.MaxNameLength} characters or fewer.";
        }

        if (!Locale.TryCreate(DefaultLocale, out _))
        {
            yield return $"'{DefaultLocale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
