using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// Which paths the LLM files skip. The prefixes end up in a hot loop in the publisher —
/// every page, every pass — so they are normalized once here rather than re-parsed there,
/// and that normalization is the behaviour worth pinning.
/// </summary>
public sealed class SiteLlmsExcludedPathsTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Site", new Locale("en"));

    [Fact]
    public void Prefixes_are_stored_as_given_when_already_clean() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths(["surveys/github", "surveys/gitlab"]))
            .ThenRaised(new SiteLlmsExcludedPathsChanged(["surveys/github", "surveys/gitlab"]));

    [Theory]
    [InlineData("/surveys/github/")]
    [InlineData("surveys/github/")]
    [InlineData("/surveys/github")]
    [InlineData("  Surveys/GitHub  ")]
    public void Slashes_and_case_are_normalized_away(string given) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths([given]))
            .ThenRaised(new SiteLlmsExcludedPathsChanged(["surveys/github"]));

    [Fact]
    public void Blank_entries_are_dropped_rather_than_stored_as_empty_prefixes() =>
        // An empty prefix would match every page on the site — the policy would silently
        // become "publish no llms.txt content at all".
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths(["surveys", "", "   ", "/"]))
            .ThenRaised(new SiteLlmsExcludedPathsChanged(["surveys"]));

    [Fact]
    public void Duplicates_collapse() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths(["surveys", "/surveys/", "SURVEYS"]))
            .ThenRaised(new SiteLlmsExcludedPathsChanged(["surveys"]));

    [Fact]
    public void A_trailing_wildcard_is_kept_so_a_family_of_generated_names_needs_one_rule() =>
        // dimensions/rubric-2026.08.12, -08.14, -08.15 … a new one most weeks. Naming them
        // individually means the policy is stale the day after it is written.
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths(["dimensions/rubric*"]))
            .ThenRaised(new SiteLlmsExcludedPathsChanged(["dimensions/rubric*"]));

    [Theory]
    [InlineData("surveys/../etc")]
    [InlineData("surveys/git hub")]
    [InlineData("surveys//github")]
    [InlineData("surveys/git?hub")]
    [InlineData("dimensions/*")]        // the whole segment: say "dimensions" instead
    [InlineData("dim*/rubric")]         // wildcard only on the last segment
    [InlineData("dimensions/ru*bric")]  // not a glob language
    public void A_prefix_that_is_not_path_shaped_is_rejected(string bad) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths([bad]))
            .ThenFails("not a valid path prefix");

    [Fact]
    public void More_than_the_cap_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLlmsExcludedPaths(
                [.. Enumerable.Range(0, Site.MaxLlmsExcludedPaths + 1).Select(i => $"path-{i}")]))
            .ThenFails("At most");

    [Fact]
    public void Setting_the_same_policy_again_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLlmsExcludedPathsChanged(["surveys/github"]))
            .When(s => s.SetLlmsExcludedPaths(["/Surveys/GitHub/"]))
            .ThenNothing();

    [Fact]
    public void Clearing_the_policy_raises_an_empty_list() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteLlmsExcludedPathsChanged(["surveys/github"]))
            .When(s => s.SetLlmsExcludedPaths(null))
            .ThenRaised(new SiteLlmsExcludedPathsChanged([]));

    [Fact]
    public void A_site_excludes_nothing_by_default() =>
        Assert.Empty(AggregateSpec.For<Site>()
            .Given(Created)
            .When(_ => { })
            .Aggregate.LlmsExcludedPaths);
}
