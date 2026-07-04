namespace Imprint.EventSourcing;

/// <summary>
/// Transforms a just-deserialized event into the shape the current domain expects,
/// before any aggregate or projection ever sees it. Upcasters are the sanctioned way to
/// evolve a stream additively: an old event written under a past schema is read back,
/// deserialized into the current CLR record (missing JSON fields take their .NET
/// defaults), and then normalized here — so the rest of the system only ever handles
/// today's shape. They run inside <see cref="EventRegistry.Deserialize"/>, are pure and
/// deterministic (same stored bytes → same upcast result on every replay), and MUST be
/// backward-compatible: an event already in the current shape must pass through
/// unchanged, so a fresh install that never wrote a legacy event is completely
/// unaffected.
/// </summary>
public interface IEventUpcaster
{
    /// <summary>
    /// The stable id (without the <c>.v{n}</c> suffix — the <see cref="EventTypeAttribute.Name"/>)
    /// this upcaster applies to. Matching by name, not version, lets one upcaster cover
    /// every past version of an event whose current CLR record it normalizes into.
    /// </summary>
    string EventName { get; }

    /// <summary>
    /// Returns the event in its current shape. Receives the freshly deserialized event
    /// (current CLR type, defaults filled for absent fields) and must return an event of
    /// the SAME CLR type. Returning <paramref name="event"/> unchanged is the correct
    /// answer for anything already current — the overwhelmingly common case.
    /// </summary>
    object Upcast(object @event);
}
