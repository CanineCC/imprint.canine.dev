using System.Text.RegularExpressions;

namespace Imprint.Publishing.Tests.Rendering;

/// <summary>
/// Two components may not share a class name.
/// </summary>
/// <remarks>
/// <para>★★ WRITTEN AFTER A LIVE DEFECT THAT NOTHING ELSE COULD SEE. The language coverage table
/// introduced <c>.ip-lang</c> for one of its rows. The header's EN/DA switcher is ALSO
/// <c>.ip-lang</c>, defined about 900 lines earlier, and the later rule wins on source order — so
/// the switcher silently became a two-column grid and its first link took the 12rem track. "EN"
/// rendered 192px wide on every page of every multilingual site, and neither the template, the
/// component's own stylesheet block, nor any test looked wrong.</para>
/// <para>The check is deliberately narrow: a bare class selector declared at TOP LEVEL more than
/// once, by rules that disagree about <c>display</c>. Sharing a name to add padding is a style
/// choice; sharing one to change the box model is two components fighting, and the loser is
/// whichever one was written first.</para>
/// </remarks>
public sealed class StylesheetCollisionTests
{
    [Fact]
    public void No_class_is_laid_out_two_different_ways_by_two_different_rules()
    {
        var css = StripComments(ReadMarketingSheet());
        var byClass = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (selector, body) in TopLevelRules(css))
        {
            // Only the simple `.foo { … }` shape — a descendant or state selector is a deliberate
            // narrowing, not a second component laying claim to the name.
            if (!Regex.IsMatch(selector, @"^\.[a-z][a-z0-9-]*$"))
            {
                continue;
            }

            var display = Regex.Match(body, @"(?<!-)\bdisplay\s*:\s*([a-z- ]+?)\s*;");
            if (!display.Success)
            {
                continue;
            }

            if (!byClass.TryGetValue(selector, out var displays))
            {
                byClass[selector] = displays = new HashSet<string>(StringComparer.Ordinal);
            }

            displays.Add(display.Groups[1].Value.Trim());
        }

        var clashes = byClass.Where(x => x.Value.Count > 1)
            .Select(x => $"{x.Key} is laid out as {string.Join(" AND ", x.Value.Order())}")
            .ToList();

        Assert.True(clashes.Count == 0,
            "Two components share a class name and disagree about how it is laid out. Rename one:\n  "
            + string.Join("\n  ", clashes));
    }

    /// <summary>
    /// Remove comments before counting braces.
    /// </summary>
    /// <remarks>
    /// ★★ WITHOUT THIS THE TEST PASSES ON EVERY INPUT, which is worse than not having it. Six
    /// braces live inside comments in this sheet (a banner drawing, and two notes that quote a
    /// selector such as <c>`.ip-nav-cta-item { display: … }`</c>). The depth counter counted them,
    /// desynced, and the walk then yielded nothing at all — so the assertion compared an empty set
    /// and reported green. It was caught by reintroducing the very collision it was written for and
    /// watching it stay green.
    /// </remarks>
    private static string StripComments(string css) =>
        System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", " ",
            System.Text.RegularExpressions.RegexOptions.Singleline);

    /// <summary>Top-level rules only: anything nested in @media/@container is a narrowing of its own rule.</summary>
    private static IEnumerable<(string Selector, string Body)> TopLevelRules(string css)
    {
        var depth = 0;
        var start = 0;
        for (var i = 0; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                if (depth == 0)
                {
                    var selector = css[start..i].Trim();
                    var close = MatchingBrace(css, i);
                    if (close > i && !selector.StartsWith('@'))
                    {
                        yield return (selector, css[(i + 1)..close]);
                    }
                }

                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    start = i + 1;
                }
            }
        }
    }

    private static int MatchingBrace(string css, int open)
    {
        var depth = 0;
        for (var i = open; i < css.Length; i++)
        {
            if (css[i] == '{') { depth++; }
            else if (css[i] == '}' && --depth == 0) { return i; }
        }

        return -1;
    }

    private static string ReadMarketingSheet()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imprint.Rendering")))
        {
            dir = dir.Parent;
        }

        var path = Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("repository root not found"),
            "src", "Imprint.Rendering", "wwwroot", "styles", "imprint-marketing.css");

        return File.ReadAllText(path);
    }
}
