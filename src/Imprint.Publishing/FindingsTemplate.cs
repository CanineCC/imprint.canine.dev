using System.Text;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// Real findings from published surveys — lens, dimension, the sentence, the file and line — rendered
/// from <c>/api/public/findings</c> into this site's own markup.
/// </summary>
/// <remarks>
/// <para>★ THIS IS THE PAGE'S EVIDENCE, AND IT WAS IN A FRAME. The claim these sections make is
/// "findings a scanner can't produce"; the proof is the wording of an actual finding, naming an actual
/// file in an actual repository. Inside an iframe none of that reached a crawler, a language model or
/// a reader without JavaScript — the claim was indexable and the evidence for it was not.</para>
/// <para>The count is deliberately conservative. A finding is a sentence about somebody's real code,
/// published with their repository's name against it: the endpoint has already decided which
/// repositories may be shown at all, and this shows a handful of them rather than everything it is
/// handed.</para>
/// </remarks>
public static class FindingsTemplate
{
    public const string Name = "findings";

    /// <summary>Repositories shown. Three is a demonstration; twenty is a data dump.</summary>
    private const int Repositories = 3;

    /// <summary>Findings per repository — enough to show the range of lenses, not a report.</summary>
    private const int PerRepository = 4;

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var repos = Array(root, "items")
            .Where(r => Str(r, "repo").Length > 0 && Array(r, "findings").Count > 0)
            .Take(Repositories)
            .ToList();

        if (repos.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-grid ip-grid-cards\">");

        foreach (var repo in repos)
        {
            html.Append("<div class=\"ip-stack ip-survey\">");
            html.Append("<h3>").Append(Esc(Str(repo, "repo"))).Append("</h3>");

            var shown = Math.Min(Array(repo, "findings").Count, PerRepository);
            if (Number(repo, "total") is { } total && total > shown)
            {
                html.Append("<div class=\"ip-prose ip-kicker\"><p>")
                    .Append(Esc($"{Group(shown)} of {Group(total)} findings"))
                    .Append("</p></div>");
            }

            html.Append("<ul class=\"ip-findings\">");
            foreach (var finding in Array(repo, "findings").Take(PerRepository))
            {
                var lens = Str(finding, "lensLabel");
                var dim = Str(finding, "dim");
                var title = Str(finding, "title");
                if (title.Length == 0)
                {
                    continue;
                }

                html.Append("<li><span class=\"ip-finding-lens\">");
                html.Append(Esc(lens.Length > 0 ? lens : "Finding"));
                if (dim.Length > 0)
                {
                    html.Append(" · ").Append(Esc(dim));
                }

                html.Append("</span><span class=\"ip-finding-title\">").Append(Esc(title)).Append("</span>");

                // file:line is the part that makes a finding checkable rather than a claim. It is also
                // the part a frame hid most completely, because it is small print inside a picture.
                var file = Str(finding, "file");
                if (file.Length > 0)
                {
                    html.Append("<span class=\"ip-finding-where\">").Append(Esc(file));
                    if (Number(finding, "line") is { } line && line > 0)
                    {
                        html.Append(':').Append(Esc(Group(line)));
                    }

                    html.Append("</span>");
                }

                html.Append("</li>");
            }

            html.Append("</ul>");

            if (Link(origin, Str(repo, "reportUrl")) is { } href)
            {
                html.Append("<p class=\"ip-survey-link\"><a href=\"").Append(Esc(href))
                    .Append("\">Read the survey →</a></p>");
            }

            html.Append("</div>");
        }

        return html.Append("</div>").ToString();
    }
}
