using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The post's own half of "somebody has to clear this before the world reads it". Whether a
/// review is REQUIRED is the site's policy and lives in the slice; what is here is what a post
/// can be asked to do about its own review state, and when it refuses.
/// </summary>
public sealed class PostReviewTests
{
    private static readonly PostId Id = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset October = new(2026, 10, 1, 7, 0, 0, TimeSpan.FromHours(2));

    private static PostCreated Created() => new(Id, Site, "hello-world", En, "Hello world");

    private static PostBodyChanged Body(string markdown = "# Hello\n\nProse.\n") => new(En, markdown);

    /// <summary>sha256 of "en\n# Hello\n\nProse.\n\0": the default body, in the canonical form.</summary>
    private const string BodySha = "e32ca9e3653bb5adc50c1901415438988ba26fea4ba1e410120aa731991fadca";

    // ------------------------------------------------------------------- submit

    [Fact]
    public void SubmitForReview_hands_it_over_with_the_proposed_date()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body())
            .When(post => post.SubmitForReview(En, September, "Cleared with legal."));

        outcome.ThenRaised(new PostSubmittedForReview(September, "Cleared with legal."));
        Assert.Equal(PostReview.Pending, outcome.Aggregate.Review);
        Assert.Equal(September, outcome.Aggregate.PublishAt);
    }

    [Fact]
    public void SubmitForReview_with_no_date_is_a_real_answer()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body())
            .When(post => post.SubmitForReview(En, null));

        outcome.ThenRaised(new PostSubmittedForReview(null, null));
        Assert.Null(outcome.Aggregate.PublishAt);
    }

    [Fact]
    public void SubmitForReview_refuses_prose_that_cannot_be_rendered() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body("Fine.\n\n    indented code\n"))
            .When(post => post.SubmitForReview(En, null))
            .ThenFails("line 3");

    [Fact]
    public void SubmitForReview_twice_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(null, null))
            .When(post => post.SubmitForReview(En, null))
            .ThenFails("already with the reviewer");

    [Fact]
    public void SubmitForReview_of_a_live_post_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostPublished(September))
            .When(post => post.SubmitForReview(En, null))
            .ThenFails("already live");

    // ------------------------------------------------------------------ approve

    [Fact]
    public void ApproveReview_can_move_the_date_the_author_proposed()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(October));

        outcome.ThenRaised(new PostReviewApproved(October, 1, BodySha));
        Assert.Equal(PostReview.Approved, outcome.Aggregate.Review);
        Assert.Equal(October, outcome.Aggregate.PublishAt);
    }

    [Fact]
    public void ApproveReview_without_a_date_approves_the_words_and_waits()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(null));

        outcome.ThenRaised(new PostReviewApproved(null, 1, BodySha));
        Assert.True(outcome.Aggregate.IsApproved);
        Assert.Null(outcome.Aggregate.PublishAt);
    }

    // The signature documents WHICH words were cleared: the body revision and a sha256 of exactly
    // those words, so anyone holding the log can show the published text is the approved text.
    // The hash is pinned as a literal on purpose: a change to its canonical form would make every
    // recorded approval unverifiable, and that must fail here rather than be discovered later.
    [Fact]
    public void ApproveReview_signs_the_revision_and_the_hash_of_the_words_it_cleared()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body("# Draft\n"), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(null));

        outcome.ThenRaised(new PostReviewApproved(null, 2, BodySha));
        Assert.Equal(2, outcome.Aggregate.ApprovedRevision);
        Assert.Equal(BodySha, outcome.Aggregate.ApprovedSha256);
    }

    [Fact]
    public void An_edit_after_approval_clears_the_signature_with_the_approval()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(null, 1, BodySha))
            .When(post => post.ChangeBody(En, "# Hello\n\nOther words.\n"));

        Assert.Null(outcome.Aggregate.ApprovedRevision);
        Assert.Null(outcome.Aggregate.ApprovedSha256);
    }

    [Fact]
    public void ApproveReview_of_a_post_nobody_submitted_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body())
            .When(post => post.ApproveReview(September))
            .ThenFails("waiting for review");

    // -------------------------------------------------------------- send it back

    [Fact]
    public void RequestChanges_sends_it_back_with_the_reason()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.RequestChanges("  Names a customer we cannot name.  "));

        outcome.ThenRaised(new PostChangesRequested("Names a customer we cannot name."));
        Assert.Equal(PostReview.ChangesRequested, outcome.Aggregate.Review);
        Assert.Equal("Names a customer we cannot name.", outcome.Aggregate.ReviewNote);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestChanges_without_a_reason_is_rejected(string reason) =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(null, null))
            .When(post => post.RequestChanges(reason))
            .ThenFails("Say what needs changing");

    // ------------------------------------------------------- approval and edits

    [Fact]
    public void Editing_after_approval_withdraws_it()
    {
        // The whole point of the gate: sign-off is on words, so different words are unsigned.
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(September))
            .When(post => post.ChangeBody(En, "# Hello\n\nSomething else entirely.\n"));

        outcome.ThenRaised(
            new PostBodyChanged(En, "# Hello\n\nSomething else entirely.\n"),
            new PostApprovalLapsed());
        Assert.False(outcome.Aggregate.Review is PostReview.Approved);
    }

    [Fact]
    public void An_autosave_that_changes_nothing_keeps_the_approval() =>
        // The editor saves on a timer. If that lapsed the approval, no post would ever stay approved.
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(September))
            .When(post => post.ChangeBody(En, "# Hello\n\nProse.\n"))
            .ThenNothing();

    [Fact]
    public void Editing_a_published_post_does_not_lapse_anything()
    {
        // It is already out. "Approval lapsed" would be a statement about a decision that has
        // already been spent, and would put a live post into a review state it cannot leave.
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null),
                new PostReviewApproved(September), new PostPublished(September))
            .When(post => post.ChangeBody(En, "# Hello\n\nA typo fixed.\n"));

        outcome.ThenRaised(new PostBodyChanged(En, "# Hello\n\nA typo fixed.\n"));
    }

    // ----------------------------------------------------------------- publish

    [Fact]
    public void Publish_while_the_reviewer_holds_it_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.Publish(En, October))
            .ThenFails("with the reviewer");

    [Fact]
    public void Publish_dates_a_scheduled_post_by_its_schedule()
    {
        // The worker polls, so it wakes up some seconds after the agreed instant. The date a
        // reader sees must be the one everybody agreed to, not the poll that noticed it.
        var wokeUpLate = September.AddSeconds(23);

        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostPublishDateSet(September))
            .When(post => post.Publish(En, wokeUpLate))
            .ThenRaised(new PostPublished(September, 1, BodySha));
    }

    [Fact]
    public void Publish_before_the_scheduled_date_uses_the_real_instant()
    {
        // "Publish now" on a post scheduled for next month: it is going out NOW, and dating it
        // in the future would put a post at the top of the index that nobody can explain.
        var now = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostPublishDateSet(October))
            .When(post => post.Publish(En, now))
            .ThenRaised(new PostPublished(now, 1, BodySha));
    }

    // -------------------------------------------------------------------- date

    [Fact]
    public void SetPublishDate_to_the_same_instant_raises_nothing() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostPublishDateSet(September))
            .When(post => post.SetPublishDate(September))
            .ThenNothing();

    [Fact]
    public void SetPublishDate_to_null_is_to_be_decided()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostPublishDateSet(September))
            .When(post => post.SetPublishDate(null));

        outcome.ThenRaised(new PostPublishDateSet(null));
        Assert.Null(outcome.Aggregate.PublishAt);
        Assert.False(outcome.Aggregate.IsScheduled(September.AddDays(-1)));
    }

    // ------------------------------------------------------------------- hold

    [Fact]
    public void A_held_post_cannot_be_published_and_says_why() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostHoldSet("Figures wait for a measurement."))
            .When(post => post.Publish(En, September))
            .ThenFails("Figures wait for a measurement.");

    [Fact]
    public void Lifting_the_hold_lets_it_publish()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostHoldSet("Figures wait."), new PostHoldSet(null))
            .When(post => post.Publish(En, September));

        Assert.True(outcome.Aggregate.IsPublished);
    }

    [Fact]
    public void A_hold_neither_needs_nor_lapses_an_approval()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(September, 1, BodySha))
            .When(post => post.SetHold("  Waits for E7.  "));

        outcome.ThenRaised(new PostHoldSet("Waits for E7."));
        Assert.True(outcome.Aggregate.IsApproved);
    }

    [Fact]
    public void Holding_again_for_the_same_reason_records_nothing() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostHoldSet("Waits."))
            .When(post => post.SetHold("Waits."))
            .ThenNothing();
}
