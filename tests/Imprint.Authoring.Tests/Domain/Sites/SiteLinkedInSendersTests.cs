using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>Who may post a site's articles to LinkedIn: a key the article names, a display name, and the
/// LinkedIn organization (a company page or a product's showcase page).</summary>
public sealed class SiteLinkedInSendersTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Site", new Locale("en"));

    [Fact]
    public void Senders_are_stored_with_their_key_lower_cased() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLinkedInSenders([new LinkedInSender(" Canine ", "Canine Development", "urn:li:organization:12345")]))
            .ThenRaised(new SiteLinkedInSendersChanged([new LinkedInSender("canine", "Canine Development", "urn:li:organization:12345")]));

    [Theory]
    [InlineData("urn:li:person:abc")]
    [InlineData("12345")]
    [InlineData("urn:li:organization:")]
    public void A_sender_that_is_not_a_linkedin_organization_is_refused(string urn) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLinkedInSenders([new LinkedInSender("canine", "Canine", urn)]))
            .ThenFails("urn:li:organization");

    [Fact]
    public void The_same_key_twice_is_refused() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLinkedInSenders([new LinkedInSender("canine", "A", "urn:li:organization:1"), new LinkedInSender("canine", "B", "urn:li:organization:2")]))
            .ThenFails("twice");

    [Fact]
    public void A_sender_needs_a_name() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetLinkedInSenders([new LinkedInSender("canine", " ", "urn:li:organization:1")]))
            .ThenFails("name");
}
