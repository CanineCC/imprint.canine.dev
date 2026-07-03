using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.EditText;

public sealed record EditText(PageId PageId, NodeId NodeId, string Field, string Locale, string Value)
    : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        // Only the locale's *shape* is a data check; whether the field exists on the
        // node and whether the value is canonical HTML are aggregate invariants.
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
