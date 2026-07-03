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
    public event Action? Changed;

    protected void NotifyChanged() => Changed?.Invoke();

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
