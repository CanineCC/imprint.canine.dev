using System.Text.Json;
using System.Text.Json.Serialization;
using Imprint.Authoring.Domain;

namespace Imprint.Authoring;

/// <summary>
/// JSON configuration for the bounded context's value objects. Passed to
/// <c>AddImprintEventSourcing</c> so every event payload round-trips; node and link
/// polymorphism is handled by [JsonPolymorphic] attributes, collections by their own
/// converters — only the primitives that serialize as bare strings live here.
/// </summary>
public static class AuthoringJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new LocaleJsonConverter());
        options.Converters.Add(new LocalizedTextJsonConverter());
    }

    private sealed class LocaleJsonConverter : JsonConverter<Locale>
    {
        public override Locale Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, Locale value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override Locale ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        public override void WriteAsPropertyName(Utf8JsonWriter writer, Locale value, JsonSerializerOptions options) =>
            writer.WritePropertyName(value.Value);
    }

    private sealed class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
    {
        public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options)!;
            var text = LocalizedText.Empty;
            foreach (var (locale, value) in map)
            {
                text = text.With(new Locale(locale), value);
            }

            return text;
        }

        public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var (locale, text) in value.Values)
            {
                writer.WriteString(locale.Value, text);
            }

            writer.WriteEndObject();
        }
    }
}
