using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;
using Imprint.Editor.Contact;
using Imprint.EventSourcing;

namespace Imprint.Editor.Tests;

/// <summary>
/// The live widget-prop lookup behind /api/contact: the submitted hostname finds its
/// site via the environment origins (www-insensitively) or the site name, and the
/// recipients come from the DRAFT contact-form node — so an editor's change is in
/// effect on the very next submission, with no republish.
/// </summary>
public sealed class SiteContactRecipientsTests
{
    private static readonly EventMetadata Meta = new("alice", DateTimeOffset.UnixEpoch, Guid.Empty, Guid.Empty);

    // Folds an aggregate's uncommitted events into the given read models, exactly as the
    // projection engine would — no test shortcut into the read model's internals.
    private static void Fold(AggregateRoot aggregate, ref long position, params ReadModel[] readModels)
    {
        long version = 0;
        foreach (var @event in aggregate.UncommittedEvents)
        {
            var stored = new StoredEvent(
                ++position, aggregate.StreamId, ++version, @event.GetType().Name, @event, Meta);
            foreach (var readModel in readModels)
            {
                readModel.Apply(stored);
            }
        }
    }

    private static Slug MakeSlug(string value)
    {
        Assert.True(Slug.TryCreate(value, out var slug, out var error), error);
        return slug;
    }

    private static Node ContactSection(string? recipients)
    {
        var props = new List<KeyValuePair<string, string>> { new("topics", "[]") };
        if (recipients is not null)
        {
            props.Add(new("recipients", recipients));
        }

        return new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new StackNode
            {
                Id = NodeId.New(),
                Children = NodeList.Of(new WidgetNode
                {
                    Id = NodeId.New(),
                    Tag = "contact-form",
                    Props = PropBag.Of(props),
                }),
            }),
        };
    }

    private static (SiteOverview Sites, PageDrafts Drafts, SiteId SiteId) Seed(
        string? recipients, string? baseUrl = "https://canine.dev", string name = "Canine")
    {
        var sites = new SiteOverview();
        var drafts = new PageDrafts();
        long position = 0;

        var siteId = SiteId.New();
        var site = Site.Create(siteId, name, new Locale("en"));
        if (baseUrl is not null)
        {
            site.SetEnvironments([new DeployEnvironment("Production", "/var/www/site", baseUrl)]);
        }

        Fold(site, ref position, sites);

        var page = Page.Create(PageId.New(), siteId, MakeSlug("contact"), new Locale("en"), "Contact");
        page.AddNode(NodeId.Root, 0, ContactSection(recipients));
        Fold(page, ref position, drafts);

        return (sites, drafts, siteId);
    }

    [Fact]
    public void Recipients_prop_is_found_by_the_environment_origin_host()
    {
        var (sites, drafts, _) = Seed("sales@canine.dev");

        Assert.Equal("sales@canine.dev", new SiteContactRecipients(sites, drafts).Find("canine.dev"));
    }

    [Fact]
    public void A_www_alias_host_finds_the_apex_origin_site()
    {
        var (sites, drafts, _) = Seed("sales@canine.dev");

        Assert.Equal("sales@canine.dev", new SiteContactRecipients(sites, drafts).Find("www.canine.dev"));
    }

    [Fact]
    public void A_site_without_origins_is_found_by_its_name()
    {
        var (sites, drafts, _) = Seed("sales@canine.dev", baseUrl: null, name: "canine.dev");

        Assert.Equal("sales@canine.dev", new SiteContactRecipients(sites, drafts).Find("canine.dev"));
    }

    [Fact]
    public void An_unknown_host_finds_nothing()
    {
        var (sites, drafts, _) = Seed("sales@canine.dev");

        Assert.Null(new SiteContactRecipients(sites, drafts).Find("unknown.example"));
    }

    [Fact]
    public void A_matched_site_without_the_prop_finds_nothing()
    {
        // Null here is what lets the resolver fall through to Contact:Recipients config.
        var (sites, drafts, _) = Seed(recipients: null);

        Assert.Null(new SiteContactRecipients(sites, drafts).Find("canine.dev"));
    }

    [Fact]
    public void A_blank_submitted_site_finds_nothing()
    {
        var (sites, drafts, _) = Seed("sales@canine.dev");
        var lookup = new SiteContactRecipients(sites, drafts);

        Assert.Null(lookup.Find(null));
        Assert.Null(lookup.Find("  "));
    }

    [Fact]
    public void Editing_the_prop_changes_the_next_lookup_with_no_republish()
    {
        // The founder's requirement in miniature: change the address in the editor
        // (ChangeNodeProps on the draft), and the very next submission uses it.
        var sites = new SiteOverview();
        var drafts = new PageDrafts();
        long position = 0;

        var siteId = SiteId.New();
        var site = Site.Create(siteId, "Canine", new Locale("en"));
        site.SetEnvironments([new DeployEnvironment("Production", "/var/www/site", "https://canine.dev")]);
        Fold(site, ref position, sites);

        var page = Page.Create(PageId.New(), siteId, MakeSlug("contact"), new Locale("en"), "Contact");
        page.AddNode(NodeId.Root, 0, ContactSection("sales@canine.dev"));
        Fold(page, ref position, drafts);
        var lookup = new SiteContactRecipients(sites, drafts);
        Assert.Equal("sales@canine.dev", lookup.Find("canine.dev"));

        page.MarkCommitted(page.UncommittedEvents.Count); // the fold above "saved" them
        var widget = page.Tree.All().OfType<WidgetNode>().Single();
        page.ChangeNodeProps(widget with { Props = widget.Props.With("recipients", "ceo@canine.dev") });
        Fold(page, ref position, drafts);

        Assert.Equal("ceo@canine.dev", lookup.Find("canine.dev"));
    }
}
