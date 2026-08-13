using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostMeta;

public sealed record ChangePostMeta(PostId PostId, string Locale, string? MetaTitle, string? MetaDescription)
    : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag.";
        }
    }
}
