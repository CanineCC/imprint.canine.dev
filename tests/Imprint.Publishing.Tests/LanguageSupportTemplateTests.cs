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

        Assert.Contains("Not applicable", html, StringComparison.Ordinal);
        var javascript = html[html.IndexOf("JavaScript", StringComparison.Ordinal)..];
        Assert.Contains("ip-lens-list-off", javascript, StringComparison.Ordinal);
        Assert.Contains(">DDD</span>", javascript, StringComparison.Ordinal);
    }

    [Fact]
    public void The_note_that_FIT_is_not_a_quality_judgement_is_rendered_not_paraphrased()
    {
        // ★ A band called LOW beside a language's name reads as a judgement on the LANGUAGE unless
        //   something says otherwise, and that misreading costs a customer. The sentence is the
        //   product's, printed verbatim.
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("NOT a measure of language or code quality", html, StringComparison.Ordinal);
    }

    [Fact]
    public void The_band_hue_is_this_sites_but_the_band_WORD_is_the_payloads()
    {
        var html = LanguageSupportTemplate.Render(Payload)!;

        Assert.Contains("ip-band-exemplary", html, StringComparison.Ordinal);
        Assert.Contains("ip-band-poor", html, StringComparison.Ordinal);
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
