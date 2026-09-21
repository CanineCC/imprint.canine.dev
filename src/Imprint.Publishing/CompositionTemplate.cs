using System.Globalization;
using System.Text;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// Slop versus brilliant, scan by scan — rendered from <c>/api/public/composition</c>.
/// </summary>
/// <remarks>
/// <para>★★ THE TREND IS THE ARGUMENT. The section's own copy promises "the split is on every report
/// card, and the trend shows it falling", and a single latest figure cannot show a trend. Each column
/// is one published scan, stacked brilliant / fine / slop, so the shape of the thing falling is the
/// picture — which is what the framed view drew and what the first replacement lost.</para>
/// <para>★ EVERY COLUMN'S NUMBERS ARE IN THE MARKUP, on the column, as its accessible label. A
/// stacked bar with no text is a picture of data; this is data with a picture over it. The latest
/// mix is also printed in words beneath, because that is the figure a reader quotes.</para>
/// <para>The curation — only repositories showing all three bands — is the product's, not this
/// file's. See the endpoint: publishing whatever came back produced a section that answered "how
/// much of your codebase is slop?" with four repositories at 0 %.</para>
/// </remarks>
public static class CompositionTemplate
{
    public const string Name = "composition";

    /// <summary>Repositories shown. Three is a comparison; ten is a data dump on a marketing page.</summary>
    private const int Repositories = 3;

    /// <summary>
    /// ★★ THE CURATION, APPLIED HERE TOO. The product's own embed only ever showed repositories whose
    /// survey has all three bands above zero, and the reason is in its source: "a repo with no slop
    /// proves nothing about measuring slop, and one with no brilliant reads as a hit piece." The
    /// first replacement dropped that rule and published a section that answered "how much of your
    /// codebase is slop?" with four repositories at 0 % — which is worse than publishing nothing.
    /// <para>It lives on BOTH sides deliberately: the composition endpoint curates, and so does this,
    /// because this also reads an older feed that does not.</para>
    /// </summary>
    private static bool ShowsAllThreeBands(System.Text.Json.JsonElement point) =>
        Number(point, "brilliant") is > 0
        && Number(point, "slop") is > 0
        && (Number(point, "fine") ?? 100 - (Number(point, "brilliant") ?? 0) - (Number(point, "slop") ?? 0)) > 0;

    /// <summary>The three bands, worst last so the column reads brilliant-up from the floor.</summary>
    private static readonly (string Key, string Label)[] Bands =
    [
        ("slop", "slop"),
        ("fine", "fine"),
        ("brilliant", "brilliant"),
    ];

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        // ★ The second envelope is GONE, as its own note said it should be once the widget was
        //   repointed. It read the older /api/public/insights (`display` + `fileQuality`) so the
        //   chart could ship before /api/public/composition reached prod — repointing first would
        //   have put a placeholder sentence on a live marketing page until the next promote. The
        //   endpoint answers 200 now, `wd-file-mix` reads it, and a branch kept past the migration it
        //   existed for is a second shape nobody tests against real data again.
        var repos = Array(root, "items")
            .Select(r => new
            {
                Repo = Str(r, "repo"),
                Url = Str(r, "reportUrl"),
                History = Array(r, "history"),
                Latest = r.TryGetProperty("latest", out var l) ? l : default,
            })
            .Where(r => r.Repo.Length > 0 && r.History.Count > 0)
            .Where(r => ShowsAllThreeBands(r.Latest.ValueKind == System.Text.Json.JsonValueKind.Object
                ? r.Latest
                : r.History[^1]))
            .Take(Repositories)
            .ToList();

        if (repos.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-mix-grid\">");

        foreach (var row in repos)
        {
            var history = row.History;
            var latest = row.Latest.ValueKind == System.Text.Json.JsonValueKind.Object
                ? row.Latest
                : history[^1];

            html.Append("<div class=\"ip-stack ip-mix\">");

            html.Append("<p class=\"ip-mix-name\">");
            if (Link(origin, row.Url) is { } href)
            {
                html.Append("<a href=\"").Append(Esc(href)).Append("\">").Append(Esc(row.Repo)).Append("</a>");
            }
            else
            {
                html.Append(Esc(row.Repo));
            }

            html.Append("</p>");

            // One column per scan, oldest on the left. The columns are a <ul> because they are a
            // list of measurements, and a screen reader should be able to walk them.
            html.Append("<ul class=\"ip-mix-chart\" aria-label=\"")
                .Append(Esc($"File-quality mix over the last {Group(history.Count)} published scans"))
                .Append("\">");

            foreach (var point in history)
            {
                var brilliant = Number(point, "brilliant") ?? 0;
                var fine = Number(point, "fine") ?? 0;
                var slop = Number(point, "slop") ?? 0;

                html.Append("<li class=\"ip-mix-col\" title=\"")
                    .Append(Esc(Label(point, brilliant, fine, slop))).Append("\">");
                foreach (var (key, _) in Bands)
                {
                    var share = key switch
                    {
                        "slop" => slop,
                        "fine" => fine,
                        _ => brilliant,
                    };

                    // A zero band emits nothing: an empty segment still shows as a sliver of a
                    // category that is not there.
                    if (share > 0)
                    {
                        html.Append("<span class=\"ip-mix-seg ip-mix-seg-").Append(key)
                            .Append("\" style=\"height:").Append(Esc(Pct(share))).Append("%\"></span>");
                    }
                }

                html.Append("<span class=\"sr-only\">").Append(Esc(Label(point, brilliant, fine, slop)))
                    .Append("</span></li>");
            }

            html.Append("</ul>");

            // ★ Which end is NOW. A column chart with no axis is a shape, and half of readers will
            //   assume the newest scan is on the left. One line of text is the whole fix.
            html.Append("<p class=\"ip-mix-axis\"><span>oldest</span><span>")
                .Append(Esc(history.Count == 1 ? "1 scan" : $"{Group(history.Count)} scans"))
                .Append("</span><span>latest</span></p>");

            {
                var brilliant = Number(latest, "brilliant") ?? 0;
                var fine = Number(latest, "fine") ?? 0;
                var slop = Number(latest, "slop") ?? 0;

                html.Append("<p class=\"ip-mix-legend\">")
                    .Append(Key("brilliant", brilliant)).Append(Key("fine", fine)).Append(Key("slop", slop))
                    .Append("</p>");

                if (Number(latest, "scoredFiles") is { } files && files > 0)
                {
                    html.Append("<p class=\"ip-mix-files\">latest scan · ")
                        .Append(Esc(Group(files))).Append(files == 1 ? " file scored" : " files scored")
                        .Append("</p>");
                }
            }

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
    }

    private static string Key(string kind, double share) =>
        $"<span class=\"ip-mix-key ip-mix-key-{kind}\">{Esc(Pct(share))} % {kind}</span>";

    private static string Label(System.Text.Json.JsonElement point, double brilliant, double fine, double slop)
    {
        var when = Str(point, "at");
        var date = DateTimeOffset.TryParse(when, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at.UtcDateTime.ToString("d MMM yyyy", CultureInfo.InvariantCulture) + ": "
            : "";

        return $"{date}{Pct(brilliant)} % brilliant, {Pct(fine)} % fine, {Pct(slop)} % slop";
    }

    /// <summary>One decimal at most, and none on a whole number — the payload's precision, not more.</summary>
    private static string Pct(double value) =>
        value.ToString(Math.Abs(value - Math.Round(value)) < 0.05 ? "0" : "0.0", CultureInfo.InvariantCulture);
}
