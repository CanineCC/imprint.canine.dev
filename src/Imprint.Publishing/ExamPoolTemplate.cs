using System.Text;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// The staff exam's question pool, rendered from <c>/api/public/exam-pool</c>.
/// </summary>
/// <remarks>
/// <para>★★ THIS REPLACES A FRAME, AND IT IS NOT A HIDDEN COPY. An earlier attempt on this estate
/// baked a machine-readable duplicate into the element's light DOM and left the iframe drawing the
/// visible page: a crawler could read it and a browser rendered none of it. That solved the
/// sub-goal and not the goal. Every question below is ordinary, visible, selectable text in the
/// served HTML, and there is no frame left to draw anything.</para>
/// <para>★ IT STAYS LIVE WITHOUT ANYONE REPUBLISHING. The bake is keyed by the fetched body's
/// content hash, so when a question is added or edited in the questionnaire database the hash moves,
/// the page is stale, and the next publish pass re-renders it. That is the same mechanism the price
/// table runs on, and it is why "up to date" is a property of the data rather than of whoever
/// remembered to press publish.</para>
/// <para>★ THE POOL IS THE QUESTIONS, NEVER THE ANSWERS. The endpoint carries prompts and nothing
/// else — no options, no correct answer, no explanation — and this renders what it is given. A
/// template that reached for an answer key would be the one change that turns a published study aid
/// into a published exam paper.</para>
/// </remarks>
public static class ExamPoolTemplate
{
    public const string Name = "exam-pool";

    public static string? Render(string? json, string? origin)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var groups = Array(root, "groups")
            .Select(g => (Label: Str(g, "label"), Questions: Array(g, "questions")))
            .Where(g => g.Questions.Count > 0)
            .ToList();

        if (groups.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"ip-stack ip-pool\">");

        // The shape of the sitting, stated before the list so a reader knows what they are looking at.
        var total = Number(root, "total");
        var perSet = Number(root, "questionsPerSet");
        var passMark = Number(root, "passMark");
        if (total is { } t && perSet is { } per && passMark is { } pass)
        {
            html.Append("<p class=\"ip-pool-rule\">")
                .Append(Esc($"A sitting draws {Group(per)} of these {Group(t)} questions at random. "))
                .Append(Esc($"{Group(pass)} correct is a pass."))
                .Append("</p>");
        }

        foreach (var (label, questions) in groups)
        {
            html.Append("<section class=\"ip-pool-group\">");
            if (label.Length > 0)
            {
                html.Append("<h3 class=\"ip-pool-heading\">").Append(Esc(label))
                    .Append(" <span class=\"ip-pool-count\">")
                    .Append(Esc(questions.Count == 1 ? "1 question" : $"{Group(questions.Count)} questions"))
                    .Append("</span></h3>");
            }

            // ★ An <ol> with the payload's own numbers, because the number IS how a colleague refers
            //   to a question ("number 42 again"). A list that renumbered itself per group would
            //   silently rename every question the moment a group gained one.
            html.Append("<ol class=\"ip-pool-list\">");
            foreach (var question in questions)
            {
                var prompt = Str(question, "prompt");
                if (prompt.Length == 0)
                {
                    continue;
                }

                html.Append("<li");
                if (Number(question, "number") is { } number)
                {
                    html.Append(" value=\"").Append(Esc(Group(number))).Append('"');
                }

                html.Append('>').Append(Esc(prompt)).Append("</li>");
            }

            html.Append("</ol></section>");
        }

        return html.Append("</div>").ToString();
    }
}
