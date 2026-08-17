using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SubmitPostForReview;

/// <param name="ProposedPublishAt">When the author would like it to go out; null is an explicit
/// "to be decided". The reviewer may overrule it either way.</param>
public sealed record SubmitPostForReview(PostId PostId, string Locale, DateTimeOffset? ProposedPublishAt, string? Note)
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
