using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.UploadAsset;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Imprint.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Authoring.Tests.Features.LinkedInUpdates;

/// <summary>
/// A scheduled LinkedIn update goes out once, at its time, as its sender, with its images; one that is late by more
/// than two hours is held back as missed; an unknown result is never sent again on its own. LinkedIn is the fake.
/// </summary>
public sealed class LinkedInUpdateSchedulingTests
{
    private const string Person = "urn:li:person:Ab3";

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }

    private sealed class FakeLinkedIn : ILinkedInUpdatePoster
    {
        public List<LinkedInUpdateRequest> Sent { get; } = [];
        public Queue<LinkedInSendResult> Answers { get; } = [];
        public List<LinkedInCommentRequest> Comments { get; } = [];
        public Queue<LinkedInSendResult> CommentAnswers { get; } = [];
        public HashSet<string> Connected { get; } = [Person];

        public bool IsConfiguredFor(string authorUrn) => Connected.Contains(authorUrn);

        public Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct)
        {
            Sent.Add(request);
            return Task.FromResult(Answers.Count > 0
                ? Answers.Dequeue()
                : new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: $"urn:li:share:{Sent.Count}"));
        }

        public Task<LinkedInSendResult> CommentAsync(LinkedInCommentRequest request, CancellationToken ct)
        {
            Comments.Add(request);
            return Task.FromResult(CommentAnswers.Count > 0
                ? CommentAnswers.Dequeue()
                : new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: $"urn:li:comment:(urn:li:activity:1,{Comments.Count})"));
        }
    }

    private static AuthoringTestHost NewHost() =>
        new(services =>
        {
            services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog());
            services.AddImprintAssetProcessing();
        });

    private static DueLinkedInUpdatePoster Poster(AuthoringTestHost host, FakeLinkedIn linkedIn, FixedClock clock) =>
        new(host.Dispatcher, host.Get<ProjectionEngine>(), host.Get<LinkedInUpdateList>(), host.Get<SiteOverview>(),
            host.Get<AssetLibrary>(), host.Get<IMediaStore>(), linkedIn,
            new LinkedInUpdateOptions(), NullLogger<DueLinkedInUpdatePoster>.Instance, clock);

    private static async Task<SiteId> ASite(AuthoringTestHost host)
    {
        var site = SiteId.New();
        await host.Ok(new Imprint.Authoring.Features.Sites.CreateBlog.CreateBlog(site, "Canine", "en"));
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("jimmy", "Jimmy Borch", Person)]));
        return site;
    }

    private static async Task<AssetId> AnImage(AuthoringTestHost host)
    {
        var id = AssetId.New();
        await host.Ok(new UploadAsset(id, "p1.png", "image/png", 4, new MemoryStream([1, 2, 3, 4])));
        return id;
    }

    private static async Task<LinkedInUpdateId> AnUpdate(
        AuthoringTestHost host, SiteId site, DateTimeOffset publishAt, IReadOnlyList<LinkedInUpdateImage>? images = null)
    {
        var id = LinkedInUpdateId.New();
        await host.Ok(new ScheduleLinkedInUpdate(id, site, "jimmy", "Review cannot keep up.", images ?? [], publishAt));
        await host.CatchUp();
        return id;
    }

    [Fact]
    public async Task A_due_update_is_posted_once_as_its_sender_with_its_images()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var image = await AnImage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at, [new LinkedInUpdateImage(image, "A chart")]);
        var linkedIn = new FakeLinkedIn();
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        var sent = Assert.Single(linkedIn.Sent);
        Assert.Equal((Person, "Review cannot keep up."), (sent.AuthorUrn, sent.Commentary));
        var picture = Assert.Single(sent.Images);
        Assert.Equal(("image/png", "A chart"), (picture.ContentType, picture.Alt));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, picture.Content);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, "urn:li:share:1"), (row.State, row.Urn));
    }

    [Fact]
    public async Task An_update_before_its_time_is_left_alone()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddHours(1);
        await AnUpdate(host, site, at);
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, new FixedClock(at.AddMinutes(-1))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task An_update_more_than_two_hours_late_is_missed_not_sent()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at);
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, new FixedClock(at.AddHours(2).AddMinutes(1))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
        await host.CatchUp();
        Assert.Equal(LinkedInUpdateState.Missed, host.Get<LinkedInUpdateList>().Get(id)!.State);
    }

    [Fact]
    public async Task An_unknown_result_is_never_sent_again_on_its_own()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at);
        var linkedIn = new FakeLinkedIn();
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "timed out"));
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = at.AddMinutes(30);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        await host.CatchUp();
        Assert.Equal(LinkedInUpdateState.Unknown, host.Get<LinkedInUpdateList>().Get(id)!.State);
    }

    [Fact]
    public async Task A_sender_who_is_not_connected_gets_nothing_sent_and_the_update_is_missed_with_that_reason()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at);
        var linkedIn = new FakeLinkedIn();
        linkedIn.Connected.Clear();

        await Poster(host, linkedIn, new FixedClock(at.AddMinutes(1))).PostDueAsync(CancellationToken.None);
        Assert.Empty(linkedIn.Sent);
        await Poster(host, linkedIn, new FixedClock(at.AddHours(3))).PostDueAsync(CancellationToken.None);

        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal(LinkedInUpdateState.Missed, row.State);
        Assert.Contains("not connected", row.Reason);
    }

    [Fact]
    public async Task A_cancelled_update_is_never_sent()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at);
        await host.Ok(new CancelLinkedInUpdate(id));
        await host.CatchUp();
        var linkedIn = new FakeLinkedIn();

        await Poster(host, linkedIn, new FixedClock(at.AddMinutes(1))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
    }

    [Fact]
    public async Task Scheduling_as_a_sender_the_site_does_not_have_is_refused()
    {
        await using var host = NewHost();
        var site = await ASite(host);

        var error = await host.Fails(new ScheduleLinkedInUpdate(
            LinkedInUpdateId.New(), site, "someone", "Hi", [], DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Contains("jimmy", error);
    }

    [Fact]
    public async Task Scheduling_an_image_that_is_not_in_the_library_is_refused()
    {
        await using var host = NewHost();
        var site = await ASite(host);

        var error = await host.Fails(new ScheduleLinkedInUpdate(
            LinkedInUpdateId.New(), site, "jimmy", "Hi", [new LinkedInUpdateImage(AssetId.New(), "x")], DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Contains("not in the media library", error);
    }

    [Fact]
    public async Task An_edit_moves_the_time_and_a_person_can_resolve_an_unknown_result()
    {
        await using var host = NewHost();
        var site = await ASite(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdate(host, site, at);
        await host.Ok(new EditLinkedInUpdate(id, "jimmy", "Changed.", [], at.AddDays(1)));
        await host.CatchUp();
        Assert.Equal(("Changed.", at.AddDays(1)), (host.Get<LinkedInUpdateList>().Get(id)!.Text, host.Get<LinkedInUpdateList>().Get(id)!.PublishAt));

        var linkedIn = new FakeLinkedIn();
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "timed out"));
        await Poster(host, linkedIn, new FixedClock(at.AddDays(1).AddMinutes(1))).PostDueAsync(CancellationToken.None);
        await host.Ok(new ResolveLinkedInUpdate(id, "urn:li:share:9"));
        await host.CatchUp();

        Assert.Equal((LinkedInUpdateState.Posted, "urn:li:share:9"),
            (host.Get<LinkedInUpdateList>().Get(id)!.State, host.Get<LinkedInUpdateList>().Get(id)!.Urn));
    }

    // ---- Page senders and first comments ------------------------------------------------------------------------

    private const string Page = "urn:li:organization:141183954";
    private const string Link = "Read it: https://canine.dev/assure/";

    private static async Task<SiteId> ASiteWithAPage(AuthoringTestHost host)
    {
        var site = SiteId.New();
        await host.Ok(new Imprint.Authoring.Features.Sites.CreateBlog.CreateBlog(site, "Canine", "en"));
        await host.Ok(new SetSiteLinkedInSenders(site,
            [new LinkedInSender("jimmy", "Jimmy Borch", Person), new LinkedInSender("canine", "Canine Development", Page)]));
        return site;
    }

    private static async Task<LinkedInUpdateId> AnUpdateAs(
        AuthoringTestHost host, SiteId site, string sender, DateTimeOffset publishAt, string? comment, IReadOnlyList<LinkedInUpdateImage>? images = null)
    {
        var id = LinkedInUpdateId.New();
        await host.Ok(new ScheduleLinkedInUpdate(id, site, sender, "Your AI says it's fixed.", images ?? [], publishAt, comment));
        await host.CatchUp();
        return id;
    }

    private static FakeLinkedIn WithPage()
    {
        var linkedIn = new FakeLinkedIn();
        linkedIn.Connected.Add(Page);
        return linkedIn;
    }

    [Fact]
    public async Task A_page_senders_update_goes_out_as_the_page_with_its_images()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var image = await AnImage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, comment: null, [new LinkedInUpdateImage(image, "The Assure board")]);
        var linkedIn = WithPage();

        await Poster(host, linkedIn, new FixedClock(at.AddMinutes(1))).PostDueAsync(CancellationToken.None);

        var sent = Assert.Single(linkedIn.Sent);
        Assert.Equal((Page, "Your AI says it's fixed.", "The Assure board"), (sent.AuthorUrn, sent.Commentary, Assert.Single(sent.Images).Alt));
        Assert.Empty(linkedIn.Comments);
        await host.CatchUp();
        Assert.Equal(LinkedInUpdateState.Posted, host.Get<LinkedInUpdateList>().Get(id)!.State);
    }

    [Fact]
    public async Task The_first_comment_goes_out_right_after_its_post_as_the_same_sender_on_that_post()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        var linkedIn = WithPage();
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        var comment = Assert.Single(linkedIn.Comments);
        Assert.Equal((Page, "urn:li:share:1", Link), (comment.AuthorUrn, comment.PostUrn, comment.Text));
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, LinkedInUpdateCommentState.Posted, "urn:li:comment:(urn:li:activity:1,1)"),
            (row.State, row.CommentState!.Value, row.CommentUrn));
    }

    [Fact]
    public async Task A_refused_comment_leaves_the_post_posted_and_the_post_is_never_sent_again()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        var linkedIn = WithPage();
        linkedIn.CommentAnswers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "403: not enough permissions"));
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = at.AddMinutes(30);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        Assert.Single(linkedIn.Comments);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, "urn:li:share:1", LinkedInUpdateCommentState.Failed), (row.State, row.Urn, row.CommentState!.Value));
        Assert.Contains("permissions", row.CommentReason);
        Assert.Null(row.Reason);
    }

    [Fact]
    public async Task A_comment_whose_result_is_unknown_is_never_sent_again_and_the_post_stays_posted()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        var linkedIn = WithPage();
        linkedIn.CommentAnswers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "500: server error"));
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = at.AddMinutes(10);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        Assert.Single(linkedIn.Comments);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, LinkedInUpdateCommentState.Unknown), (row.State, row.CommentState!.Value));
    }

    [Fact]
    public async Task A_comment_LinkedIn_asked_to_wait_is_sent_again_after_the_wait_and_the_post_is_not()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        var linkedIn = WithPage();
        linkedIn.CommentAnswers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Refused, Reason: "429: throttled", Retryable: true, RetryAfter: TimeSpan.FromMinutes(1)));
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = at.AddMinutes(1).AddSeconds(30);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        Assert.Single(linkedIn.Comments);
        clock.Now = at.AddMinutes(3);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        Assert.Equal(2, linkedIn.Comments.Count);
        await host.CatchUp();
        Assert.Equal(LinkedInUpdateCommentState.Posted, host.Get<LinkedInUpdateList>().Get(id)!.CommentState);
    }

    // The post went out and was recorded, and the server stopped before its comment was tried.
    private static async Task PostedWithoutItsComment(AuthoringTestHost host, LinkedInUpdateId id, DateTimeOffset at)
    {
        await host.Ok(new StartLinkedInUpdate(id, "sha", at));
        await host.Ok(new RecordLinkedInUpdateOutcome(id, new LinkedInSendResult(LinkedInSendStatus.Posted, Urn: "urn:li:share:5"), at));
        await host.CatchUp();
    }

    [Fact]
    public async Task A_comment_left_waiting_by_a_restart_goes_out_while_its_post_is_recent()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        await PostedWithoutItsComment(host, id, at);
        var linkedIn = WithPage();

        await Poster(host, linkedIn, new FixedClock(at.AddMinutes(10))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
        Assert.Equal("urn:li:share:5", Assert.Single(linkedIn.Comments).PostUrn);
    }

    [Fact]
    public async Task A_comment_still_waiting_two_hours_after_its_post_is_missed_not_sent()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        await PostedWithoutItsComment(host, id, at);
        var linkedIn = WithPage();

        await Poster(host, linkedIn, new FixedClock(at.AddHours(2).AddMinutes(1))).PostDueAsync(CancellationToken.None);

        Assert.Empty(linkedIn.Sent);
        Assert.Empty(linkedIn.Comments);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, LinkedInUpdateCommentState.Missed), (row.State, row.CommentState!.Value));
        Assert.Contains("by hand", row.CommentReason);
    }

    [Fact]
    public async Task A_comment_whose_sender_lost_its_connection_waits_and_is_then_missed()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        await PostedWithoutItsComment(host, id, at);
        var offline = new FakeLinkedIn();

        await Poster(host, offline, new FixedClock(at.AddMinutes(10))).PostDueAsync(CancellationToken.None);
        await host.CatchUp();
        Assert.Equal(LinkedInUpdateCommentState.Waiting, host.Get<LinkedInUpdateList>().Get(id)!.CommentState);
        await Poster(host, offline, new FixedClock(at.AddHours(3))).PostDueAsync(CancellationToken.None);

        Assert.Empty(offline.Comments);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, LinkedInUpdateCommentState.Missed), (row.State, row.CommentState!.Value));
        Assert.Contains("not connected", row.CommentReason);
    }

    [Fact]
    public async Task A_members_update_without_a_comment_is_one_post_and_no_comment_call()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "jimmy", at, comment: null);
        var linkedIn = WithPage();
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        clock.Now = at.AddMinutes(30);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        var sent = Assert.Single(linkedIn.Sent);
        Assert.Equal(Person, sent.AuthorUrn);
        Assert.Empty(linkedIn.Comments);
        await host.CatchUp();
        var row = host.Get<LinkedInUpdateList>().Get(id)!;
        Assert.Equal((LinkedInUpdateState.Posted, (LinkedInUpdateCommentState?)null), (row.State, row.CommentState));
    }

    [Fact]
    public async Task After_a_person_resolves_an_unclear_post_as_posted_its_comment_goes_out()
    {
        await using var host = NewHost();
        var site = await ASiteWithAPage(host);
        var at = DateTimeOffset.UtcNow.AddMinutes(5);
        var id = await AnUpdateAs(host, site, "canine", at, Link);
        var linkedIn = WithPage();
        linkedIn.Answers.Enqueue(new LinkedInSendResult(LinkedInSendStatus.Unknown, Reason: "timed out"));
        var clock = new FixedClock(at.AddMinutes(1));

        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);
        Assert.Empty(linkedIn.Comments);
        await host.Ok(new ResolveLinkedInUpdate(id, "urn:li:share:42"));
        await host.CatchUp();
        clock.Now = DateTimeOffset.UtcNow.AddMinutes(1);
        await Poster(host, linkedIn, clock).PostDueAsync(CancellationToken.None);

        Assert.Single(linkedIn.Sent);
        Assert.Equal("urn:li:share:42", Assert.Single(linkedIn.Comments).PostUrn);
    }
}
