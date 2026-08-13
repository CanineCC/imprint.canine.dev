using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.PublishPost;

/// <param name="Locale">The locale whose body must convert. Publishing is judged on the post's
/// own primary language: a translation that lags is a coverage problem, not a reason to block
/// the original from going live.</param>
public sealed record PublishPost(PostId PostId, string Locale) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag.";
        }
    }
}
