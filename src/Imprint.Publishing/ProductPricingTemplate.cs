using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Imprint.Publishing;

/// <summary>
/// Renders one product's prices — Assert, Assure or CCA, fetched as JSON from the product's
/// <c>/api/public/pricing/{product}</c> — into imprint's own markup, as <see cref="PricingTemplate"/> does for Watchdog.
/// </summary>
/// <remarks>
/// <para>★ WHY. The three product sites had their prices typed into the page, so a price changed in the
/// product's console changed none of them, and nothing said so. Here the product supplies the numbers,
/// the bake's hash is a page dependency, and the product's "look again" after a price edit re-renders
/// exactly the pages whose prices moved.</para>
/// <para>A price is printed exactly as the product formatted it (its <c>price</c> text): formatting a
/// currency twice is how two pages come to disagree about what something costs. A payload with nothing
/// on sale renders nothing, so the widget's placeholder stays rather than a page reading as free.</para>
/// </remarks>
public static class ProductPricingTemplate
{
    /// <summary>Assert: the plans, priced per author a month.</summary>
    public const string AssertName = "assert-pricing";

    /// <summary>Assure: one price per repository a month.</summary>
    public const string AssureName = "assure-pricing";

    /// <summary>CCA: the free lines, pay as you go, and the monthly tiers.</summary>
    public const string CcaName = "cca-pricing";

    /// <summary>Assay: a yearly fee per repository from its rebuild cost, or a single report at a share of it.</summary>
    public const string AssayName = "assay-pricing";

    /// <summary>Assert's plans as cards, or null when none is on sale.</summary>
    public static string? RenderAssert(string? json)
    {
        if (Parse(json) is not { } root || !root.TryGetProperty("plans", out var plans) || plans.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var cards = plans.EnumerateArray().Where(p => Str(p, "name").Length > 0 && Str(p, "price").Length > 0).ToList();
        if (cards.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder("<div class=\"ip-grid ip-grid-3up\">");
        foreach (var plan in cards)
        {
            html.Append("<div class=\"ip-stack\"><h3>").Append(Esc(Str(plan, "name"))).Append("</h3>");
            html.Append("<p class=\"ip-price\">").Append(Esc(Str(plan, "price")))
                .Append("<span class=\"ip-price-unit\">per author a month</span>");
            if (Long(plan, "linesPerAuthorPerMonth") is { } lines and > 0)
            {
                html.Append("<span class=\"ip-price-allowance\">Fair use: ").Append(Esc(Lines(lines)))
                    .Append(" lines scanned a month per paid author, pooled across the organisation</span>");
            }

            html.Append("</p>");
            if (Str(plan, "tagline") is { Length: > 0 } tagline)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(tagline)).Append("</p></div>");
            }

            AppendList(html, plan, "features");
            html.Append("</div>");
        }

        html.Append("</div>");
        return html.ToString();
    }

    /// <summary>Assure's price, or null when it is not on sale.</summary>
    public static string? RenderAssure(string? json)
    {
        if (Parse(json) is not { } root || root.TryGetProperty("onSale", out var onSale) && onSale.ValueKind == JsonValueKind.False
            || Str(root, "price") is not { Length: > 0 } price)
        {
            return null;
        }

        var vat = root.TryGetProperty("vatIncluded", out var v) && v.ValueKind == JsonValueKind.True;
        var html = new StringBuilder("<div class=\"ip-stack\">");
        html.Append("<p class=\"ip-price\">").Append(Esc(price))
            .Append("<span class=\"ip-price-unit\">a month for each repository we scan").Append(vat ? ", VAT included" : "").Append("</span>");
        if (Long(root, "repositoryLineCap") is { } cap and > 0)
        {
            html.Append("<span class=\"ip-price-allowance\">Up to ").Append(Esc(Lines(cap))).Append(" lines of code per repository</span>");
        }

        html.Append("</p></div>");
        return html.ToString();
    }

    /// <summary>CCA's prices as one table, or null when nothing is on sale: no rate and no tier.</summary>
    public static string? RenderCca(string? json)
    {
        if (Parse(json) is not { } root)
        {
            return null;
        }

        var tiers = root.TryGetProperty("tiers", out var t) && t.ValueKind == JsonValueKind.Array
            ? t.EnumerateArray().Where(x => Str(x, "name").Length > 0 && Str(x, "price").Length > 0).ToList()
            : [];
        var perMillion = Str(root, "pricePerMillion");
        if (tiers.Count == 0 && perMillion.Length == 0)
        {
            return null;
        }

        var html = new StringBuilder("<table class=\"ip-price-table ip-price-table-wide\"><caption class=\"sr-only\">CCA prices</caption>");
        html.Append("<thead><tr><th scope=\"col\">Plan</th><th scope=\"col\">Price</th><th scope=\"col\">Lines scanned</th><th scope=\"col\">Notes</th></tr></thead><tbody>");
        if (Long(root, "freeGrantLines") is { } grant and > 0)
        {
            html.Append("<tr><th scope=\"row\">Free</th><td>€0</td><td>The first ").Append(Esc(Lines(grant)))
                .Append(" lines, once per account</td><td>The full scanner.</td></tr>");
        }

        if (perMillion.Length > 0)
        {
            var validity = Long(root, "creditValidityMonths") is { } months and > 0
                ? $"Credit is valid for {months.ToString(CultureInfo.InvariantCulture)} months."
                : "";
            var minimum = Long(root, "minimumPurchaseLines") is { } min and > 0 ? $" The smallest purchase is {Lines(min)} lines." : "";
            html.Append("<tr><th scope=\"row\">Pay as you go</th><td>").Append(Esc(perMillion)).Append(" per 1M lines</td><td>Prepaid</td><td>")
                .Append(Esc((validity + minimum).Trim())).Append("</td></tr>");
        }

        foreach (var tier in tiers)
        {
            var rate = Long(tier, "requestsPerMinute") is { } r and > 0 ? $"{r.ToString(CultureInfo.InvariantCulture)} API requests a minute. " : "";
            html.Append("<tr><th scope=\"row\">").Append(Esc(Str(tier, "name"))).Append("</th><td>").Append(Esc(Str(tier, "price")))
                .Append(" a month</td><td>").Append(Long(tier, "linesPerMonth") is { } l ? Esc(Lines(l)) + " a month" : "")
                .Append("</td><td>").Append(Esc(rate + "Unused lines lapse at the end of the month.")).Append("</td></tr>");
        }

        html.Append("</tbody></table>");
        return html.ToString();
    }

    /// <summary>Assay's base prices per repository, or null when the payload names no flat fee.</summary>
    public static string? RenderAssay(string? json)
    {
        if (Parse(json) is not { } root || Str(root, "flatFee") is not { Length: > 0 } flat || Str(root, "ratePercent") is not { Length: > 0 } rate)
        {
            return null;
        }

        var share = Long(root, "perReportDivisor") switch
        {
            2 => "half of the yearly fee",
            3 => "a third of the yearly fee",
            4 => "a quarter of the yearly fee",
            { } n and > 1 => $"1/{n.ToString(CultureInfo.InvariantCulture)} of the yearly fee",
            _ => "the yearly fee",
        };
        var html = new StringBuilder("<div class=\"ip-grid ip-grid-3up\">");
        html.Append("<div class=\"ip-stack\"><h3>A year for a repository</h3><p class=\"ip-price\">").Append(Esc(flat))
            .Append("<span class=\"ip-price-unit\">plus ").Append(Esc(rate)).Append(" of the repository's estimated rebuild cost, a year</span>")
            .Append("<span class=\"ip-price-allowance\">As many reports on that repository as you like that year</span></p></div>");
        html.Append("<div class=\"ip-stack\"><h3>One report</h3><p class=\"ip-price\">").Append(Esc(share))
            .Append("<span class=\"ip-price-allowance\">For a single decision report on a repository</span></p></div>");
        html.Append("</div>");
        return html.ToString();
    }

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var root = JsonDocument.Parse(json).RootElement.Clone();
            return root.ValueKind == JsonValueKind.Object ? root : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void AppendList(StringBuilder html, JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var items = list.EnumerateArray().Select(static i => i.ValueKind == JsonValueKind.String ? i.GetString() ?? "" : "").Where(static i => i.Length > 0).ToList();
        if (items.Count == 0)
        {
            return;
        }

        html.Append("<div class=\"ip-prose\"><ul>");
        foreach (var item in items)
        {
            html.Append("<li>").Append(Esc(item)).Append("</li>");
        }

        html.Append("</ul></div>");
    }

    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static long? Long(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : null;

    /// <summary>A line count as people read it: <c>1,000,000</c>.</summary>
    private static string Lines(long lines) => lines.ToString("N0", CultureInfo.InvariantCulture);

    private static string Esc(string s) => WebUtility.HtmlEncode(s);
}
