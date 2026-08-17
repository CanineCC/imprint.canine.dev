using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Api;

namespace Imprint.Editor.Tests;

/// <summary>
/// The two shapes a post is returned in. They exist as a pair on purpose: a list of posts must not
/// carry every body, and a single post is useless without one — the API could write a post and
/// never read it back, which is a write-only surface pretending to be a resource.
/// </summary>
public sealed class AuthoringPostViewTests
{
    private static readonly Locale En = new("en");
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));

    private static PostSummary Summary(PostReview review = PostReview.None, DateTimeOffset? publishAt = null) =>
        new(PostId.New(), SiteId.New(), MakeSlug("hello-world"), LocalizedText.Of(En, "Hello world"),
            PublishedAt: null, Version: 3, PublishedVersion: null, UpdatedAt: September,
            review, publishAt);

    private static Slug MakeSlug(string value)
    {
        Assert.True(Slug.TryCreate(value, out var slug, out var error), error);
        return slug;
    }

    private static Post NewPost(string markdown)
    {
        var post = Post.Create(PostId.New(), SiteId.New(), MakeSlug("hello-world"), En, "Hello world");
        post.ChangeBody(En, markdown);
        post.ChangeMeta(En, "Hello world · Canine", "The first post.");
        return post;
    }

    [Fact]
    public void PostView_carries_no_body()
    {
        var view = AuthoringApi.PostView(Summary(), site: null);

        Assert.False(view.ContainsKey("body"));
        Assert.Equal("hello-world", view["slug"]);
    }

    [Fact]
    public void PostDetailView_carries_the_markdown_and_the_meta()
    {
        var view = AuthoringApi.PostDetailView(Summary(), site: null, NewPost("# Hello\n\nProse.\n"));

        var body = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(view["body"]);
        Assert.Equal("# Hello\n\nProse.\n", body["en"]);
        var meta = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(view["metaDescription"]);
        Assert.Equal("The first post.", meta["en"]);
    }

    [Fact]
    public void PostDetailView_keeps_every_field_the_list_shows()
    {
        // The two shapes must not drift: the detail is the list plus the body, never a different
        // spelling of the same facts.
        var summary = Summary();
        var list = AuthoringApi.PostView(summary, site: null);
        var detail = AuthoringApi.PostDetailView(summary, site: null, NewPost("Prose."));

        foreach (var (key, value) in list)
        {
            Assert.Equal(value, detail[key]);
        }
    }

    [Fact]
    public void Status_is_computed_against_the_sites_review_policy()
    {
        // A future date on a site that reviews is a PROPOSAL, not a schedule — nothing will
        // publish it until a person says so, and the view must not promise otherwise.
        var summary = Summary(PostReview.None, publishAt: DateTimeOffset.UtcNow.AddDays(30));

        var reviewed = Site.Create(SiteId.New(), "Canine Blog", En);
        reviewed.SetReviewer("Lasse", "lasse@example.com");

        Assert.Equal("Scheduled", AuthoringApi.PostView(summary, site: null)["status"]);
        Assert.Equal("Draft", AuthoringApi.PostView(summary, reviewed)["status"]);
    }
}
