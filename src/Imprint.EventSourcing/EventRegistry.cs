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

    // Keyed by the versionless event name (EventTypeAttribute.Name) so one upcaster
    // covers every past version of an event that folds into the same current record.
    // Empty for a stock build — Deserialize skips the lookup entirely when there are none.
    private readonly FrozenDictionary<string, IEventUpcaster> _upcasterByName;

    /// <summary>Options used for every event (and metadata) payload in the store.</summary>
    public JsonSerializerOptions JsonOptions { get; }

    public EventRegistry(IEnumerable<Assembly> eventAssemblies, Action<JsonSerializerOptions>? configureJson = null)
        : this(eventAssemblies, upcasters: [], configureJson)
    {
    }

    public EventRegistry(
        IEnumerable<Assembly> eventAssemblies,
        IEnumerable<IEventUpcaster> upcasters,
        Action<JsonSerializerOptions>? configureJson = null)
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

        var upcasterByName = new Dictionary<string, IEventUpcaster>(StringComparer.Ordinal);
        foreach (var upcaster in upcasters)
        {
            if (!upcasterByName.TryAdd(upcaster.EventName, upcaster))
            {
                throw new InvalidOperationException(
                    $"Two upcasters registered for event '{upcaster.EventName}': "
                    + $"{upcasterByName[upcaster.EventName].GetType().FullName} and {upcaster.GetType().FullName}.");
            }
        }

        _upcasterByName = upcasterByName.ToFrozenDictionary(StringComparer.Ordinal);
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

        var @event = JsonSerializer.Deserialize(json, type, JsonOptions)
                     ?? throw new InvalidOperationException($"Event '{stableId}' deserialized to null.");

        // Upcast BEFORE anyone else sees it, so aggregates and projections only ever
        // handle the current shape. Lookup is by versionless name ('page.created' out of
        // 'page.created.v1'); with no upcasters registered this is a single cheap miss.
        if (_upcasterByName.Count > 0
            && _upcasterByName.TryGetValue(NameOf(stableId), out var upcaster))
        {
            @event = upcaster.Upcast(@event);
            if (@event.GetType() != type)
            {
                throw new InvalidOperationException(
                    $"Upcaster {upcaster.GetType().FullName} for '{stableId}' returned a "
                    + $"{@event.GetType().FullName}; it must return the same event type ({type.FullName}).");
            }
        }

        return @event;
    }

    // 'page.created.v1' → 'page.created'. The version suffix is always '.v{digits}';
    // an event id without it (defensive) is returned whole.
    private static string NameOf(string stableId)
    {
        var lastDot = stableId.LastIndexOf('.');
        return lastDot > 0 && stableId.AsSpan(lastDot + 1) is ['v', .. var digits] && digits.Length > 0
            ? stableId[..lastDot]
            : stableId;
    }
}
