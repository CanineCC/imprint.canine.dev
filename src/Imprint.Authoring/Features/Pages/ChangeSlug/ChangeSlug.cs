using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.ChangeSlug;

public sealed record ChangeSlug(PageId PageId, string Slug) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Pages.Slug.TryCreate(Slug, out _, out var error))
        {
            yield return error!;
        }
    }
}
