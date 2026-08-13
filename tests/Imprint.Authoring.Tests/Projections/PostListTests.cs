using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Projections;

/// <summary>
/// The post list: what the editor's index and the published blog index both read. Its one
/// non-obvious rule is the ORDER — a blog is read newest-first by publication date, not by
/// slug and not by when someone last fixed a typo.
/// </summary>
public sealed class PostListTests
{
    private static readonly Locale En = new("en");
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Day1 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private long _position;

    private PostList Fold(params (string Stream, object Event)[] events)
    {
        var list = new PostList();
        var versions = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (stream, @event) in events)
        {
            versions[stream] = versions.GetValueOrDefault(stream) + 1;
            _position++;
            list.Apply(new StoredEvent(
                _position, stream, versions[stream], $"e{_position}", @event,
                new EventMetadata("test", Day1.AddMinutes(_position), Guid.Empty, Guid.Empty)));
        }

        return list;
    }

    private static (string, object) Created(PostId id, string slug, string title) =>
        ($"post-{id.Value:N}", new PostCreated(id, Site, slug, En, title));

    private static (string, object) Published(PostId id, DateTimeOffset at) =>
        ($"post-{id.Value:N}", new PostPublished(at));

    [Fact]
    public void A_created_post_is_a_draft()
    {
        var id = PostId.New();

        var summary = Assert.Single(Fold(Created(id, "hello", "Hello")).All(Site));

        Assert.Equal(id, summary.Id);
        Assert.Equal("hello", summary.Slug.Value);
        Assert.Equal("Hello", summary.Title.Get(En));
        Assert.Equal(PostStatus.Draft, summary.Status);
        Assert.Null(summary.PublishedAt);
    }

    [Fact]
    public void Publishing_moves_it_out_of_draft_and_records_the_date()
    {
        var id = PostId.New();

        var summary = Assert.Single(Fold(Created(id, "hello", "Hello"), Published(id, Day1)).All(Site));

        Assert.Equal(PostStatus.Published, summary.Status);
        Assert.Equal(Day1, summary.PublishedAt);
    }

    [Fact]
    public void An_edit_after_publishing_shows_as_modified()
    {
        var id = PostId.New();

        var summary = Assert.Single(Fold(
            Created(id, "hello", "Hello"),
            Published(id, Day1),
            ($"post-{id.Value:N}", new PostBodyChanged(En, "edited"))).All(Site));

        Assert.Equal(PostStatus.Modified, summary.Status);
        // The date a reader cites does not move because the author fixed a typo.
        Assert.Equal(Day1, summary.PublishedAt);
    }

    [Fact]
    public void Unpublishing_returns_it_to_draft()
    {
        var id = PostId.New();

        var summary = Assert.Single(Fold(
            Created(id, "hello", "Hello"),
            Published(id, Day1),
            ($"post-{id.Value:N}", new PostUnpublished())).All(Site));

        Assert.Equal(PostStatus.Draft, summary.Status);
        Assert.Null(summary.PublishedAt);
    }

    [Fact]
    public void Published_posts_come_back_newest_first()
    {
        var oldest = PostId.New();
        var newest = PostId.New();
        var middle = PostId.New();

        var list = Fold(
            Created(oldest, "a", "A"), Published(oldest, Day1),
            Created(newest, "b", "B"), Published(newest, Day1.AddDays(10)),
            Created(middle, "c", "C"), Published(middle, Day1.AddDays(5)));

        Assert.Equal([newest, middle, oldest], list.Published(Site).Select(p => p.Id));
    }

    [Fact]
    public void Drafts_are_not_in_the_published_list()
    {
        var draft = PostId.New();
        var live = PostId.New();

        var list = Fold(Created(draft, "d", "D"), Created(live, "l", "L"), Published(live, Day1));

        Assert.Equal(live, Assert.Single(list.Published(Site)).Id);
        Assert.Equal(2, list.All(Site).Count);
    }

    [Fact]
    public void A_deleted_post_disappears_from_both_lists()
    {
        var id = PostId.New();

        var list = Fold(Created(id, "hello", "Hello"), Published(id, Day1), ($"post-{id.Value:N}", new PostDeleted()));

        Assert.Empty(list.All(Site));
        Assert.Empty(list.Published(Site));
    }

    [Fact]
    public void A_slug_is_taken_within_its_own_site_only()
    {
        var id = PostId.New();
        var other = SiteId.New();
        var list = Fold(Created(id, "hello", "Hello"));

        Assert.True(list.SlugTaken(Site, SlugOf("hello")));
        Assert.False(list.SlugTaken(other, SlugOf("hello")));
        Assert.False(list.SlugTaken(Site, SlugOf("hello"), except: id));
    }

    [Fact]
    public void Renaming_frees_the_old_slug()
    {
        var id = PostId.New();

        var list = Fold(Created(id, "hello", "Hello"), ($"post-{id.Value:N}", new PostSlugChanged("goodbye")));

        Assert.False(list.SlugTaken(Site, SlugOf("hello")));
        Assert.True(list.SlugTaken(Site, SlugOf("goodbye")));
    }

    [Fact]
    public void Titles_accumulate_per_locale()
    {
        var id = PostId.New();
        var da = new Locale("da");

        var summary = Assert.Single(Fold(
            Created(id, "hello", "Hello"),
            ($"post-{id.Value:N}", new PostTitleChanged(da, "Hej"))).All(Site));

        Assert.Equal("Hello", summary.Title.Get(En));
        Assert.Equal("Hej", summary.Title.Get(da));
    }

    private static Slug SlugOf(string value)
    {
        Assert.True(Slug.TryCreate(value, out var slug, out var error), error);
        return slug;
    }
}
