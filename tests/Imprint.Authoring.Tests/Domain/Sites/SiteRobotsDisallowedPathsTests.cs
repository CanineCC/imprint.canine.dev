using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// Paths a site asks crawlers to skip in its robots.txt. Owner 2026-11-18: cca.canine.dev shares its host with a public
/// survey gallery under /api/oss/, served by another application, and a crawler fetched every old run and raw file of it
/// (~24,000 requests in hours), overloading production. The site declares the paths; nothing guesses them.
/// </summary>
public sealed class SiteRobotsDisallowedPathsTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Site", new Locale("en"));

    [Fact]
    public void Patterns_are_stored_as_given_with_wildcards_and_query_strings() =>
        // RFC 9309 patterns: '*' anywhere and a query string are what makes "old runs only" expressible.
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRobotsDisallowedPaths(["/api/oss/*/report?run=", "/api/oss/*/artifacts/"]))
            .ThenRaised(new SiteRobotsDisallowedPathsChanged(["/api/oss/*/report?run=", "/api/oss/*/artifacts/"]));

    [Fact]
    public void Blank_entries_are_dropped_and_duplicates_collapse() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRobotsDisallowedPaths(["  /api/oss/*/artifacts/  ", "", "   ", "/api/oss/*/artifacts/"]))
            .ThenRaised(new SiteRobotsDisallowedPathsChanged(["/api/oss/*/artifacts/"]));

    [Theory]
    [InlineData("/")]                        // the whole site: that is de-indexing, not a crawl policy
    [InlineData("/*")]
    [InlineData("api/oss/")]                 // a robots path starts at the root
    [InlineData("/api/oss/\nDisallow: /")]   // one line each: no injected directives
    [InlineData("/api oss/")]
    [InlineData("/api/oss/#frag")]
    public void A_pattern_that_is_not_a_robots_path_is_rejected(string bad) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRobotsDisallowedPaths([bad]))
            .ThenFails("robots");

    [Fact]
    public void Too_many_patterns_are_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetRobotsDisallowedPaths(Enumerable.Range(0, Site.MaxRobotsDisallowedPaths + 1).Select(i => $"/p{i}/").ToList()))
            .ThenFails("At most");

    [Fact]
    public void Setting_the_same_list_again_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteRobotsDisallowedPathsChanged(["/api/oss/*/artifacts/"]))
            .When(s => s.SetRobotsDisallowedPaths(["/api/oss/*/artifacts/"]))
            .ThenNothing();

    [Fact]
    public void Null_clears_the_policy() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteRobotsDisallowedPathsChanged(["/api/oss/*/artifacts/"]))
            .When(s => s.SetRobotsDisallowedPaths(null))
            .ThenRaised(new SiteRobotsDisallowedPathsChanged([]));
}
