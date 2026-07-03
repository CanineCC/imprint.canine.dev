using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imprint.EventSourcing;

/// <summary>
/// Contract for strongly-typed, Guid-backed identifiers
/// (<c>readonly record struct PageId(Guid Value) : IGuidId&lt;PageId&gt;</c>).
/// Gives every id JSON support (via <see cref="GuidIdJsonConverterFactory"/>) and a
/// uniform compact string form for stream names and DOM attributes.
/// </summary>
public interface IGuidId<TSelf> where TSelf : struct, IGuidId<TSelf>
{
    Guid Value { get; }
    static abstract TSelf From(Guid value);
}

/// <summary>Serializes any <see cref="IGuidId{TSelf}"/> as its bare Guid string.</summary>
public sealed class GuidIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsValueType &&
        typeToConvert.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IGuidId<>) &&
            i.GenericTypeArguments[0] == typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    private sealed class Converter<T> : JsonConverter<T> where T : struct, IGuidId<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            T.From(reader.GetGuid());

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            T.From(Guid.Parse(reader.GetString()!));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value.ToString("D"));
    }
}
