using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateSiteFromTemplate;

public sealed record CreateSiteFromTemplate(
    SiteId SiteId,
    string Name,
    string DefaultLocale,
    string TemplateKey,
    IReadOnlyList<string> ExtraLocales) : IValidatableCommand
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

        foreach (var extra in ExtraLocales)
        {
            if (!Locale.TryCreate(extra, out _))
            {
                yield return $"'{extra}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
            }
        }
    }
}
