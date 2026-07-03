using Imprint.EventSourcing;

namespace Imprint.TestKit;

/// <summary>
/// Given/When/Then testing for aggregates, no store involved:
/// <code>
/// AggregateSpec.For&lt;Page&gt;()
///     .Given(created, nodeAdded)
///     .When(p => p.MoveNode(id, parent, 0))
///     .ThenRaised(new NodeMoved(id, parent, 0));
/// </code>
/// Framework-agnostic on purpose: failures throw <see cref="SpecException"/> with a
/// readable expected/actual diff, which every test runner renders fine.
/// </summary>
public static class AggregateSpec
{
    public static AggregateSpec<T> For<T>() where T : AggregateRoot, new() => new();
}

public sealed class SpecException(string message) : Exception(message);

public sealed class AggregateSpec<T> where T : AggregateRoot, new()
{
    private readonly List<object> _history = [];

    public AggregateSpec<T> Given(params IReadOnlyList<object> events)
    {
        _history.AddRange(events);
        return this;
    }

    public Outcome When(Action<T> behavior)
    {
        var aggregate = new T();
        aggregate.LoadFrom(_history);
        try
        {
            behavior(aggregate);
            return new Outcome(aggregate, aggregate.UncommittedEvents, null);
        }
        catch (DomainException exception)
        {
            return new Outcome(aggregate, [], exception);
        }
    }

    public sealed class Outcome(T aggregate, IReadOnlyList<object> raised, DomainException? exception)
    {
        /// <summary>The aggregate after the behavior — for asserting on derived state.</summary>
        public T Aggregate => aggregate;

        public IReadOnlyList<object> Raised => exception is null
            ? raised
            : throw new SpecException($"Expected events but the behavior failed: {exception.Message}");

        /// <summary>Asserts the exact raised events, in order, by record equality.</summary>
        public void ThenRaised(params IReadOnlyList<object> expected)
        {
            if (exception is not null)
            {
                throw new SpecException(
                    $"Expected {expected.Count} event(s) but the behavior failed: {exception.Message}");
            }

            if (expected.Count != raised.Count || expected.Where((e, i) => !e.Equals(raised[i])).Any())
            {
                throw new SpecException(
                    $"""
                     Raised events differ.
                     Expected: {Describe(expected)}
                     Actual:   {Describe(raised)}
                     """);
            }
        }

        public void ThenNothing() => ThenRaised();

        /// <summary>Asserts the behavior failed with a DomainException mentioning <paramref name="messagePart"/>.</summary>
        public void ThenFails(string messagePart)
        {
            if (exception is null)
            {
                throw new SpecException(
                    $"Expected a domain error containing '{messagePart}' but the behavior raised: " +
                    (raised.Count == 0 ? "nothing" : Describe(raised)));
            }

            if (!exception.Message.Contains(messagePart, StringComparison.OrdinalIgnoreCase))
            {
                throw new SpecException(
                    $"Expected the error to mention '{messagePart}' but it was: {exception.Message}");
            }
        }

        private static string Describe(IReadOnlyList<object> events) =>
            events.Count == 0 ? "(none)" : string.Join(Environment.NewLine + "           ", events);
    }
}
