using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Markdown;

namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// Turns a post's markdown into the page shape the renderer expects.
///
/// <para>The converter emits CONTENT nodes — headings, prose, code — but the page root holds
/// sections and nothing else (<see cref="Placement"/>), so somebody has to wrap them. Doing it
/// here, in one function both the preview and the publisher call, is what makes the preview
/// honest: the two cannot drift into different shapes because there is only one shape.</para>
///
/// <para>The wrapper is a <see cref="SectionAppearance.Doc"/> section — the measure-width reading
/// column the theme already defines, described in its own comment as "the marketing look for a
/// whole markdown page". A blog post is exactly that, so this reuses the appearance rather than
/// inventing a second one that would drift from it.</para>
/// </summary>
public static class PostContent
{
    /// <summary>The page roots for a post body. Empty markdown yields an empty section rather than
    /// no section: a post being written is still a page, and a preview pane that vanishes between
    /// keystrokes is worse than one showing an empty column.</summary>
    public static NodeList Compose(NodeList body, Func<NodeId>? newId = null) =>
        NodeList.Of(new SectionNode
        {
            Id = (newId ?? NodeId.New)(),
            Appearance = SectionAppearance.Doc,
            Children = body,
        });

    /// <summary>Convert and wrap in one step — what the preview and the publisher both want.</summary>
    public static (NodeList Roots, IReadOnlyList<MarkdownProblem> Problems) Render(
        string markdown, Locale locale, Func<NodeId>? newId = null)
    {
        var conversion = MarkdownToNodes.Convert(markdown ?? "", locale, newId);
        return (Compose(conversion.Nodes, newId), conversion.Problems);
    }
}
