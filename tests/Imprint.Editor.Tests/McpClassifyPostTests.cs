using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts;
using Imprint.Authoring.Projections;
using Imprint.Editor.Mcp;
using Imprint.EventSourcing;
using Microsoft.Extensions.Configuration;
using ClassifyPostCmd = Imprint.Authoring.Features.Posts.ClassifyPost.ClassifyPost;

namespace Imprint.Editor.Tests;

/// <summary>The MCP tool that sets an article's kind and products forwards to the classify command.</summary>
public sealed class McpClassifyPostTests
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

    [Fact]
    public async Task Classify_dispatches_the_kind_and_the_products()
    {
        var (id, posts) = APost();
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.ClassifyPost(
            id.Compact, "guide", ["assert", "assay"], dispatcher, new ConfigurationBuilder().Build(), posts);

        Assert.True(JsonSerializer.SerializeToElement(result).GetProperty("ok").GetBoolean());
        var sent = Assert.IsType<ClassifyPostCmd>(Assert.Single(dispatcher.Dispatched));
        Assert.Equal(id, sent.PostId);
        Assert.Equal("guide", sent.Kind);
        Assert.Equal(["assert", "assay"], sent.Products);
    }

    [Fact]
    public async Task Classify_refuses_a_post_it_cannot_see_and_dispatches_nothing()
    {
        var dispatcher = new FakeDispatcher();

        var result = await ImprintAuthoringMcpTools.ClassifyPost(
            PostId.New().Compact, "guide", null, dispatcher, new ConfigurationBuilder().Build(), new PostList());

        Assert.False(JsonSerializer.SerializeToElement(result).GetProperty("ok").GetBoolean());
        Assert.Empty(dispatcher.Dispatched);
    }
}
