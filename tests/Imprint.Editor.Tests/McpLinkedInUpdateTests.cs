using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.LinkedIn;
using Imprint.Authoring.Domain.LinkedIn.Events;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.LinkedIn;
using Imprint.Authoring.Features.LinkedInUpdates;
using Imprint.Authoring.Projections;
using Imprint.Editor.LinkedIn;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;

namespace Imprint.Editor.Tests;

/// <summary>
/// The MCP tools for LinkedIn updates carry the optional first comment to their commands and read it back; an update
/// without one reads exactly as before. get_site reads back the senders, which set_site_linkedin_senders replaces whole,
/// with whether an update as each would be sent now.
/// </summary>
public sealed class McpLinkedInUpdateTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);
    private static readonly IConfiguration Config = new ConfigurationBuilder().Build();
    private static readonly DateTimeOffset At = new(2026, 11, 11, 8, 0, 0, TimeSpan.FromHours(1));

    private sealed class FakeDispatcher : ICommandDispatcher
    {
        public List<ICommand> Dispatched { get; } = [];

        public Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
        {
            Dispatched.Add(command);
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class Connected(params string[] urns) : ILinkedInUpdatePoster
    {
        public bool IsConfiguredFor(string authorUrn) => urns.Contains(authorUrn);

        public Task<LinkedInSendResult> PostUpdateAsync(LinkedInUpdateRequest request, CancellationToken ct) => throw new NotSupportedException();

        public Task<LinkedInSendResult> CommentAsync(LinkedInCommentRequest request, CancellationToken ct) => throw new NotSupportedException();
    }

    // As the MCP server writes a tool's result (nulls left out).
    private static JsonElement Json(object result) => JsonSerializer.SerializeToElement(result, McpJsonUtilities.DefaultOptions);

    private static (SiteOverview Sites, SiteId Id) ASite(params LinkedInSender[] senders)
    {
        var id = SiteId.New();
        var site = Site.Create(id, "Canine", new Locale("en"));
        if (senders.Length > 0)
        {
            site.SetLinkedInSenders(senders);
        }

        var overview = new SiteOverview();
        long position = 0, version = 0;
        foreach (var @event in site.UncommittedEvents)
        {
            overview.Apply(new StoredEvent(++position, site.StreamId, ++version, @event.GetType().Name, @event, Meta));
        }

        return (overview, id);
    }

    private static LinkedInUpdateList Updates(params (LinkedInUpdateId Id, object[] Events)[] streams)
    {
        var list = new LinkedInUpdateList();
        long position = 0;
        foreach (var (id, events) in streams)
        {
            long version = 0;
            foreach (var @event in events)
            {
                list.Apply(new StoredEvent(++position, id.Stream, ++version, @event.GetType().Name, @event, Meta));
            }
        }

        return list;
    }

    [Fact]
    public async Task Schedule_carries_the_first_comment_and_leaves_it_out_when_none_is_given()
    {
        var site = SiteId.New();
        var dispatcher = new FakeDispatcher();

        var with = Json(await ImprintAuthoringMcpTools.ScheduleLinkedInUpdate(
            site.Compact, "canine", "Your AI says it's fixed.", [], "2026-11-11T08:00:00+01:00", dispatcher, Config, "Read it: https://canine.dev/"));
        await ImprintAuthoringMcpTools.ScheduleLinkedInUpdate(
            site.Compact, "jimmy", "Hej.", [], "2026-11-12T07:45:00+01:00", dispatcher, Config);

        Assert.True(with.GetProperty("firstComment").GetBoolean());
        var first = Assert.IsType<ScheduleLinkedInUpdate>(dispatcher.Dispatched[0]);
        Assert.Equal(("canine", "Read it: https://canine.dev/"), (first.Sender, first.FirstComment));
        Assert.Null(Assert.IsType<ScheduleLinkedInUpdate>(dispatcher.Dispatched[1]).FirstComment);
    }

    [Fact]
    public async Task Edit_restates_the_first_comment()
    {
        var dispatcher = new FakeDispatcher();
        var id = LinkedInUpdateId.New();

        await ImprintAuthoringMcpTools.EditLinkedInUpdate(
            id.Compact, "canine", "New text.", [], "2026-11-11T09:00:00+01:00", dispatcher, Config, "New link: https://canine.dev/");

        var sent = Assert.IsType<EditLinkedInUpdate>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal((id, "New link: https://canine.dev/"), (sent.Id, sent.FirstComment));
    }

    [Fact]
    public void List_shows_a_first_comment_and_its_state_and_an_update_without_one_reads_as_before()
    {
        var (sites, site) = ASite();
        var page = LinkedInUpdateId.New();
        var member = LinkedInUpdateId.New();
        var updates = Updates(
            (page, [
                new LinkedInUpdateScheduled(page, site, "canine", "Your AI says it's fixed.", [], At, At, "Read it: https://canine.dev/"),
                new LinkedInUpdateAttempting("sha", At), new LinkedInUpdatePosted("urn:li:share:1", At),
                new LinkedInUpdateCommentAttempting("sha-c", At),
                new LinkedInUpdateCommentFailed("403: not enough permissions", false, At),
            ]),
            (member, [
                new LinkedInUpdateScheduled(member, site, "jimmy", "Hej.\nAnden linje.", [], At.AddDays(1), At),
                new LinkedInUpdateAttempting("sha", At.AddDays(1)), new LinkedInUpdatePosted("urn:li:share:2", At.AddDays(1)),
            ]));

        var listed = Json(ImprintAuthoringMcpTools.ListLinkedInUpdates(site.Compact, sites, updates)).GetProperty("updates");

        var comment = listed[0].GetProperty("firstComment");
        Assert.Equal(("Read it: https://canine.dev/", "Failed", "403: not enough permissions"),
            (comment.GetProperty("text").GetString(), comment.GetProperty("state").GetString(), comment.GetProperty("reason").GetString()));
        Assert.Equal("Posted", listed[0].GetProperty("state").GetString());

        // The owner's own updates have no comment: the same fields as before this change, and nothing more.
        Assert.Equal(["updateId", "sender", "publishAt", "state", "urn", "images", "firstLine"],
            listed[1].EnumerateObject().Select(p => p.Name));
        Assert.Equal(("jimmy", "Posted", "urn:li:share:2", 0, "Hej."),
            (listed[1].GetProperty("sender").GetString(), listed[1].GetProperty("state").GetString(), listed[1].GetProperty("urn").GetString(),
             listed[1].GetProperty("images").GetInt32(), listed[1].GetProperty("firstLine").GetString()));
    }

    [Fact]
    public void Get_site_reads_back_the_senders_with_their_kind_and_whether_an_update_as_each_would_be_sent()
    {
        var (sites, site) = ASite(
            new LinkedInSender("jimmy", "Jimmy Borch", "urn:li:person:Ab3"),
            new LinkedInSender("canine", "Canine Development", "urn:li:organization:141183954"));

        var json = Json(ImprintAuthoringMcpTools.GetSite(site.Compact, sites, new PageList(), new Connected("urn:li:person:Ab3"), McpTestPage.NotSetUp));
        var senders = json.GetProperty("linkedInSenders");

        Assert.Equal(2, senders.GetArrayLength());
        Assert.Equal(("jimmy", "person", true), (senders[0].GetProperty("key").GetString(), senders[0].GetProperty("kind").GetString(), senders[0].GetProperty("connected").GetBoolean()));
        Assert.Equal(("canine", "page", false, "urn:li:organization:141183954"),
            (senders[1].GetProperty("key").GetString(), senders[1].GetProperty("kind").GetString(), senders[1].GetProperty("connected").GetBoolean(),
             senders[1].GetProperty("organizationUrn").GetString()));

        // An install without the page app: said plainly, and nothing about a connection that does not exist.
        var page = json.GetProperty("linkedInPage");
        Assert.Equal((false, false), (page.GetProperty("appSetUp").GetBoolean(), page.GetProperty("usable").GetBoolean()));
        Assert.False(page.TryGetProperty("connectedAt", out _));
    }

    [Fact]
    public void Get_site_says_when_the_page_was_connected_and_until_when_it_renews_without_reading_out_a_token()
    {
        var (sites, site) = ASite(new LinkedInSender("canine", "Canine Development", "urn:li:organization:141183954"));
        var now = new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
        var data = Path.Combine(Path.GetTempPath(), $"imprint-mcp-page-{Guid.NewGuid():N}");
        try
        {
            var connection = McpTestPage.Connected(data,
                new LinkedInTokens("page-access-secret", now.AddDays(50), "page-refresh-secret", now.AddDays(300), now.AddDays(-10)), new FixedClock(now));

            var json = Json(ImprintAuthoringMcpTools.GetSite(site.Compact, sites, new PageList(), new Connected(), connection));

            var page = json.GetProperty("linkedInPage");
            Assert.True(page.GetProperty("appSetUp").GetBoolean());
            Assert.True(page.GetProperty("usable").GetBoolean());
            Assert.Equal(now.AddDays(-10), page.GetProperty("connectedAt").GetDateTimeOffset());
            Assert.Equal(now.AddDays(300), page.GetProperty("renewsUntil").GetDateTimeOffset());
            Assert.DoesNotContain("secret", json.GetRawText(), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(data))
            {
                Directory.Delete(data, recursive: true);
            }
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
