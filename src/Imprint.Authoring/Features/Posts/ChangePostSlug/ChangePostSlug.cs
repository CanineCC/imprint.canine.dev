using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostSlug;

public sealed record ChangePostSlug(PostId PostId, string Slug) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Pages.Slug.TryCreate(Slug, out _, out var error))
        {
            yield return error!;
        }
    }
}
