using System.Text;
using System.Text.Json;

namespace Imprint.Publishing;

/// <summary>
/// Builds the JSON-LD graph a page carries in its head.
///
/// Everything here is <em>derived</em> from metadata the page already has — title,
/// description, canonical URL, locale, site name. Nothing new has to be authored, which is
/// the point: a site of three pages and a site of three thousand syndicated ones both get
/// complete structured data, and it can never drift out of step with the title it was
/// derived from.
///
/// The publisher builds this and hands <see cref="StaticPageDocument"/> a finished string,
/// keeping the component a dumb template.
/// </summary>
public static class StructuredData
{
    // The default encoder escapes '<' and '>' to < / >, so the payload can never
    // close its own <script> element. That is a security property, not a formatting
    // preference — do not "relax" it.
    private static readonly JsonWriterOptions WriterOptions = new() { Indented = false };

    /// <summary>
    /// The page graph: the site it belongs to, the page itself, the trail that leads to it,
    /// and — on the home page — who publishes it.
    /// </summary>
    /// <param name="siteName">The site's display name.</param>
    /// <param name="lang">BCP-47 tag for this rendering's locale.</param>
    /// <param name="pageUrl">The page's canonical URL (absolute in production).</param>
    /// <param name="homeUrl">The site root in this locale.</param>
    /// <param name="title">The page's own title, without the site-name suffix.</param>
    /// <param name="description">The meta description, or null.</param>
    /// <param name="logoUrl">The site logo, when one is published.</param>
    /// <param name="isHome">Whether this is the site's front page.</param>
    /// <param name="trail">Ancestor (name, url) pairs from the root down to — but excluding — this page.</param>
    public static string PageGraph(
        string siteName,
        string lang,
        string pageUrl,
        string homeUrl,
        string title,
        string? description,
        string? logoUrl,
        bool isHome,
        IReadOnlyList<(string Name, string Url)> trail)
    {
        var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteString("@context", "https://schema.org");
            json.WriteStartArray("@graph");

            var websiteId = $"{homeUrl}#website";
            var organizationId = $"{homeUrl}#organization";

            json.WriteStartObject();
            json.WriteString("@type", "WebSite");
            json.WriteString("@id", websiteId);
            json.WriteString("url", homeUrl);
            json.WriteString("name", siteName);
            json.WriteString("inLanguage", lang);
            json.WriteStartObject("publisher");
            json.WriteString("@id", organizationId);
            json.WriteEndObject();
            json.WriteEndObject();

            // The publisher is declared once, on the front page. Repeating a full
            // Organization on every page would say the same thing many times and let the
            // copies disagree; every other page refers to it by @id instead.
            if (isHome)
            {
                json.WriteStartObject();
                json.WriteString("@type", "Organization");
                json.WriteString("@id", organizationId);
                json.WriteString("url", homeUrl);
                json.WriteString("name", siteName);
                if (logoUrl is { Length: > 0 })
                {
                    json.WriteStartObject("logo");
                    json.WriteString("@type", "ImageObject");
                    json.WriteString("url", logoUrl);
                    json.WriteEndObject();
                }

                json.WriteEndObject();
            }

            json.WriteStartObject();
            json.WriteString("@type", "WebPage");
            json.WriteString("@id", $"{pageUrl}#webpage");
            json.WriteString("url", pageUrl);
            json.WriteString("name", title);
            if (description is { Length: > 0 })
            {
                json.WriteString("description", description);
            }

            json.WriteString("inLanguage", lang);
            json.WriteStartObject("isPartOf");
            json.WriteString("@id", websiteId);
            json.WriteEndObject();
            json.WriteEndObject();

            // A breadcrumb for the front page would be a trail of one, which says nothing.
            if (trail.Count > 0)
            {
                json.WriteStartObject();
                json.WriteString("@type", "BreadcrumbList");
                json.WriteString("@id", $"{pageUrl}#breadcrumb");
                json.WriteStartArray("itemListElement");
                var position = 1;
                foreach (var (name, url) in trail)
                {
                    WriteListItem(json, position++, name, url);
                }

                WriteListItem(json, position, title, pageUrl);
                json.WriteEndArray();
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteListItem(Utf8JsonWriter json, int position, string name, string url)
    {
        json.WriteStartObject();
        json.WriteString("@type", "ListItem");
        json.WriteNumber("position", position);
        json.WriteString("name", name);
        json.WriteString("item", url);
        json.WriteEndObject();
    }
}
