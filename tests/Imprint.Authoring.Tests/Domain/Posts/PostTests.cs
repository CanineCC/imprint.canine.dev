using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Posts;

/// <summary>
/// The Post aggregate: a blog entry whose authored state is MARKDOWN, not a node tree.
///
/// <para>The load-bearing decision these tests pin is where representability is enforced.
/// A draft is free — an author mid-sentence must be able to save a stray backtick, and an
/// aggregate that refused would make the editor unusable. Publishing is the gate: that is
/// the moment the markdown has to become nodes, so that is where it must be provable.</para>
/// </summary>
public sealed class PostTests
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");
    private static readonly DateTimeOffset Noon = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly PostId _id = PostId.New();
    private readonly SiteId _site = SiteId.New();

    private static Slug SlugOf(string value)
    {
        Assert.True(Slug.TryCreate(value, out var slug, out var error), error);
        return slug;
    }

    private PostCreated Created(string slug = "hello-world", string title = "Hello world") =>
        new(_id, _site, slug, En, title);

    // ------------------------------------------------------------------------- creation

    [Fact]
    public void Create_raises_created()
    {
        var post = Post.Create(_id, _site, SlugOf("hello-world"), En, "Hello world");

        Assert.Equal(new PostCreated(_id, _site, "hello-world", En, "Hello world"), Assert.Single(post.UncommittedEvents));
    }

    [Fact]
    public void Create_initializes_state()
    {
        var post = Post.Create(_id, _site, SlugOf("hello-world"), En, "Hello world");

        Assert.Equal(_id, post.Id);
        Assert.Equal(_site, post.SiteId);
        Assert.Equal("hello-world", post.Slug.Value);
        Assert.Equal("Hello world", post.Title.Get(En));
        Assert.Equal("", post.Body.Get(En) ?? "");
        Assert.Null(post.PublishedAt);
        Assert.False(post.IsPublished);
        Assert.False(post.IsDeleted);
        Assert.Equal(_id.Stream, post.StreamId);
    }

    [Fact]
    public void Create_without_a_title_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(() => Post.Create(_id, _site, SlugOf("x"), En, "  "));

        Assert.Contains("title", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------------------------------------------------- the body

    [Fact]
    public void Changing_the_body_stores_the_markdown_verbatim()
    {
        const string markdown = "# Heading\n\nSome **prose**.";

        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.ChangeBody(En, markdown))
            .ThenRaised(new PostBodyChanged(En, markdown));
    }

    [Fact]
    public void A_draft_may_hold_markdown_that_cannot_yet_be_represented()
    {
        // The author is mid-edit. Refusing this is how you build an editor nobody can type in.
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.ChangeBody(En, "A `stray backtick and an unfinished"))
            .ThenRaised(new PostBodyChanged(En, "A `stray backtick and an unfinished"));
    }

    [Fact]
    public void The_body_is_per_locale()
    {
        var post = new Post();
        post.LoadFrom([Created(), new PostBodyChanged(En, "Hello"), new PostBodyChanged(Da, "Hej")]);

        Assert.Equal("Hello", post.Body.Get(En));
        Assert.Equal("Hej", post.Body.Get(Da));
    }

    [Fact]
    public void An_unreasonably_long_body_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.ChangeBody(En, new string('x', Post.MaxBodyLength + 1)))
            .ThenFails("long");

    // ------------------------------------------------------------------- publishing

    [Fact]
    public void Publishing_records_when()
    {
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Real prose."))
            .When(p => p.Publish(En, Noon))
            .ThenRaised(new PostPublished(Noon));
    }

    [Fact]
    public void Publishing_is_refused_while_the_body_cannot_become_nodes()
    {
        // The whole point of the aggregate: a post that cannot be rendered must not be
        // publishable, and the author must be told which line is the problem.
        var outcome = AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Fine.\n\n> a blockquote"))
            .When(p => p.Publish(En, Noon));

        outcome.ThenFails("line 3");
        outcome.ThenFails("blockquote");
    }

    [Fact]
    public void Publishing_an_empty_post_is_refused() =>
        AggregateSpec.For<Post>().Given(Created()).When(p => p.Publish(En, Noon)).ThenFails("empty");

    [Fact]
    public void Publishing_twice_keeps_the_first_date()
    {
        // Re-publishing after an edit is an update, not a new post: the date a reader sorts
        // and cites by must not move because a typo was fixed.
        var post = new Post();
        post.LoadFrom([Created(), new PostBodyChanged(En, "Prose."), new PostPublished(Noon)]);

        post.Publish(En, Noon.AddDays(3));

        Assert.Empty(post.UncommittedEvents);
        Assert.Equal(Noon, post.PublishedAt);
    }

    [Fact]
    public void Unpublishing_returns_it_to_a_draft_and_forgets_the_date()
    {
        var post = new Post();
        post.LoadFrom([Created(), new PostBodyChanged(En, "Prose."), new PostPublished(Noon)]);

        post.Unpublish();

        Assert.Equal(new PostUnpublished(), Assert.Single(post.UncommittedEvents));
        Assert.False(post.IsPublished);
        Assert.Null(post.PublishedAt);
    }

    [Fact]
    public void Unpublishing_a_draft_does_nothing()
    {
        AggregateSpec.For<Post>().Given(Created()).When(p => p.Unpublish()).ThenNothing();
    }

    // --------------------------------------------------------------- title, slug, meta

    [Fact]
    public void Changing_the_title_raises_it_per_locale()
    {
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.ChangeTitle(Da, "Hej verden"))
            .ThenRaised(new PostTitleChanged(Da, "Hej verden"));
    }

    [Fact]
    public void Changing_the_slug_to_the_same_value_does_nothing()
    {
        AggregateSpec.For<Post>()
            .Given(Created("hello-world"))
            .When(p => p.ChangeSlug(SlugOf("hello-world")))
            .ThenNothing();
    }

    [Fact]
    public void Changing_the_meta_raises_it()
    {
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.ChangeMeta(En, "Title", "Description"))
            .ThenRaised(new PostMetaChanged(En, "Title", "Description"));
    }

    // ------------------------------------------------------------------------ deletion

    [Fact]
    public void Deleting_marks_it_deleted()
    {
        AggregateSpec.For<Post>()
            .Given(Created())
            .When(p => p.Delete())
            .ThenRaised(new PostDeleted());
    }

    [Fact]
    public void Editing_a_deleted_post_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostDeleted())
            .When(p => p.ChangeBody(En, "anything"))
            .ThenFails("deleted");

    [Fact]
    public void Publishing_a_deleted_post_is_rejected() =>
        AggregateSpec.For<Post>()
            .Given(Created(), new PostBodyChanged(En, "Prose."), new PostDeleted())
            .When(p => p.Publish(En, Noon))
            .ThenFails("deleted");
}
