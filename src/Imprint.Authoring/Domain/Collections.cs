using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Domain;

// Value-semantic immutable collections. Records containing raw ImmutableList/Dictionary
// silently lose structural equality (reference compare) — these wrappers restore it, so
// events and nodes compare by value in aggregates and tests alike.

/// <summary>An ordered list of child nodes with structural equality. JSON form: a plain array.</summary>
[JsonConverter(typeof(NodeListJsonConverter))]
public sealed class NodeList : IReadOnlyList<Node>, IEquatable<NodeList>
{
    public static readonly NodeList Empty = new(ImmutableList<Node>.Empty);

    private readonly ImmutableList<Node> _items;

    private NodeList(ImmutableList<Node> items) => _items = items;

    public static NodeList Of(params IReadOnlyList<Node> nodes) => new([.. nodes]);

    public Node this[int index] => _items[index];
    public int Count => _items.Count;

    public NodeList Insert(int index, Node node) => new(_items.Insert(index, node));
    public NodeList Add(Node node) => new(_items.Add(node));
    public NodeList RemoveAt(int index) => new(_items.RemoveAt(index));
    public NodeList SetItem(int index, Node node) => new(_items.SetItem(index, node));
    public int IndexOf(NodeId id) => _items.FindIndex(n => n.Id == id);

    public IEnumerator<Node> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(NodeList? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => Equals(obj as NodeList);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_items.Count);
        foreach (var item in _items)
        {
            hash.Add(item.Id);
        }

        return hash.ToHashCode();
    }

    internal sealed class NodeListJsonConverter : JsonConverter<NodeList>
    {
        public override NodeList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<ImmutableList<Node>>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, NodeList value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value._items, options);
    }
}

/// <summary>String key/value props for widgets, with structural equality. JSON form: an object.</summary>
[JsonConverter(typeof(PropBagJsonConverter))]
public sealed class PropBag : IEquatable<PropBag>, IEnumerable<KeyValuePair<string, string>>
{
    public static readonly PropBag Empty = new(ImmutableSortedDictionary<string, string>.Empty);

    private readonly ImmutableSortedDictionary<string, string> _values;

    private PropBag(ImmutableSortedDictionary<string, string> values) => _values = values;

    public static PropBag Of(IEnumerable<KeyValuePair<string, string>> values) =>
        new(ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, values));

    public string? Get(string key) => _values.GetValueOrDefault(key);
    public PropBag With(string key, string value) => new(_values.SetItem(key, value));
    public PropBag Without(string key) => new(_values.Remove(key));
    public int Count => _values.Count;

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(PropBag? other) => other is not null && _values.SequenceEqual(other._values);
    public override bool Equals(object? obj) => Equals(obj as PropBag);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (key, value) in _values)
        {
            hash.Add(key, StringComparer.Ordinal);
            hash.Add(value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    internal sealed class PropBagJsonConverter : JsonConverter<PropBag>
    {
        public override PropBag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<ImmutableSortedDictionary<string, string>>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, PropBag value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value._values, options);
    }
}

/// <summary>
/// Per-instance content overrides for a block instance: definition node id + field +
/// locale → text. JSON form: an array of entries (dictionary keys would be composite).
/// </summary>
[JsonConverter(typeof(OverrideSetJsonConverter))]
public sealed class OverrideSet : IEquatable<OverrideSet>
{
    public sealed record Entry(NodeId DefinitionNodeId, string Field, Locale Locale, string Value);

    public static readonly OverrideSet Empty = new(ImmutableSortedDictionary<string, Entry>.Empty);

    private readonly ImmutableSortedDictionary<string, Entry> _entries;

    private OverrideSet(ImmutableSortedDictionary<string, Entry> entries) => _entries = entries;

    private static string KeyOf(NodeId node, string field, Locale locale) => $"{node.Compact}|{field}|{locale}";

    public string? Get(NodeId definitionNodeId, string field, Locale locale) =>
        _entries.GetValueOrDefault(KeyOf(definitionNodeId, field, locale))?.Value;

    public OverrideSet With(NodeId definitionNodeId, string field, Locale locale, string? value)
    {
        var key = KeyOf(definitionNodeId, field, locale);
        return value is null
            ? new OverrideSet(_entries.Remove(key))
            : new OverrideSet(_entries.SetItem(key, new Entry(definitionNodeId, field, locale, value)));
    }

    public IEnumerable<Entry> Entries => _entries.Values;
    public int Count => _entries.Count;

    public bool Equals(OverrideSet? other) => other is not null && _entries.SequenceEqual(other._entries);
    public override bool Equals(object? obj) => Equals(obj as OverrideSet);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var key in _entries.Keys)
        {
            hash.Add(key, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    internal sealed class OverrideSetJsonConverter : JsonConverter<OverrideSet>
    {
        public override OverrideSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var entries = JsonSerializer.Deserialize<List<Entry>>(ref reader, options)!;
            var set = Empty;
            foreach (var entry in entries)
            {
                set = set.With(entry.DefinitionNodeId, entry.Field, entry.Locale, entry.Value);
            }

            return set;
        }

        public override void Write(Utf8JsonWriter writer, OverrideSet value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Entries.ToList(), options);
    }
}
