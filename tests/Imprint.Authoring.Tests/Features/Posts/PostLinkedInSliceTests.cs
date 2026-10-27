using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.SetPostLinkedIn;
using Imprint.Authoring.Features.Sites.SetSiteLinkedInSenders;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Posts;

/// <summary>An article's LinkedIn post may only name a sender its site has set up.</summary>
public sealed class PostLinkedInSliceTests
{
    private const string En = "en";

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));

    [Fact]
    public async Task A_post_names_a_sender_of_its_site_and_the_list_shows_it()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine Development", "urn:li:organization:1")]));
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello", "hello", En));

        await host.Ok(new SetPostLinkedIn(id, "Read it.", "canine"));
        await host.CatchUp();

        var listed = host.Get<PostList>().Get(id)!;
        Assert.Equal("Read it.", listed.LinkedInText);
        Assert.Equal("canine", listed.LinkedInSender);
    }

    [Fact]
    public async Task A_sender_the_site_has_not_set_up_is_refused_by_name()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        await host.Ok(new SetSiteLinkedInSenders(site, [new LinkedInSender("canine", "Canine Development", "urn:li:organization:1")]));
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello", "hello", En));

        var error = await host.Fails(new SetPostLinkedIn(id, "Read it.", "assay"));

        Assert.Contains("use one of: canine", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Clearing_needs_no_sender()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var id = PostId.New();
        await host.Ok(new CreatePost(id, site, "Hello", "hello", En));

        await host.Ok(new SetPostLinkedIn(id, null, null));
    }
}
