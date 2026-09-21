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
        html.Append("<div class=\"ip-grid\" style=\"--ip-min-item: 240px\">");
        foreach (var card in cards)
        {
            html.Append("<div class=\"ip-stack\">");
            html.Append("<h3>").Append(Esc(Str(card, "name"))).Append("</h3>");

            // ★ THE PRICE LINE IS THE ONE A READER LOOKS FOR, so it says what it means. A free lane
            //   whose first bucket is €0 is not "from €0 a month" — that is technically true and
            //   reads as a sales line. It is free up to an allowance, and then it is not, and both
            //   halves come from the catalogue rather than from prose somebody has to maintain.
            var buckets = card.TryGetProperty("buckets", out var b) && b.ValueKind == JsonValueKind.Array
                ? b.EnumerateArray().ToList()
                : [];
            var free = buckets.Count > 0 && IsZero(Str(buckets[0], "baseEur"));
            var firstPaid = buckets.FirstOrDefault(x => !IsZero(Str(x, "baseEur")));

            html.Append("<div class=\"ip-prose\"><p class=\"ip-kicker\">");
            var flat = card.TryGetProperty("isFlatPrice", out var f) && f.ValueKind == JsonValueKind.True;
            if (free)
            {
                html.Append("Free up to ").Append(Esc(Lines(buckets[0]))).Append(" lines a month");
                if (firstPaid.ValueKind == JsonValueKind.Object)
                {
                    html.Append(", then from <strong>").Append(Esc(Str(firstPaid, "baseEur"))).Append("</strong>");
                }
            }
            else if (flat)
            {
                // One price, not a starting price: "from" would promise a ladder that does not exist.
                html.Append("<strong>").Append(Esc(Str(card, "fromEur"))).Append("</strong> a month");
            }
            else
            {
                html.Append("From <strong>").Append(Esc(Str(card, "fromEur"))).Append("</strong> a month");
            }

            html.Append("</p></div>");

            if (Str(card, "tagline") is { Length: > 0 } tagline)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(tagline)).Append("</p></div>");
            }

            if (card.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array)
            {
                var names = modules.EnumerateArray().Select(m => m.GetString() ?? "").Where(m => m.Length > 0).ToList();
                if (names.Count > 0)
                {
                    html.Append("<div class=\"ip-prose\"><ul>");
                    foreach (var module in names)
                    {
                        html.Append("<li>").Append(Esc(module)).Append("</li>");
                    }

                    html.Append("</ul></div>");
                }
            }

            if (buckets.Count > 0)
            {
                // Two columns, not three: the bucket and the lines it allows are ONE fact and read as
                // one. Three columns inside a card forced the grid to two across and the page to twice
                // the height it needed.
                html.Append("<table><caption class=\"ip-kicker\">Up to this many line-scans a month</caption><tbody>");
                foreach (var bucket in buckets)
                {
                    html.Append("<tr><td>").Append(Esc(Str(bucket, "key"))).Append(" · ")
                        .Append(Esc(Lines(bucket))).Append("</td>")
                        .Append("<td style=\"text-align:right;white-space:nowrap\">")
                        .Append(Esc(Str(bucket, "baseEur"))).Append("</td></tr>");
                }

                html.Append("</tbody></table>");
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
        html.Append("<table><thead><tr><th>Package</th>")
            .Append("<th style=\"text-align:right\">Allowance a year</th>")
            .Append("<th style=\"text-align:right\">Price a year</th></tr></thead><tbody>");
        foreach (var row in onPrem.EnumerateArray())
        {
            html.Append("<tr><td><strong>").Append(Esc(Str(row, "name"))).Append("</strong></td>")
                .Append("<td style=\"text-align:right;white-space:nowrap\">").Append(Esc(YearlyAllowance(row))).Append("</td>")
                .Append("<td style=\"text-align:right;white-space:nowrap\"><strong>").Append(Esc(Eur(row, "pricePerYearEur"))).Append("</strong></td></tr>");
        }

        return html.Append("</tbody></table>").ToString();
    }

    /// <summary>A formatted price that is zero, whatever currency symbol it carries.</summary>
    private static bool IsZero(string price) =>
        price.Any(char.IsAsciiDigit) && price.Where(char.IsAsciiDigit).All(digit => digit == '0');

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
