using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Projections;

/// <summary>
/// The publisher's post source. Its contract is about TIME: what a reader sees is what was
/// approved at publish, and an edit that has not been published again must not leak onto the
/// live site.
/// </summary>
public sealed class PublishedPostsTests
{
    private static readonly Locale En = new("en");
    private static readonly Locale Da = new("da");
    private static readonly SiteId Site = SiteId.New();
    private static readonly DateTimeOffset Day1 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private long _position;

    private PublishedPosts Fold(params (string Stream, object Event)[] events)
    {
        var model = new PublishedPosts();
        var versions = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (stream, @event) in events)
        {
            versions[stream] = versions.GetValueOrDefault(stream) + 1;
            _position++;
            model.Apply(new StoredEvent(
                _position, stream, versions[stream], $"e{_position}", @event,
                new EventMetadata("test", Day1.AddMinutes(_position), Guid.Empty, Guid.Empty)));
        }

        return model;
    }

    private static string Stream(PostId id) => $"post-{id.Value:N}";

    private static string TextOf(NodeList roots) =>
        string.Join(" ", ((SectionNode)roots[0]).Children.OfType<RichTextNode>().Select(n => n.Html.Get(En)));

    [Fact]
    public void A_draft_is_not_published()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "Prose.")));

        Assert.Empty(model.AllForSite(Site));
    }

    [Fact]
    public void Publishing_snapshots_the_converted_tree()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "# Title\n\nProse.")),
            (Stream(id), new PostPublished(Day1)));

        var post = Assert.Single(model.AllForSite(Site));
        Assert.Equal(Day1, post.PublishedAt);
        var roots = post.RootsFor(En, En);
        var section = Assert.IsType<SectionNode>(Assert.Single(roots));
        Assert.Equal(2, section.Children.Count);
    }

    [Fact]
    public void An_unpublished_edit_does_not_reach_the_live_post()
    {
        // The whole reason this projection snapshots instead of reading the draft.
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "First cut.")),
            (Stream(id), new PostPublished(Day1)),
            (Stream(id), new PostBodyChanged(En, "Second cut, still being written.")));

        Assert.Contains("First cut.", TextOf(Assert.Single(model.AllForSite(Site)).RootsFor(En, En)), StringComparison.Ordinal);
    }

    [Fact]
    public void Publishing_again_replaces_the_live_content_but_keeps_the_date()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "First cut.")),
            (Stream(id), new PostPublished(Day1)),
            (Stream(id), new PostBodyChanged(En, "Second cut.")),
            (Stream(id), new PostPublished(Day1.AddDays(2))));

        var post = Assert.Single(model.AllForSite(Site));
        Assert.Contains("Second cut.", TextOf(post.RootsFor(En, En)), StringComparison.Ordinal);
        Assert.Equal(Day1, post.PublishedAt);
    }

    [Fact]
    public void Unpublishing_withdraws_it_immediately()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "Prose.")),
            (Stream(id), new PostPublished(Day1)),
            (Stream(id), new PostUnpublished()));

        Assert.Empty(model.AllForSite(Site));
    }

    [Fact]
    public void Deleting_withdraws_it_too()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "Prose.")),
            (Stream(id), new PostPublished(Day1)),
            (Stream(id), new PostDeleted()));

        Assert.Empty(model.AllForSite(Site));
    }

    [Fact]
    public void Each_locale_gets_its_own_tree()
    {
        // A translation is written independently: different paragraph count, its own shape.
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "One paragraph.")),
            (Stream(id), new PostBodyChanged(Da, "Et afsnit.\n\nOg et til.")),
            (Stream(id), new PostPublished(Day1)));

        var post = Assert.Single(model.AllForSite(Site));
        Assert.Equal(2, post.Locales.Count);
        Assert.Single(((SectionNode)post.RootsFor(En, En)[0]).Children);
        Assert.Equal(2, ((SectionNode)post.RootsFor(Da, En)[0]).Children.Count);
    }

    [Fact]
    public void An_untranslated_locale_falls_back_to_the_default()
    {
        var id = PostId.New();

        var model = Fold(
            (Stream(id), new PostCreated(id, Site, "hello", En, "Hello")),
            (Stream(id), new PostBodyChanged(En, "Only English.")),
            (Stream(id), new PostPublished(Day1)));

        var post = Assert.Single(model.AllForSite(Site));
        Assert.Contains("Only English.", TextOf(post.RootsFor(Da, En)), StringComparison.Ordinal);
    }

    [Fact]
    public void Posts_come_back_newest_first()
    {
        var older = PostId.New();
        var newer = PostId.New();

        var model = Fold(
            (Stream(older), new PostCreated(older, Site, "a", En, "A")),
            (Stream(older), new PostBodyChanged(En, "A.")),
            (Stream(older), new PostPublished(Day1)),
            (Stream(newer), new PostCreated(newer, Site, "b", En, "B")),
            (Stream(newer), new PostBodyChanged(En, "B.")),
            (Stream(newer), new PostPublished(Day1.AddDays(7))));

        Assert.Equal([newer, older], model.AllForSite(Site).Select(p => p.Id));
    }
}
