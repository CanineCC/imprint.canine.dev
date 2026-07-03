namespace Imprint.EventSourcing;

/// <summary>
/// Assigns a stable, serialized name to a domain event type. The stable name — not the
/// CLR type name — is what the event store persists, so refactoring C# names is free
/// while renaming a stable name is an explicit data migration.
/// </summary>
/// <remarks>
/// Stable names are lower-case dot-paths (<c>page.node-added</c>). The persisted
/// discriminator is <c>{name}.v{version}</c>; bump <paramref name="version"/> and keep
/// the old record type around (upcasting it in its aggregate) when a payload has to
/// change shape.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventTypeAttribute(string name, int version = 1) : Attribute
{
    public string Name { get; } = name;
    public int Version { get; } = version;

    /// <summary>The full discriminator persisted in the store, e.g. <c>page.node-added.v1</c>.</summary>
    public string StableId => $"{Name}.v{Version}";
}
