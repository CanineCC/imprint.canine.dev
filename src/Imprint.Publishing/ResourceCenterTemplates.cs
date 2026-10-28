using System.Globalization;
using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// The resource center's server-rendered pieces: the index (<c>rc-hub</c>), an article's header
/// (<c>rc-article-head</c>), its left column (<c>rc-toc</c>: contents, the documentation tree or a guide's
/// steps), its right column (<c>rc-context</c>: the document record with its versions, and Go deeper), and
/// the site header. Each is rendered from the widget's OWN props at page-render time, so the published HTML
/// carries every word and link with no script (see <c>WidgetDescriptor.PropTemplate</c>); the islands
/// (<c>rc-filter</c>, <c>rc-reader</c>, <c>rc-search</c>) only enhance markup that already works.
/// </summary>
/// <remarks>
/// <para>The design is <c>~/Hentet/Canine Resource Center.html</c> (v2). Round 2 (owner, 2026-10-28) builds
/// the parts the first cut deferred: search, Go deeper, the bento, guide ticks, the hand-off and the version
/// list. The version list names versions and dates only; no old text is served (ARTICLE-BAR §1).</para>
/// <para>★ A TEMPLATE HERE NEVER RENDERS NOTHING. A templated widget that returns null is not baked, and an
/// unbaked widget is styled as a loading placeholder: the dashed empty box the owner found beside a draft on
/// 2026-10-28. The record and the left column therefore always say something, a draft included.</para>
/// <para>★ A SIGNER IS A NAME OR NOTHING. The publisher resolves the approving login to the site
/// reviewer's name; a login that is not the reviewer's reaches this file as an empty string, and the
/// record then says "A named person". An email address never reaches a template.</para>
/// </remarks>
public static class ResourceCenterTemplates
{
    public const string Hub = "rc-hub";
    public const string ArticleHead = "rc-article-head";
    public const string Context = "rc-context";
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

    /// <summary>The filter chips and the header's product links, in the design's order.</summary>
    private static readonly string[] FilterProducts = ["assert", "assure", "assay", "cca"];

    private static readonly Dictionary<string, string> StatusWords = new(StringComparer.Ordinal)
    {
        ["signed"] = "Signed",
        ["published"] = "Published",
        ["draft"] = "Draft",
        ["review"] = "In review",
        ["changes"] = "Changes requested",
        ["approved"] = "Approved",
    };

    public static string? Render(string? name, Func<string, string?> props) => name switch
    {
        Hub => RenderHub(props),
        ArticleHead => RenderArticleHead(props),
        Context => RenderContext(props),
        Toc => RenderToc(props),
        _ => null,
    };

    public static string KindWord(string kind) =>
        Kinds.FirstOrDefault(k => k.Kind == kind).Word ?? "Article";

    public static string ProductName(string product) => ProductNames.GetValueOrDefault(product, product);

    // ------------------------------------------------------------------ the site header

    /// <summary>
    /// The resource center's header, left of the actions: the site's own mark (the family braces holding a
    /// pilcrow), its name and "by Canine Development", and a link per product to the index filtered to it.
    /// </summary>
    public static string HeaderBrand(string siteName, string homeHref, string indexHref)
    {
        var html = new StringBuilder();
        html.Append("<a class=\"ip-brand rc-brand\" href=\"").Append(Esc(homeHref)).Append("\">").Append(Mark("canine"))
            .Append("<span>").Append(Esc(siteName)).Append("</span> <small class=\"rc-by\">by Canine Development</small></a>");
        html.Append("<nav class=\"rc-nav\" aria-label=\"Products\">");
        foreach (var product in FilterProducts)
        {
            html.Append("<a class=\"p-").Append(product).Append("\" href=\"").Append(Esc(indexHref)).Append("?product=").Append(product)
                .Append("#rc-index\">").Append(Mark(product)).Append(Esc(ProductNames[product])).Append("</a>");
        }

        html.Append("</nav>");
        return html.ToString();
    }

    /// <summary>The search button among the header's actions; shown once the palette's island is live.</summary>
    public static string HeaderSearch() =>
        "<button type=\"button\" class=\"rc-kbtn\" data-rc-search aria-label=\"Search\">" + Icon("search", 15)
        + "<span class=\"rc-lbl\">Search</span><span class=\"rc-kbd\">⌘K</span></button>";

    // ------------------------------------------------------------------ the index

    private static string? RenderHub(Func<string, string?> props)
    {
        var articles = Root(props("articles")) is { ValueKind: JsonValueKind.Array } root
            ? root.EnumerateArray().Where(a => Str(a, "title").Length > 0 && SafeHref(Str(a, "href")) is not null).ToList()
            : [];

        var html = new StringBuilder();
        html.Append("<div class=\"rc-hub\">");

        html.Append("<section class=\"rc-hero\"><div class=\"rc-hero-main\">");
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
        // The search field. A button, because what it opens is the palette (rc-search); shown once that
        // island is live, and until then it holds its place so nothing shifts when it appears.
        html.Append("<button type=\"button\" class=\"rc-omni\" data-rc-search aria-label=\"Search everything\">").Append(Icon("search", 19))
            .Append("<span class=\"rc-ph\">Search essays, papers, guides and docs</span><span class=\"rc-scope\">")
            .Append("<span>essays</span><span>papers</span><span>guides</span><span>docs</span></span><span class=\"rc-kbd\">⌘K</span></button>");
        html.Append("</div>");

        if (articles.Count > 0)
        {
            html.Append("<nav class=\"rc-latest\" aria-label=\"Latest\"><span class=\"rc-eyebrow\">Latest</span>");
            foreach (var article in articles.Take(5))
            {
                var accent = Accent(article);
                html.Append("<a class=\"p-").Append(accent).Append("\" href=\"").Append(Esc(Str(article, "href"))).Append("\">")
                    .Append(Mark(accent)).Append("<b>").Append(Esc(Str(article, "title"))).Append("</b><span class=\"rc-d\">")
                    .Append(Esc(KindWord(Str(article, "kind")))).Append("</span></a>");
            }

            html.Append("</nav>");
        }

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
                .Append("\" aria-selected=\"false\">").Append(KindIcon(kind)).Append(Esc(group)).Append("<span class=\"rc-n\">")
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

        AppendBento(html, articles);

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
                AppendCard(html, article, kind, feature: false);
            }

            html.Append("</ul></section>");
        }

        html.Append("<p class=\"rc-empty\"").Append(articles.Count == 0 ? "" : " hidden").Append('>')
            .Append(articles.Count == 0 ? "Nothing has been published here yet." : "Nothing of this kind for that product yet.")
            .Append("</p>");
        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>
    /// The featured cards over the grouped lists, in the design's six places: the newest essay large, then
    /// the newest whitepaper, guide, the next essay, the newest documentation page and the next whitepaper.
    /// Chosen by recency, so there is nothing to curate; an empty place is skipped. Hidden while a filter is
    /// on (the lists below then answer it).
    /// </summary>
    private static void AppendBento(StringBuilder html, List<JsonElement> articles)
    {
        if (articles.Count < 3)
        {
            return;
        }

        JsonElement? Nth(string kind, int n) =>
            articles.Where(a => Str(a, "kind") == kind).Skip(n).Cast<JsonElement?>().FirstOrDefault();

        var places = new[] { Nth("essay", 0), Nth("whitepaper", 0), Nth("guide", 0), Nth("essay", 1), Nth("documentation", 0), Nth("whitepaper", 1) };
        html.Append("<div class=\"rc-bento\"><ul class=\"rc-bento-grid\" aria-label=\"Featured\">");
        var first = true;
        foreach (var place in places)
        {
            if (place is not { } article)
            {
                continue;
            }

            AppendCard(html, article, Str(article, "kind"), feature: first && Str(article, "kind") == "essay");
            first = false;
        }

        html.Append("</ul></div>");
    }

    private static void AppendCard(StringBuilder html, JsonElement article, string kind, bool feature)
    {
        var href = SafeHref(Str(article, "href"));
        if (href is null)
        {
            return;
        }

        var products = Strings(article, "products").Where(ProductNames.ContainsKey).ToList();
        var accent = Accent(article);
        html.Append("<li class=\"rc-card c-").Append(kind).Append(" p-").Append(accent).Append(feature ? " rc-feature" : "")
            .Append("\" data-kind=\"").Append(kind).Append("\" data-products=\"").Append(string.Join(' ', products)).Append("\">");
        html.Append("<a href=\"").Append(Esc(href)).Append("\">");

        if (kind == "whitepaper")
        {
            html.Append("<span class=\"rc-sheethead\"><span>").Append(Esc(VersionText(article)));
            if (Number(article, "words") is { } words and > 0)
            {
                html.Append(" · ").Append((words / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)).Append("k words");
            }

            html.Append("</span><span>").Append(Esc(Str(article, "date"))).Append("</span></span>");
            html.Append("<span class=\"rc-cardbody\"><span class=\"rc-rule\"></span>");
            AppendTitleAndDek(html, article);
            html.Append("</span>");
            var sha = Str(article, "sha256");
            html.Append("<span class=\"rc-seal\">");
            if (sha.Length > 0)
            {
                html.Append("<span>sha256</span><b>").Append(Esc(sha.Length >= 24 ? $"{sha[..16]}…{sha[^8..]}" : sha)).Append("</b>");
            }

            html.Append("<span>status</span><b>").Append(Esc(StatusWord(Status(article)))).Append("</b></span>");
            html.Append("</a></li>");
            return;
        }

        if (feature && Strings(article, "sections") is { Count: > 1 } sections)
        {
            html.Append("<ol class=\"rc-qmarks\">");
            foreach (var section in sections)
            {
                html.Append("<li>").Append(Esc(section)).Append("</li>");
            }

            html.Append("</ol>");
        }

        html.Append("<span class=\"rc-cardtop\"><span class=\"rc-kind\">").Append(KindIcon(kind)).Append(Esc(KindWord(kind))).Append("</span>");
        AppendProductTags(html, products);
        html.Append("</span>");
        if (kind == "documentation")
        {
            html.Append("<span class=\"rc-path\">").Append(Esc(href)).Append("</span>");
        }

        AppendTitleAndDek(html, article);
        if (kind == "guide" && Number(article, "steps") is { } steps and > 0)
        {
            html.Append("<span class=\"rc-steps\" aria-hidden=\"true\">");
            for (var i = 0; i < Math.Min(steps, 12); i++)
            {
                html.Append("<i style=\"--i:").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\"></i>");
            }

            html.Append("</span>");
        }

        html.Append("<span class=\"rc-meta\">");
        var minutes = Minutes(article);
        if (kind == "guide" && Number(article, "steps") is { } n and > 0)
        {
            html.Append("<span>").Append(n.ToString(CultureInfo.InvariantCulture)).Append(" steps · ").Append(minutes).Append("</span>");
        }
        else if (kind == "essay")
        {
            html.Append("<span>").Append(minutes).Append(" read</span>");
        }

        html.Append("<span class=\"rc-rec-line\"><span class=\"rc-st ").Append(Status(article)).Append("\">")
            .Append(Esc(StatusWord(Status(article)))).Append("</span>");
        if (VersionText(article) is { Length: > 0 } version)
        {
            html.Append("<span>").Append(Esc(version)).Append("</span>");
        }

        html.Append("</span>").Append(Icon("arrow-right", 15, "rc-go")).Append("</span>");
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
        var status = props("status") is { Length: > 0 } s ? s : "published";
        var version = props("version") ?? "";
        var date = props("date") ?? "";
        var signer = props("signer") ?? "";
        var read = props("read") ?? "";
        var html = new StringBuilder();

        if (kind == "whitepaper")
        {
            html.Append("<header class=\"rc-art-head rc-paperhead\"><p class=\"rc-sheetline\">");
            AppendIf(html, "<span>{0}</span>", version.Length > 0 ? $"v{version}" : StatusWord(status));
            if (int.TryParse(props("words"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var words) && words > 0)
            {
                html.Append("<span>").Append((words / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)).Append("k words</span>");
            }

            AppendIf(html, "<span>{0}</span>", date);
            html.Append("</p>");
            AppendTopline(html, kind, products, status);
            html.Append("<h1>").Append(Esc(title)).Append("</h1>");
            AppendIf(html, "<p class=\"rc-dek rc-sub\">{0}</p>", props("dek"));
            html.Append("<p class=\"rc-byline\"><span>Canine Development</span>");
            if (version.Length > 0 && props("sha256") is { Length: >= 12 } sha)
            {
                html.Append("<span>Cite as ").Append(Esc(props("cite") ?? "")).Append(" v").Append(Esc(version))
                    .Append(", sha256 <span class=\"rc-mono\">").Append(Esc(sha[..12])).Append("</span></span>");
            }

            AppendIf(html, "<span>{0} read</span>", read);
            html.Append("</p></header>");
            return html.ToString();
        }

        html.Append("<header class=\"rc-art-head\">");
        AppendTopline(html, kind, products, status);
        html.Append("<h1>").Append(Esc(title)).Append("</h1>");
        AppendIf(html, "<p class=\"rc-dek\">{0}</p>", props("dek"));

        html.Append("<p class=\"rc-byline\">");
        if (kind == "essay")
        {
            AppendIf(html, "<span>By {0}</span>", props("byline"));
        }

        if (kind == "guide" && int.TryParse(props("steps"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var steps) && steps > 0)
        {
            html.Append("<span>").Append(steps.ToString(CultureInfo.InvariantCulture)).Append(" steps · about ").Append(Esc(read)).Append("</span>");
        }
        else if (kind == "documentation")
        {
            html.Append("<span>Reference</span>");
        }
        else
        {
            AppendIf(html, "<span>{0} read</span>", read);
        }

        if (version.Length > 0 || date.Length > 0)
        {
            html.Append("<span>").Append(Esc(string.Join(" · ", new[] { version.Length > 0 ? $"v{version}" : "", date }.Where(x => x.Length > 0)))).Append("</span>");
        }

        if (status == "signed" && signer.Length > 0)
        {
            html.Append("<span>Signed by ").Append(Esc(signer)).Append("</span>");
        }

        html.Append("</p></header>");
        return html.ToString();
    }

    private static void AppendTopline(StringBuilder html, string kind, IReadOnlyList<string> products, string status)
    {
        html.Append("<div class=\"rc-topline\"><span class=\"rc-kind\">").Append(KindIcon(kind)).Append(Esc(KindWord(kind))).Append("</span>");
        AppendProductTags(html, products);
        html.Append("<span class=\"rc-st ").Append(Esc(status)).Append("\">").Append(Esc(StatusWord(status))).Append("</span></div>");
    }

    // ------------------------------------------------------------------ the right column

    private static string RenderContext(Func<string, string?> props)
    {
        var status = props("status") is { Length: > 0 } s ? s : "published";
        var products = Csv(props("products"));
        var accent = products.FirstOrDefault(p => p != "canine" && ProductNames.ContainsKey(p)) ?? "canine";
        var html = new StringBuilder();
        html.Append("<div class=\"rc-ctx\">");

        if (Root(props("sections")) is { ValueKind: JsonValueKind.Array } onpage && onpage.GetArrayLength() > 1)
        {
            html.Append("<p class=\"rc-eyebrow\">On this page</p><nav class=\"rc-onpage\" aria-label=\"On this page\">");
            foreach (var heading in onpage.EnumerateArray().Where(h => Str(h, "id").Length > 0))
            {
                html.Append("<a href=\"#").Append(Esc(Str(heading, "id"))).Append("\">").Append(Esc(Str(heading, "text"))).Append("</a>");
            }

            html.Append("</nav>");
        }

        AppendRecord(html, props, status, accent);

        if (Root(props("related")) is { ValueKind: JsonValueKind.Array } related && related.GetArrayLength() > 0)
        {
            html.Append("<p class=\"rc-eyebrow rc-ch\">Go deeper</p>");
            foreach (var item in related.EnumerateArray())
            {
                if (SafeHref(Str(item, "href")) is not { } href || Str(item, "title").Length == 0)
                {
                    continue;
                }

                var itemAccent = Strings(item, "products").FirstOrDefault(p => p != "canine" && ProductNames.ContainsKey(p)) ?? "canine";
                html.Append("<a class=\"rc-ctx-item p-").Append(itemAccent).Append("\" href=\"").Append(Esc(href)).Append("\"><span class=\"rc-kind\">")
                    .Append(KindIcon(Str(item, "kind"))).Append(Esc(KindWord(Str(item, "kind")))).Append("</span><b>").Append(Esc(Str(item, "title")))
                    .Append("</b>");
                AppendIf(html, "<span class=\"rc-why\">{0}</span>", Str(item, "why"));
                html.Append("</a>");
            }
        }

        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>The document record: always, whatever state the article is in (see the class remarks).</summary>
    private static void AppendRecord(StringBuilder html, Func<string, string?> props, string status, string accent)
    {
        var version = props("version") ?? "";
        var sha = props("sha256") ?? "";
        var reviewer = props("reviewer") is { Length: > 0 } r ? r : "the reviewer";
        html.Append("<section class=\"rc-record p-").Append(accent).Append("\" aria-label=\"Document record\">");
        html.Append("<div class=\"rc-rh\"><b>Document record</b><span class=\"rc-st ").Append(Esc(status)).Append("\">")
            .Append(Esc(StatusWord(status))).Append("</span></div><dl>");
        html.Append("<dt>Version</dt><dd>").Append(version.Length > 0 ? "v" + Esc(version) : "Not yet published").Append("</dd>");
        AppendIf(html, "<dt>Published</dt><dd>{0}</dd>", props("date"));

        switch (status)
        {
            case "signed":
                var signer = props("signer") ?? "";
                html.Append("<dt>Signed by</dt><dd>").Append(signer.Length > 0 ? Esc(signer) : "A named person")
                    .Append("<span class=\"rc-rec-line\">signed with a login");
                if (props("signed-at") is { Length: > 0 } at)
                {
                    html.Append(" · ").Append(Esc(at));
                }

                html.Append("</span></dd>");
                break;
            case "published":
                html.Append("<dt>Signature</dt><dd>Not signed</dd>");
                break;
            case "review":
                html.Append("<dt>Signature</dt><dd>Awaiting ").Append(Esc(reviewer)).Append("<span class=\"rc-rec-line\">submitted for review</span></dd>");
                break;
            case "changes":
                html.Append("<dt>Signature</dt><dd>Changes requested by ").Append(Esc(reviewer)).Append("</dd>");
                break;
            case "approved":
                html.Append("<dt>Signature</dt><dd>Approved by ").Append(Esc(reviewer)).Append("<span class=\"rc-rec-line\">not yet published</span></dd>");
                break;
            default:
                html.Append("<dt>Signature</dt><dd>Not yet submitted</dd>");
                break;
        }

        html.Append("<dt>sha256</dt>");
        if (sha.Length > 0)
        {
            html.Append("<dd class=\"rc-mono\">").Append(Esc(sha.Length > 32 ? sha[..32] : sha));
            if (sha.Length > 32)
            {
                html.Append("<br />").Append(Esc(sha[32..]));
            }

            html.Append("</dd>");
        }
        else
        {
            html.Append("<dd class=\"rc-faint\">Fixed when it is signed</dd>");
        }

        html.Append("</dl>");

        if (Root(props("versions")) is { ValueKind: JsonValueKind.Array } versions && versions.GetArrayLength() > 1)
        {
            html.Append("<ol class=\"rc-versions\" aria-label=\"Versions\">");
            var first = true;
            foreach (var v in versions.EnumerateArray())
            {
                if (Number(v, "version") is not { } n)
                {
                    continue;
                }

                html.Append(first ? "<li aria-current=\"true\">" : "<li>").Append("<span class=\"rc-v\">v").Append(n.ToString(CultureInfo.InvariantCulture))
                    .Append("</span><span>").Append(Esc(Str(v, "date"))).Append(first ? " · this version" : "").Append("</span></li>");
                first = false;
            }

            html.Append("</ol>");
        }

        html.Append("</section>");
    }

    // ------------------------------------------------------------------ the left column

    private static string RenderToc(Func<string, string?> props)
    {
        var mode = props("mode") ?? "contents";
        var html = new StringBuilder();
        html.Append("<div class=\"rc-side\">");
        if (SafeHref(props("back")) is { } back)
        {
            html.Append("<a class=\"rc-back\" href=\"").Append(Esc(back)).Append("\">All resources</a>");
        }

        var steps = Root(props("steps")) is { ValueKind: JsonValueKind.Array } s
            ? s.EnumerateArray().Where(h => Str(h, "id").Length > 0 && Str(h, "text").Length > 0).ToList()
            : [];

        if (mode == "steps" && steps.Count > 0)
        {
            // The steps' own progress, filled by the reader island from the ticks (kept in this browser).
            html.Append("<div class=\"rc-steps-progress\" hidden><div class=\"rc-prog rc-prog-steps\" aria-hidden=\"true\"><i></i></div>")
                .Append("<p class=\"rc-prog-label\">0 of ").Append(steps.Count.ToString(CultureInfo.InvariantCulture)).Append(" steps done</p></div>");
            html.Append("<ol class=\"rc-steps-nav\" aria-label=\"Steps\">");
            var number = 0;
            foreach (var step in steps)
            {
                number++;
                var text = Str(step, "text");
                var words = text.TrimStart("0123456789".ToCharArray()).TrimStart('.', ')').Trim();
                html.Append("<li data-step=\"").Append(Esc(Str(step, "id"))).Append("\"><a href=\"#").Append(Esc(Str(step, "id")))
                    .Append("\"><span class=\"rc-dot\">").Append(number.ToString(CultureInfo.InvariantCulture)).Append("</span><span>")
                    .Append(Esc(words.Length > 0 ? words : text)).Append("</span></a></li>");
            }

            html.Append("</ol></div>");
            return html.ToString();
        }

        AppendReadingProgress(html, props("read"));

        if (mode == "docs" && Root(props("docs")) is { ValueKind: JsonValueKind.Array } groups && groups.GetArrayLength() > 0)
        {
            html.Append("<nav class=\"rc-docnav\" aria-label=\"Documentation\">");
            foreach (var group in groups.EnumerateArray())
            {
                html.Append("<div><h4>").Append(Esc(Str(group, "group"))).Append("</h4>");
                if (group.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (SafeHref(Str(item, "href")) is not { } href)
                        {
                            continue;
                        }

                        html.Append("<a href=\"").Append(Esc(href)).Append('"').Append(Flag(item, "current") ? " aria-current=\"page\"" : "")
                            .Append('>').Append(Esc(Str(item, "title"))).Append("</a>");
                    }
                }

                html.Append("</div>");
            }

            html.Append("</nav></div>");
            return html.ToString();
        }

        var headings = Root(props("headings")) is { ValueKind: JsonValueKind.Array } root
            ? root.EnumerateArray().Where(h => Str(h, "id").Length > 0 && Str(h, "text").Length > 0).ToList()
            : [];
        // A contents list of one entry is a heading repeated, not a way through.
        if (headings.Count >= 2)
        {
            html.Append("<nav class=\"rc-toc\" aria-label=\"On this page\"><ol>");
            foreach (var heading in headings)
            {
                html.Append("<li><a href=\"#").Append(Esc(Str(heading, "id"))).Append("\">").Append(Esc(Str(heading, "text"))).Append("</a></li>");
            }

            html.Append("</ol></nav>");
        }

        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>The reading progress: a real one, filled by the reader island as the article scrolls, in
    /// the sticky column (and pinned under the header on a narrow screen), so it stays in view.</summary>
    private static void AppendReadingProgress(StringBuilder html, string? read)
    {
        html.Append("<div class=\"rc-prog\" role=\"progressbar\" aria-label=\"Reading progress\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"0\"><i></i></div>");
        html.Append("<p class=\"rc-prog-label\"><span class=\"rc-pct\">0 % read</span>");
        AppendIf(html, " · {0}", read);
        html.Append("</p>");
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

    private static string Accent(JsonElement article) =>
        Strings(article, "products").FirstOrDefault(p => p != "canine" && ProductNames.ContainsKey(p)) ?? "canine";

    private static string Status(JsonElement article) =>
        Str(article, "status") is { Length: > 0 } status ? status : Flag(article, "signed") ? "signed" : "published";

    private static string StatusWord(string status) => StatusWords.GetValueOrDefault(status, "Published");

    private static string VersionText(JsonElement article) =>
        Number(article, "version") is { } v ? $"v{v.ToString(CultureInfo.InvariantCulture)}" : "";

    private static string Minutes(JsonElement article) =>
        $"{Math.Max(1, (int)Math.Ceiling((Number(article, "words") ?? 0) / 230.0)).ToString(CultureInfo.InvariantCulture)} min";

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

    private static string KindIcon(string kind) => kind switch
    {
        "essay" => Icon("pen"),
        "whitepaper" => Icon("file"),
        "guide" => Icon("compass"),
        "documentation" => Icon("book"),
        _ => "",
    };

    /// <summary>The design's line icons (Lucide, ISC licence), inline and decorative.</summary>
    private static string Icon(string name, int size = 13, string? cls = null)
    {
        var paths = name switch
        {
            "search" => "<circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"m21 21-4.3-4.3\"/>",
            "arrow-right" => "<path d=\"M5 12h14\"/><path d=\"m12 5 7 7-7 7\"/>",
            "pen" => "<path d=\"M12 20h9\"/><path d=\"M16.4 3.6a2.1 2.1 0 0 1 3 3L7.4 18.6a2 2 0 0 1-.9.5l-2.9.8a.5.5 0 0 1-.6-.6l.8-2.9a2 2 0 0 1 .5-.8z\"/>",
            "file" => "<path d=\"M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z\"/><path d=\"M14 2v4a2 2 0 0 0 2 2h4\"/><path d=\"M16 13H8\"/><path d=\"M16 17H8\"/>",
            "compass" => "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m16.2 7.8-2.1 6.3-6.3 2.1 2.1-6.3z\"/>",
            "book" => "<path d=\"M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z\"/><path d=\"M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z\"/>",
            _ => "",
        };
        var s = size.ToString(CultureInfo.InvariantCulture);
        return "<svg class=\"rc-icon" + (cls is null ? "" : " " + cls) + "\" width=\"" + s + "\" height=\"" + s
            + "\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">"
            + paths + "</svg>";
    }

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
