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
/// <para>
/// The same bytes for the same TREE, note — not for the same PUSH. A producer sends content, never
/// node ids: those are minted per parse, so two pushes of identical content are two different trees
/// by this serializer's reckoning. That is why the hash reads <see cref="NodeForHash"/> instead.
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

    /// <summary>The node tree as the CONTENT HASH sees it: the stored JSON, with node identity blanked out.</summary>
    /// <remarks>
    /// Node ids are not content. A syndicated page's ids are minted by the wire parser on every push — it
    /// mints one per node so an authored add can never collide with or hijack an existing node — so hashing
    /// them made the hash of byte-identical input different every time: the push endpoint could never answer
    /// <c>changed: false</c>, and the publisher, which reads that hash as a syndicated page's version, found
    /// every page stale on every sweep.
    /// <para>
    /// Excluding them is honest rather than convenient: published markup carries no node ids at all
    /// (<c>data-node-id</c> is the editor canvas's selection contract, emitted only in
    /// <c>RenderMode.Editor</c>), so two trees differing only in ids render the same bytes and must hash the
    /// same. A block instance's override keys are NOT touched: those name the DEFINITION's nodes, which are
    /// another aggregate's stable ids, and changing which node an override lands on changes the page.
    /// </para>
    /// </remarks>
    public static string NodeForHash(Node node) => Node(WithoutIdentity(node));

    /// <summary>
    /// The same subtree with every node id blanked. The mirror image of <c>Page.CloneWithFreshIds</c>:
    /// that one exists so a copy is a new node, this one so a re-push is the same page.
    /// </summary>
    private static Node WithoutIdentity(Node node)
    {
        // NodeId.Root is the sentinel no real node carries, so nothing can mistake this tree for a
        // storable one — it exists only long enough to be serialized into the hash.
        var anonymous = node with { Id = NodeId.Root };
        return anonymous is IContainerNode container
            ? container.WithChildren(NodeList.Of([.. container.Children.Select(WithoutIdentity)]))
            : anonymous;
    }
}
