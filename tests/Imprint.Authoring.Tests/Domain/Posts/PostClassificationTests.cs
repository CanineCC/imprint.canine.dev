using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// What kind of article a post is (essay, guide, whitepaper, documentation) and which products it
/// is about. The resource center's template and index are driven by the kind, and its accent by
/// the products, so both are part of what a reader sees.
/// </summary>
public sealed class PostClassificationTests
{
    private static readonly PostId Id = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));

    private static PostCreated Created() => new(Id, Site, "hello-world", En, "Hello world");

    private static PostBodyChanged Body() => new(En, "# Hello\n\nProse.\n");

    [Fact]
    public void Classify_records_the_kind_and_the_products_in_canonical_order()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.Classify(" Guide ", ["Assay", "assert", "assay"]));

        outcome.ThenRaised(new PostClassified("guide", ["assert", "assay"]));
        Assert.Equal("guide", outcome.Aggregate.Kind);
        Assert.Equal(["assert", "assay"], outcome.Aggregate.Products);
    }

    [Fact]
    public void A_post_with_no_classification_has_no_kind_and_no_products()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created())
            .When(_ => { });

        Assert.Null(outcome.Aggregate.Kind);
        Assert.Empty(outcome.Aggregate.Products);
    }

    [Fact]
    public void An_unknown_kind_is_refused_and_the_refusal_names_the_kinds() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.Classify("tutorial", []))
            .ThenFails("essay, guide, whitepaper, documentation");

    [Fact]
    public void An_unknown_product_is_refused_by_name() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(post => post.Classify("essay", ["watchdog"]))
            .ThenFails("watchdog");

    [Fact]
    public void The_same_classification_again_records_nothing() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostClassified("essay", ["canine"]))
            .When(post => post.Classify("Essay", ["canine"]))
            .ThenNothing();

    [Fact]
    public void Reclassifying_an_approved_post_lapses_the_approval_because_the_reader_sees_it()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), Body(), new PostClassified("essay", []),
                new PostSubmittedForReview(September, null), new PostReviewApproved(September, 1, "x"))
            .When(post => post.Classify("guide", []));

        outcome.ThenRaised(new PostClassified("guide", []), new PostApprovalLapsed());
        Assert.False(outcome.Aggregate.IsApproved);
    }

    [Fact]
    public void A_deleted_post_cannot_be_classified() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostDeleted())
            .When(post => post.Classify("essay", []))
            .ThenFails("deleted");
}
