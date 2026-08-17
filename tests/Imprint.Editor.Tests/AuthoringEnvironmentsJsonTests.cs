using System.Text.Json;
using Imprint.Editor.Api;

namespace Imprint.Editor.Tests;

/// <summary>
/// The wire mapping for a site's deploy environments. These were readable and writable only in the
/// interactive editor, which made the one operation they exist for — moving a site to a new domain,
/// which is a BaseUrl change plus a republish — unreachable from a script or an off-network session.
/// These pin the rules that keep that write path safe: a BaseUrl is an ORIGIN and is normalised as
/// one, and a malformed entry fails with a sentence rather than reaching the aggregate.
/// </summary>
public sealed class AuthoringEnvironmentsJsonTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    [Fact]
    public void Parses_A_Named_Folder_And_Its_Canonical_Origin()
    {
        Assert.True(AuthoringApi.TryParseEnvironments(
            Json("""[{"name":"Production","path":"/srv/cai","baseUrl":"https://codeassuranceindex.info"}]"""),
            out var parsed, out var error));

        Assert.Null(error);
        var environment = Assert.Single(parsed);
        Assert.Equal("Production", environment.Name);
        Assert.Equal("/srv/cai", environment.Path);
        Assert.Equal("https://codeassuranceindex.info", environment.BaseUrl);
    }

    [Fact]
    public void The_Order_Sent_Is_The_Order_Kept()
    {
        Assert.True(AuthoringApi.TryParseEnvironments(
            Json("""[{"name":"Test","path":"/a"},{"name":"Production","path":"/b"}]"""),
            out var parsed, out _));

        Assert.Equal(["Test", "Production"], parsed.Select(e => e.Name));
    }

    [Fact]
    public void A_Trailing_Slash_Is_Trimmed_So_Canonical_Urls_Do_Not_Double_Up()
    {
        Assert.True(AuthoringApi.TryParseEnvironments(
            Json("""[{"name":"Production","path":"/srv/cai","baseUrl":"https://codeassuranceindex.info/"}]"""),
            out var parsed, out _));

        Assert.Equal("https://codeassuranceindex.info", Assert.Single(parsed).BaseUrl);
    }

    [Fact]
    public void An_Omitted_Or_Empty_BaseUrl_Means_Origin_Relative_Output()
    {
        Assert.True(AuthoringApi.TryParseEnvironments(
            Json("""[{"name":"Test","path":"/a"},{"name":"Staging","path":"/b","baseUrl":""}]"""),
            out var parsed, out _));

        Assert.All(parsed, environment => Assert.Null(environment.BaseUrl));
    }

    [Theory]
    [InlineData("/just/a/path")]
    [InlineData("codeassuranceindex.info")]
    [InlineData("ftp://codeassuranceindex.info")]
    public void A_BaseUrl_That_Is_Not_An_Http_Origin_Is_Refused(string baseUrl)
    {
        Assert.False(AuthoringApi.TryParseEnvironments(
            Json($$"""[{"name":"Production","path":"/srv/cai","baseUrl":"{{baseUrl}}"}]"""),
            out _, out var error));

        Assert.Contains(baseUrl, error);
    }

    [Fact]
    public void An_Environment_Without_A_Name_Is_Refused_Rather_Than_Silently_Dropped()
    {
        Assert.False(AuthoringApi.TryParseEnvironments(
            Json("""[{"path":"/srv/cai"}]"""), out _, out var error));

        Assert.Contains("name", error);
    }

    [Fact]
    public void An_Environment_Without_A_Path_Names_The_One_That_Is_Wrong()
    {
        Assert.False(AuthoringApi.TryParseEnvironments(
            Json("""[{"name":"Production"}]"""), out _, out var error));

        Assert.Contains("Production", error);
        Assert.Contains("path", error);
    }

    [Fact]
    public void An_Entry_That_Is_Not_An_Object_Is_Refused()
    {
        Assert.False(AuthoringApi.TryParseEnvironments(
            Json("""["Production"]"""), out _, out var error));

        Assert.Contains("JSON object", error);
    }

    [Fact]
    public void An_Empty_List_Clears_The_Environments()
    {
        Assert.True(AuthoringApi.TryParseEnvironments(Json("[]"), out var parsed, out var error));

        Assert.Empty(parsed);
        Assert.Null(error);
    }
}
