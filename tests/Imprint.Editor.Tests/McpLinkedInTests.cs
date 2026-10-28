using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.Editor.Api;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using SetPostLinkedInCmd = Imprint.Authoring.Features.Posts.SetPostLinkedIn.SetPostLinkedIn;
using SetSiteLinkedInSendersCmd = Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders.SetSiteLinkedInSenders;

namespace Imprint.Editor.Tests;

/// <summary>The MCP tools for an article's LinkedIn text and a site's senders forward to their commands.</summary>
public sealed class McpLinkedInTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    private sealed class FakeDispatcher : ICommandDispatcher
    {
        public List<ICommand> Dispatched { get; } = [];

        public Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
        {
            Dispatched.Add(command);
            return Task.FromResult(Result.Ok());
        }
    }

    [Fact]
    public async Task Set_post_linkedin_dispatches_the_text_and_the_sender()
    {
        var id = PostId.New();
        Slug.TryCreate("hello", out var slug, out _);
        var post = Post.Create(id, SiteId.New(), slug, new Locale("en"), "Hello");
        var posts = new PostList();
        long position = 0, version = 0;
        foreach (var @event in post.UncommittedEvents)
        {
            posts.Apply(new StoredEvent(++position, post.StreamId, ++version, @event.GetType().Name, @event, Meta));
        }

        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SetPostLinkedIn(
            id.Compact, "Read it.", "Canine", dispatcher, new ConfigurationBuilder().Build(), posts);

        Assert.True(JsonSerializer.SerializeToElement(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<SetPostLinkedInCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(id, sent.PostId);
        Assert.Equal("Read it.", sent.Text);
        Assert.Equal("Canine", sent.Sender);
    }

    [Fact]
    public async Task Set_site_linkedin_senders_dispatches_the_list()
    {
        var site = SiteId.New();
        var dispatcher = new FakeDispatcher();

        await ImprintAuthoringMcpTools.SetSiteLinkedInSenders(
            site.Compact, [new AuthoringApi.SiteLinkedInSenderRequest("assert", "Canine Assert", "urn:li:organization:7")],
            dispatcher, new ConfigurationBuilder().Build());

        var sent = Assert.IsType<SetSiteLinkedInSendersCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(site, sent.SiteId);
        var sender = Assert.Single(sent.Senders);
        Assert.Equal(("assert", "Canine Assert", "urn:li:organization:7"), (sender.Key, sender.Name, sender.OrganizationUrn));
    }
}
