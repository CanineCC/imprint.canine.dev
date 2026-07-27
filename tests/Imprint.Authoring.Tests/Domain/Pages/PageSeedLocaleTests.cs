using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

/// <summary>
/// Seeding is the step between "the locale exists" and "the locale can be translated".
/// The behaviour worth pinning is not that it copies — it is <em>what it refuses to
/// copy over</em>, because that is what makes it safe to run again after a half-done run.
/// </summary>
public sealed class PageSeedLocaleTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());
    private readonly HeadingNode _heading = Heading("Hello");
    private readonly RichTextNode _richText = RichText("<p>Hi</p>");
    private readonly ButtonNode _button = Button("Go");
    private readonly DividerNode _divider = Divider();

    private AggregateSpec<Page> Spec(params object[] extra) => AggregateSpec.For<Page>().Given(
        [_created,
         new NodeAdded(NodeId.Root, 0, Section(Stack(_heading, _richText, _button, _divider))),
         .. extra]);

    [Fact]
    public void SeedLocale_copies_every_text_field_into_the_new_locale()
    {
        var outcome = Spec().When(p => p.SeedLocale(Da, En));

        outcome.ThenRaised(
            new TitleChanged(Da, "About"),
            new TextChanged(_heading.Id, "text", Da, "Hello"),
            new TextChanged(_richText.Id, "html", Da, "<p>Hi</p>"),
            new TextChanged(_button.Id, "label", Da, "Go"));
    }

    [Fact]
    public void SeedLocale_leaves_a_translation_that_already_exists()
    {
        var outcome = Spec(new TextChanged(_heading.Id, "text", Da, "Hej"))
            .When(p => p.SeedLocale(Da, En));

        var heading = Assert.IsType<HeadingNode>(outcome.Aggregate.Tree.Find(_heading.Id));
        Assert.Equal("Hej", heading.Text.Get(Da));
        Assert.DoesNotContain(outcome.Raised, e => e is TextChanged { NodeId: var id } t
            && id == _heading.Id && t.Locale == Da);
    }

    [Fact]
    public void SeedLocale_run_twice_changes_nothing_the_second_time()
    {
        var once = Spec().When(p => p.SeedLocale(Da, En));
        var twice = AggregateSpec.For<Page>()
            .Given([_created,
                    new NodeAdded(NodeId.Root, 0, Section(Stack(_heading, _richText, _button, _divider))),
                    .. once.Raised])
            .When(p => p.SeedLocale(Da, En));

        Assert.Empty(twice.Raised);
    }

    [Fact]
    public void SeedLocale_carries_the_meta_description_across()
    {
        var outcome = Spec(new MetaChanged(En, "Title", "Description"))
            .When(p => p.SeedLocale(Da, En));

        Assert.Contains(new MetaChanged(Da, "Title", "Description"), outcome.Raised);
    }

    [Fact]
    public void SeedLocale_fills_only_the_missing_half_of_the_meta_pair()
    {
        var outcome = Spec(
                new MetaChanged(En, "Title", "Description"),
                new MetaChanged(Da, "Dansk titel", null))
            .When(p => p.SeedLocale(Da, En));

        Assert.Equal("Dansk titel", outcome.Aggregate.MetaTitle.Get(Da));
        Assert.Equal("Description", outcome.Aggregate.MetaDescription.Get(Da));
    }

    [Fact]
    public void SeedLocale_skips_a_node_with_no_source_text() =>
        Assert.DoesNotContain(
            Spec().When(p => p.SeedLocale(Da, En)).Raised,
            e => e is TextChanged { NodeId: var id } && id == _divider.Id);

    [Fact]
    public void SeedLocale_from_itself_is_rejected() =>
        Spec().When(p => p.SeedLocale(Da, Da))
            .ThenFails("cannot be seeded from itself");
}
