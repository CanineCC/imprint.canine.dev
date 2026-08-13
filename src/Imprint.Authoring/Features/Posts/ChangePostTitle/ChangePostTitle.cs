using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostTitle;

public sealed record ChangePostTitle(PostId PostId, string Locale, string Title) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return "A post needs a title.";
        }

        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag.";
        }
    }
}
