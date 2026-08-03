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
    // A version identifier minted elsewhere and printed on the artifact it names must survive into the
    // address verbatim, so a reader can paste what the report shows them. Dots are in the alphabet for this.
    [InlineData("dimensions/rubric-2026.08.19", "dimensions/rubric-2026.08.19")]
    public void Accepts_a_nested_slug_shaped_path(string input, string expected) =>
        Assert.Equal(expected, SyndicatedPath.Sanitize(input));

    [Theory]
    [InlineData("../../etc/passwd")]          // traversal
    [InlineData("registry/../../secrets")]    // traversal, mid-path
    [InlineData("/etc/passwd")]               // absolute
    [InlineData("registry\\github\\thing")]   // backslashes
    [InlineData("registry/with space")]       // space
    [InlineData("registry/über")]             // outside the alphabet
    [InlineData("..")]                        // a bare dot-segment is not a segment
    [InlineData("registry/..")]               // …at any depth
    [InlineData("registry/.hidden")]          // a segment may not START with a dot
    [InlineData("registry/trailing.")]        // …nor end with one
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

    [Theory]
    [InlineData("da")]        // a locale we publish
    [InlineData("en")]
    [InlineData("fr")]        // and one we do not — the shape is what collides, not the config
    public void Refuses_a_FIRST_segment_that_would_shadow_a_locale_prefix(string input)
    {
        // /da/ is the Danish site. A page addressed "da" would sit on top of it.
        Assert.Null(SyndicatedPath.Sanitize(input));
        Assert.Null(SyndicatedPath.Sanitize($"{input}/anything"));
    }

    [Theory]
    [InlineData("surveys/lang/go", "surveys/lang/go")]
    [InlineData("surveys/lang/php", "surveys/lang/php")]
    [InlineData("surveys/github/nektos/act", "surveys/github/nektos/act")]
    [InlineData("surveys/github/junegunn/fzf", "surveys/github/junegunn/fzf")]
    [InlineData("surveys/github/lfe/lfe", "surveys/github/lfe/lfe")]
    [InlineData("surveys/github/jet/propulsion", "surveys/github/jet/propulsion")]
    public void Accepts_a_short_segment_that_is_not_the_first(string input, string expected)
    {
        // A locale only ever occupies the FIRST segment of a URL, so a two- or three-letter
        // segment deeper in a path cannot shadow one. Holding every depth to the locale
        // reservation refused these real addresses: it cost the corpus its Go and PHP field
        // guides and 64 repository pages, while the index that linked to them published fine —
        // so a 2,700-page public corpus linked to its own 404s, and the only visible symptom was
        // a 400 in a worker log nobody was reading.
        Assert.Equal(expected, SyndicatedPath.Sanitize(input));
    }

    [Fact]
    public void Refuses_a_path_deeper_than_a_path_should_be()
    {
        // host/owner/name needs three, and a nested namespace needs more — a real address in the corpus
        // is already surveys/gitlab/teo-dotnet/backend/restapi/orderingapi, which is six. Eight keeps the
        // headroom; beyond that something is being encoded in the URL that belongs in the page.
        Assert.NotNull(SyndicatedPath.Sanitize("surveys/gitlab/teo-dotnet/backend/restapi/orderingapi"));
        Assert.NotNull(SyndicatedPath.Sanitize("a/b/c/d/e/f/g/h"));
        Assert.Null(SyndicatedPath.Sanitize("a/b/c/d/e/f/g/h/i"));
    }
}
