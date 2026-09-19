using System.Reflection;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Domain;

// Every domain event carries [EventType]. Without it the EventRegistry cannot name the event,
// so StableIdOf throws and the command fails — at RUNTIME, on the first write, in production.
//
// This exists because that happened: SiteBylineChanged shipped without the attribute. Nothing
// caught it, because the registry is built FROM the attribute — an event that lacks one is not
// an event the registry knows is missing, it is simply invisible to it. Every other test passed,
// including the aggregate tests for the feature, because applying an event in memory never
// consults the registry. Only storing one does.
//
// So the check cannot come from the registry. It comes from the namespace: a record under
// Domain.*.Events is a domain event by construction, and must be storable.
public sealed class EventTypeAttributeTests
{
    [Fact]
    public void Every_event_can_be_stored()
    {
        var missing = EventRecords()
            .Where(t => t.GetCustomAttribute<EventTypeAttribute>() is null)
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "These events have no [EventType] and would throw on the first attempt to store one:"
            + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    // The stable id is what a stored event is read back by, so a duplicate silently makes two
    // event types indistinguishable in the stream — and the second one to register wins.
    [Fact]
    public void Every_event_name_is_unique()
    {
        var duplicates = EventRecords()
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<EventTypeAttribute>()))
            .Where(x => x.Attr is not null)
            .GroupBy(x => x.Attr!.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}': {string.Join(", ", g.Select(x => x.Type.Name))}")
            .ToList();

        Assert.True(duplicates.Count == 0,
            "These event names are used by more than one type:"
            + Environment.NewLine + string.Join(Environment.NewLine, duplicates));
    }

    private static IReadOnlyList<Type> EventRecords()
    {
        // Anchored on an event, not on AggregateRoot: that base class lives in Imprint.EventSourcing,
        // and reflecting over THAT assembly finds no Imprint.Authoring types at all — which is how the
        // first version of this test passed while the bug it was written for was still in the tree.
        var found = typeof(SiteCreated).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace is { } ns
                        && ns.StartsWith("Imprint.Authoring.Domain.", StringComparison.Ordinal)
                        && ns.EndsWith(".Events", StringComparison.Ordinal))
            .ToList();

        // A discovery bug must fail the test, not empty it. If this ever trips, the namespace
        // convention moved and the two checks above are silently asserting nothing.
        Assert.True(found.Count >= 20, $"only {found.Count} domain events discovered — the scan is broken, not the tree");
        return found;
    }
}
