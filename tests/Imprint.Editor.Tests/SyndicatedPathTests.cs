using Imprint.Authoring.Syndication;

namespace Imprint.Editor.Tests;

/// <summary>
/// The address of a pushed page. It arrives from another system and becomes a DIRECTORY in the
/// published output, so the interesting cases are the ones that must be refused — containment here
/// is a property of the alphabet, not of a traversal check somebody has to remember to write.
/// </summary>
public sealed class SyndicatedPathTests
{
    [Theory]
    [InlineData("registry/github/jasperfx/marten", "registry/github/jasperfx/marten")]
    [InlineData("/registry/github/jasperfx/marten/", "registry/github/jasperfx/marten")]  // trimmed
    [InlineData("Registry/GitHub/JasperFx/Marten", "registry/github/jasperfx/marten")]    // lower-cased
    [InlineData("registry", "registry")]                                                  // one segment is fine
    public void Accepts_a_nested_slug_shaped_path(string input, string expected) =>
        Assert.Equal(expected, SyndicatedPath.Sanitize(input));

    [Theory]
    [InlineData("../../etc/passwd")]          // traversal
    [InlineData("registry/../../secrets")]    // traversal, mid-path
    [InlineData("/etc/passwd")]               // absolute
    [InlineData("registry\\github\\thing")]   // backslashes
    [InlineData("registry/with space")]       // space
    [InlineData("registry/über")]             // outside the alphabet
    [InlineData("")]
    [InlineData(null)]
    public void Refuses_anything_that_is_not_slug_shaped(string? input) =>
        Assert.Null(SyndicatedPath.Sanitize(input));

    [Theory]
    [InlineData("assets/evil")]
    [InlineData("css/site")]
    [InlineData("widgets/thing")]
    [InlineData("sitemap/anything")]
    public void Refuses_a_path_that_would_shadow_the_sites_own_files(string input) =>
        Assert.Null(SyndicatedPath.Sanitize(input));

    [Fact]
    public void Refuses_a_path_deeper_than_a_path_should_be()
    {
        // host/owner/name needs three. Six is generous; beyond that something is being encoded in
        // the URL that belongs in the page.
        Assert.NotNull(SyndicatedPath.Sanitize("a/b/c/d/e/f"));
        Assert.Null(SyndicatedPath.Sanitize("a/b/c/d/e/f/g"));
    }
}
