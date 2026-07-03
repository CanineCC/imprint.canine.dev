using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteRenameTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static SiteCreated Created => new(Id, "Original name", En);

    [Fact]
    public void Rename_to_a_new_name_raises_renamed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Rename("New name"))
            .ThenRaised(new SiteRenamed("New name"));

    [Fact]
    public void Rename_to_the_unchanged_name_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Rename("Original name"))
            .ThenNothing();

    [Fact]
    public void Rename_to_the_unchanged_name_with_surrounding_whitespace_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Rename("  Original name  "))
            .ThenNothing();

    [Fact]
    public void Rename_to_empty_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Rename("   "))
            .ThenFails("name cannot be empty");

    [Fact]
    public void Rename_to_more_than_100_characters_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.Rename(new string('x', 101)))
            .ThenFails("100 characters");
}
