using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteEnvironmentsTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static SiteCreated Created => new(Id, "Site", En);

    private static DeployEnvironment[] Pipeline =>
    [
        new("Test", "/var/www/test"),
        new("Staging", "/var/www/stag"),
        new("Production", "/var/www/prod"),
    ];

    [Fact]
    public void SetEnvironments_valid_pipeline_raises_environments_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(Pipeline))
            .ThenRaised(new SiteEnvironmentsChanged(Pipeline));

    [Fact]
    public void SetEnvironments_trims_names_and_paths_before_raising() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("  Test  ", "  /var/www/test  ")]))
            .ThenRaised(new SiteEnvironmentsChanged([new DeployEnvironment("Test", "/var/www/test")]));

    [Fact]
    public void SetEnvironments_updates_the_environments_state()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(Pipeline));

        Assert.Equal(Pipeline, outcome.Aggregate.Environments);
    }

    [Fact]
    public void SetEnvironments_empty_name_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("   ", "/var/www/test")]))
            .ThenFails("must have a name");

    [Fact]
    public void SetEnvironments_overlong_name_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment(new string('x', 41), "/var/www/test")]))
            .ThenFails("40 characters");

    [Fact]
    public void SetEnvironments_empty_path_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("Test", "   ")]))
            .ThenFails("must have a publish folder");

    [Fact]
    public void SetEnvironments_duplicate_names_are_rejected_case_insensitively() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(
                [new DeployEnvironment("Prod", "/a"), new DeployEnvironment("prod", "/b")]))
            .ThenFails("unique");

    [Fact]
    public void SetEnvironments_two_environments_may_share_a_folder() =>
        // Distinct names pointing at the same folder is unusual but not invalid — the
        // operator, not the aggregate, owns folder-collision policy.
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(
                [new DeployEnvironment("Test", "/same"), new DeployEnvironment("Prod", "/same")]))
            .ThenRaised(new SiteEnvironmentsChanged(
                [new DeployEnvironment("Test", "/same"), new DeployEnvironment("Prod", "/same")]));

    [Fact]
    public void SetEnvironments_at_the_maximum_is_accepted()
    {
        var max = Enumerable.Range(0, Site.MaxEnvironments)
            .Select(i => new DeployEnvironment($"env{i}", $"/var/www/{i}"))
            .ToArray();

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(max))
            .ThenRaised(new SiteEnvironmentsChanged(max));
    }

    [Fact]
    public void SetEnvironments_above_the_maximum_is_rejected()
    {
        var tooMany = Enumerable.Range(0, Site.MaxEnvironments + 1)
            .Select(i => new DeployEnvironment($"env{i}", $"/var/www/{i}"))
            .ToArray();

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(tooMany))
            .ThenFails($"at most {Site.MaxEnvironments}");
    }

    [Fact]
    public void SetEnvironments_reordering_raises_environments_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteEnvironmentsChanged(Pipeline))
            .When(s => s.SetEnvironments([Pipeline[2], Pipeline[1], Pipeline[0]]))
            .ThenRaised(new SiteEnvironmentsChanged([Pipeline[2], Pipeline[1], Pipeline[0]]));

    [Fact]
    public void SetEnvironments_unchanged_value_raises_nothing() =>
        // The gear always sends a freshly built list; the no-op guard is value equality.
        AggregateSpec.For<Site>()
            .Given(Created, new SiteEnvironmentsChanged(Pipeline))
            .When(s => s.SetEnvironments(
                [new("Test", "/var/www/test"), new("Staging", "/var/www/stag"), new("Production", "/var/www/prod")]))
            .ThenNothing();

    [Fact]
    public void SetEnvironments_unchanged_after_whitespace_normalization_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteEnvironmentsChanged([new DeployEnvironment("Test", "/var/www/test")]))
            .When(s => s.SetEnvironments([new DeployEnvironment("  Test  ", "  /var/www/test  ")]))
            .ThenNothing();

    [Fact]
    public void SetEnvironments_clearing_existing_raises_environments_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteEnvironmentsChanged(Pipeline))
            .When(s => s.SetEnvironments([]))
            .ThenRaised(new SiteEnvironmentsChanged([]));

    [Fact]
    public void SetEnvironments_empty_when_already_empty_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([]))
            .ThenNothing();

    // ------------------------------------------------- the optional site address (BaseUrl)

    [Fact]
    public void SetEnvironments_valid_base_url_raises_environments_changed_with_it() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(
                [new DeployEnvironment("Production", "/var/www/prod", "https://acme.example")]))
            .ThenRaised(new SiteEnvironmentsChanged(
                [new DeployEnvironment("Production", "/var/www/prod", "https://acme.example")]));

    [Fact]
    public void SetEnvironments_base_url_trailing_slash_is_normalized_off() =>
        // The origin is prepended to rooted paths ("/about/"), so a kept trailing slash
        // would double it in every canonical/sitemap URL.
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments(
                [new DeployEnvironment("Production", "/var/www/prod", "  https://acme.example/  ")]))
            .ThenRaised(new SiteEnvironmentsChanged(
                [new DeployEnvironment("Production", "/var/www/prod", "https://acme.example")]));

    [Fact]
    public void SetEnvironments_blank_base_url_is_stored_as_null() =>
        // The editor round-trips null as "": blank means "not set", never an empty string.
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("Test", "/var/www/test", "   ")]))
            .ThenRaised(new SiteEnvironmentsChanged([new DeployEnvironment("Test", "/var/www/test", null)]));

    [Fact]
    public void SetEnvironments_relative_base_url_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("Test", "/var/www/test", "example.com")]))
            .ThenFails("absolute http(s) URL");

    [Fact]
    public void SetEnvironments_non_http_scheme_base_url_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetEnvironments([new DeployEnvironment("Test", "/var/www/test", "ftp://acme.example")]))
            .ThenFails("absolute http(s) URL");

    [Fact]
    public void SetEnvironments_unchanged_base_url_after_normalization_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteEnvironmentsChanged(
                [new DeployEnvironment("Production", "/var/www/prod", "https://acme.example")]))
            .When(s => s.SetEnvironments(
                [new DeployEnvironment("Production", "/var/www/prod", "https://acme.example/")]))
            .ThenNothing();
}
