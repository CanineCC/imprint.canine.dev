using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ChangePostBody;

/// <summary>
/// Replaces a post's markdown for one locale. Note what is NOT validated here: whether the
/// markdown converts. A draft is allowed to be mid-sentence — see <see cref="Post"/> — and the
/// conversion is checked at publish, where it has to hold.
/// </summary>
public sealed record ChangePostBody(PostId PostId, string Locale, string Markdown) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (!Domain.Locale.TryCreate(Locale, out _))
        {
            yield return $"'{Locale}' is not a valid locale tag.";
        }

        if (Markdown is null)
        {
            yield return "A body is required (send an empty string to clear it).";
        }
        else if (Markdown.Length > Post.MaxBodyLength)
        {
            yield return $"A post body cannot be longer than {Post.MaxBodyLength:N0} characters.";
        }
    }
}
