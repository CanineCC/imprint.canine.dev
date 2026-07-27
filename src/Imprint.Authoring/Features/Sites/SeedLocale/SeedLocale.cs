using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SeedLocale;

/// <summary>
/// Fill a locale's empty text from another locale, across the whole site — the step that
/// turns "we added Danish" into "Danish is ready to translate".
/// </summary>
public sealed record SeedLocale(Domain.SiteId SiteId, string Target, string Source) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Locale.TryCreate(Target, out _))
        {
            yield return $"'{Target}' is not a valid locale.";
        }

        if (!Locale.TryCreate(Source, out _))
        {
            yield return $"'{Source}' is not a valid locale.";
        }
    }
}
