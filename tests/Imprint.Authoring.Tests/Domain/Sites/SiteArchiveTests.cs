using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteArchiveTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Old blog", new Locale("en"));

    [Fact]
    public void Archiving_raises_archived() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Archive())
            .ThenRaised(new SiteArchived());

    [Fact]
    public void Archiving_an_archived_site_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteArchived())
            .When(s => s.Archive())
            .ThenNothing();

    [Fact]
    public void Restoring_an_archived_site_raises_restored() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteArchived())
            .When(s => s.Restore())
            .ThenRaised(new SiteRestored());

    [Fact]
    public void Restoring_a_site_that_is_not_archived_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Restore())
            .ThenNothing();

    [Fact]
    public void An_archived_site_says_so_and_a_restored_one_does_not()
    {
        var site = new Site();
        site.LoadFrom([Created, new SiteArchived()]);
        Assert.True(site.IsArchived);

        site.LoadFrom([new SiteRestored()]);
        Assert.False(site.IsArchived);
    }

    [Fact]
    public void An_archived_site_refuses_to_publish()
    {
        var site = new Site();
        site.LoadFrom([Created, new SiteArchived()]);

        var refused = Assert.Throws<DomainException>(site.EnsureMayPublish);

        Assert.Contains("archived", refused.Message, StringComparison.OrdinalIgnoreCase);
    }
}
