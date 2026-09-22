using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The staff exam's question pool — the last framed view on the estate.
/// </summary>
public sealed class ExamPoolTemplateTests
{
    private const string Origin = "https://app.watchdog.canine.dev";

    private const string Payload = """
        {
          "title": "Staff exam",
          "passMark": 6,
          "questionsPerSet": 8,
          "total": 169,
          "groups": [
            { "category": "canine", "label": "The studio",
              "questions": [ { "number": 1, "prompt": "Why does the studio take no build work?" },
                             { "number": 74, "prompt": "How do the three values hang together?" } ] },
            { "category": "cai", "label": "The standard",
              "questions": [ { "number": 5, "prompt": "Who owns the CAI number?" } ] }
          ]
        }
        """;

    [Fact]
    public void Every_question_is_ordinary_visible_text_in_the_markup()
    {
        var html = ExamPoolTemplate.Render(Payload, Origin)!;

        Assert.Contains("Why does the studio take no build work?", html, StringComparison.Ordinal);
        Assert.Contains("Who owns the CAI number?", html, StringComparison.Ordinal);
        Assert.Contains("The studio", html, StringComparison.Ordinal);
        Assert.Contains("The standard", html, StringComparison.Ordinal);

        // ★★ AND NOTHING IS HIDDEN. The earlier attempt on this estate baked a machine-readable
        //    duplicate into the light DOM and left the iframe drawing the visible page. Nothing here
        //    may be display:none, aria-hidden, sr-only or an <iframe>: the questions ARE the page.
        foreach (var hidden in new[] { "sr-only", "aria-hidden", "display:none", "hidden", "<iframe" })
        {
            Assert.DoesNotContain(hidden, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// ★ The numbers come from the PAYLOAD, not from the list's own counter. A colleague refers to
    /// "number 74"; a list that renumbered per group would rename every question the moment a group
    /// gained one.
    /// </summary>
    [Fact]
    public void A_question_keeps_the_number_it_is_known_by()
    {
        var html = ExamPoolTemplate.Render(Payload, Origin)!;

        Assert.Contains("<li value=\"74\">How do the three values hang together?</li>", html, StringComparison.Ordinal);
        Assert.Contains("<li value=\"5\">Who owns the CAI number?</li>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_shape_of_a_sitting_is_stated_before_the_list()
    {
        var html = ExamPoolTemplate.Render(Payload, Origin)!;

        Assert.Contains("draws 8 of these 169 questions", html, StringComparison.Ordinal);
        Assert.Contains("6 correct is a pass", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// ★★ THE POOL IS THE QUESTIONS, NEVER THE ANSWERS. The endpoint carries prompts only. If a
    /// payload ever grew options or a correct answer, this template must not start printing them —
    /// that is the one change that turns a published study aid into a published exam paper.
    /// </summary>
    [Fact]
    public void An_answer_key_in_the_payload_is_not_rendered()
    {
        const string withAnswers = """
            {"total":1,"questionsPerSet":1,"passMark":1,
             "groups":[{"label":"G","questions":[
               {"number":1,"prompt":"Who owns the CAI number?",
                "options":["The customer","Nobody"],"answer":"Nobody","explanation":"Because it is open."}]}]}
            """;

        var html = ExamPoolTemplate.Render(withAnswers, Origin)!;

        Assert.Contains("Who owns the CAI number?", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Nobody", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The customer", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Because it is open.", html, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_pool_renders_nothing_rather_than_an_empty_page()
    {
        Assert.Null(ExamPoolTemplate.Render("{\"groups\":[]}", Origin));
        Assert.Null(ExamPoolTemplate.Render("{\"groups\":[{\"label\":\"G\",\"questions\":[]}]}", Origin));
        Assert.Null(ExamPoolTemplate.Render(null, Origin));
    }

    /// <summary>A prompt is another service's text landing in our HTML, so it is escaped.</summary>
    [Fact]
    public void A_prompt_is_escaped()
    {
        const string nasty = """
            {"groups":[{"label":"G","questions":[{"number":1,"prompt":"<script>alert(1)</script>"}]}]}
            """;

        var html = ExamPoolTemplate.Render(nasty, Origin)!;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }
}
