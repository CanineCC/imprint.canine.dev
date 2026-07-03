using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageLifecycleTests
{
    private readonly PageId _id = PageId.New();
    private readonly PageCreated _created;

    public PageLifecycleTests() => _created = Created(_id, SiteId.New());

    // ------------------------------------------------------------------ publish

    [Fact]
    public void Publish_first_time_covers_the_publish_events_own_version()
    {
        // History: created (v1); the publish event itself will land at v2.
        AggregateSpec.For<Page>()
            .Given(_created)
            .When(p => p.Publish())
            .ThenRaised(new PagePublished(2));
    }

    [Fact]
    public void Publish_with_uncommitted_events_is_a_programmer_error()
    {
        var page = Page.Create(_id, SiteId.New(), SlugOf("about"), En, "About");
        Assert.Throws<InvalidOperationException>(page.Publish);
    }

    [Fact]
    public void Publish_mixed_into_another_command_is_a_programmer_error()
    {
        var page = new Page();
        page.LoadFrom([_created]);
        page.ChangeTitle(En, "New title");

        Assert.Throws<InvalidOperationException>(page.Publish);
    }

    [Fact]
    public void Publish_with_no_changes_since_last_publish_is_rejected()
    {
        AggregateSpec.For<Page>()
            .Given(_created, new PagePublished(2)) // v2 = the publish event itself
            .When(p => p.Publish())
            .ThenFails("no changes to publish");
    }

    [Fact]
    public void Publish_after_new_edits_covers_the_new_version()
    {
        AggregateSpec.For<Page>()
            .Given(_created, new PagePublished(2), new TitleChanged(En, "Fresh")) // v3
            .When(p => p.Publish())
            .ThenRaised(new PagePublished(4));
    }

    [Fact]
    public void Publish_version_bookkeeping_holds_across_multiple_publishes()
    {
        var outcome = AggregateSpec.For<Page>()
            .Given(
                _created,                        // v1
                new PagePublished(2),            // v2 — first publish
                new TitleChanged(En, "Second"),  // v3
                new PagePublished(4),            // v4 — second publish
                new SlugChanged("about-acme"),   // v5
                new TitleChanged(Da, "Om"))      // v6
            .When(p => p.Publish());

        outcome.ThenRaised(new PagePublished(7));
        Assert.Equal(7, outcome.Aggregate.PublishedVersion);
    }

    [Fact]
    public void Publish_folds_the_published_version()
    {
        var page = new Page();
        page.LoadFrom([_created, new PagePublished(2)]);

        Assert.Equal(2, page.PublishedVersion);
    }

    // ---------------------------------------------------------------- unpublish

    [Fact]
    public void Unpublish_a_published_page_raises_event()
    {
        var outcome = AggregateSpec.For<Page>()
            .Given(_created, new PagePublished(2))
            .When(p => p.Unpublish());

        outcome.ThenRaised(new PageUnpublished());
        Assert.Null(outcome.Aggregate.PublishedVersion);
    }

    [Fact]
    public void Unpublish_a_never_published_page_is_rejected()
    {
        AggregateSpec.For<Page>()
            .Given(_created)
            .When(p => p.Unpublish())
            .ThenFails("not published");
    }

    [Fact]
    public void Unpublish_twice_is_rejected()
    {
        AggregateSpec.For<Page>()
            .Given(_created, new PagePublished(2), new PageUnpublished())
            .When(p => p.Unpublish())
            .ThenFails("not published");
    }

    [Fact]
    public void Publish_after_unpublish_is_allowed()
    {
        AggregateSpec.For<Page>()
            .Given(_created, new PagePublished(2), new PageUnpublished()) // v3
            .When(p => p.Publish())
            .ThenRaised(new PagePublished(4));
    }

    // ------------------------------------------------------------------- delete

    [Fact]
    public void Delete_raises_event_and_folds()
    {
        var outcome = AggregateSpec.For<Page>()
            .Given(_created)
            .When(p => p.Delete());

        outcome.ThenRaised(new PageDeleted());
        Assert.True(outcome.Aggregate.IsDeleted);
    }

    [Fact]
    public void Delete_twice_is_rejected()
    {
        AggregateSpec.For<Page>()
            .Given(_created, new PageDeleted())
            .When(p => p.Delete())
            .ThenFails("deleted");
    }

    [Fact]
    public void Every_behavior_after_delete_is_rejected()
    {
        // One case per public behavior: deletion must be terminal for all of them.
        (string Name, Action<Page> Behavior)[] behaviors =
        [
            ("ChangeTitle", p => p.ChangeTitle(En, "New")),
            ("ChangeSlug", p => p.ChangeSlug(SlugOf("elsewhere"))),
            ("ChangeMeta", p => p.ChangeMeta(En, "t", "d")),
            ("AddNode", p => p.AddNode(NodeId.Root, 0, Section())),
            ("MoveNode", p => p.MoveNode(NodeId.New(), NodeId.Root, 0)),
            ("RemoveNode", p => p.RemoveNode(NodeId.New())),
            ("DuplicateNode", p => p.DuplicateNode(NodeId.New())),
            ("ChangeNodeProps", p => p.ChangeNodeProps(Heading())),
            ("EditText", p => p.EditText(NodeId.New(), "text", En, "x")),
            ("SetBlockOverride", p => p.SetBlockOverride(NodeId.New(), NodeId.New(), "text", En, "x")),
            ("DetachBlockInstance", p => p.DetachBlockInstance(NodeId.New(), Stack())),
            ("Publish", p => p.Publish()),
            ("Unpublish", p => p.Unpublish()),
            ("Delete", p => p.Delete()),
        ];

        foreach (var (name, behavior) in behaviors)
        {
            try
            {
                AggregateSpec.For<Page>()
                    .Given(_created, new PageDeleted())
                    .When(behavior)
                    .ThenFails("deleted");
            }
            catch (SpecException exception)
            {
                Assert.Fail($"{name} did not fail as deleted: {exception.Message}");
            }
        }
    }
}
