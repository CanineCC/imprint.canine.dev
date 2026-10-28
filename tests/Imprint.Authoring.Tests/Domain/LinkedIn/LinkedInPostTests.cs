using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

/// <summary>An article's LinkedIn post goes out at most once: the attempt is recorded before the call, a result
/// that is not known stops everything until a person checks, and only a refusal that proves nothing was posted
/// may be sent again.</summary>
public sealed class LinkedInPostTests
{
    private static readonly PostId Post = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Nine = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static LinkedInPostAttempting Attempting(DateTimeOffset? at = null) =>
        new(Post, Site, "canine", "sha", "https://resourcecenter.canine.dev/x/", at ?? Nine);

    [Fact]
    public void A_new_post_begins_with_an_attempt_and_records_where_it_points()
    {
        var record = LinkedInPost.Begin(Post, Site, "canine", "sha", "https://resourcecenter.canine.dev/x/", Nine);

        Assert.Equal(Attempting(), Assert.Single(record.UncommittedEvents));
        Assert.Equal(LinkedInPostState.Attempting, record.State);
        Assert.Equal($"linkedin-post-{Post.Value:N}", record.StreamId);
        Assert.False(record.MaySendAt(Nine.AddDays(1)));
    }

    [Fact]
    public void A_posted_post_is_never_sent_again() =>
        AggregateSpec.For<LinkedInPost>()
            .Given(Attempting(), new LinkedInPostPosted("urn:li:share:1", Nine))
            .When(r => r.Attempt("canine", "sha", "u", Nine.AddDays(1)))
            .ThenFails("already gone out");

    [Fact]
    public void An_unknown_result_is_never_retried_on_its_own() =>
        AggregateSpec.For<LinkedInPost>()
            .Given(Attempting(), new LinkedInPostUnknown("timed out", Nine))
            .When(r => r.Attempt("canine", "sha", "u", Nine.AddDays(1)))
            .ThenFails("unknown");

    [Fact]
    public void An_open_attempt_left_by_a_crash_counts_as_unknown() =>
        AggregateSpec.For<LinkedInPost>()
            .Given(Attempting())
            .When(r => r.Attempt("canine", "sha", "u", Nine.AddDays(1)))
            .ThenFails("unknown");

    [Fact]
    public void A_retryable_refusal_may_be_sent_again_once_its_time_has_come()
    {
        var record = new LinkedInPost();
        record.LoadFrom([Attempting(), new LinkedInPostFailed("rate limited", true, Nine, Nine.AddMinutes(30))]);

        Assert.False(record.MaySendAt(Nine.AddMinutes(29)));
        Assert.True(record.MaySendAt(Nine.AddMinutes(30)));
    }

    [Fact]
    public void A_refusal_that_is_not_retryable_stays_refused()
    {
        var record = new LinkedInPost();
        record.LoadFrom([Attempting(), new LinkedInPostFailed("no page role", false, Nine)]);

        Assert.False(record.MaySendAt(Nine.AddDays(30)));
    }

    [Fact]
    public void A_person_resolving_an_unknown_as_not_posted_lets_it_go_again()
    {
        var outcome = AggregateSpec.For<LinkedInPost>()
            .Given(Attempting(), new LinkedInPostUnknown("timed out", Nine))
            .When(r => r.Resolve(null, Nine.AddHours(1)));

        outcome.ThenRaised(new LinkedInPostResolved(null, Nine.AddHours(1)));
        Assert.True(outcome.Aggregate.MaySendAt(Nine.AddHours(1)));
    }

    [Fact]
    public void A_person_resolving_an_unknown_as_posted_records_the_post()
    {
        var outcome = AggregateSpec.For<LinkedInPost>()
            .Given(Attempting(), new LinkedInPostUnknown("timed out", Nine))
            .When(r => r.Resolve("urn:li:share:9", Nine.AddHours(1)));

        Assert.Equal(LinkedInPostState.Posted, outcome.Aggregate.State);
        Assert.Equal("urn:li:share:9", outcome.Aggregate.Urn);
    }

    [Fact]
    public void A_result_without_an_open_attempt_is_refused() =>
        AggregateSpec.For<LinkedInPost>()
            .Given(Attempting(), new LinkedInPostPosted("urn:li:share:1", Nine))
            .When(r => r.Posted("urn:li:share:2", Nine))
            .ThenFails("no open attempt");
}
