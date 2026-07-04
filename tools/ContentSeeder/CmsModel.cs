using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContentSeeder;

/// <summary>
/// A single surface read from the CMS content tree: either a block page
/// (<c>content/pages/*.json</c>) or a long-form doc (<c>content/docs/*.md</c>).
/// <see cref="Rel"/> is the CMS relative path minus extension (e.g. <c>home</c>,
/// <c>about</c>, <c>reports/tender</c>, <c>security</c>) — the exact route key the
/// Next.js catch-all derives (sites/*/pages/[[...slug]].tsx).
/// </summary>
public sealed record CmsSurface(
    string Rel,
    bool IsDoc,
    string Title,
    string? Description,
    JsonArray Blocks,     // page blocks (empty for docs)
    DocContent? Doc);     // parsed markdown doc (null for pages)

public sealed record DocContent(
    string Title,
    string? Description,
    string? Kicker,
    string Heading,
    string? DocMeta,
    string MarkdownBody);

/// <summary>Reads a site's CMS content tree deterministically (ordinal-sorted by rel path).</summary>
public static class CmsReader
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<CmsSurface> Read(string siteContentRoot)
    {
        var surfaces = new List<CmsSurface>();
        var pagesRoot = Path.Combine(siteContentRoot, "pages");
        if (Directory.Exists(pagesRoot))
        {
            foreach (var file in Directory
                         .EnumerateFiles(pagesRoot, "*.json", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                var rel = RelOf(pagesRoot, file, ".json");
                var node = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
                var title = node["title"]?.GetValue<string>() ?? rel;
                var description = node["description"]?.GetValue<string>();
                var blocks = node["blocks"]?.AsArray() ?? new JsonArray();
                surfaces.Add(new CmsSurface(rel, IsDoc: false, title, description, blocks, Doc: null));
            }
        }

        var docsRoot = Path.Combine(siteContentRoot, "docs");
        if (Directory.Exists(docsRoot))
        {
            foreach (var file in Directory
                         .EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                var rel = RelOf(docsRoot, file, ".md");
                var doc = ParseDoc(File.ReadAllText(file), rel);
                surfaces.Add(new CmsSurface(
                    rel, IsDoc: true, doc.Title, doc.Description, new JsonArray(), doc));
            }
        }

        // Deterministic order: home first, then ordinal by rel — matches the publisher's
        // page ordering intent and makes the seed reproducible run-to-run.
        return
        [
            .. surfaces
                .OrderByDescending(s => s.Rel == "home")
                .ThenBy(s => s.Rel, StringComparer.Ordinal),
        ];
    }

    private static string RelOf(string root, string file, string ext)
    {
        var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
        return rel[..^ext.Length];
    }

    /// <summary>Splits YAML-ish frontmatter (title/description/kicker/heading/docMeta) from the markdown body.</summary>
    private static DocContent ParseDoc(string raw, string rel)
    {
        var text = raw.Replace("\r\n", "\n");
        string frontmatter = "";
        string body = text;
        if (text.StartsWith("---\n", StringComparison.Ordinal))
        {
            var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end >= 0)
            {
                frontmatter = text[4..end];
                var afterMarker = text.IndexOf('\n', end + 1);
                body = afterMarker >= 0 ? text[(afterMarker + 1)..] : "";
            }
        }

        var fm = ParseFrontmatter(frontmatter);
        var title = fm.GetValueOrDefault("title") ?? rel;
        return new DocContent(
            Title: title,
            Description: fm.GetValueOrDefault("description"),
            Kicker: fm.GetValueOrDefault("kicker"),
            Heading: fm.GetValueOrDefault("heading") ?? title,
            DocMeta: fm.GetValueOrDefault("docMeta"),
            MarkdownBody: body.Trim('\n'));
    }

    private static Dictionary<string, string> ParseFrontmatter(string frontmatter)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in frontmatter.Split('\n'))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                value = value[1..^1];
            }

            map[key] = value;
        }

        return map;
    }

    // ---- typed accessors over JsonNode block objects (verbatim, never invented) ----

    public static string Template(this JsonNode block) =>
        block["_template"]?.GetValue<string>() ?? "";

    public static string? Str(this JsonNode block, string field)
    {
        var v = block[field];
        return v is null ? null : v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : v.ToString();
    }

    public static JsonArray Arr(this JsonNode block, string field) =>
        block[field]?.AsArray() ?? new JsonArray();

    public static bool Bool(this JsonNode block, string field) =>
        block[field]?.GetValueKind() == JsonValueKind.True;

    public static JsonObject? Obj(this JsonNode block, string field) =>
        block[field] as JsonObject;
}
