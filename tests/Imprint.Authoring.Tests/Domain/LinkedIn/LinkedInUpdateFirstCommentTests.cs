using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.LinkedIn;

/// <summary>
/// An update's first comment (usually the link) goes out after its post, as the same sender, at most once. It has a
/// record of its own: whatever happens to the comment, the post that went out stays Posted and is never sent again.
/// </summary>
public sealed class LinkedInUpdateFirstCommentTests
{
    private static readonly LinkedInUpdateId Id = LinkedInUpdateId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Nine = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);
    private const string Link = "Read the whole piece: https://canine.dev/assure/";

    private static LinkedInUpdateScheduled Scheduled(string? comment = Link) =>
        new(Id, Site, "canine", "Your AI says it's fixed.", [], Nine, Now, comment);

    private static readonly LinkedInUpdateAttempting Attempting = new("sha", Nine);
    private static readonly LinkedInUpdatePosted Posted = new("urn:li:share:7", Nine.AddSeconds(2));
    private static readonly LinkedInUpdateCommentAttempting CommentAttempting = new("sha-c", Nine.AddSeconds(3));

    private static LinkedInUpdate Folded(params object[] events)
    {
        var update = new LinkedInUpdate();
        update.LoadFrom(events);
        return update;
    }

    [Fact]
    public void Scheduling_records_the_first_comment_trimmed_and_blank_means_none()
    {
        var with = LinkedInUpdate.Schedule(Id, Site, "canine", "Your AI says it's fixed.", [], Nine, Now, "  " + Link + "\n");
        var without = LinkedInUpdate.Schedule(Id, Site, "canine", "Your AI says it's fixed.", [], Nine, Now, "   ");

        Assert.Equal(Scheduled(), Assert.Single(with.UncommittedEvents));
        Assert.Equal((Link, LinkedInUpdateCommentState.Waiting), (with.FirstComment, with.CommentState!.Value));
        Assert.Equal(Scheduled(comment: null), Assert.Single(without.UncommittedEvents));
        Assert.Null(without.CommentState);
    }

    [Fact]
    public void A_comment_longer_than_linkedins_comment_box_is_refused() =>
        Assert.Contains("1,250", Assert.Throws<DomainException>(() =>
            LinkedInUpdate.Schedule(Id, Site, "canine", "Hi", [], Nine, Now, new string('a', 1251))).Message);

    [Fact]
    public void The_comment_waits_for_its_post_and_may_go_out_once_the_post_is_out()
    {
        Assert.False(Folded(Scheduled()).MayCommentAt(Nine.AddHours(1)));
        Assert.False(Folded(Scheduled(), Attempting).MayCommentAt(Nine.AddHours(1)));
        Assert.True(Folded(Scheduled(), Attempting, Posted).MayCommentAt(Nine));
    }

    [Fact]
    public void An_update_without_a_comment_never_comments() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(comment: null), Attempting, Posted)
            .When(u => u.AttemptComment("sha-c", Nine))
            .ThenFails("no first comment");

    [Fact]
    public void Sending_the_comment_before_its_post_is_out_is_refused() =>
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.AttemptComment("sha-c", Nine))
            .ThenFails("waits for its post");

    [Fact]
    public void A_posted_comment_is_recorded_and_never_sent_again()
    {
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, Posted, CommentAttempting)
            .When(u => u.CommentPosted("urn:li:comment:(urn:li:activity:1,2)", Nine.AddSeconds(4)))
            .ThenRaised(new LinkedInUpdateCommentPosted("urn:li:comment:(urn:li:activity:1,2)", Nine.AddSeconds(4)));
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, Posted, CommentAttempting, new LinkedInUpdateCommentPosted("urn:li:comment:x", Nine))
            .When(u => u.AttemptComment("sha-c", Nine.AddMinutes(1)))
            .ThenFails("already on LinkedIn");
    }

    [Fact]
    public void A_refused_comment_leaves_the_post_posted_and_never_lets_it_be_sent_again()
    {
        var update = Folded(Scheduled(), Attempting, Posted, CommentAttempting,
            new LinkedInUpdateCommentFailed("403: not enough permissions", false, Nine.AddSeconds(4)));

        Assert.Equal((LinkedInUpdateState.Posted, "urn:li:share:7"), (update.State, update.Urn));
        Assert.Equal(LinkedInUpdateCommentState.Failed, update.CommentState);
        Assert.False(update.MaySendAt(Nine.AddDays(1)));
        Assert.False(update.MayCommentAt(Nine.AddDays(1)));
        Assert.Throws<DomainException>(() => update.Edit("canine", "x", [], Nine.AddDays(1), Now));
        Assert.Throws<DomainException>(() => update.Attempt("sha", Nine.AddDays(1)));
    }

    [Fact]
    public void A_comment_with_an_unknown_result_is_never_sent_again_and_the_post_stays_posted()
    {
        var update = Folded(Scheduled(), Attempting, Posted, CommentAttempting, new LinkedInUpdateCommentUnknown("timed out", Nine.AddSeconds(30)));

        Assert.Equal(LinkedInUpdateState.Posted, update.State);
        Assert.False(update.MayCommentAt(Nine.AddDays(1)));
        Assert.Contains("unknown", Assert.Throws<DomainException>(() => update.AttemptComment("sha-c", Nine.AddDays(1))).Message);
    }

    [Fact]
    public void An_open_comment_attempt_seen_after_a_restart_is_not_sent_again() =>
        Assert.False(Folded(Scheduled(), Attempting, Posted, CommentAttempting).MayCommentAt(Nine.AddMinutes(5)));

    [Fact]
    public void A_retryable_refusal_may_be_sent_again_once_its_wait_is_over()
    {
        var update = Folded(Scheduled(), Attempting, Posted, CommentAttempting,
            new LinkedInUpdateCommentFailed("429: throttled", true, Nine.AddSeconds(4), Nine.AddMinutes(1)));

        Assert.False(update.MayCommentAt(Nine.AddSeconds(30)));
        Assert.True(update.MayCommentAt(Nine.AddMinutes(1)));
    }

    [Fact]
    public void A_comment_that_could_not_go_out_in_time_is_missed_and_the_post_stays_posted()
    {
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled(), Attempting, Posted)
            .When(u => u.MissComment("too late", Nine.AddHours(3)))
            .ThenRaised(new LinkedInUpdateCommentMissed("too late", Nine.AddHours(3)));

        var missed = Folded(Scheduled(), Attempting, Posted, new LinkedInUpdateCommentMissed("too late", Nine.AddHours(3)));
        Assert.Equal((LinkedInUpdateState.Posted, LinkedInUpdateCommentState.Missed), (missed.State, missed.CommentState!.Value));
        Assert.False(missed.MayCommentAt(Nine.AddHours(4)));
    }

    [Fact]
    public void An_edit_restates_the_comment_and_leaving_it_out_removes_it()
    {
        AggregateSpec.For<LinkedInUpdate>()
            .Given(Scheduled())
            .When(u => u.Edit("canine", "New text", [], Nine.AddDays(1), Now, "New link: https://canine.dev/"))
            .ThenRaised(new LinkedInUpdateEdited("canine", "New text", [], Nine.AddDays(1), Now, "New link: https://canine.dev/"));

        var removed = Folded(Scheduled(), new LinkedInUpdateEdited("canine", "New text", [], Nine.AddDays(1), Now));
        Assert.Null(removed.FirstComment);
        Assert.Null(removed.CommentState);
    }

    [Fact]
    public void A_person_resolving_an_unclear_post_as_posted_lets_its_comment_go_out()
    {
        var resolved = Folded(Scheduled(), Attempting, new LinkedInUpdateUnknown("timed out", Nine), new LinkedInUpdateResolved("urn:li:share:9", Nine.AddMinutes(20)));
        var notPosted = Folded(Scheduled(), Attempting, new LinkedInUpdateUnknown("timed out", Nine), new LinkedInUpdateResolved(null, Nine.AddMinutes(20)));

        Assert.True(resolved.MayCommentAt(Nine.AddMinutes(21)));
        Assert.Equal(Nine.AddMinutes(20), resolved.PostedAt);
        Assert.False(notPosted.MayCommentAt(Nine.AddMinutes(21)));
    }
}
