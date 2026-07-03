using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangePageMeta;

public sealed record ChangePageMeta(PageId PageId, string Locale, string? MetaTitle, string? MetaDescription)
    : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
