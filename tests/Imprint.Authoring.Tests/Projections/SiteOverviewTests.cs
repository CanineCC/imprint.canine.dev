using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Projections;

/// <summary>
/// The multi-site read model: it lists every site in creation order and records each
/// site's owner — the actor who raised its <c>site.created</c> event, read from the
/// envelope metadata (no change to the event payload). This is the seam the SaaS
/// "a user's own sites" list is built on.
/// </summary>
public sealed class SiteOverviewTests
{
    private static async Task<SiteId> CreateSiteAs(AuthoringTestHost host, string actor, string name)
    {
        host.Get<EventMetadataProvider>().ActorSource = () => actor;
        var id = SiteId.New();
        await host.SaveAggregate(Site.Create(id, name, new Locale("en")));
        return id;
    }

    [Fact]
    public async Task Lists_all_sites_in_creation_order_with_their_owners()
    {
        await using var host = new AuthoringTestHost();
        var alice = await CreateSiteAs(host, "alice@example.com", "Alice Co");
        var bob = await CreateSiteAs(host, "bob@example.com", "Bob Ltd");

        var overview = host.Get<SiteOverview>();

        Assert.Equal(new[] { alice, bob }, overview.All.Select(s => s.Id).ToArray());
        Assert.Equal("alice@example.com", overview.OwnerOf(alice));
        Assert.Equal("bob@example.com", overview.OwnerOf(bob));
        Assert.Equal(new[] { alice }, overview.OwnedBy("alice@example.com").Select(s => s.Id).ToArray());
        Assert.Equal(new[] { bob }, overview.OwnedBy("bob@example.com").Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task Owner_matching_is_case_insensitive()
    {
        await using var host = new AuthoringTestHost();
        var alice = await CreateSiteAs(host, "Alice@Example.com", "Alice Co");

        Assert.Equal(new[] { alice }, host.Get<SiteOverview>().OwnedBy("alice@example.com").Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task A_legacy_site_with_no_recorded_owner_is_visible_to_everyone()
    {
        await using var host = new AuthoringTestHost();
        var legacy = await CreateSiteAs(host, actor: "", name: "Legacy Site");

        var overview = host.Get<SiteOverview>();
        Assert.Equal(string.Empty, overview.OwnerOf(legacy));
        Assert.Contains(legacy, overview.OwnedBy("anyone@example.com").Select(s => s.Id));
    }

    [Fact]
    public async Task Current_is_the_first_created_site()
    {
        await using var host = new AuthoringTestHost();
        var first = await CreateSiteAs(host, "alice@example.com", "First");
        await CreateSiteAs(host, "alice@example.com", "Second");

        Assert.Equal(first, host.Get<SiteOverview>().Current?.Id);
    }
}
