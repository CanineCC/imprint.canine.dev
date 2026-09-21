using System.Text;
using System.Text.Json;
using static Imprint.Publishing.TemplateJson;

namespace Imprint.Publishing;

/// <summary>
/// A numbered flow — scan, task, fix, verify — rendered from the widget's own <c>nodes</c> prop.
/// </summary>
/// <remarks>
/// <para>★ A DIAGRAM MADE ENTIRELY OF SENTENCES. Each step carries a title and a line of prose, and
/// the whole thing lived in a shadow root: the page that explains how the agent loop works said
/// nothing a crawler or a language model could read. Nothing here is pictorial — the boxes were the
/// decoration, the words were the content.</para>
/// <para>An ordered list, because the steps ARE ordered and that is the one fact a row of boxes
/// leaves to visual inference. A screen reader now hears "1 of 4" instead of four unrelated
/// headings.</para>
/// </remarks>
public static class FlowTemplate
{
    public const string Name = "flow";

    private const string NodesProp = "nodes";
    private const string LoopLabelProp = "loop-label";

    public static string? Render(Func<string, string?> props)
    {
        ArgumentNullException.ThrowIfNull(props);

        if (Root(props(NodesProp)) is not { ValueKind: JsonValueKind.Array } root)
        {
            return null;
        }

        var steps = root.EnumerateArray().Where(n => Str(n, "title").Length > 0).ToList();
        if (steps.Count == 0)
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<ol class=\"ip-flow\">");
        foreach (var step in steps)
        {
            html.Append("<li class=\"ip-flow-step\">");
            html.Append("<span class=\"ip-flow-title\">").Append(Esc(Str(step, "title"))).Append("</span>");
            if (Str(step, "body") is { Length: > 0 } body)
            {
                html.Append("<span class=\"ip-flow-body\">").Append(Esc(body)).Append("</span>");
            }

            html.Append("</li>");
        }

        html.Append("</ol>");

        // The loop label is the point of the diagram — that it comes back round — and it is the part
        // an arrow was carrying. An arrow is not readable; this sentence is.
        if (props(LoopLabelProp) is { Length: > 0 } loop)
        {
            html.Append("<p class=\"ip-flow-loop\">").Append(Esc(loop)).Append("</p>");
        }

        return html.ToString();
    }
}
