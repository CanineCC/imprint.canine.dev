using Imprint.Authoring;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests;

/// <summary>
/// Implemented once per aggregate (in its test folder): supplies at least one fully
/// populated sample of every event the aggregate raises. The theory below fails when
/// an event type exists without samples — you cannot add an event and forget its
/// serialization test.
/// </summary>
public interface IEventSampleProvider
{
    IEnumerable<object> Samples { get; }
}

public sealed class EventRoundTripTests
{
    private static readonly EventRegistry Registry =
        new([typeof(AuthoringJson).Assembly], AuthoringJson.Configure);

    private static IReadOnlyList<object> AllSamples() =>
        [.. typeof(EventRoundTripTests).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false } && typeof(IEventSampleProvider).IsAssignableFrom(t))
            .Select(t => (IEventSampleProvider)Activator.CreateInstance(t)!)
            .SelectMany(p => p.Samples)];

    [Fact]
    public void Every_registered_event_type_has_at_least_one_sample()
    {
        var sampled = AllSamples().Select(s => s.GetType()).ToHashSet();
        var missing = Registry.EventTypes.Where(t => !sampled.Contains(t)).ToList();

        Assert.True(missing.Count == 0,
            "Events without serialization samples (add them to an IEventSampleProvider): " +
            string.Join(", ", missing.Select(t => t.Name)));
    }

    [Fact]
    public void Every_sample_round_trips_with_value_equality()
    {
        foreach (var sample in AllSamples())
        {
            var stableId = Registry.StableIdOf(sample);
            var json = Registry.Serialize(sample);
            var back = Registry.Deserialize(stableId, json);

            Assert.True(sample.Equals(back),
                $"{sample.GetType().Name} did not round-trip.\nOriginal: {sample}\nRound-tripped: {back}\nJson: {json}");
        }
    }
}
