using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// Old addresses that now live somewhere else (a guide moved to the resource center). The publisher
/// writes a small redirect page at each one; here the table is normalized and anything that could
/// turn a redirect into a script sink or a loop is refused.
/// </summary>
public sealed class SiteRedirectsTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Site", new Locale("en"));

    [Fact]
    public void A_redirect_is_stored_with_its_path_normalized_to_a_folder() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect(" guide-read-only-by-design ", "https://resourcecenter.canine.dev/read-only-by-design/")]))
            .ThenRaised(new SiteRedirectsChanged([new SiteRedirect("/guide-read-only-by-design/", "https://resourcecenter.canine.dev/read-only-by-design/")]));

    [Fact]
    public void A_site_relative_target_is_allowed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect("/old/", "/new/")]))
            .ThenRaised(new SiteRedirectsChanged([new SiteRedirect("/old/", "/new/")]));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://example.com/")]
    [InlineData("//example.com/")]
    [InlineData("")]
    public void A_target_that_is_not_https_or_a_site_path_is_refused(string target) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect("/old/", target)]))
            .ThenFails("https");

    [Theory]
    [InlineData("/")]
    [InlineData("/a/../b/")]
    [InlineData("/a?b")]
    [InlineData("//x/")]
    public void A_source_that_is_the_root_or_not_a_plain_path_is_refused(string source) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect(source, "/new/")]))
            .ThenFails("path");

    [Fact]
    public void The_same_source_twice_is_refused() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect("/old/", "/a/"), new SiteRedirect("old", "/b/")]))
            .ThenFails("twice");

    [Fact]
    public void A_redirect_to_itself_is_refused() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRedirects([new SiteRedirect("/old/", "/old/")]))
            .ThenFails("itself");

    [Fact]
    public void Setting_the_same_table_again_records_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteRedirectsChanged([new SiteRedirect("/old/", "/new/")]))
            .When(s => s.SetRedirects([new SiteRedirect("/old/", "/new/")]))
            .ThenNothing();
}
