using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Posts.SetPostLinkedIn;
using Imprint.Authoring.Features.Posts.SignPublishedPost;
using Imprint.Authoring.Features.Sites.ConfigureEnvironments;
using Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;
using Imprint.Authoring.Features.Sites.SetSiteReviewer;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Authoring.Tests.Features.Posts;

/// <summary>
/// An article that went live without a signature (published before signatures existed, or from a copy of words
/// approved elsewhere) can be signed where it stands: the signature names the login and the words that are LIVE,
/// so it is refused while the draft differs from them, and refused to a service identity like any approval.
/// </summary>
public sealed class SignPublishedPostTests
{
    private const string En = "en";
    private const string Body = "# Hello\n\nProse that went live unsigned.\n";
    private const string Person = "jimmy@example.com";

    [Fact]
    public async Task A_live_unsigned_article_is_signed_by_the_person_over_the_live_words()
    {
        await using var host = NewHost();
        var (site, id) = await PublishedUnsigned(host);

        await AsPerson(host, () => host.Ok(new SignPublishedPost(id)));
        await host.CatchUp();

        var signature = host.Get<PostList>().Get(id)!.Signature!;
        Assert.Equal(Person, signature.SignedBy);
        Assert.Equal(Post.BodySha256(LocalizedText.Of(new Locale(En), Body)), signature.Sha256);
        var live = host.Get<PublishedPosts>().AllForSite(site).Single();
        Assert.Equal(Person, live.SignedBy);
        Assert.True(live.SignedAfterGoingLive);
        Assert.Equal(PostStatus.Published, host.Get<PostList>().Get(id)!.Status);
    }

    [Theory]
    [InlineData("service:authoring-api")]
    [InlineData("service:authoring-mcp")]
    public async Task A_service_identity_cannot_sign(string service)
    {
        await using var host = NewHost();
        var (_, id) = await PublishedUnsigned(host);

        var error = await RefusedTo(host, service, () => host.Fails(new SignPublishedPost(id)));

        Assert.Contains("signed in", error, StringComparison.OrdinalIgnoreCase);
        await host.CatchUp();
        Assert.Null(host.Get<PostList>().Get(id)!.Signature);
    }

    [Fact]
    public async Task A_draft_that_differs_from_the_live_words_cannot_be_signed_as_live()
    {
        await using var host = NewHost();
        var (_, id) = await PublishedUnsigned(host);
        await host.Ok(new ChangePostBody(id, En, "# Hello\n\nWords that are not live.\n"));

        var error = await RefusedTo(host, Person, () => host.Fails(new SignPublishedPost(id)));

        Assert.Contains("not what is live", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_article_never_published_is_signed_through_review_not_here()
    {
        await using var host = NewHost();
        var site = await ReviewedSite(host);
        var id = await Article(host, site);

        var error = await RefusedTo(host, Person, () => host.Fails(new SignPublishedPost(id)));

        Assert.Contains("review", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Signing_a_signed_article_again_changes_nothing()
    {
        await using var host = NewHost();
        var (_, id) = await PublishedUnsigned(host);
        await AsPerson(host, () => host.Ok(new SignPublishedPost(id)));
        await host.CatchUp();
        var first = host.Get<PostList>().Get(id)!.Signature;

        await As(host, "someone.else@example.com", () => host.Ok(new SignPublishedPost(id)));
        await host.CatchUp();

        Assert.Equal(first, host.Get<PostList>().Get(id)!.Signature);
    }

    [Fact]
    public async Task Sign_all_signs_every_live_unsigned_article_and_leaves_the_rest()
    {
        await using var host = NewHost();
        var (site, first) = await PublishedUnsigned(host);
        var second = await Article(host, site, "second");
        await Publish(host, site, second);
        var edited = await Article(host, site, "edited");
        await Publish(host, site, edited);
        await host.Ok(new ChangePostBody(edited, En, "# Hello\n\nNot live yet.\n"));
        var draft = await Article(host, site, "draft");

        await AsPerson(host, () => host.Ok(new SignPublishedPosts(site)));
        await host.CatchUp();

        var posts = host.Get<PostList>();
        Assert.Equal(Person, posts.Get(first)!.Signature?.SignedBy);
        Assert.Equal(Person, posts.Get(second)!.Signature?.SignedBy);
        Assert.Null(posts.Get(edited)!.Signature);
        Assert.Null(posts.Get(draft)!.Signature);
    }

    [Fact]
    public async Task Sign_all_is_refused_to_a_service_identity()
    {
        await using var host = NewHost();
        var (site, id) = await PublishedUnsigned(host);

        var error = await RefusedTo(host, "service:authoring-api", () => host.Fails(new SignPublishedPosts(site)));

        Assert.Contains("signed in", error, StringComparison.OrdinalIgnoreCase);
        await host.CatchUp();
        Assert.Null(host.Get<PostList>().Get(id)!.Signature);
    }

    // A LinkedIn post announces an article going live. Signing one that has been live for days is not news, so a
    // signature given after the article went live never posts it.
    [Fact]
    public async Task An_article_signed_after_it_went_live_is_not_announced_on_LinkedIn()
    {
        await using var host = NewHost();
        var site = await ReviewedSite(host);
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine", "urn:li:organization:42")]));
        await host.Ok(new ConfigureEnvironments(site, [new DeployEnvironment("Production", "rc", "https://rc.example.com")]));
        var id = await Article(host, site);
        await host.Ok(new SetPostLinkedIn(id, "Read it.", "canine"));
        await Publish(host, site, id);
        await AsPerson(host, () => host.Ok(new SignPublishedPost(id)));
        await host.CatchUp();
        var linkedIn = new FakeLinkedIn();

        await new DueLinkedInPoster(host.Dispatcher, host.Get<ProjectionEngine>(), host.Get<PublishedPosts>(),
                host.Get<SiteOverview>(), host.Get<LinkedInPosts>(), linkedIn, new LinkedInPostingOptions(),
                NullLogger<DueLinkedInPoster>.Instance, new FixedClock(DateTimeOffset.UtcNow.AddMinutes(1)))
            .PostDueAsync(CancellationToken.None);

        Assert.Equal(0, linkedIn.Sent);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private sealed class FakeLinkedIn : ILinkedInPoster
    {
        public int Sent { get; private set; }
        public bool IsConfigured => true;

        public Task<LinkedInSendResult> PostAsync(LinkedInPostRequest request, CancellationToken ct)
        {
            Sent++;
            return Task.FromResult(new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: "urn:li:share:1"));
        }
    }

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));

    private static async Task<SiteId> ReviewedSite(AuthoringTestHost host)
    {
        var site = SiteId.New();
        await host.Ok(new Imprint.Authoring.Features.Sites.CreateBlog.CreateBlog(site, "Resource Center", En, Kind: SiteKind.ResourceCenter));
        return site;
    }

    private static async Task<PostId> Article(AuthoringTestHost host, SiteId site, string slug = "hello")
    {
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello " + slug, slug, En, "guide", []));
        await host.Ok(new ChangePostBody(id, En, Body));
        return id;
    }

    // Published as the 24 resource center articles were: with the site's reviewer set aside for the publish, then
    // named again, so the site reviews and the article is live without a signature.
    private static async Task Publish(AuthoringTestHost host, SiteId site, PostId id)
    {
        await host.Ok(new SetSiteReviewer(site, null, null));
        await host.Ok(new PublishPost(id, En));
        await host.Ok(new SetSiteReviewer(site, "Jimmy", Person));
    }

    private static async Task<(SiteId Site, PostId Id)> PublishedUnsigned(AuthoringTestHost host)
    {
        var site = await ReviewedSite(host);
        var id = await Article(host, site);
        await Publish(host, site, id);
        await host.CatchUp();
        return (site, id);
    }

    private static Task AsPerson(AuthoringTestHost host, Func<Task> act) => As(host, Person, act);

    private static async Task<string> RefusedTo(AuthoringTestHost host, string actor, Func<Task<string>> act)
    {
        var error = "";
        await As(host, actor, async () => error = await act());
        return error;
    }

    private static async Task As(AuthoringTestHost host, string actor, Func<Task> act)
    {
        var metadata = host.Get<EventMetadataProvider>();
        var before = metadata.ActorSource;
        metadata.ActorSource = () => actor;
        try
        {
            await act();
        }
        finally
        {
            metadata.ActorSource = before;
        }
    }
}
