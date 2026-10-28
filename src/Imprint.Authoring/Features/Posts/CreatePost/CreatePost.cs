using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.CreatePost;

/// <param name="Kind">Optional article kind (essay, guide, whitepaper, documentation); classified at
/// creation when given, so a resource-center article never exists without one.</param>
/// <param name="Products">The products the article is about, used only with a kind.</param>
public sealed record CreatePost(
    PostId PostId, SiteId SiteId, string Title, string Slug, string Locale,
    string? Kind = null, IReadOnlyList<string>? Products = null)
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
