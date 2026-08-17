using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts.Events;

namespace Imprint.Authoring.Tests.Domain.Posts;

// Fully-populated serialization samples for every Post event. The round-trip harness fails
// when an event type exists without a sample here, so a payload shape change is always
// exercised against the real registry serialization.
public sealed class PostEventSamples : IEventSampleProvider
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");

    public IEnumerable<object> Samples
    {
        get
        {
            yield return new PostCreated(PostId.New(), SiteId.New(), "hello-world", En, "Hello world");
            yield return new PostTitleChanged(Da, "Hej verden");
            yield return new PostSlugChanged("goodbye-world");
            yield return new PostMetaChanged(En, "Hello — Acme", "The first post.");
            yield return new PostMetaChanged(Da, null, null);

            // Multi-line markdown with the characters JSON has opinions about: quotes,
            // backslashes and newlines all have to survive the log unchanged, because the
            // node tree is rebuilt from this text and nothing else.
            yield return new PostBodyChanged(En, "# Title\n\nProse with \"quotes\", a \\* escape and 5 < 6.\n\n```sh\nls -la\n```\n");
            yield return new PostBodyChanged(Da, "");

            yield return new PostPublished(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
            yield return new PostUnpublished();

            // Review and scheduling. Both shapes of every optional date, because "no date" is a
            // real answer here ("to be decided") and a null that fails to round-trip would read
            // back as "publish immediately".
            yield return new PostPublishDateSet(new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2)));
            yield return new PostPublishDateSet(null);
            yield return new PostSubmittedForReview(
                new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2)), "Cleared with legal on the 28th.");
            yield return new PostSubmittedForReview(null, null);
            yield return new PostReviewApproved(new DateTimeOffset(2026, 9, 2, 6, 0, 0, TimeSpan.FromHours(2)));
            yield return new PostReviewApproved(null);
            yield return new PostChangesRequested("The second paragraph names a customer we cannot name.");
            yield return new PostApprovalLapsed();

            yield return new PostDeleted();
        }
    }
}
