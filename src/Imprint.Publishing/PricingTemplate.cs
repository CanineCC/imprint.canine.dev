using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Imprint.Publishing;

/// <summary>
/// Renders the package catalogue — fetched as JSON from the product — into IMPRINT'S OWN markup.
/// </summary>
/// <remarks>
/// <para>★ THE POINT OF THIS CLASS IS WHERE IT LIVES. The marketing site used to show prices by
/// framing a page the product rendered, which meant the product owned the look of a marketing page
/// and the prices were in a document the marketing page did not contain — invisible to a crawler,
/// to a language model, and to anyone without JavaScript. Here the product supplies DATA and imprint
/// decides how it reads, in the same <c>ip-</c> markup as every other section, so the result is
/// styled by the site's own stylesheet and needs no frame, no bundle and no script.</para>
/// <para>The numbers are never transformed: a price is printed exactly as the catalogue formatted
/// it. Formatting a currency twice is how two pages come to disagree about what something costs.</para>
/// </remarks>
public static class PricingTemplate
{
    /// <summary>The packages — cohorts, their modules and their size buckets.</summary>
    public const string Name = "pricing";

    /// <summary>
    /// The self-hosted rows only. A SEPARATE template because the page keeps them in their own
    /// section with its own heading and copy: one template rendering both would either duplicate the
    /// on-prem table or force the page to be restructured around what the renderer happens to emit.
    /// The page's shape is the page's business.
    /// </summary>
    public const string OnPremName = "pricing-onprem";

    /// <summary>
    /// The catalogue as imprint markup, or null when the payload carries no packages — an empty
    /// price list is worse than the fallback, because it reads as "free" rather than as "unknown".
    /// </summary>
    public static string? Render(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }

        if (!root.TryGetProperty("cohorts", out var cohorts) || cohorts.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var cards = cohorts.EnumerateArray().Where(c => Str(c, "name").Length > 0).ToList();
        if (cards.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid\" style=\"--ip-min-item: 280px\">");
        foreach (var card in cards)
        {
            html.Append("<div class=\"ip-stack\">");
            html.Append("<h3>").Append(Esc(Str(card, "name"))).Append("</h3>");

            if (Str(card, "tagline") is { Length: > 0 } tagline)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(tagline)).Append("</p></div>");
            }

            var from = Str(card, "fromEur");
            if (from.Length > 0)
            {
                var flat = card.TryGetProperty("isFlatPrice", out var f) && f.ValueKind == JsonValueKind.True;
                html.Append("<div class=\"ip-prose\"><p><strong>").Append(Esc(from)).Append("</strong>")
                    .Append(flat ? "" : " / month and up").Append("</p></div>");
            }

            if (card.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array)
            {
                var names = modules.EnumerateArray().Select(m => m.GetString() ?? "").Where(m => m.Length > 0).ToList();
                if (names.Count > 0)
                {
                    html.Append("<div class=\"ip-prose\"><p>Includes</p><ul>");
                    foreach (var module in names)
                    {
                        html.Append("<li>").Append(Esc(module)).Append("</li>");
                    }

                    html.Append("</ul></div>");
                }
            }

            if (card.TryGetProperty("buckets", out var buckets) && buckets.ValueKind == JsonValueKind.Array)
            {
                var rows = buckets.EnumerateArray().ToList();
                if (rows.Count > 0)
                {
                    html.Append("<table><caption>By bucket — up to this many line-scans a month</caption>")
                        .Append("<thead><tr><th>Bucket</th><th>Lines</th><th>Price</th></tr></thead><tbody>");
                    foreach (var row in rows)
                    {
                        html.Append("<tr><td>").Append(Esc(Str(row, "key"))).Append("</td><td>")
                            .Append(Esc(Lines(row))).Append("</td><td>")
                            .Append(Esc(Str(row, "baseEur"))).Append("</td></tr>");
                    }

                    html.Append("</tbody></table>");
                }
            }

            html.Append("</div>");
        }

        html.Append("</div>");

        if (root.TryGetProperty("distribution", out var distribution)
            && distribution.ValueKind == JsonValueKind.String
            && distribution.GetString() is { Length: > 0 } sentence)
        {
            html.Append("<div class=\"ip-prose\"><p>").Append(Esc(sentence)).Append("</p></div>");
        }

        return html.ToString();
    }

    /// <summary>The self-hosted rows as a table, or null when there are none to show.</summary>
    public static string? RenderOnPrem(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }

        if (!root.TryGetProperty("onPrem", out var onPrem)
            || onPrem.ValueKind != JsonValueKind.Array
            || !onPrem.EnumerateArray().Any())
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<table><thead><tr><th>Package</th><th>Allowance a year</th><th>Price a year</th></tr></thead><tbody>");
        foreach (var row in onPrem.EnumerateArray())
        {
            html.Append("<tr><td>").Append(Esc(Str(row, "name"))).Append("</td><td>")
                .Append(Esc(YearlyAllowance(row))).Append("</td><td>")
                .Append(Esc(Eur(row, "pricePerYearEur"))).Append("</td></tr>");
        }

        return html.Append("</tbody></table>").ToString();
    }

    private static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    /// <summary>
    /// ★ The ValueKind check is load-bearing, not defensive noise: <c>TryGetInt64</c> THROWS on a JSON
    /// null rather than returning false, and a null allowance is the top on-prem tier's whole point.
    /// Without it, the one row that means "unlimited" takes the whole price table down.
    /// </summary>
    private static long? Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? n
            : null;

    private static string Lines(JsonElement row) =>
        Number(row, "lineScansPerMonth") is { } n ? n.ToString("#,##0", CultureInfo.InvariantCulture) : "";

    /// <summary>Null allowance is the top tier's whole point — it is unlimited, not missing.</summary>
    private static string YearlyAllowance(JsonElement row) =>
        Number(row, "lineScansPerYear") is { } n
            ? n.ToString("#,##0", CultureInfo.InvariantCulture) + " line-scans"
            : "Unlimited";

    private static string Eur(JsonElement row, string name) =>
        row.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)
            ? "€" + d.ToString("#,##0", CultureInfo.InvariantCulture)
            : "";

    private static string Esc(string text) => WebUtility.HtmlEncode(text);
}
