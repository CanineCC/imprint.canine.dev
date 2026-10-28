using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SitePostOrderTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly PostId First = PostId.New();
    private static readonly PostId Second = PostId.New();
    private static SiteCreated Created => new(Id, "Resource Center", new Locale("en"));

    [Fact]
    public void Setting_an_order_raises_it() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetPostOrder([Second, First]))
            .ThenRaised(new SitePostOrderChanged([Second, First]));

    [Fact]
    public void The_same_order_again_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SitePostOrderChanged([Second, First]))
            .When(s => s.SetPostOrder([Second, First]))
            .ThenNothing();

    [Fact]
    public void A_post_twice_in_the_order_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetPostOrder([First, Second, First]))
            .ThenFails("more than once");

    [Fact]
    public void The_site_keeps_the_order_it_was_given()
    {
        var site = new Site();
        site.LoadFrom([Created, new SitePostOrderChanged([Second, First])]);

        Assert.Equal([Second, First], site.PostOrder);
    }
}
