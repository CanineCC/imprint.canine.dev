using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Sites.SetPostOrder;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Sites;

/// <summary>A site's posts in the order its editors put them: the order the list shows and the plan they publish by.</summary>
public sealed class SetPostOrderTests
{
    [Fact]
    public async Task The_order_is_kept_on_the_site_and_a_post_of_another_site_is_refused()
    {
        await using var host = new AuthoringTestHost(s => s.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));
        var site = await host.CreateTestSite();
        var other = await host.CreateTestSite();
        var (a, b, foreign) = (PostId.New(), PostId.New(), PostId.New());
        await host.Ok(new CreatePost(a, site, "A", "a", "en"));
        await host.Ok(new CreatePost(b, site, "B", "b", "en"));
        await host.Ok(new CreatePost(foreign, other, "C", "c", "en"));

        await host.Ok(new SetPostOrder(site, [b, a]));
        await host.CatchUp();
        Assert.Equal([b, a], host.Get<SiteOverview>().Get(site)!.PostOrder);

        var error = await host.Fails(new SetPostOrder(site, [a, foreign]));
        Assert.Contains("not a post of this site", error, StringComparison.OrdinalIgnoreCase);
    }
}
