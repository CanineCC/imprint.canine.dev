using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Imprint.Publishing;

/// <summary>
/// HTML text encoding for the document chrome. The default Blazor encoder turns every
/// non-ASCII character into a numeric entity (<c>·</c> → <c>&amp;#xB7;</c>), which is
/// pointless noise in UTF-8 output — this encoder still escapes the HTML-sensitive
/// characters but lets Unicode through as-is.
/// </summary>
internal static class HtmlText
{
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static string Encode(string value) => Encoder.Encode(value);
}
