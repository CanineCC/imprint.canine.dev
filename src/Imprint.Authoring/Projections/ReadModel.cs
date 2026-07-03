using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// Base for Imprint's in-memory read models. All of them are rebuilt by full replay at
/// startup and updated synchronously after each command — the editor reads its own
/// writes the moment a dispatch returns. <see cref="Changed"/> lets Blazor components
/// subscribe for live updates (fired from the projection engine's thread; UI code
/// marshals via InvokeAsync).
/// </summary>
public abstract class ReadModel : IProjection
{
    /// <summary>
    /// Raised after this read model folds an event. Subscribers are UI components,
    /// and this fires synchronously inside the projection fold — which runs inside the
    /// command dispatch a live editor is awaiting. A subscriber that throws (a Blazor
    /// component on a disconnected-but-not-yet-disposed circuit can, mid-teardown) must
    /// therefore never break the fold or starve the other subscribers: see
    /// <see cref="NotifyChanged"/>. Always unsubscribe in the component's Dispose.
    /// </summary>
    public event Action? Changed;

    protected void NotifyChanged()
    {
        if (Changed is null)
        {
            return;
        }

        // Isolate each subscriber: the fold is load-bearing (a live editor is awaiting
        // it for read-your-writes), so one faulting UI handler cannot be allowed to
        // abort the multicast or bubble out into the dispatcher. Handlers are expected
        // to be trivial (marshal a StateHasChanged), so the per-cast delegate walk is
        // negligible.
        foreach (var handler in Changed.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch
            {
                // A doomed circuit's handler threw; nothing the read model can do about
                // it, and nothing it should let that break for everyone else.
            }
        }
    }

    public abstract void Apply(StoredEvent @event);

    public abstract void Reset();
}

/// <summary>Stream-name parsing for projections routing events by aggregate identity.</summary>
public static class StreamIds
{
    public static Guid? IdOf(string streamId, string prefix) =>
        streamId.StartsWith(prefix, StringComparison.Ordinal) &&
        Guid.TryParseExact(streamId[prefix.Length..], "N", out var id)
            ? id
            : null;
}
