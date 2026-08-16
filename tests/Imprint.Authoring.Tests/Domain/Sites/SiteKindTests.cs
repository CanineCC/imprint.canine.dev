using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// A blog and a marketing site are the same kind of thing at the level that matters —
/// something addressed by its own origin that publishes pages — and differ in the shape
/// of what is inside: a page tree, or a dated stream with an index and a feed. So the
/// distinction is a KIND on the site, not a section hanging off one.
///
/// Kind is an additive property on the stored <c>site.created</c> payload, the same
/// contract <see cref="DeployEnvironmentCompatibilityTests"/> pins for BaseUrl: every
/// event written before it existed carries no Kind and must fold to a site, so the four
/// sites that already exist keep working with no migration.
/// </summary>
public sealed class SiteKindTests
{
    private static readonly Locale En = new("en");

    private static readonly EventRegistry Registry =
        new([typeof(AuthoringJson).Assembly], AuthoringJson.Configure);

    private const string StableId = "site.created.v1";

    [Fact]
    public void Create_makes_a_site()
    {
        var site = Site.Create(SiteId.New(), "Marketing site", En);

        Assert.Equal(SiteKind.Site, site.Kind);
    }

    [Fact]
    public void Create_with_the_blog_kind_makes_a_blog()
    {
        var id = SiteId.New();

        var site = Site.Create(id, "Canine blog", En, SiteKind.Blog);

        Assert.Equal(SiteKind.Blog, site.Kind);
        Assert.Equal(
            new object[] { new SiteCreated(id, "Canine blog", En, SiteKind.Blog) },
            site.UncommittedEvents);
    }

    [Fact]
    public void A_blog_is_a_site_in_every_other_respect()
    {
        // The whole point of making this a kind rather than a separate aggregate: a blog
        // carries the same locales, theme and settings surface a site does, so the
        // settings page needs no blog-specific branch to keep working.
        var id = SiteId.New();

        var blog = Site.Create(id, "Canine blog", En, SiteKind.Blog);

        Assert.Equal(id, blog.Id);
        Assert.Equal(new[] { En }, blog.Locales);
        Assert.Equal(En, blog.DefaultLocale);
        Assert.Equal(Theme.Default, blog.Theme);
        Assert.Equal(id.Stream, blog.StreamId);
    }

    [Fact]
    public void Stored_event_without_Kind_folds_to_a_site()
    {
        // Verbatim the payload shape written before Kind existed.
        const string storedJson =
            """{"SiteId":"63030ae7-aeb9-4355-a019-362c150fd420","Name":"Watchdog","DefaultLocale":"en"}""";

        var @event = Assert.IsType<SiteCreated>(Registry.Deserialize(StableId, storedJson));

        Assert.Equal(SiteKind.Site, @event.Kind);
    }

    [Fact]
    public void Stored_event_with_Kind_folds_to_a_blog()
    {
        const string storedJson =
            """{"SiteId":"63030ae7-aeb9-4355-a019-362c150fd420","Name":"Canine blog","DefaultLocale":"en","Kind":"Blog"}""";

        var @event = Assert.IsType<SiteCreated>(Registry.Deserialize(StableId, storedJson));

        Assert.Equal(SiteKind.Blog, @event.Kind);
    }

    [Fact]
    public void Serialized_event_round_trips_both_kinds_under_the_stable_id()
    {
        foreach (var kind in new[] { SiteKind.Site, SiteKind.Blog })
        {
            var original = new SiteCreated(SiteId.New(), "Canine", En, kind);

            Assert.Equal(StableId, Registry.StableIdOf(original));
            Assert.Equal(original, Registry.Deserialize(StableId, Registry.Serialize(original)));
        }
    }

    [Fact]
    public void A_site_created_before_Kind_existed_folds_to_a_site()
    {
        // The aggregate half of the same contract: replaying a legacy stream must not
        // turn an existing marketing site into a blog, or the dashboard would move it.
        var site = new Site();

        site.LoadFrom([new SiteCreated(SiteId.New(), "Watchdog", En)]);

        Assert.Equal(SiteKind.Site, site.Kind);
    }
}
