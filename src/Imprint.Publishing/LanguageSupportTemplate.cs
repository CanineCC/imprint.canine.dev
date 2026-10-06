using System.Text;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// Which languages the product models, how clearly each one surveys, and which lenses resolve —
/// rendered from <c>/api/public/language-support</c>.
/// </summary>
/// <remarks>
/// <para>★ THE WHOLE POINT OF THE PAGE WAS IN A SHADOW ROOT. <c>/languages/</c> exists to answer
/// "do you cover my stack?", and its answer — seventeen languages, each with a clarity band, a
/// one-line summary and the lenses that resolve for it — reached no crawler, no language model and
/// no reader without JavaScript. The claim ("multi-language") was indexable; the table proving it
/// was not. This is the single densest piece of buying information on the site.</para>
/// <para><b>The band word must never read as a grade of the language.</b> The page glosses the words
/// in its own copy above the table, and each word carries its meaning as a mouseover here. The
/// payload's <c>note</c> ("FIT is survey clarity — …") is deliberately NOT rendered: it defended
/// the score colours the band words used to borrow, and those are gone (owner's decision,
/// 2026-10-06).</para>
/// <para>★ THE BAND HUE IS ONE COLOUR IN SHADES, NOT THE SCORE BANDS. The words used to take
/// <c>ip-band-{bandCss}</c>, the same green-to-red the site uses for code scores, so JavaScript's
/// LOW printed in the colour of a poor score. The shade now follows the band WORD, darkest for
/// FULL. Nothing here decides which band a language is in.</para>
/// </remarks>
public static class LanguageSupportTemplate
{
    public const string Name = "language-support";

    private static readonly Dictionary<string, string> BandGloss = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FULL"] = "Every design grade that applies is read in full.",
        ["VERY HIGH"] = "Every design grade is read, except one or two named checks.",
        ["HIGH"] = "Most design grades are read; a few checks don't apply to this language or are read in part.",
        ["MEDIUM"] = "Part of the design is read; checks that need facts the language doesn't state are left out.",
        ["LOW"] = "How the code is put together is read, but not the design built on it.",
    };

    private static readonly Dictionary<string, string> KindGloss = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Deep"] = "Watchdog reads the language's types and the design built on them.",
        ["Structural"] = "Watchdog reads how the files and parts depend on each other.",
    };

    /// <summary>The grades a reader can find on What we measure, in the order the chips are shown.</summary>
    /// <remarks>
    /// ★ THE PRODUCT'S LENS NAMES ARE ITS OWN, NOT THE READER'S. "DDD", "Vertical slice", "Structural" and
    /// "Correctness" appear nowhere a customer is told what is graded, so each covered lens is shown under
    /// the grade it counts toward. Several product names fold into one chip ("Structural" and "Vertical
    /// slice" are both how the product is put together), so a row shows each grade once. A lens with no
    /// mapping is shown under its own name rather than dropped: a new lens must stay visible until it is
    /// named here. The product's data is not changed by any of this.
    /// </remarks>
    private static readonly (string Grade, string Gloss)[] Grades =
    [
        ("Architecture", "How the parts of the product depend on each other and how the code is organised."),
        ("Domain Modelling", "How the business data and its rules are modelled in the code."),
        ("Event-Driven", "How the parts send and handle messages."),
        ("Event Sourcing", "How the product stores its history as a series of events."),
        ("Code-level traps", "Errors that are caught and then ignored, and similar traps that break software quietly."),
    ];

    private static string? GradeOf(string lens) => lens.Trim() switch
    {
        var l when l.StartsWith("DDD", StringComparison.OrdinalIgnoreCase) => "Domain Modelling",
        var l when l.Equals("Event-driven", StringComparison.OrdinalIgnoreCase) => "Event-Driven",
        var l when l.Equals("Event sourcing", StringComparison.OrdinalIgnoreCase) => "Event Sourcing",
        var l when l.Equals("Structural", StringComparison.OrdinalIgnoreCase)
                || l.Equals("Vertical slice", StringComparison.OrdinalIgnoreCase)
                || l.Equals("Module graph", StringComparison.OrdinalIgnoreCase)
                || l.Equals("Dependency hygiene", StringComparison.OrdinalIgnoreCase)
                || l.Equals("Complexity", StringComparison.OrdinalIgnoreCase) => "Architecture",
        var l when l.Equals("Correctness", StringComparison.OrdinalIgnoreCase) => "Code-level traps",
        _ => null,
    };

    /// <summary>Plain names for the product's "not applicable" lens names, filled in language by language as
    /// each row's description is reviewed. A name not listed here is shown as the product wrote it.</summary>
    private static readonly Dictionary<string, (string Name, string Gloss)> NotReadNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Syntax-level correctness"] = ("Code-level traps", "Errors that are caught and then ignored, and similar traps that break software quietly."),
        ["DI segregation"] = ("Dependency wiring", "How each part is handed the services it depends on (dependency injection)."),
        ["DI"] = ("Dependency wiring", "How each part is handed the services it depends on (dependency injection)."),
        ["god-class/LCOM"] = ("Classes that do too much", "A class that holds several unrelated jobs, which makes it hard to change."),
        ["god-class"] = ("Classes that do too much", "A class that holds several unrelated jobs, which makes it hard to change."),
        ["Anemic/mutable-setter (immutable)"] = ("Records that only hold data", "Business records that hold data without the rules that belong to it, or that any part of the code can change."),
        ["inheritance & DU lenses"] = ("Class and type hierarchies", "Classes that build on other classes, and events written as one closed set of types."),
        ["Inheritance & annotation lenses"] = ("Class and type hierarchies", "Classes that build on other classes, and events written as one closed set of types."),
        ["Sealed/DU carrier"] = ("Closed sets of event types", "Events written as one closed set of types, so the code has to handle every kind."),
        ["Sealed/DU lens"] = ("Closed sets of event types", "Events written as one closed set of types, so the code has to handle every kind."),
        ["Deterministic DDD/Event-sourcing model-shape lenses (advisory only — untyped)"] = ("Domain Modelling and Event Sourcing (as advice only)", "For JavaScript these checks are shown as advice and don't count toward the score, because the code doesn't state its types."),
        ["Compiled-output efficiency"] = ("Compiled .NET program", "A check of the program the .NET compiler produces, which finds waste the source code doesn't show."),
        ["strongly-typed-id (field-based)"] = ("Typed IDs", "Whether each kind of ID has its own type, so an order number can't be mixed up with a customer number."),
    };

    private static string NotReadChip(string lens) =>
        NotReadNames.TryGetValue(lens.Trim(), out var plain) ? Chip(plain.Name, plain.Gloss) : Chip(lens, null);

    private static string Chip(string text, string? gloss) =>
        $"<span class=\"ip-lens\"{(gloss is null ? "" : $" title=\"{Esc(gloss)}\"")}>{Esc(text)}</span>";

    private static string Slug(string band) => band.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string Title(Dictionary<string, string> gloss, string word) =>
        gloss.TryGetValue(word, out var text) ? $" title=\"{Esc(text)}\"" : "";

    public static string? Render(string? json)
    {
        if (Root(json) is not { } root)
        {
            return null;
        }

        var languages = Array(root, "languages")
            .Where(l => Str(l, "displayName").Length > 0)
            .ToList();

        if (languages.Count == 0)
        {
            return null;
        }

        // ★ A LIST, NOT A GRID OF CARDS. These summaries run from eleven words to two hundred and
        //   fifty, and in a grid the row height is set by the tallest card in the row: the first
        //   rendering gave C# a mostly-empty box as tall as Dart's essay. A reference table lets a
        //   long row simply be long, and costs the reader nothing.
        var html = new StringBuilder();
        html.Append("<div class=\"ip-langs\">");

        foreach (var language in languages)
        {
            var band = Str(language, "bandLabel");

            html.Append("<div class=\"ip-langrow\">");

            html.Append("<div class=\"ip-langrow-head\">");
            html.Append("<h3>").Append(Esc(Str(language, "displayName"))).Append("</h3>");
            if (band.Length > 0)
            {
                html.Append("<span class=\"ip-band-strong ip-langband-").Append(Esc(Slug(band))).Append('"')
                    .Append(Title(BandGloss, band)).Append('>')
                    .Append(Esc(band)).Append("</span>");
            }

            if (Str(language, "supportKind") is { Length: > 0 } kind)
            {
                html.Append("<span class=\"ip-cai-unit ip-langkind\"").Append(Title(KindGloss, kind)).Append('>')
                    .Append(Esc(kind)).Append("</span>");
            }

            html.Append("</div>");

            html.Append("<div class=\"ip-langrow-body\">");
            if (Str(language, "summary") is { Length: > 0 } summary)
            {
                html.Append("<div class=\"ip-prose\"><p>").Append(Esc(summary)).Append("</p></div>");
            }

            var covered = Strings(language, "coveredLenses");
            if (covered.Count > 0)
            {
                var mapped = covered.Select(GradeOf).Where(g => g is not null).ToHashSet(StringComparer.Ordinal);
                var chips = Grades.Where(g => mapped.Contains(g.Grade)).Select(g => Chip(g.Grade, g.Gloss))
                    .Concat(covered.Where(l => GradeOf(l) is null).Select(l => Chip(l, null)));

                html.Append("<p class=\"ip-lens-list\"><span class=\"ip-lens-label\">Read for</span>")
                    .Append(string.Join("", chips))
                    .Append("</p>");
            }

            // ★ What does NOT resolve is published beside what does. A coverage table that lists only
            //   the wins reads as a sales page; the reason this one is worth anything to a buyer is
            //   that it says where a language stops.
            var notApplicable = Strings(language, "notApplicableLenses");
            if (notApplicable.Count > 0)
            {
                html.Append("<p class=\"ip-lens-list ip-lens-list-off\"><span class=\"ip-lens-label\">Not read</span>")
                    .Append(string.Join("", notApplicable.Select(NotReadChip).Distinct(StringComparer.Ordinal)))
                    .Append("</p>");
            }

            html.Append("</div></div>");
        }

        html.Append("</div>");

        return html.ToString();
    }
}
