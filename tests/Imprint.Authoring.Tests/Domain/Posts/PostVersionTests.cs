using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// Every publish records WHICH text went out: a version number that moves only when the words do,
/// and the same sha256 a sign-off records (<see cref="Post.BodySha256"/>), so anyone can check that
/// the published text is the approved text. Old versions are never served; the number is shown.
/// </summary>
public sealed class PostVersionTests
{
    private static readonly PostId Id = PostId.New();
    private static readonly SiteId Site = SiteId.New();
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset Noon = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private static PostCreated Created() => new(Id, Site, "hello-world", En, "Hello world");

    private static string Sha(string markdown) => Post.BodySha256(LocalizedText.Of(En, markdown));

    [Fact]
    public void The_first_publish_is_version_one_and_names_the_words()
    {
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."))
            .When(post => post.Publish(En, Noon));

        outcome.ThenRaised(new PostPublished(Noon, 1, Sha("Real prose.")));
        Assert.Equal(1, outcome.Aggregate.PublishedVersion);
        Assert.Equal(Sha("Real prose."), outcome.Aggregate.PublishedSha256);
    }

    [Fact]
    public void Publishing_changed_words_is_the_next_version()
    {
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."), new PostPublished(Noon, 1, Sha("Real prose.")),
                new PostBodyChanged(En, "Better prose."))
            .When(post => post.Publish(En, Noon.AddDays(1)))
            .ThenRaised(new PostPublished(Noon.AddDays(1), 2, Sha("Better prose.")));
    }

    [Fact]
    public void Publishing_the_same_words_again_keeps_the_version()
    {
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."), new PostPublished(Noon, 1, Sha("Real prose.")))
            .When(post => post.Publish(En, Noon.AddDays(1)))
            .ThenRaised(new PostPublished(Noon.AddDays(1), 1, Sha("Real prose.")));
    }

    [Fact]
    public void Unpublishing_and_republishing_the_same_words_keeps_the_version()
    {
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."), new PostPublished(Noon, 1, Sha("Real prose.")),
                new PostUnpublished())
            .When(post => post.Publish(En, Noon.AddDays(1)))
            .ThenRaised(new PostPublished(Noon.AddDays(1), 1, Sha("Real prose.")));
    }

    [Fact]
    public void A_post_published_before_versions_were_recorded_continues_from_version_one()
    {
        // A publish event from before 2026-10-27 carries no version and no hash: it was version one of
        // something nobody can recompute, so the next publish of any text is version two.
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."), new PostPublished(Noon))
            .When(post => post.Publish(En, Noon.AddDays(1)))
            .ThenRaised(new PostPublished(Noon.AddDays(1), 2, Sha("Real prose.")));
    }
}
