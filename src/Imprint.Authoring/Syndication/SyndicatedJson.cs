using System.Text.Json;
using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;

namespace Imprint.Authoring.Syndication;

/// <summary>
/// How a syndicated page is written to and read from storage.
/// </summary>
/// <remarks>
/// It uses <see cref="AuthoringJson"/> — the same configuration every event payload round-trips
/// through — rather than its own. Node and link polymorphism already work there, and a second
/// serializer for the same types would be free to disagree with the first about a value object
/// nobody thought to test twice.
/// <para>
/// Property order is not sorted and does not need to be: the shape comes from the record
/// definitions, so the same tree always writes the same bytes, which is what lets the content hash
/// mean "this page changed".
/// </para>
/// </remarks>
internal static class SyndicatedJson
{
    private static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions();
        AuthoringJson.Configure(options);
        return options;
    }

    public static string Localized(LocalizedText text) => JsonSerializer.Serialize(text, Options);

    public static LocalizedText ReadLocalized(string json) =>
        JsonSerializer.Deserialize<LocalizedText>(json, Options) ?? LocalizedText.Empty;

    public static string Node(Node node) => JsonSerializer.Serialize(node, Options);

    public static Node ReadNode(string json) =>
        JsonSerializer.Deserialize<Node>(json, Options)
        ?? throw new InvalidOperationException("A stored syndicated page has an unreadable node tree.");
}
