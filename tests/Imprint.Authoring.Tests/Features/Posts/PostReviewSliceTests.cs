using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.ApprovePostReview;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.RequestPostChanges;
using Imprint.Authoring.Features.Posts.SchedulePost;
using Imprint.Authoring.Features.Posts.SubmitPostForReview;
using Imprint.Authoring.Features.Sites.SetSiteReviewer;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Posts;

/// <summary>
/// The half of the review workflow that is a SITE policy rather than a post's own state: a site
/// with a named reviewer publishes only what that reviewer cleared, and a site with nobody named
/// publishes exactly as it always did. That second sentence is the one that matters most — this
/// change must be invisible to every site that does not opt into it.
/// </summary>
public sealed class PostReviewSliceTests
{
    private const string En = "en";
    private const string Body = "# Hello\n\nProse worth clearing.\n";
    private static readonly DateTimeOffset September = new(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public async Task A_site_with_no_reviewer_publishes_directly_exactly_as_before()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);

        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();

        Assert.Equal(PostStatus.Published, host.Get<PostList>().Get(id)!.Status);
    }

    [Fact]
    public async Task A_reviewed_site_refuses_a_direct_publish_and_names_the_reviewer()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));
        await host.CatchUp();

        var error = await host.Fails(new PublishPost(id, En));

        Assert.Contains("Lasse", error, StringComparison.Ordinal);
        Assert.Equal(PostStatus.Draft, host.Get<PostList>().Get(id)!.Status);
    }

    [Fact]
    public async Task Submit_then_approve_lets_it_publish()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));

        await host.Ok(new SubmitPostForReview(id, En, September, "Cleared with legal."));
        await host.CatchUp();
        Assert.Equal(PostStatus.InReview, host.Get<PostList>().Get(id)!.Status);

        // The reviewer moves the date, which is the point of them holding it.
        await host.Ok(new ApprovePostReview(id, September.AddDays(1)));
        await host.CatchUp();
        var approved = host.Get<PostList>().Get(id)!;
        Assert.Equal(September.AddDays(1), approved.PublishAt);

        await host.Ok(new PublishPost(id, En));
        await host.CatchUp();
        Assert.Equal(PostStatus.Published, host.Get<PostList>().Get(id)!.Status);
    }

    [Fact]
    public async Task Submitting_where_nobody_is_named_is_refused_with_a_way_out()
    {
        await using var host = NewHost();
        var (_, id) = await NewPost(host);

        var error = await host.Fails(new SubmitPostForReview(id, En, September, null));

        Assert.Contains("no reviewer configured", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sent_back_the_post_returns_to_the_author_with_the_reason()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));
        await host.Ok(new SubmitPostForReview(id, En, September, null));

        await host.Ok(new RequestPostChanges(id, "Names a customer we cannot name."));
        await host.CatchUp();

        var post = host.Get<PostList>().Get(id)!;
        Assert.Equal(PostStatus.ChangesRequested, post.Status);
        Assert.Equal("Names a customer we cannot name.", post.ReviewNote);

        // …and it still cannot go out on its own.
        Assert.Contains("Lasse", await host.Fails(new PublishPost(id, En)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_edit_after_approval_sends_it_back_to_the_reviewer()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));
        await host.Ok(new SubmitPostForReview(id, En, September, null));
        await host.Ok(new ApprovePostReview(id, September));

        await host.Ok(new ChangePostBody(id, En, "# Hello\n\nSomething the reviewer never saw.\n"));
        await host.CatchUp();

        Assert.Equal(PostReview.None, host.Get<PostList>().Get(id)!.Review);
        Assert.Contains("Lasse", await host.Fails(new PublishPost(id, En)), StringComparison.Ordinal);
    }

    // --------------------------------------------------------------- scheduling

    [Fact]
    public async Task A_future_date_makes_a_post_scheduled_and_a_past_one_due()
    {
        await using var host = NewHost();
        var (_, id) = await NewPost(host);

        await host.Ok(new SchedulePost(id, September));
        await host.CatchUp();

        var post = host.Get<PostList>().Get(id)!;
        Assert.Equal(PostStatus.Scheduled, post.StatusAt(September.AddDays(-1)));
        Assert.False(post.IsDueAt(September.AddDays(-1), siteRequiresReview: false));
        Assert.True(post.IsDueAt(September.AddMinutes(1), siteRequiresReview: false));
    }

    [Fact]
    public async Task A_scheduled_post_on_a_reviewed_site_is_not_due_until_it_is_approved()
    {
        // The clock does not overrule the reviewer: this is the rule that stops "schedule it and
        // it goes out anyway" from being a way around the gate.
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));
        await host.Ok(new SchedulePost(id, September));
        await host.CatchUp();

        Assert.False(host.Get<PostList>().Get(id)!.IsDueAt(September.AddMinutes(1), siteRequiresReview: true));

        await host.Ok(new SubmitPostForReview(id, En, September, null));
        await host.Ok(new ApprovePostReview(id, September));
        await host.CatchUp();

        Assert.True(host.Get<PostList>().Get(id)!.IsDueAt(September.AddMinutes(1), siteRequiresReview: true));
    }

    [Fact]
    public async Task Clearing_the_date_leaves_an_approved_post_waiting_rather_than_publishing_it()
    {
        await using var host = NewHost();
        var (site, id) = await NewPost(host);
        await host.Ok(new SetSiteReviewer(site, "Lasse", "lasse@example.com"));
        await host.Ok(new SubmitPostForReview(id, En, September, null));

        // "Approved, timing to be decided" — a real answer, and nothing must act on it.
        await host.Ok(new ApprovePostReview(id, null));
        await host.CatchUp();

        var post = host.Get<PostList>().Get(id)!;
        Assert.Equal(PostStatus.Approved, post.Status);
        Assert.False(post.IsDueAt(September.AddYears(1), siteRequiresReview: true));
    }

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));

    private static async Task<(SiteId Site, PostId Id)> NewPost(AuthoringTestHost host)
    {
        var site = await host.CreateTestSite();
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello world", "hello-world", En));
        await host.Ok(new ChangePostBody(id, En, Body));
        await host.CatchUp();
        return (site, id);
    }
}
