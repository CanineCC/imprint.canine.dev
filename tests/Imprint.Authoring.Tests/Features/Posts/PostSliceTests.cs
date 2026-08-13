using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.ChangePostSlug;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.DeletePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.UnpublishPost;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Posts;

/// <summary>
/// The post slices against the real dispatcher, a real SQLite event store and the real
/// projections — the exact path production takes, so what these assert is that a command
/// reaches the log AND the read model the editor will render from.
/// </summary>
public sealed class PostSliceTests
{
    private const string En = "en";

    [Fact]
    public async Task Creating_a_post_puts_it_in_the_list_as_a_draft()
    {
        await using var host = new AuthoringTestHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();

        await host.Ok(new CreatePost(id, site, "Hello world", "hello-world", En));
        await host.CatchUp();

        var post = Assert.Single(host.Get<PostList>().All(site));
        Assert.Equal(id, post.Id);
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.Empty(host.Get<PostList>().Published(site));
    }

    [Fact]
    public async Task A_slug_already_used_by_another_post_is_refused()
    {
        await using var host = new AuthoringTestHost();
        var site = await host.CreateTestSite();
        await host.Ok(new CreatePost(PostId.New(), site, "First", "hello", En));
        await host.CatchUp();

        var message = await host.Fails(new CreatePost(PostId.New(), site, "Second", "hello", En));

        Assert.Contains("already used", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_locale_the_site_does_not_have_is_refused()
    {
        await using var host = new AuthoringTestHost();
        var site = await host.CreateTestSite();

        // The slug is deliberately NOT language-code-shaped: Slug reserves those (they would
        // collide with the locale prefix in a path), and that rule would fail this command
        // before the locale check under test ever ran.
        var message = await host.Fails(new CreatePost(PostId.New(), site, "Hej", "hej-verden", "da"));

        Assert.Contains("not one of this site's locales", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_draft_body_that_cannot_convert_is_still_saved()
    {
        // The editor must be able to save mid-sentence; the gate is publish, not this.
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);

        await host.Ok(new ChangePostBody(id, En, "An unfinished ** and a `stray backtick"));
        await host.CatchUp();

        Assert.Equal(PostStatus.Draft, host.Get<PostList>().Get(id)!.Status);
    }

    [Fact]
    public async Task Publishing_a_convertible_post_makes_it_live()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new ChangePostBody(id, En, "# Title\n\nReal prose with a [link](https://example.test/)."));

        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();

        var post = Assert.Single(host.Get<PostList>().Published(site));
        Assert.Equal(id, post.Id);
        Assert.Equal(PostStatus.Published, post.Status);
        Assert.NotNull(post.PublishedAt);
    }

    [Fact]
    public async Task Publishing_is_refused_while_the_body_cannot_convert()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new ChangePostBody(id, En, "Fine.\n\n| a | table |"));

        var message = await host.Fails(new PublishPost(id, En));
        await host.CatchUp();

        Assert.Contains("line 3", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("table", message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(host.Get<PostList>().Published(site));
    }

    [Fact]
    public async Task Publishing_an_empty_post_is_refused()
    {
        await using var host = new AuthoringTestHost();
        var (_, id) = await NewPost(host);

        var message = await host.Fails(new PublishPost(id, En));

        Assert.Contains("empty", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_edit_after_publishing_shows_as_modified_then_republishes_clean()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new ChangePostBody(id, En, "First cut."));
        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();
        var firstDate = host.Get<PostList>().Get(id)!.PublishedAt;

        await host.Ok(new ChangePostBody(id, En, "Second cut."));
        await host.CatchUp();
        Assert.Equal(PostStatus.Modified, host.Get<PostList>().Get(id)!.Status);

        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();

        // Publishing again is an update: the post is clean, and the date a reader cites is
        // still the first one.
        Assert.Equal(firstDate, host.Get<PostList>().Get(id)!.PublishedAt);
    }

    [Fact]
    public async Task Unpublishing_takes_it_off_the_public_list()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new ChangePostBody(id, En, "Prose."));
        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();

        await host.Ok(new UnpublishPost(id));
        await host.CatchUp();

        Assert.Empty(host.Get<PostList>().Published(site));
        Assert.Single(host.Get<PostList>().All(site));
    }

    [Fact]
    public async Task Renaming_to_a_free_slug_works_and_frees_the_old_one()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host, "first-slug");

        await host.Ok(new ChangePostSlug(id, "second-slug"));
        await host.CatchUp();

        Assert.Equal("second-slug", host.Get<PostList>().Get(id)!.Slug.Value);
        await host.Ok(new CreatePost(PostId.New(), site, "Another", "first-slug", En));
    }

    [Fact]
    public async Task Renaming_onto_another_posts_slug_is_refused()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host, "mine");
        await host.Ok(new CreatePost(PostId.New(), site, "Theirs", "theirs", En));
        await host.CatchUp();

        var message = await host.Fails(new ChangePostSlug(id, "theirs"));

        Assert.Contains("already used", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deleting_removes_it_from_the_list()
    {
        await using var host = new AuthoringTestHost();
        var (site, id) = await NewPost(host);

        await host.Ok(new DeletePost(id));
        await host.CatchUp();

        Assert.Empty(host.Get<PostList>().All(site));
    }

    private static async Task<(SiteId Site, PostId Id)> NewPost(AuthoringTestHost host, string slug = "hello-world")
    {
        var site = await host.CreateTestSite();
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello world", slug, En));
        await host.CatchUp();
        return (site, id);
    }
}
