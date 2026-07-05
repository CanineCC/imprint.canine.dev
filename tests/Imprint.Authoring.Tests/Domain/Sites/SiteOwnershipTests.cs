using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteOwnershipTests
{
    private static readonly SiteId Id = SiteId.New();
    private static SiteCreated Created => new(Id, "Acme", new Locale("en"));

    [Fact]
    public void Claiming_ownership_raises_claimed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.ClaimOwnership())
            .ThenRaised(new SiteOwnershipClaimed());
}
