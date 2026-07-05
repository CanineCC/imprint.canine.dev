using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteCollaboratorTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Acme", new Locale("en"));

    [Fact]
    public void Adding_a_collaborator_raises_added() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddCollaborator("bob@example.com"))
            .ThenRaised(new SiteCollaboratorAdded("bob@example.com"));

    [Fact]
    public void The_email_is_trimmed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddCollaborator("  bob@example.com  "))
            .ThenRaised(new SiteCollaboratorAdded("bob@example.com"));

    [Fact]
    public void Adding_the_same_email_twice_is_rejected_case_insensitively() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteCollaboratorAdded("Bob@Example.com"))
            .When(s => s.AddCollaborator("bob@example.com"))
            .ThenFails("already a collaborator");

    [Fact]
    public void An_empty_email_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddCollaborator("   "))
            .ThenFails("cannot be empty");

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("bob@")]
    [InlineData("bob@@example.com")]
    [InlineData("bob smith@example.com")]
    public void A_value_that_does_not_look_like_an_email_is_rejected(string email) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.AddCollaborator(email))
            .ThenFails("does not look like an email");

    [Fact]
    public void Adding_beyond_the_cap_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given([
                Created,
                .. Enumerable.Range(0, Site.MaxCollaborators)
                    .Select(i => new SiteCollaboratorAdded($"person{i}@example.com")),
            ])
            .When(s => s.AddCollaborator("one-too-many@example.com"))
            .ThenFails($"at most {Site.MaxCollaborators}");

    [Fact]
    public void Removing_a_collaborator_raises_removed() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteCollaboratorAdded("bob@example.com"))
            .When(s => s.RemoveCollaborator("bob@example.com"))
            .ThenRaised(new SiteCollaboratorRemoved("bob@example.com"));

    [Fact]
    public void Removal_matches_case_insensitively()
    {
        // The event carries the address as typed; the fold removes the stored entry anyway.
        var site = new Site();
        site.LoadFrom([Created, new SiteCollaboratorAdded("Bob@Example.com")]);
        site.RemoveCollaborator("bob@example.com");
        Assert.Empty(site.Collaborators);
    }

    [Fact]
    public void Removing_an_unknown_email_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.RemoveCollaborator("stranger@example.com"))
            .ThenFails("not a collaborator");

    [Fact]
    public void A_removed_collaborator_can_be_added_again() =>
        AggregateSpec.For<Site>()
            .Given(
                Created,
                new SiteCollaboratorAdded("bob@example.com"),
                new SiteCollaboratorRemoved("bob@example.com"))
            .When(s => s.AddCollaborator("bob@example.com"))
            .ThenRaised(new SiteCollaboratorAdded("bob@example.com"));
}
