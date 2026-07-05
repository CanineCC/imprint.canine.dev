using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain.Sites;

/// <summary>
/// BaseUrl is an additive property on the stored <c>site.environments-changed</c>
/// payload: every event written before it existed carries only Name + Path. These tests
/// pin the wire-level contract — the exact JSON older deployments wrote deserializes
/// with a null BaseUrl (today's behavior), and the new shape carries the origin through.
/// </summary>
public sealed class DeployEnvironmentCompatibilityTests
{
    private static readonly EventRegistry Registry =
        new([typeof(AuthoringJson).Assembly], AuthoringJson.Configure);

    private const string StableId = "site.environments-changed.v1";

    [Fact]
    public void Stored_event_without_BaseUrl_deserializes_with_null_BaseUrl()
    {
        // Verbatim the payload shape written before BaseUrl existed.
        const string storedJson =
            """{"Environments":[{"Name":"Test","Path":"/srv/www/acme/test"},{"Name":"Production","Path":"/srv/www/acme"}]}""";

        var @event = Assert.IsType<SiteEnvironmentsChanged>(Registry.Deserialize(StableId, storedJson));

        Assert.Equal(2, @event.Environments.Count);
        Assert.Equal(new DeployEnvironment("Test", "/srv/www/acme/test", BaseUrl: null), @event.Environments[0]);
        Assert.Equal(new DeployEnvironment("Production", "/srv/www/acme", BaseUrl: null), @event.Environments[1]);
    }

    [Fact]
    public void Stored_event_with_BaseUrl_deserializes_with_the_origin()
    {
        const string storedJson =
            """{"Environments":[{"Name":"Production","Path":"/srv/www/acme","BaseUrl":"https://acme.example"}]}""";

        var @event = Assert.IsType<SiteEnvironmentsChanged>(Registry.Deserialize(StableId, storedJson));

        Assert.Equal(
            new DeployEnvironment("Production", "/srv/www/acme", "https://acme.example"),
            Assert.Single(@event.Environments));
    }

    [Fact]
    public void Serialized_event_round_trips_both_shapes_under_the_stable_id()
    {
        var original = new SiteEnvironmentsChanged([
            new DeployEnvironment("Test", "/srv/www/acme/test"),
            new DeployEnvironment("Production", "/srv/www/acme", "https://acme.example"),
        ]);

        Assert.Equal(StableId, Registry.StableIdOf(original));
        var back = Registry.Deserialize(StableId, Registry.Serialize(original));

        Assert.Equal(original, back);
    }
}
