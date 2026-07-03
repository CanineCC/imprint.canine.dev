using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;

namespace Imprint.EventSourcing;

/// <summary>
/// Maps stable event names to CLR types and owns the JSON serialization of event
/// payloads. Built once at startup from an assembly scan — one of the two sanctioned
/// reflection scans in Imprint (the other registers handlers and projections).
/// </summary>
public sealed class EventRegistry
{
    private readonly FrozenDictionary<string, Type> _typeByStableId;
    private readonly FrozenDictionary<Type, string> _stableIdByType;

    /// <summary>Options used for every event (and metadata) payload in the store.</summary>
    public JsonSerializerOptions JsonOptions { get; }

    public EventRegistry(IEnumerable<Assembly> eventAssemblies, Action<JsonSerializerOptions>? configureJson = null)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = false,
        };
        options.Converters.Add(new GuidIdJsonConverterFactory());
        configureJson?.Invoke(options);
        JsonOptions = options;

        // The sanctioned scan: every [EventType]-annotated record in the supplied
        // assemblies becomes storable. A type without the attribute cannot be
        // appended — Serialize throws — which keeps the catalog in docs/domain-model.md
        // and the code from drifting silently.
        var byId = new Dictionary<string, Type>(StringComparer.Ordinal);
        var byType = new Dictionary<Type, string>();
        foreach (var assembly in eventAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<EventTypeAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                if (!byId.TryAdd(attribute.StableId, type))
                {
                    throw new InvalidOperationException(
                        $"Duplicate stable event id '{attribute.StableId}' on {type.FullName} and {byId[attribute.StableId].FullName}.");
                }

                byType.Add(type, attribute.StableId);
            }
        }

        _typeByStableId = byId.ToFrozenDictionary(StringComparer.Ordinal);
        _stableIdByType = byType.ToFrozenDictionary();
    }

    /// <summary>All registered event CLR types — used by the round-trip test battery.</summary>
    public IReadOnlyCollection<Type> EventTypes => _stableIdByType.Keys;

    public string StableIdOf(object @event) =>
        _stableIdByType.TryGetValue(@event.GetType(), out var id)
            ? id
            : throw new InvalidOperationException(
                $"{@event.GetType().FullName} has no [EventType] attribute and cannot be stored.");

    public string Serialize(object @event) =>
        JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions);

    public object Deserialize(string stableId, string json)
    {
        if (!_typeByStableId.TryGetValue(stableId, out var type))
        {
            throw new InvalidOperationException(
                $"Unknown event type '{stableId}' in the store. Was an event type removed without a migration?");
        }

        return JsonSerializer.Deserialize(json, type, JsonOptions)
               ?? throw new InvalidOperationException($"Event '{stableId}' deserialized to null.");
    }
}
