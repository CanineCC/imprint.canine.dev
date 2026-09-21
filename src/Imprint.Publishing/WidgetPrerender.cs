using System.Net;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Imprint.Publishing;

/// <summary>
/// Reduces a fetched widget fragment to the semantic subset that is safe to bake into a published
/// page — headings, paragraphs, lists, tables and inline emphasis, and nothing else.
/// </summary>
/// <remarks>
/// <para>★ THE OUTPUT GOES INTO OUR OWN PUBLISHED HTML, so this is a trust boundary even though the
/// far end is our own service. A fragment arrives as a whole document with its own
/// <c>&lt;style&gt;</c> and <c>&lt;script&gt;</c>; inlining that verbatim would execute another
/// origin's script inside our page and dump its class names into our stylesheet's namespace. Both
/// are dropped outright — element by element, against an allowlist, because a denylist is a promise
/// to have thought of everything.</para>
/// <para>★ ATTRIBUTES ARE DROPPED WHOLESALE, including class. The bake is read by crawlers and
/// language models, not painted: it needs the words and their structure, and every attribute is
/// either irrelevant to that or a way for markup we did not write to reach into a page we did.
/// The one exception is <c>href</c> on a link, kept only when it is absolute https.</para>
/// <para>What this is NOT: a general-purpose HTML sanitiser. It is deliberately narrow — if a tag is
/// not in <see cref="Keep"/> its markup is discarded and its text kept, so an unknown wrapper costs a
/// little structure and never a security property.</para>
/// </remarks>
public static class WidgetPrerender
{
    /// <summary>Elements whose markup survives. Everything else contributes its text only.</summary>
    private static readonly HashSet<string> Keep = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1", "h2", "h3", "h4", "h5", "h6",
        "p", "ul", "ol", "li", "dl", "dt", "dd",
        "table", "thead", "tbody", "tr", "th", "td", "caption",
        "strong", "em", "b", "i", "small", "sup", "sub", "a", "br",
    };

    /// <summary>Elements whose CONTENT is discarded too — script and style are not text.</summary>
    private static readonly HashSet<string> Drop = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "template", "noscript", "svg", "head", "iframe", "object", "embed",
    };

    /// <summary>The cap on a single bake. A fragment larger than this is not a fragment.</summary>
    public const int MaxCharacters = 24_000;

    /// <summary>
    /// The semantic reduction of <paramref name="html"/>, or null when nothing survived — an empty
    /// bake must not be emitted, because an empty element is exactly the state this feature exists
    /// to remove.
    /// </summary>
    public static string? Reduce(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Drop the elements whose content is not prose, comments included, before anything else
        // looks at the markup.
        var body = Regex.Replace(html, "<!--.*?-->", " ", RegexOptions.Singleline);
        foreach (var tag in Drop)
        {
            body = Regex.Replace(
                body, $"<{tag}\\b[^>]*>.*?</{tag}\\s*>", " ",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // A self-closed or unclosed instance leaves no content to drop — remove the tag itself.
            body = Regex.Replace(body, $"</?{tag}\\b[^>]*>", " ", RegexOptions.IgnoreCase);
        }

        var output = new StringBuilder();
        foreach (Match token in Regex.Matches(body, "<[^>]+>|[^<]+"))
        {
            var text = token.Value;
            if (text.Length == 0)
            {
                continue;
            }

            if (text[0] != '<')
            {
                output.Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text)));
                continue;
            }

            var closing = text.StartsWith("</", StringComparison.Ordinal);
            var name = Regex.Match(text, "^</?\\s*([A-Za-z][A-Za-z0-9]*)").Groups[1].Value;
            if (name.Length == 0 || !Keep.Contains(name))
            {
                // An unknown or unwanted wrapper: its text is already being kept by the branch
                // above, so dropping the tag costs structure and nothing else.
                output.Append(' ');
                continue;
            }

            var lower = name.ToLowerInvariant();
            if (closing)
            {
                output.Append("</").Append(lower).Append('>');
                continue;
            }

            if (lower == "br")
            {
                output.Append("<br>");
                continue;
            }

            // Only an absolute https href crosses. Everything else — every other attribute on every
            // other element — is dropped; see the type remarks.
            if (lower == "a"
                && Regex.Match(text, "href\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase) is { Success: true } href
                && Uri.TryCreate(WebUtility.HtmlDecode(href.Groups[1].Value), UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps)
            {
                output.Append("<a href=\"").Append(WebUtility.HtmlEncode(uri.ToString())).Append("\">");
                continue;
            }

            output.Append('<').Append(lower).Append('>');
        }

        var reduced = Regex.Replace(output.ToString(), "\\s+", " ").Trim();

        // Structure with no words in it is not worth baking, and is indistinguishable from a
        // fetch that returned a shell.
        var words = Regex.Replace(reduced, "<[^>]+>", " ").Trim();
        if (words.Length == 0)
        {
            return null;
        }

        return reduced.Length > MaxCharacters ? reduced[..MaxCharacters] : reduced;
    }
}

/// <summary>Fetches a widget fragment at publish time. Injected so a publish can be tested — and
/// run — without reaching the network.</summary>
public interface IWidgetPrerenderSource
{
    /// <summary>The fragment's HTML, or null when it could not be fetched. Must not throw.</summary>
    Task<string?> FetchAsync(Uri url, CancellationToken ct);
}

/// <summary>
/// The real fetcher: one short-timeout GET per distinct URL, failures swallowed into null.
/// </summary>
/// <remarks>
/// ★ A PUBLISH MUST NEVER FAIL BECAUSE A FRAGMENT DID NOT ARRIVE. The bake is an enhancement to the
/// published HTML; the site is correct without it. So every failure — timeout, 500, DNS, a body that
/// is not HTML — returns null, the element renders as it always did, and the publish proceeds. The
/// cost of that choice is that a fetch outage is invisible in the output, which is why it is logged
/// at warning with the URL.
/// </remarks>
public sealed class HttpWidgetPrerenderSource(HttpClient http, ILogger<HttpWidgetPrerenderSource> logger)
    : IWidgetPrerenderSource
{
    public async Task<string?> FetchAsync(Uri url, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(url);
        try
        {
            using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Prerender fetch for {Url} returned {Status}; the widget publishes without its bake.",
                    url, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(e, "Prerender fetch for {Url} failed; the widget publishes without its bake.", url);
            return null;
        }
    }
}
