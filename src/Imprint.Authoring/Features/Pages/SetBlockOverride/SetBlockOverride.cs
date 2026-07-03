using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.SetBlockOverride;

/// <summary>Null <paramref name="Value"/> clears the override.</summary>
public sealed record SetBlockOverride(
    PageId PageId,
    NodeId InstanceId,
    NodeId DefinitionNodeId,
    string Field,
    string Locale,
    string? Value) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
