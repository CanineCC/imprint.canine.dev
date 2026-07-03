using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class EditTextTests
{
    private static async Task<(PageId PageId, HeadingNode Heading, RichTextNode Text)> Arrange(
        AuthoringTestHost host, params string[] locales)
    {
        var siteId = await PagesHost.SeedSite(host, locales);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var heading = new HeadingNode { Id = NodeId.New() };
        var text = new RichTextNode { Id = NodeId.New() };
        await host.Ok(new AddNode(pageId, sectionId, 0, heading));
        await host.Ok(new AddNode(pageId, sectionId, 1, text));
        return (pageId, heading, text);
    }

    [Fact]
    public async Task EditText_updates_the_draft()
    {
        await using var host = PagesHost.Create();
        var (pageId, heading, _) = await Arrange(host);

        await host.Ok(new EditText(pageId, heading.Id, "text", "en", "Welcome"));

        var updated = (HeadingNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(heading.Id)!;
        Assert.Equal("Welcome", updated.Text.Get(new Locale("en")));
    }

    [Fact]
    public async Task EditText_accepts_valid_canonical_html()
    {
        await using var host = PagesHost.Create();
        var (pageId, _, text) = await Arrange(host);

        await host.Ok(new EditText(pageId, text.Id, "html", "en", "<p>Hello <strong>world</strong></p>"));

        var updated = (RichTextNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(text.Id)!;
        Assert.Equal("<p>Hello <strong>world</strong></p>", updated.Html.Get(new Locale("en")));
    }

    [Fact]
    public async Task EditText_invalid_canonical_html_surfaces_the_validator_message()
    {
        await using var host = PagesHost.Create();
        var (pageId, _, text) = await Arrange(host);

        var error = await host.Fails(new EditText(pageId, text.Id, "html", "en", "<script>alert(1)</script>"));
        Assert.Contains("Expected <p>, <ul> or <ol>", error);
    }

    [Fact]
    public async Task EditText_in_a_locale_not_on_the_site_is_rejected()
    {
        await using var host = PagesHost.Create();
        var (pageId, heading, _) = await Arrange(host, "en");

        var error = await host.Fails(new EditText(pageId, heading.Id, "text", "da", "Velkommen"));
        Assert.Contains("'da' is not one of this site's locales", error);
    }

    [Fact]
    public async Task EditText_with_malformed_locale_is_rejected()
    {
        await using var host = PagesHost.Create();
        var (pageId, heading, _) = await Arrange(host);

        var error = await host.Fails(new EditText(pageId, heading.Id, "text", "danish", "Velkommen"));
        Assert.Contains("not a valid locale tag", error);
    }

    [Fact]
    public async Task EditText_on_a_field_the_node_does_not_carry_is_rejected()
    {
        await using var host = PagesHost.Create();
        var (pageId, heading, _) = await Arrange(host);

        var error = await host.Fails(new EditText(pageId, heading.Id, "label", "en", "Click"));
        Assert.Contains("has no editable 'label' text", error);
    }
}
