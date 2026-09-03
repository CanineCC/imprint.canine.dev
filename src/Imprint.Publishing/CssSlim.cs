using System.Text;
using System.Text.RegularExpressions;

namespace Imprint.Publishing;

/// <summary>
/// Publish-plane comment stripping for the composed stylesheet. The source files stay
/// documented — the comments are for the next maintainer — but a visitor pays for none
/// of them. Deliberately NOT a general minifier: rules, selectors and values ship
/// byte-for-byte as authored, so a rendering diff can never be introduced here; only
/// <c>/* … */</c> blocks and the blank lines they leave go.
/// </summary>
public static partial class CssSlim
{
    [GeneratedRegex(@"/\*[\s\S]*?\*/")]
    private static partial Regex Comments();

    public static string Strip(string css)
    {
        var stripped = Comments().Replace(css, string.Empty);
        var builder = new StringBuilder(stripped.Length);
        foreach (var line in stripped.Split('\n'))
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Length > 0)
            {
                builder.Append(trimmed).Append('\n');
            }
        }

        return builder.ToString();
    }
}
