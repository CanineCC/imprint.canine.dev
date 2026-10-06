using Imprint.Publishing;

namespace Imprint.Publishing.Tests;

/// <summary>
/// The page that answers "do you cover my stack?" — and whose answer used to live in a shadow root.
/// </summary>
public sealed class LanguageSupportTemplateTests
{
    private const string Payload = """
        {
          "note": "FIT is survey clarity — how clearly a language leads to a complete architecture survey. It is NOT a measure of language or code quality.",
          "bands": [ { "band": "FULL", "label": "FULL", "cssKey": "exemplary" } ],
          "languages": [
            { "code": "csharp", "displayName": "C#", "supportKind": "Deep",
              "band": "FULL", "bandLabel": "FULL", "bandCss": "exemplary",
              "summary": "Native Roslyn symbols resolve every lens.",
              "coveredLenses": ["DDD", "Event sourcing"], "notApplicableLenses": [] },
            { "code": "javascript", "displayName": "JavaScript", "supportKind": "Structural",
              "band": "LOW", "bandLabel": "LOW", "bandCss": "poor",
              "summary": "Structural only — no type graph to resolve a domain lens against.",
              "coveredLenses": ["Structural"], "notApplicableLenses": ["DDD", "Event sourcing"] }
          ]
        }
        """;

    [Fact]
    public void Every_language_publishes_its_band_kind_and_summary_as_text()
    {
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("<h3>C#</h3>", html, StringComparison.Ordinal);
        Assert.Contains(">FULL</span>", html, StringComparison.Ordinal);
        Assert.Contains(">Deep</span>", html, StringComparison.Ordinal);
        Assert.Contains("Native Roslyn symbols resolve every lens.", html, StringComparison.Ordinal);
        Assert.Contains("<h3>JavaScript</h3>", html, StringComparison.Ordinal);
        Assert.Contains("Structural only", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Where_a_language_STOPS_is_published_beside_where_it_works()
    {
        // ★ A coverage table that lists only the wins is a sales page. The reason this one is worth
        //   anything to a buyer is that it says where a language stops.
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("Not read", html, StringComparison.Ordinal);
        var javascript = html[html.IndexOf("JavaScript", StringComparison.Ordinal)..];
        Assert.Contains("ip-lens-list-off", javascript, StringComparison.Ordinal);
        Assert.Contains(">DDD</span>", javascript, StringComparison.Ordinal);
    }

    [Fact]
    public void Each_band_word_and_kind_carries_its_meaning_as_a_mouseover()
    {
        // ★ A band called LOW beside a language's name reads as a judgement on the LANGUAGE unless
        //   something says otherwise. The page glosses the words; the mouseover glosses them where
        //   the reader is looking.
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("title=\"Every design grade that applies is read in full.\">FULL</span>", html, StringComparison.Ordinal);
        Assert.Contains("title=\"How the code is put together is read, but not the design built on it.\">LOW</span>", html, StringComparison.Ordinal);
        Assert.Contains("title=\"Watchdog reads the language&#39;s types and the design built on them.\">Deep</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Covered_lenses_show_as_the_grades_a_reader_can_find_each_once_with_a_mouseover()
    {
        var html = LanguageSupportTemplate.Render(Payload
            .Replace("\"coveredLenses\": [\"DDD\", \"Event sourcing\"]",
                "\"coveredLenses\": [\"DDD\", \"Event sourcing\", \"Vertical slice\", \"Correctness\", \"Structural\", \"Brand new lens\"]",
                StringComparison.Ordinal))!;
        var csharp = html[..html.IndexOf("JavaScript", StringComparison.Ordinal)];

        Assert.Contains(">Read for</span>", csharp, StringComparison.Ordinal);
        Assert.Contains("title=\"How the business data and its rules are modelled in the code.\">Domain Modelling</span>", csharp, StringComparison.Ordinal);
        Assert.Contains(">Event Sourcing</span>", csharp, StringComparison.Ordinal);
        Assert.Contains(">Code-level traps</span>", csharp, StringComparison.Ordinal);
        // "Structural" and "Vertical slice" fold into one Architecture chip.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(csharp, ">Architecture</span>"));
        Assert.DoesNotContain(">DDD</span>", csharp, StringComparison.Ordinal);
        Assert.DoesNotContain(">Vertical slice</span>", csharp, StringComparison.Ordinal);
        // A lens nobody has named yet stays visible under its own name.
        Assert.Contains("<span class=\"ip-lens\">Brand new lens</span>", csharp, StringComparison.Ordinal);
    }

    [Fact]
    public void The_payload_note_is_not_rendered_because_the_page_glosses_the_words()
    {
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.DoesNotContain("FIT is survey clarity", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_band_hue_follows_the_band_word_never_the_score_colours()
    {
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("ip-langband-full", html, StringComparison.Ordinal);
        Assert.Contains("ip-langband-low", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-band-exemplary", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-band-poor", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"languages\":[]}")]
    public void A_payload_with_no_languages_renders_nothing(string? payload) =>
        Assert.Null(LanguageSupportTemplate.Render(payload));

    [Fact]
    public void Text_from_the_payload_is_escaped()
    {
        var html = LanguageSupportTemplate.Render(
            Payload.Replace("Native Roslyn symbols resolve every lens.", "<script>x</script>", StringComparison.Ordinal))!;

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }
}
