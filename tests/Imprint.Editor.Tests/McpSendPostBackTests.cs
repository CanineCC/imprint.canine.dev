using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using RequestPostChangesCmd = Imprint.Authoring.Features.Posts.RequestPostChanges.RequestPostChanges;

namespace Imprint.Editor.Tests;

/// <summary>
/// The MCP half of a review that a service may do: sending a post back. It forwards to the same command the
/// editor's "Send back…" uses (whose rules are pinned in the Authoring tests); what is pinned here is that the
/// tool refuses without a reason and for a post it cannot see, and dispatches nothing when it refuses.
/// </summary>
public sealed class McpSendPostBackTests
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

    private static (PostId Id, PostList Posts) APost()
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

        return (id, posts);
    }

    private static JsonElement Json(object result) => JsonSerializer.SerializeToElement(result);

    [Fact]
    public async Task Sending_back_dispatches_the_reviewers_command_with_the_reason()
    {
        var (id, posts) = APost();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SendPostBack(
            id.Compact, "  Merged into another post; do not publish.  ", dispatcher, new ConfigurationBuilder().Build(), posts);

        Assert.True(Json(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<RequestPostChangesCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(id, sent.PostId);
        Assert.Contains("Merged into another post", sent.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_rejection_without_a_reason_is_refused_and_nothing_is_sent(string reason)
    {
        var (id, posts) = APost();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SendPostBack(id.Compact, reason, dispatcher, new ConfigurationBuilder().Build(), posts);

        Assert.False(Json(result).GetProperty("ok").GetBoolean());
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task An_unknown_post_is_refused_and_nothing_is_sent()
    {
        var (_, posts) = APost();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.SendPostBack(
            Guid.NewGuid().ToString("N"), "Reason.", dispatcher, new ConfigurationBuilder().Build(), posts);

        Assert.False(Json(result).GetProperty("ok").GetBoolean());
        Assert.Empty(dispatcher.Dispatched);
    }
}
