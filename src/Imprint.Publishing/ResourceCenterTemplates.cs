using System.Globalization;
using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// The resource center's four server-rendered pieces: the grouped index (<c>rc-hub</c>), an article's
/// header (<c>rc-article-head</c>), its document record (<c>rc-record</c>) and its contents
/// (<c>rc-toc</c>). Each is rendered from the widget's OWN props at page-render time, so the published
/// HTML carries every word and link with no script (see <c>WidgetDescriptor.PropTemplate</c>); the
/// index's tabs and product filter are a progressive enhancement over markup that already lists
/// everything, grouped by kind.
/// </summary>
/// <remarks>
/// <para>The design is <c>~/Hentet/Canine Resource Center.html</c> (v2), cut by the gatekeeping review
/// of 2026-10-25: no paper codes, no public old versions, no search palette and no related panel yet.</para>
/// <para>★ A SIGNER IS A NAME OR NOTHING. The publisher resolves the approving login to the site
/// reviewer's name; a login that is not the reviewer's reaches this file as an empty string, and the
/// record then says "signed with a login". An email address never reaches a template.</para>
/// </remarks>
public static class ResourceCenterTemplates
{
    public const string Hub = "rc-hub";
    public const string ArticleHead = "rc-article-head";
    public const string Record = "rc-record";
    public const string Toc = "rc-toc";

    /// <summary>Index order and the words for each kind: a card's label, and its group's heading.</summary>
    private static readonly (string Kind, string Word, string Group)[] Kinds =
    [
        ("essay", "Thought leadership", "Thought leadership"),
        ("whitepaper", "Whitepaper", "Whitepapers"),
        ("guide", "Guide", "Guides"),
        ("documentation", "Documentation", "Documentation"),
    ];

    private static readonly Dictionary<string, string> ProductNames = new(StringComparer.Ordinal)
    {
        ["canine"] = "Canine Development",
        ["assert"] = "Assert",
        ["assure"] = "Assure",
        ["assay"] = "Assay",
        ["cca"] = "CCA",
        ["cai"] = "Code Assurance Index",
    };

    /// <summary>The filter chips, in the header's order; the company is "all products".</summary>
    private static readonly string[] FilterProducts = ["assert", "assure", "assay", "cca"];

    public static string? Render(string? name, Func<string, string?> props) => name switch
    {
        Hub => RenderHub(props),
        ArticleHead => RenderArticleHead(props),
        Record => RenderRecord(props),
        Toc => RenderToc(props),
        _ => null,
    };

    public static string KindWord(string kind) =>
        Kinds.FirstOrDefault(k => k.Kind == kind).Word ?? "Article";

    // ------------------------------------------------------------------ the index

    private static string? RenderHub(Func<string, string?> props)
    {
        var articles = Root(props("articles")) is { ValueKind: JsonValueKind.Array } root
            ? root.EnumerateArray().Where(a => Str(a, "title").Length > 0 && Str(a, "href").Length > 0).ToList()
            : [];

        var html = new StringBuilder();
        html.Append("<div class=\"rc-hub\">");

        html.Append("<section class=\"rc-hero\">");
        AppendIf(html, "<p class=\"rc-eyebrow\">{0}</p>", props("eyebrow"));
        var heading = props("heading") ?? "";
        var emphasis = props("emphasis") ?? "";
        if (heading.Length > 0 || emphasis.Length > 0)
        {
            html.Append("<h1>").Append(Esc(heading));
            if (emphasis.Length > 0)
            {
                html.Append(heading.Length > 0 ? " " : "").Append("<em>").Append(Esc(emphasis)).Append("</em>");
            }

            html.Append("</h1>");
        }

        AppendIf(html, "<p class=\"rc-lede\">{0}</p>", props("lede"));
        html.Append("</section>");

        // The controls. Hidden until the enhancement script runs (.rc-hub.rc-live): without it they
        // would be buttons that do nothing, and every article is already listed below, grouped.
        html.Append("<div class=\"rc-filters\" id=\"rc-index\">");
        html.Append("<div class=\"rc-tabs\" role=\"tablist\" aria-label=\"Content type\">");
        html.Append("<button type=\"button\" role=\"tab\" class=\"rc-tab\" data-kind=\"all\" aria-selected=\"true\">All<span class=\"rc-n\">")
            .Append(articles.Count.ToString(CultureInfo.InvariantCulture)).Append("</span></button>");
        foreach (var (kind, _, group) in Kinds)
        {
            var n = articles.Count(a => Str(a, "kind") == kind);
            html.Append("<button type=\"button\" role=\"tab\" class=\"rc-tab\" data-kind=\"").Append(kind)
                .Append("\" aria-selected=\"false\">").Append(Esc(group)).Append("<span class=\"rc-n\">")
                .Append(n.ToString(CultureInfo.InvariantCulture)).Append("</span></button>");
        }

        html.Append("</div><div class=\"rc-prods\" role=\"group\" aria-label=\"Product\">");
        html.Append("<button type=\"button\" class=\"rc-chip p-canine\" data-product=\"all\" aria-pressed=\"true\">")
            .Append(Mark("canine")).Append("All products</button>");
        foreach (var product in FilterProducts)
        {
            html.Append("<button type=\"button\" class=\"rc-chip p-").Append(product).Append("\" data-product=\"")
                .Append(product).Append("\" aria-pressed=\"false\">").Append(Mark(product))
                .Append(Esc(ProductNames[product])).Append("</button>");
        }

        html.Append("</div></div>");
        html.Append("<p class=\"rc-filterbar\" role=\"status\" hidden></p>");

        foreach (var (kind, _, group) in Kinds)
        {
            var inGroup = articles.Where(a => Str(a, "kind") == kind).ToList();
            if (inGroup.Count == 0)
            {
                continue;
            }

            html.Append("<section class=\"rc-group\" data-kind=\"").Append(kind).Append("\">");
            html.Append("<div class=\"rc-group-h\"><h2>").Append(Esc(group)).Append("</h2><span class=\"rc-eyebrow rc-count\">")
                .Append(inGroup.Count.ToString(CultureInfo.InvariantCulture)).Append("</span></div>");
            html.Append("<ul class=\"rc-list\">");
            foreach (var article in inGroup)
            {
                AppendCard(html, article, kind);
            }

            html.Append("</ul></section>");
        }

        html.Append("<p class=\"rc-empty\"").Append(articles.Count == 0 ? "" : " hidden").Append('>')
            .Append(articles.Count == 0 ? "Nothing has been published here yet." : "Nothing of this kind for that product yet.")
            .Append("</p>");
        html.Append("</div>");
        return html.ToString();
    }

    private static void AppendCard(StringBuilder html, JsonElement article, string kind)
    {
        var href = SafeHref(Str(article, "href"));
        if (href is null)
        {
            return;
        }

        var products = Strings(article, "products").Where(ProductNames.ContainsKey).ToList();
        var accent = products.FirstOrDefault(p => p != "canine") ?? "canine";
        html.Append("<li class=\"rc-card c-").Append(kind).Append(" p-").Append(accent)
            .Append("\" data-kind=\"").Append(kind).Append("\" data-products=\"").Append(string.Join(' ', products)).Append("\">");
        html.Append("<a href=\"").Append(Esc(href)).Append("\">");

        if (kind == "whitepaper")
        {
            html.Append("<span class=\"rc-sheethead\"><span>").Append(Esc(VersionText(article))).Append("</span><span>")
                .Append(Esc(Str(article, "date"))).Append("</span></span>");
            html.Append("<span class=\"rc-cardbody\"><span class=\"rc-rule\"></span>");
            AppendTitleAndDek(html, article);
            html.Append("</span>");
            var sha = Str(article, "sha256");
            html.Append("<span class=\"rc-seal\"><span>sha256</span><b>")
                .Append(Esc(sha.Length >= 24 ? $"{sha[..16]}…{sha[^8..]}" : sha)).Append("</b><span>status</span><b>")
                .Append(Esc(StatusWord(article))).Append("</b></span>");
        }
        else
        {
            html.Append("<span class=\"rc-cardtop\"><span class=\"rc-kind\">").Append(Esc(KindWord(kind))).Append("</span>");
            AppendProductTags(html, products);
            html.Append("</span>");
            if (kind == "documentation")
            {
                html.Append("<span class=\"rc-path\">").Append(Esc(href)).Append("</span>");
            }

            AppendTitleAndDek(html, article);
            html.Append("<span class=\"rc-meta\"><span>").Append(Esc(VersionText(article))).Append("</span>");
            if (Str(article, "date") is { Length: > 0 } date)
            {
                html.Append("<span>").Append(Esc(date)).Append("</span>");
            }

            html.Append("<span class=\"rc-st ").Append(Flag(article, "signed") ? "signed" : "published").Append("\">")
                .Append(Esc(StatusWord(article))).Append("</span></span>");
        }

        html.Append("</a></li>");
    }

    private static void AppendTitleAndDek(StringBuilder html, JsonElement article)
    {
        html.Append("<h3>").Append(Esc(Str(article, "title"))).Append("</h3>");
        if (Str(article, "dek") is { Length: > 0 } dek)
        {
            html.Append("<span class=\"rc-dek\">").Append(Esc(dek)).Append("</span>");
        }
    }

    // ------------------------------------------------------------------ an article's header

    private static string? RenderArticleHead(Func<string, string?> props)
    {
        var title = props("title") ?? "";
        if (title.Length == 0)
        {
            return null;
        }

        var kind = props("kind") ?? "";
        var products = Csv(props("products"));
        var html = new StringBuilder();
        html.Append("<header class=\"rc-art-head").Append(kind == "whitepaper" ? " rc-paperhead" : "").Append("\">");
        if (SafeHref(props("back")) is { } back)
        {
            html.Append("<a class=\"rc-back\" href=\"").Append(Esc(back)).Append("\">All resources</a>");
        }

        html.Append("<span class=\"rc-bar\"></span><div class=\"rc-topline\"><span class=\"rc-kind\">").Append(Esc(KindWord(kind))).Append("</span>");
        AppendProductTags(html, products);
        var signed = (props("signed") ?? "") == "true";
        html.Append("<span class=\"rc-st ").Append(signed ? "signed" : "published").Append("\">")
            .Append(signed ? "Signed" : "Published").Append("</span></div>");
        html.Append("<h1>").Append(Esc(title)).Append("</h1>");
        AppendIf(html, "<p class=\"rc-dek\">{0}</p>", props("dek"));

        html.Append("<p class=\"rc-byline\">");
        AppendIf(html, "<span>By {0}</span>", props("byline"));
        var version = props("version") ?? "";
        var date = props("date") ?? "";
        if (version.Length > 0 || date.Length > 0)
        {
            html.Append("<span>").Append(Esc(string.Join(" · ", new[] { version.Length > 0 ? $"version {version}" : "", date }.Where(s => s.Length > 0)))).Append("</span>");
        }

        if (props("signer") is { Length: > 0 } signer)
        {
            html.Append("<span>Signed by ").Append(Esc(signer)).Append("</span>");
        }

        html.Append("</p></header>");
        return html.ToString();
    }

    // ------------------------------------------------------------------ the document record

    private static string? RenderRecord(Func<string, string?> props)
    {
        var sha = props("sha256") ?? "";
        var version = props("version") ?? "";
        if (sha.Length == 0 && version.Length == 0)
        {
            return null;
        }

        var signed = (props("signed") ?? "") == "true";
        var html = new StringBuilder();
        html.Append("<section class=\"rc-record\" aria-label=\"Document record\">");
        html.Append("<div class=\"rc-rh\"><b>Document record</b><span class=\"rc-st ").Append(signed ? "signed" : "published").Append("\">")
            .Append(signed ? "Signed" : "Published").Append("</span></div><dl>");
        if (version.Length > 0)
        {
            html.Append("<dt>Version</dt><dd>").Append(Esc(version)).Append("</dd>");
        }

        AppendIf(html, "<dt>Published</dt><dd>{0}</dd>", props("date"));
        if (signed)
        {
            var signer = props("signer") ?? "";
            html.Append("<dt>Signed by</dt><dd>").Append(signer.Length > 0 ? Esc(signer) : "A named person")
                .Append("<span class=\"rc-rec-line\">signed with a login");
            if (props("signed-at") is { Length: > 0 } at)
            {
                html.Append(" · ").Append(Esc(at));
            }

            html.Append("</span></dd>");
        }

        if (sha.Length > 0)
        {
            html.Append("<dt>sha256</dt><dd class=\"rc-mono\">").Append(Esc(sha.Length > 32 ? sha[..32] : sha));
            if (sha.Length > 32)
            {
                html.Append("<br />").Append(Esc(sha[32..]));
            }

            html.Append("</dd>");
        }

        html.Append("</dl>");
        if (props("controlled") is "true")
        {
            html.Append("<p class=\"rc-claims\">Publication control notes are signed with the text.</p>");
        }

        html.Append("</section>");
        return html.ToString();
    }

    // ------------------------------------------------------------------ contents

    private static string? RenderToc(Func<string, string?> props)
    {
        var headings = Root(props("headings")) is { ValueKind: JsonValueKind.Array } root
            ? root.EnumerateArray().Where(h => Str(h, "id").Length > 0 && Str(h, "text").Length > 0).ToList()
            : [];
        if (headings.Count < 2)
        {
            return null;   // a contents list of one entry is a heading repeated, not a way through
        }

        var html = new StringBuilder();
        html.Append("<nav class=\"rc-toc\" aria-label=\"On this page\"><p class=\"rc-eyebrow\">On this page</p><ol>");
        foreach (var heading in headings)
        {
            html.Append("<li><a href=\"#").Append(Esc(Str(heading, "id"))).Append("\">").Append(Esc(Str(heading, "text"))).Append("</a></li>");
        }

        html.Append("</ol></nav>");
        return html.ToString();
    }

    // ------------------------------------------------------------------ shared pieces

    private static void AppendProductTags(StringBuilder html, IEnumerable<string> products)
    {
        foreach (var product in products.Where(ProductNames.ContainsKey))
        {
            html.Append("<span class=\"rc-prodtag p-").Append(product).Append("\">").Append(Mark(product))
                .Append(Esc(ProductNames[product])).Append("</span>");
        }
    }

    private static string VersionText(JsonElement article) =>
        Number(article, "version") is { } v ? $"version {v.ToString(CultureInfo.InvariantCulture)}" : "";

    private static string StatusWord(JsonElement article) => Flag(article, "signed") ? "Signed" : "Published";

    private static IReadOnlyList<string> Csv(string? value) =>
        [.. (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private static void AppendIf(StringBuilder html, string format, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            html.Append(string.Format(CultureInfo.InvariantCulture, format, Esc(value)));
        }
    }

    /// <summary>Only a site-relative path; anything else (a scheme, a protocol-relative URL) is refused.</summary>
    private static string? SafeHref(string? value) =>
        value is { Length: > 0 } v && v.StartsWith('/') && !v.StartsWith("//", StringComparison.Ordinal) ? v : null;

    /// <summary>The family mark: braces in ink holding the product's sign in its colour (CSS var --{product}).</summary>
    public static string Mark(string product)
    {
        var sign = product switch
        {
            "assert" => "<path d=\"M21.5 32.5 L28.5 39.5 L42.5 24.5\" fill=\"none\" stroke=\"var(--assert)\" stroke-width=\"4.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            "assure" => "<circle cx=\"32\" cy=\"32\" r=\"7.6\" fill=\"var(--assure)\"/>",
            "assay" => "<g><circle cx=\"29.5\" cy=\"29.5\" r=\"8\" fill=\"none\" stroke=\"var(--assay)\" stroke-width=\"3.7\"/><path d=\"M35.6 35.6 L41.5 41.5\" stroke=\"var(--assay)\" stroke-width=\"4.4\" stroke-linecap=\"round\"/></g>",
            "cca" => "<g><path d=\"M20.2 41.5 A13 13 0 1 1 43.8 41.5\" fill=\"none\" stroke=\"var(--cca)\" stroke-width=\"3.8\" stroke-linecap=\"round\"/><path d=\"M32 38 L38.5 29\" stroke=\"var(--cca)\" stroke-width=\"3.4\" stroke-linecap=\"round\"/><circle cx=\"32\" cy=\"38\" r=\"3.2\" fill=\"var(--cca)\"/></g>",
            "canine" => "<g><path d=\"M35 22 L35 43 M40 22 L40 43\" stroke=\"var(--company)\" stroke-width=\"3.4\" stroke-linecap=\"round\"/><path d=\"M35 22 L29.5 22 C25 22 23 25 23 28.5 C23 32 25 35 29.5 35 L35 35\" fill=\"var(--company)\" stroke=\"var(--company)\" stroke-width=\"3\" stroke-linejoin=\"round\"/><path d=\"M33 22 L42 22\" stroke=\"var(--company)\" stroke-width=\"3.4\" stroke-linecap=\"round\"/></g>",
            _ => null,   // the Code Assurance Index stands apart from Canine: no family mark
        };
        if (sign is null)
        {
            return "";
        }

        const string braces = "<path d=\"M13 10 C7 10 7 14 7 19 L7 25 C7 29 4 32 1 32 C4 32 7 35 7 39 L7 45 C7 50 7 54 13 54\" fill=\"none\" stroke=\"var(--rc-ink)\" stroke-width=\"5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M51 10 C57 10 57 14 57 19 L57 25 C57 29 60 32 63 32 C60 32 57 35 57 39 L57 45 C57 50 57 54 51 54\" fill=\"none\" stroke=\"var(--rc-ink)\" stroke-width=\"5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>";
        return "<svg class=\"rc-mark\" viewBox=\"-1 6 66 52\" aria-hidden=\"true\" focusable=\"false\">" + braces
            + "<g transform=\"translate(32 32) scale(1.3) translate(-32 -32)\">" + sign + "</g></svg>";
    }
}
