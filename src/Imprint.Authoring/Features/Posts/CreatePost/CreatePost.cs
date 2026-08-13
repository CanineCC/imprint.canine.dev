using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.CreatePost;

public sealed record CreatePost(PostId PostId, SiteId SiteId, string Title, string Slug, string Locale)
    : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return "A post needs a title.";
        }

        if (!Domain.Pages.Slug.TryCreate(Slug, out _, out var slugError))
        {
            yield return slugError!;
        }

        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }
    }
}
