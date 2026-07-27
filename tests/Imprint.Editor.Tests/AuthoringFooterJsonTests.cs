using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Editor.Api;

namespace Imprint.Editor.Tests;

/// <summary>
/// The wire mapping for footer columns, shared by the authoring API and the MCP tool. The footer was
/// previously readable but not writable through either surface, so a broken link in it could only be fixed
/// in the interactive editor - which is unreachable from an off-network session. These pin the rules that
/// keep the write path safe: a link resolves the same way a navigation link does, a column without a heading
/// is refused rather than silently dropped, and a bad spec fails with a sentence.
/// </summary>
public sealed class AuthoringFooterJsonTests
{
    private static readonly Locale En = new("en");

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static FooterLinkGroup Parse(string raw) => AuthoringApi.ParseFooterGroup(Json(raw), En);

    [Fact]
    public void Parses_A_Column_Of_Page_And_External_Links()
    {
        var group = Parse("""
            {"heading":"The CAI standard","links":[
              {"label":"The standard","url":"https://cai.canine.dev/spec"},
              {"label":"Reference scorer / CLI","url":"https://cai.canine.dev/page-cli"}]}
            """);

        Assert.Equal("The CAI standard", group.Heading.Get(En));
        Assert.Equal(2, group.Links.Count);
        Assert.Equal("Reference scorer / CLI", group.Links[1].Label!.Get(En));
        Assert.Equal("https://cai.canine.dev/page-cli", Assert.IsType<ExternalLink>(group.Links[1].Link).Url);
    }

    [Fact]
    public void A_Page_Link_Resolves_To_A_PageId()
    {
        var group = Parse("""
            {"heading":"Product","links":[{"label":"Pricing","pageId":"7e12e9a225214121acfd4ff14542c220"}]}
            """);

        Assert.NotNull(group.Links[0].PageId);
    }

    [Fact]
    public void A_Column_Without_A_Heading_Is_Refused()
    {
        // Silently dropping it would delete a footer column on a write that looked like it succeeded.
        var ex = Assert.Throws<ArgumentException>(() => Parse("""{"links":[{"label":"x","url":"https://a.b"}]}"""));
        Assert.Contains("heading", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_Column_Without_Links_Is_Refused_By_Name()
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse("""{"heading":"Trust"}"""));
        Assert.Contains("Trust", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Link_With_Neither_PageId_Nor_Url_Is_Refused()
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse("""{"heading":"Product","links":[{"label":"x"}]}"""));
        Assert.Contains("pageId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_Disallowed_Href_Is_Refused()
    {
        // Same allow-list as navigation: a footer is rendered on every page, so it is the worst place to
        // accept a javascript: URL.
        Assert.Throws<ArgumentException>(() =>
            Parse("""{"heading":"Product","links":[{"label":"x","url":"javascript:alert(1)"}]}"""));
    }

    [Fact]
    public void A_Non_Object_Column_Fails_With_A_Sentence()
    {
        var ex = Assert.Throws<ArgumentException>(() => Parse("\"not an object\""));
        Assert.Contains("JSON object", ex.Message, StringComparison.Ordinal);
    }
}
