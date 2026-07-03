using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Pages.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.Pages;

using static PageTestData;

public sealed class PageCreateTests
{
    private readonly PageId _id = PageId.New();
    private readonly SiteId _site = SiteId.New();

    [Fact]
    public void Create_with_valid_input_raises_created()
    {
        var page = Page.Create(_id, _site, SlugOf("about"), En, "About");

        var raised = Assert.Single(page.UncommittedEvents);
        Assert.Equal(new PageCreated(_id, _site, "about", En, "About"), raised);
    }

    [Fact]
    public void Create_with_valid_input_initializes_state()
    {
        var page = Page.Create(_id, _site, SlugOf("about"), En, "About");

        Assert.Equal(_id, page.Id);
        Assert.Equal(_site, page.SiteId);
        Assert.Equal("about", page.Slug.Value);
        Assert.Equal("About", page.Title.Get(En));
        Assert.Equal(PageTree.Empty, page.Tree);
        Assert.Null(page.PublishedVersion);
        Assert.False(page.IsDeleted);
        Assert.Equal(_id.Stream, page.StreamId);
    }

    [Fact]
    public void Create_with_empty_title_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => Page.Create(_id, _site, SlugOf("about"), En, ""));
        Assert.Contains("title", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_with_whitespace_title_is_rejected()
    {
        Assert.Throws<DomainException>(() => Page.Create(_id, _site, SlugOf("about"), En, "   "));
    }

    [Fact]
    public void Create_with_title_over_200_characters_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => Page.Create(_id, _site, SlugOf("about"), En, new string('x', 201)));
        Assert.Contains("200", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_with_title_of_exactly_200_characters_is_accepted()
    {
        var page = Page.Create(_id, _site, SlugOf("about"), En, new string('x', 200));
        Assert.Single(page.UncommittedEvents);
    }

    [Fact]
    public void Loading_an_unknown_event_type_throws()
    {
        var page = new Page();
        Assert.Throws<InvalidOperationException>(() => page.LoadFrom([new object()]));
    }
}
