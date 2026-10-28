using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The publication-control notes: where each figure in an article comes from, what was checked and
/// by whom. Never published. The signer signs them with the words, so their hash is in the approval,
/// and changing them after approval withdraws it exactly as an edit to the words does.
/// </summary>
public sealed class PostControlNotesTests
{
    private static readonly PostId Id = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));

    private static PostCreated Created() => new(Id, Site, "hello-world", En, "Hello world");

    private static PostBodyChanged Body() => new(En, "# Hello\n\nProse.\n");

    private const string Notes = "Figure 1: CYCLE-1-FINDINGS.md §4a, re-derived 2026-10-25.";

    [Fact]
    public void Control_notes_are_recorded_trimmed()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.SetControlNotes("  " + Notes + "\n"));

        outcome.ThenRaised(new PostControlNotesChanged(Notes));
        Assert.Equal(Notes, outcome.Aggregate.ControlNotes);
    }

    [Fact]
    public void The_same_notes_again_record_nothing() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostControlNotesChanged(Notes))
            .When(post => post.SetControlNotes(Notes))
            .ThenNothing();

    [Fact]
    public void An_approval_signs_the_hash_of_the_notes()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostControlNotesChanged(Notes), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(September));

        var approved = Assert.IsType<PostReviewApproved>(Assert.Single(outcome.Aggregate.UncommittedEvents));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Notes))), approved.ControlNotesSha256);
    }

    [Fact]
    public void An_approval_of_a_post_without_notes_signs_no_notes_hash()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(September));

        var approved = Assert.IsType<PostReviewApproved>(Assert.Single(outcome.Aggregate.UncommittedEvents));
        Assert.Null(approved.ControlNotesSha256);
    }

    [Fact]
    public void Changing_the_notes_after_approval_withdraws_it()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(September, 1, "x"))
            .When(post => post.SetControlNotes(Notes));

        outcome.ThenRaised(new PostControlNotesChanged(Notes), new PostApprovalLapsed());
    }

    [Fact]
    public void Notes_too_long_are_refused() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.SetControlNotes(new string('x', Post.MaxBodyLength + 1)))
            .ThenFails("long");
}
