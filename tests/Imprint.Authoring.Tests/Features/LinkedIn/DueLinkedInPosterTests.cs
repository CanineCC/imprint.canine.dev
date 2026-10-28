using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.ApprovePostReview;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.SetPostLinkedIn;
using Imprint.Authoring.Features.Posts.SubmitPostForReview;
using Imprint.Authoring.Features.Sites.ConfigureEnvironments;
using Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;
using Imprint.Authoring.Features.Sites.SetSiteReviewer;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Authoring.Tests.Features.LinkedIn;

/// <summary>
/// A signed, published article with a LinkedIn text is posted once, as the sender it names, linking to the article
/// on the site's production address. Nothing else is posted, an unknown result is never sent again on its own, and
/// a launch that publishes many articles at once is paced. LinkedIn here is always the fake.
/// </summary>
public sealed class DueLinkedInPosterTests
{
    private const string En = "en";
    private const string Body = "# Hello\n\nProse worth clearing.\n";
    private const string Org = "urn:li:organization:42";

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }

    private sealed class FakeLinkedIn : ILinkedInPoster
    {
        public List<LinkedInPostRequest> Sent { get; } = [];
        public Queue<LinkedInSendResult> Answers { get; } = [];
        public bool IsConfigured { get; set; } = true;
        public HashSet<string> ConnectedMembers { get; } = [];

        public bool IsConfiguredFor(string authorUrn) => authorUrn.StartsWith("urn:li:person:", StringComparison.Ordinal)
            ? ConnectedMembers.Contains(authorUrn)
            : IsConfigured;

        public Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct)
        {
            Sent.Add(request);
            return Task.FromResult(Answers.Count > 0
                ? Answers.Dequeue()
                : new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: $"urn:li:share:{Sent.Count}"));
        }
    }

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));

    private static DueLinkedInPoster Poster(AuthoringTestHost host, FakeLinkedIn linkedIn, FixedClock clock, int perHour = 2) =>
        new(host.Dispatcher, host.Get<ProjectionEngine>(), host.Get<PublishedPosts>(), host.Get<SiteOverview>(),
            host.Get<LinkedInPosts>(), linkedIn, new LinkedInPostingOptions { MaxPerSitePerHour = perHour },
            NullLogger<DueLinkedInPoster>.Instance, clock);

    private static FixedClock Soon() => new(DateTimeOffset.UtcNow.AddMinutes(1));

    private static async Task<SiteId> ASite(AuthoringTestHost host, string? baseUrl = "https://rc.example.com")
    {
        var site = SiteId.New();
        await host.Ok(new Imprint.Authoring.Features.Sites.CreateBlog.CreateBlog(site, "Resource Center", En, Kind: SiteKind.ResourceCenter));
        await host.Ok(new SetSiteReviewer(site, "Jimmy", "jimmy@example.com"));
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine", Org)]));
        await host.Ok(new ConfigureEnvironments(site, [new DeployEnvironment("Production", "rc", baseUrl)]));
        return site;
    }

    private static async Task<PostId> AnArticle(
        AuthoringTestHost host, SiteId site, string slug, string? linkedIn = "Read it.", bool sign = true, bool publish = true)
    {
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello " + slug, slug, En, "essay", []));
        await host.Ok(new Imprint.Authoring.Features.Posts.ChangePostBody.ChangePostBody(id, En, Body));
        if (linkedIn is not null)
        {
            await host.Ok(new SetPostLinkedIn(id, linkedIn, "canine"));
        }

        if (sign)
        {
            await host.Ok(new SubmitPostForReview(id, En, null, null));
            await host.Ok(new ApprovePostReview(id, null));
        }

        if (publish)
        {
            await host.Ok(new PublishPost(id, En));
        }

        await host.CatchUp();
        return id;
    }

    [Fact]
    public async Task A_signed_published_article_is_posted_once_as_its_sender_linking_to_the_article()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var id = await AnArticle(host, site, "first");
        var linkedIn = new FakeLinkedIn();
        var clock = Soon();

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = clock.Now.AddHours(2);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        var sent = Assert.Single(linkedIn.Sent);
        Assert.Equal(Org, sent.AuthorUrn);
        Assert.Equal("Read it.", sent.Commentary);
        Assert.Equal("https://rc.example.com/first/", sent.ArticleUrl);
        Assert.Equal("Hello first", sent.Title);
        var row = host.Get<LinkedInPosts>().Get(id)!;
        Assert.Equal(LinkedInPostState.Posted, row.State);
        Assert.Equal("urn:li:share:1", row.Urn);
    }

    [Fact]
    public async Task An_unpublished_or_unsigned_article_or_one_without_text_is_not_posted()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        await AnArticle(host, site, "draft", publish: false);
        await AnArticle(host, site, "no-text", linkedIn: null);
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task A_published_article_on_a_site_that_does_not_review_is_not_signed_and_is_not_posted()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine", Org)]));
        await host.Ok(new ConfigureEnvironments(site, [new DeployEnvironment("Production", "rc", "https://rc.example.com")]));
        await AnArticle(host, site, "unsigned", sign: false);
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task On_a_site_with_a_blog_the_link_is_under_blog()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        await host.Ok(new SetSiteReviewer(site, "Jimmy", "jimmy@example.com"));
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine", Org)]));
        await host.Ok(new ConfigureEnvironments(site, [new DeployEnvironment("production", "site", "https://www.example.com/")]));
        await AnArticle(host, site, "first");
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        Assert.Equal("https://www.example.com/blog/first/", Assert.Single(linkedIn.Sent).ArticleUrl);
    }

    [Fact]
    public async Task A_site_without_a_production_address_posts_nothing()
    {
        await using var host = NewHost();
        var site = await ASite(host, baseUrl: null);
        await AnArticle(host, site, "first");
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task LinkedIn_not_set_up_sends_and_records_nothing()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var id = await AnArticle(host, site, "first");
        var linkedIn = new FakeLinkedIn { IsConfigured = false };

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
        Assert.Null(host.Get<LinkedInPosts>().Get(id));
    }

    // A person posts with their own connection, so an article as a member goes out while the page is not set up,
    // and one as the page waits unrecorded (for the day the page is connected, within the window).
    [Fact]
    public async Task An_article_as_a_connected_member_goes_out_while_the_page_is_not_set_up()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        await host.Ok(new SetSiteLinkedInSenders(site,
            [new LinkedInSender("canine", "Canine", Org), new LinkedInSender("jimmy", "Jimmy Borch", "urn:li:person:Ab3")]));
        var asPage = await AnArticle(host, site, "as-page");
        var asMember = PostId.New();
        await host.Ok(new CreatePost(asMember, site, "Hello as-member", "as-member", En, "essay", []));
        await host.Ok(new Imprint.Authoring.Features.Posts.ChangePostBody.ChangePostBody(asMember, En, Body));
        await host.Ok(new SetPostLinkedIn(asMember, "Mine.", "jimmy"));
        await host.Ok(new SubmitPostForReview(asMember, En, null, null));
        await host.Ok(new ApprovePostReview(asMember, null));
        await host.Ok(new PublishPost(asMember, En));
        await host.CatchUp();
        var linkedIn = new FakeLinkedIn { IsConfigured = false };
        linkedIn.ConnectedMembers.Add("urn:li:person:Ab3");

        await Poster(host, linkedIn, Soon()).PostDueAsync(CancellationToken.None);

        var sent = Assert.Single(linkedIn.Sent);
        Assert.Equal("urn:li:person:Ab3", sent.AuthorUrn);
        Assert.Equal(LinkedInPostState.Posted, host.Get<LinkedInPosts>().Get(asMember)!.State);
        Assert.Null(host.Get<LinkedInPosts>().Get(asPage));
    }

    [Fact]
    public async Task An_article_published_longer_ago_than_the_window_is_not_announced()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        await AnArticle(host, site, "stale");
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, new FixedClock(DateTimeOffset.UtcNow.AddDays(8))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task An_unknown_result_is_never_sent_again_until_a_person_resolves_it()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var id = await AnArticle(host, site, "first");
        var linkedIn = new FakeLinkedIn();
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "timed out"));
        var clock = Soon();

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = clock.Now.AddHours(3);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        Assert.Equal(LinkedInPostState.Unknown, host.Get<LinkedInPosts>().Get(id)!.State);

        // The person looked: it is not on the page, so it may go again.
        await host.Ok(new ResolveLinkedInPost(id, null, clock.Now));
        await host.CatchUp();
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Equal(2, linkedIn.Sent.Count);
        Assert.Equal(LinkedInPostState.Posted, host.Get<LinkedInPosts>().Get(id)!.State);
    }

    [Fact]
    public async Task A_retryable_refusal_is_sent_again_after_its_wait_and_a_final_one_never()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var retry = await AnArticle(host, site, "a-retry");
        var final = await AnArticle(host, site, "b-final");
        var linkedIn = new FakeLinkedIn();
        // Articles go oldest publish first (then by slug): "a-retry", then "b-final".
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "429", Retryable: true, RetryAfter: TimeSpan.FromMinutes(30)));
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "403 ACCESS_DENIED"));
        var clock = Soon();

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = clock.Now.AddMinutes(10);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        Assert.Equal(2, linkedIn.Sent.Count);

        clock.Now = clock.Now.AddHours(2);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Equal(3, linkedIn.Sent.Count);
        Assert.Equal("https://rc.example.com/a-retry/", linkedIn.Sent[2].ArticleUrl);
        Assert.Equal(LinkedInPostState.Posted, host.Get<LinkedInPosts>().Get(retry)!.State);
        Assert.Equal(LinkedInPostState.Failed, host.Get<LinkedInPosts>().Get(final)!.State);
    }

    [Fact]
    public async Task A_launch_of_many_articles_is_paced_per_site_per_hour()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        for (var i = 0; i < 5; i++)
        {
            await AnArticle(host, site, $"a{i}");
        }

        var linkedIn = new FakeLinkedIn();
        var clock = Soon();

        await Poster(host, linkedIn, clock, perHour: 2).PostDueAsync(CancellationToken.None);
        await Poster(host, linkedIn, clock, perHour: 2).PostDueAsync(CancellationToken.None);
        Assert.Equal(2, linkedIn.Sent.Count);

        clock.Now = clock.Now.AddMinutes(61);
        await Poster(host, linkedIn, clock, perHour: 2).PostDueAsync(CancellationToken.None);
        Assert.Equal(4, linkedIn.Sent.Count);
    }
}
