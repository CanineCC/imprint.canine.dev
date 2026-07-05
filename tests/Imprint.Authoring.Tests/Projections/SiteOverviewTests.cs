using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.AddCollaborator;
using Imprint.Authoring.Features.Sites.ClaimSite;
using Imprint.Authoring.Features.Sites.RemoveCollaborator;
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
    public async Task A_collaborator_gains_access_but_not_ownership()
    {
        await using var host = new AuthoringTestHost();
        var alice = await CreateSiteAs(host, "alice@example.com", "Alice Co");

        var overview = host.Get<SiteOverview>();
        Assert.False(overview.CanAccess(alice, "bob@example.com"));

        // Grant through the real slice so the projection folds a stored event.
        await host.Ok(new AddCollaborator(alice, "Bob@Example.com"));

        Assert.True(overview.CanAccess(alice, "bob@example.com"));
        Assert.False(overview.IsOwner(alice, "bob@example.com"));
        Assert.Equal([alice], overview.AccessibleTo("bob@example.com").Select(s => s.Id).ToArray());
        Assert.Empty(overview.OwnedBy("bob@example.com"));

        await host.Ok(new RemoveCollaborator(alice, "bob@example.com"));
        Assert.False(overview.CanAccess(alice, "bob@example.com"));
        Assert.Empty(overview.AccessibleTo("bob@example.com"));
    }

    [Fact]
    public async Task The_owner_can_access_without_being_a_collaborator()
    {
        await using var host = new AuthoringTestHost();
        var alice = await CreateSiteAs(host, "Alice@Example.com", "Alice Co");

        var overview = host.Get<SiteOverview>();
        Assert.True(overview.CanAccess(alice, "alice@example.com"));
        Assert.True(overview.IsOwner(alice, "alice@example.com"));
        Assert.Equal([alice], overview.AccessibleTo("alice@example.com").Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task A_legacy_site_with_no_recorded_owner_is_accessible_to_everyone()
    {
        await using var host = new AuthoringTestHost();
        var legacy = await CreateSiteAs(host, actor: "", name: "Legacy Site");

        var overview = host.Get<SiteOverview>();
        Assert.True(overview.IsUnclaimed(legacy));
        Assert.True(overview.CanAccess(legacy, "anyone@example.com"));
        Assert.True(overview.IsOwner(legacy, "anyone@example.com"));
    }

    [Fact]
    public async Task A_site_owned_by_an_os_username_is_unclaimed_and_accessible_to_everyone()
    {
        // Pre-auth installs stamp the OS user (e.g. "jimmy") as the creator — an actor
        // that can never match a login email, so it must not lock anyone out.
        await using var host = new AuthoringTestHost();
        var seeded = await CreateSiteAs(host, actor: "jimmy", name: "Seeded Site");

        var overview = host.Get<SiteOverview>();
        Assert.True(overview.IsUnclaimed(seeded));
        Assert.True(overview.CanAccess(seeded, "anyone@example.com"));
        Assert.True(overview.IsOwner(seeded, "anyone@example.com"));
        Assert.Contains(seeded, overview.AccessibleTo("anyone@example.com").Select(s => s.Id));
    }

    [Fact]
    public async Task Claiming_an_unclaimed_site_makes_the_claimant_its_owner()
    {
        await using var host = new AuthoringTestHost();
        var seeded = await CreateSiteAs(host, actor: "jimmy", name: "Seeded Site");

        host.Get<EventMetadataProvider>().ActorSource = () => "alice@example.com";
        await host.Ok(new ClaimSite(seeded));

        var overview = host.Get<SiteOverview>();
        Assert.False(overview.IsUnclaimed(seeded));
        Assert.Equal("alice@example.com", overview.OwnerOf(seeded));
        Assert.True(overview.IsOwner(seeded, "alice@example.com"));
        Assert.False(overview.CanAccess(seeded, "bob@example.com"));
        Assert.Empty(overview.AccessibleTo("bob@example.com"));
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
