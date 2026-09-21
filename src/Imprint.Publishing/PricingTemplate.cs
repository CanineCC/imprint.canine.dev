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

        // ★ THE FREE LANES GO LAST, and the layout depends on it. `.ip-grid-5up` steps from five
        //   across to THREE across, which puts whatever comes fourth and fifth on a second row —
        //   and that reads as "the paid packages, with the free lanes under them" only while the
        //   order is this one. A stable partition, so the catalogue still decides the order within
        //   each group; this class only decides which group a card is in.
        cards = cards.Where(c => !IsZero(Str(c, "fromEur")))
            .Concat(cards.Where(c => IsZero(Str(c, "fromEur"))))
            .ToList();

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-grid-5up\">");
        foreach (var card in cards)
        {
            var name = Str(card, "name");
            html.Append("<div class=\"ip-stack\">");
            html.Append("<h3>").Append(Esc(name)).Append("</h3>");

            // ★ THE PRICE LINE IS THE ONE A READER LOOKS FOR, so it says what it means. A free lane
            //   is not "from €0 a month" — that is technically true and reads as a sales line. It is
            //   free up to an allowance, and then it is not, and both halves come from the catalogue
            //   rather than from prose somebody has to maintain.
            var buckets = card.TryGetProperty("buckets", out var b) && b.ValueKind == JsonValueKind.Array
                ? b.EnumerateArray().ToList()
                : [];
            var free = IsZero(Str(card, "fromEur"));
            var flat = card.TryGetProperty("isFlatPrice", out var f) && f.ValueKind == JsonValueKind.True;

            html.Append("<p class=\"ip-price\">");
            if (free)
            {
                html.Append("Free");
                if (buckets.Count > 0)
                {
                    var firstPaid = buckets.FirstOrDefault(x => !IsZero(Str(x, "baseEur")));
                    html.Append("<span class=\"ip-price-allowance\">up to ")
                        .Append(Esc(Lines(buckets[0]))).Append(" lines a month");
                    if (firstPaid.ValueKind == JsonValueKind.Object)
                    {
                        html.Append(", then from ").Append(Esc(Str(firstPaid, "baseEur")));
                    }

                    html.Append("</span>");
                }
            }
            else
            {
                // A flat package has one price, not a starting price: "from" would promise a ladder
                // that does not exist.
                if (!flat)
                {
                    html.Append("<span class=\"ip-price-lead\">From</span>");
                }

                html.Append(Esc(Str(card, "fromEur"))).Append("<span class=\"ip-price-unit\">a month</span>");
            }

            html.Append("</p>");

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
                // one. ★ The caption is .sr-only — a screen reader needs to know WHICH package's
                // ladder it has landed in, and a sighted reader had the same sentence printed five
                // times, once per card.
                html.Append("<table class=\"ip-price-table\"><caption class=\"sr-only\">")
                    .Append(Esc(name)).Append(" — price a month by line-scan allowance</caption><tbody>");
                foreach (var bucket in buckets)
                {
                    html.Append("<tr><th scope=\"row\">").Append(Esc(Str(bucket, "key"))).Append(" · ")
                        .Append(Esc(CompactLines(bucket))).Append("</th><td>")
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

    /// <summary>The self-hosted sizes as cards, or null when there are none to show.</summary>
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

        // Cards, not a table. Three self-hosted sizes are three OFFERS — each one a package a reader
        // picks between — and they sit in the same section as three authored cards that explain the
        // terms. A three-row table beside three cards reads as a footnote to them.
        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-grid-3up\">");
        foreach (var row in onPrem.EnumerateArray())
        {
            html.Append("<div class=\"ip-stack\">");
            html.Append("<h3>").Append(Esc(Str(row, "name"))).Append("</h3>");
            html.Append("<p class=\"ip-price\">").Append(Esc(Eur(row, "pricePerYearEur")))
                .Append("<span class=\"ip-price-unit\">a year</span></p>");
            html.Append("<div class=\"ip-prose\"><p>").Append(Esc(YearlyAllowance(row))).Append("</p></div>");
            if (Str(row, "blurb") is { Length: > 0 } blurb)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(blurb)).Append("</p></div>");
            }

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
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

    /// <summary>
    /// The allowance abbreviated — "250k", "1M", "2.5M" — for the bucket ladder, which sits inside a
    /// card about 230px wide where "1,000,000" does not fit beside a price.
    /// <para>★ It abbreviates only where the abbreviation is EXACT. A value that does not divide
    /// cleanly is printed in full, so a reader can never take a rounded figure for the allowance they
    /// are being charged against. Everywhere there is room — the free-lane line, the on-prem cards —
    /// the full grouped number is used instead.</para>
    /// </summary>
    private static string CompactLines(JsonElement row)
    {
        if (Number(row, "lineScansPerMonth") is not { } n || n <= 0)
        {
            return "";
        }

        foreach (var (unit, suffix) in Units)
        {
            if (n < unit)
            {
                continue;
            }

            var scaled = (decimal)n / unit;
            if (decimal.Round(scaled, 1) == scaled)
            {
                return scaled.ToString("0.#", CultureInfo.InvariantCulture) + suffix;
            }
        }

        return n.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    private static readonly (long Unit, string Suffix)[] Units =
        [(1_000_000_000L, "B"), (1_000_000L, "M"), (1_000L, "k")];

    /// <summary>Null allowance is the top tier's whole point — it is unlimited, not missing.</summary>
    private static string YearlyAllowance(JsonElement row) =>
        Number(row, "lineScansPerYear") is { } n
            ? n.ToString("#,##0", CultureInfo.InvariantCulture) + " line-scans a year"
            : "Unlimited line-scans";

    private static string Eur(JsonElement row, string name) =>
        row.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)
            ? "€" + d.ToString("#,##0", CultureInfo.InvariantCulture)
            : "";

    private static string Esc(string text) => WebUtility.HtmlEncode(text);
}
