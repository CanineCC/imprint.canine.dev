using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageTitleSlugMetaTests
{
    private readonly PageCreated _created = Created(PageId.New(), SiteId.New());

    private AggregateSpec<Page> Spec(params object[] furtherEvents) =>
        AggregateSpec.For<Page>().Given(_created).Given(furtherEvents);

    // ------------------------------------------------------------------- titles

    [Fact]
    public void ChangeTitle_with_new_value_raises_event()
    {
        Spec()
            .When(p => p.ChangeTitle(En, "About us"))
            .ThenRaised(new TitleChanged(En, "About us"));
    }

    [Fact]
    public void ChangeTitle_in_another_locale_adds_the_translation()
    {
        var outcome = Spec().When(p => p.ChangeTitle(Da, "Om os"));

        outcome.ThenRaised(new TitleChanged(Da, "Om os"));
        Assert.Equal("About", outcome.Aggregate.Title.Get(En));
        Assert.Equal("Om os", outcome.Aggregate.Title.Get(Da));
    }

    [Fact]
    public void ChangeTitle_with_empty_value_clears_that_locale()
    {
        var outcome = Spec(new TitleChanged(Da, "Om os")).When(p => p.ChangeTitle(Da, ""));

        outcome.ThenRaised(new TitleChanged(Da, ""));
        Assert.False(outcome.Aggregate.Title.Has(Da));
        Assert.Equal("About", outcome.Aggregate.Title.Get(En));
    }

    [Fact]
    public void ChangeTitle_over_200_characters_is_rejected()
    {
        Spec()
            .When(p => p.ChangeTitle(En, new string('x', 201)))
            .ThenFails("200");
    }

    // -------------------------------------------------------------------- slugs

    [Fact]
    public void ChangeSlug_to_a_new_slug_raises_event()
    {
        var outcome = Spec().When(p => p.ChangeSlug(SlugOf("contact")));

        outcome.ThenRaised(new SlugChanged("contact"));
        Assert.Equal("contact", outcome.Aggregate.Slug.Value);
    }

    [Fact]
    public void ChangeSlug_to_the_same_slug_raises_nothing()
    {
        Spec()
            .When(p => p.ChangeSlug(SlugOf("about")))
            .ThenNothing();
    }

    // --------------------------------------------------------------------- meta

    [Fact]
    public void ChangeMeta_raises_event_and_folds_both_fields()
    {
        var outcome = Spec().When(p => p.ChangeMeta(En, "About — Acme", "Everything about Acme."));

        outcome.ThenRaised(new MetaChanged(En, "About — Acme", "Everything about Acme."));
        Assert.Equal("About — Acme", outcome.Aggregate.MetaTitle.Get(En));
        Assert.Equal("Everything about Acme.", outcome.Aggregate.MetaDescription.Get(En));
    }

    [Fact]
    public void ChangeMeta_with_null_values_clears_that_locale()
    {
        var outcome = Spec(new MetaChanged(En, "About — Acme", "Everything about Acme."))
            .When(p => p.ChangeMeta(En, null, null));

        outcome.ThenRaised(new MetaChanged(En, null, null));
        Assert.False(outcome.Aggregate.MetaTitle.Has(En));
        Assert.False(outcome.Aggregate.MetaDescription.Has(En));
    }

    [Fact]
    public void ChangeMeta_title_over_300_characters_is_rejected()
    {
        Spec()
            .When(p => p.ChangeMeta(En, new string('x', 301), null))
            .ThenFails("300");
    }

    [Fact]
    public void ChangeMeta_description_over_300_characters_is_rejected()
    {
        Spec()
            .When(p => p.ChangeMeta(En, null, new string('x', 301)))
            .ThenFails("300");
    }

    [Fact]
    public void ChangeMeta_values_of_exactly_300_characters_are_accepted()
    {
        Spec()
            .When(p => p.ChangeMeta(En, new string('x', 300), new string('y', 300)))
            .ThenRaised(new MetaChanged(En, new string('x', 300), new string('y', 300)));
    }
}
