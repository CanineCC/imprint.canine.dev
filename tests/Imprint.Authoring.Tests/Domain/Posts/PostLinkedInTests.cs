using System.Security.Cryptography;
using System.Text;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The LinkedIn post that goes out with an article: its text and which sender posts it. Signed with the words,
/// because it is public text under the company's name; an article without one never posts.
/// </summary>
public sealed class PostLinkedInTests
{
    private static readonly PostId Id = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));

    private static PostCreated Created() => new(Id, Site, "hello-world", En, "Hello world");

    private static PostBodyChanged Body() => new(En, "# Hello\n\nProse.\n");

    private const string Text = "We measured our own engine against experts. What would you ask a benchmark first?";

    [Fact]
    public void The_text_and_sender_are_recorded_trimmed()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.SetLinkedIn("  " + Text + "\n", " canine "));

        outcome.ThenRaised(new PostLinkedInChanged(Text, "canine"));
        Assert.Equal(Text, outcome.Aggregate.LinkedInText);
        Assert.Equal("canine", outcome.Aggregate.LinkedInSender);
    }

    [Fact]
    public void Blank_text_clears_the_post_and_its_sender() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostLinkedInChanged(Text, "canine"))
            .When(post => post.SetLinkedIn("  ", "canine"))
            .ThenRaised(new PostLinkedInChanged(null, null));

    [Fact]
    public void A_text_needs_a_sender() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.SetLinkedIn(Text, " "))
            .ThenFails("sender");

    [Fact]
    public void A_text_longer_than_linkedin_takes_is_refused() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.SetLinkedIn(new string('x', Post.MaxLinkedInLength + 1), "canine"))
            .ThenFails("3,000");

    [Fact]
    public void The_same_again_records_nothing() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostLinkedInChanged(Text, "canine"))
            .When(post => post.SetLinkedIn(Text, "canine"))
            .ThenNothing();

    [Fact]
    public void An_approval_signs_the_hash_of_the_sender_and_the_text()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostLinkedInChanged(Text, "canine"), new PostSubmittedForReview(September, null))
            .When(post => post.ApproveReview(September));

        var approved = Assert.IsType<PostReviewApproved>(Assert.Single(outcome.Aggregate.UncommittedEvents));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("canine\n" + Text))), approved.LinkedInSha256);
    }

    [Fact]
    public void Changing_the_text_after_approval_withdraws_it() =>
        AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostSubmittedForReview(September, null), new PostReviewApproved(September, 1, "x"))
            .When(post => post.SetLinkedIn(Text, "canine"))
            .ThenRaised(new PostLinkedInChanged(Text, "canine"), new PostApprovalLapsed());
}
