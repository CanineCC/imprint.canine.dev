using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.ClassifyPost;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Posts;

/// <summary>A post's kind and products, set at creation or later, as the post list shows them.</summary>
public sealed class PostClassificationSliceTests
{
    private const string En = "en";

    [Fact]
    public async Task A_post_created_with_a_kind_lists_its_kind_and_products()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();

        await host.Ok(new CreatePost(id, site, "Read-only by design", "read-only-by-design", En, "guide", ["assert"]));
        await host.CatchUp();

        var listed = host.Get<PostList>().Get(id)!;
        Assert.Equal("guide", listed.Kind);
        Assert.Equal(["assert"], listed.Products);
    }

    [Fact]
    public async Task A_post_created_without_a_kind_lists_none()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();

        await host.Ok(new CreatePost(id, site, "Hello world", "hello-world", En));
        await host.CatchUp();

        var listed = host.Get<PostList>().Get(id)!;
        Assert.Null(listed.Kind);
        Assert.Empty(listed.Products);
    }

    [Fact]
    public async Task Create_refuses_an_unknown_kind_and_leaves_no_post_behind()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();

        var error = await host.Fails(new CreatePost(id, site, "Hello world", "hello-world", En, "tutorial", []));
        await host.CatchUp();

        Assert.Contains("essay, guide, whitepaper, documentation", error, StringComparison.Ordinal);
        Assert.Null(host.Get<PostList>().Get(id));
    }

    [Fact]
    public async Task Classify_changes_what_the_list_shows()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello world", "hello-world", En, "essay", []));

        await host.Ok(new ClassifyPost(id, "whitepaper", ["assay", "canine"]));
        await host.CatchUp();

        var listed = host.Get<PostList>().Get(id)!;
        Assert.Equal("whitepaper", listed.Kind);
        Assert.Equal(["canine", "assay"], listed.Products);
    }

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));
}
