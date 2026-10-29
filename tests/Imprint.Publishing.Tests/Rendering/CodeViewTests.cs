using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// The code block is the one node whose whole job is to show characters that mean something
/// else in HTML, so its contract is narrow and worth pinning: the text is CONTENT (escaped by
/// the renderer, never a MarkupString), whitespace survives, and the language is a label —
/// never a script.
/// </summary>
public sealed class CodeViewTests
{
    private static readonly RenderContext Static = RenderHarness.Context(RenderMode.Static);

    private static CodeNode Code(string text, string? language = null) =>
        new() { Id = NodeId.New(), Text = text, Language = language };

    [Fact]
    public async Task Code_containing_markup_is_escaped_not_executed()
    {
        var html = await RenderHarness.RenderNode(Static, Code("<script>alert(1)</script>"));

        // The sample is what a reader must SEE; if it ever reaches the page as markup, a code
        // block about XSS becomes one.
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ampersands_and_angle_brackets_survive_as_themselves()
    {
        var html = await RenderHarness.RenderNode(Static, Code("if (a && b < c) { }"));

        Assert.Contains("&amp;&amp;", html, StringComparison.Ordinal);
        Assert.Contains("&lt; c", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Indentation_and_line_breaks_are_preserved()
    {
        const string source = "class A\n{\n    int B;\n}";

        var html = await RenderHarness.RenderNode(Static, Code(source));

        // Asserted on the DECODED text, not the bytes: the renderer writes newlines as `&#xA;`,
        // which a parser turns back into a newline inside <pre> — equivalent, and not a detail
        // worth freezing. What must hold is that the author's sample survives byte-for-byte
        // once decoded; a view that trimmed or re-indented would silently rewrite the program.
        var inner = System.Net.WebUtility.HtmlDecode(
            html[(html.IndexOf("<code>", StringComparison.Ordinal) + 6)..html.IndexOf("</code>", StringComparison.Ordinal)]);
        Assert.Equal(source, inner);
    }

    [Fact]
    public async Task A_language_becomes_a_language_class()
    {
        var html = await RenderHarness.RenderNode(Static, Code("SELECT 1", "sql"));

        Assert.Contains("class=\"language-sql\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"ip-code\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_language_emits_no_class_attribute_on_the_code_element()
    {
        var html = await RenderHarness.RenderNode(Static, Code("plain"));

        Assert.Contains("<code>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("csharp")]
    [InlineData("c#")]
    [InlineData("c++")]
    [InlineData("objective-c")]
    [InlineData(null)]
    public void Ordinary_language_tags_are_accepted(string? language) =>
        Assert.True(CodeNode.IsValidLanguage(language));

    [Theory]
    [InlineData("\" onload=\"alert(1)")]   // the attribute-escape attempt this guard exists for
    [InlineData("has space")]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void A_language_that_could_break_out_of_the_class_attribute_is_refused(string language) =>
        Assert.False(CodeNode.IsValidLanguage(language));

    [Fact]
    public async Task Json_keys_and_punctuation_are_marked_and_values_are_not()
    {
        var html = await RenderHarness.RenderNode(Static, Code("{\n  \"cai\": 74.2,\n  \"band\": \"Strong\"\n}", "json"));

        Assert.Contains("<span class=\"ip-code-key\">&quot;cai&quot;</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ip-code-key\">&quot;band&quot;</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ip-code-punct\">{</span>", html, StringComparison.Ordinal);
        // A string that is a value, not a key, stays plain text.
        Assert.DoesNotContain("<span class=\"ip-code-key\">&quot;Strong&quot;</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Yaml_keys_are_marked()
    {
        var html = await RenderHarness.RenderNode(Static, Code("- name: Measure\n  run: >\n    curl -X POST", "yaml"));

        Assert.Contains("<span class=\"ip-code-key\">name</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"ip-code-key\">run</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<span class=\"ip-code-key\">curl", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\n  \"a\": \"<script>\",\n  \"b\": [1, 2]\n}", "json")]
    [InlineData("- name: Measure\n  run: >\n    curl -H \"Authorization: Bearer $KEY\"", "yaml")]
    [InlineData("{ \"unterminated\": \"value", "json")]
    public async Task Marked_code_reads_back_as_exactly_the_authors_text(string source, string language)
    {
        var html = await RenderHarness.RenderNode(Static, Code(source, language));

        // The marks are spans only: strip them and the author's sample is unchanged, character for character.
        var start = html.IndexOf('>', html.IndexOf("<code", StringComparison.Ordinal)) + 1;
        var inner = html[start..html.IndexOf("</code>", StringComparison.Ordinal)];
        var text = System.Text.RegularExpressions.Regex.Replace(inner, "</?span[^>]*>", "");
        Assert.Equal(source, System.Net.WebUtility.HtmlDecode(text));
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Other_languages_are_not_marked()
    {
        var html = await RenderHarness.RenderNode(Static, Code("curl -H \"Authorization: Bearer $KEY\"", "bash"));

        Assert.DoesNotContain("<span", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Display_name_names_the_language_when_there_is_one()
    {
        Assert.Equal("Code (rust)", Code("fn main() {}", "rust").DisplayName);
        Assert.Equal("Code", Code("fn main() {}").DisplayName);
    }
}
