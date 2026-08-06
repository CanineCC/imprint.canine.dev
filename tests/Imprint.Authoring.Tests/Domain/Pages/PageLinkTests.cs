using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Tests.Domain.Pages;

/// <summary>
/// A same-site link, optionally narrowed to one section of the page it points at.
/// <para>The fragment exists because the two obvious ways to link a section both fail once a
/// site has more than one page or more than one language: a bare <c>#anchor</c> only reaches the
/// page you are already on, and an absolute <c>https://site/#anchor</c> pins the default locale.
/// A page reference is resolved per locale at render time, so the anchor can ride along.</para>
/// </summary>
public sealed class PageLinkTests
{
    private static readonly PageId Home = PageId.New();

    [Fact]
    public void A_plain_page_link_has_no_section()
    {
        var link = new PageLink(Home);

        Assert.Null(link.Fragment);
        Assert.Equal("/da/", link.Href("/da/"));
    }

    [Fact]
    public void A_section_link_appends_its_anchor_to_whatever_path_the_locale_resolved()
    {
        var link = new PageLink(Home, "independence");

        Assert.Equal("/#independence", link.Href("/"));
        Assert.Equal("/da/#independence", link.Href("/da/"));
    }

    [Fact]
    public void An_unresolvable_page_stays_unresolvable()
    {
        // Null in, null out: the caller drops the link rather than emitting "#independence"
        // on its own, which would silently become a link into the page the reader is on.
        Assert.Null(new PageLink(Home, "independence").Href(null));
    }

    [Theory]
    [InlineData("Independence", "independence")]
    [InlineData("  what we make  ", "what-we-make")]
    [InlineData("#pricing", "pricing")]
    public void An_anchor_is_normalised_the_same_way_a_section_normalises_its_own(string written, string stored) =>
        Assert.Equal(stored, new PageLink(Home, written).Fragment);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!")]
    [InlineData("123")]
    public void An_anchor_no_section_could_carry_reads_back_as_no_section(string written) =>
        // Degrading to the page itself keeps the reader one scroll from the right place; a
        // broken href would take them nowhere at all.
        Assert.Null(new PageLink(Home, written).Fragment);

    [Fact]
    public void The_same_gate_applies_to_an_edit_of_an_existing_link()
    {
        // `with` writes the property directly, so a constructor-only guard would let the
        // editor's one-field edit through unchecked.
        var edited = new PageLink(Home, "independence") with { Fragment = "What We Make!" };

        Assert.Equal("what-we-make", edited.Fragment);
    }

    [Fact]
    public void Two_sections_of_one_page_are_two_different_links()
    {
        // What lets navigation hold the front page and three of its sections at once.
        Assert.NotEqual(new PageLink(Home, "independence"), new PageLink(Home, "products"));
        Assert.NotEqual(new PageLink(Home, "independence"), new PageLink(Home));
        Assert.Equal(new PageLink(Home, "independence"), new PageLink(Home, "Independence"));
    }
}
