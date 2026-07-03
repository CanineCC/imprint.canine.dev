using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.ChangeNodeProps;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Projections;
using Imprint.Editor.Services;
using Microsoft.AspNetCore.Components;

namespace Imprint.Editor.Components.Panels.Inspector;

/// <summary>
/// Shared plumbing for the typed node editors: the selected node arrives as a
/// parameter, prop changes leave as full-replacement <see cref="ChangeNodeProps"/>
/// commands whose compensating inverse is the node as it is right now, and localized
/// text edits leave as <see cref="EditText"/> pairs the same way. Editors only decide
/// *what* changed; this class owns *how* a change travels.
/// </summary>
public abstract class NodeEditorBase<TNode> : ComponentBase where TNode : Node
{
    [Inject] protected EditorSession Session { get; set; } = null!;
    [Inject] protected CommandRunner Commands { get; set; } = null!;
    [Inject] protected SiteOverview Site { get; set; } = null!;

    [Parameter, EditorRequired] public TNode Node { get; set; } = null!;

    /// <summary>" (en)" behind per-language field labels — only when the site has several languages.</summary>
    protected string LocaleSuffix =>
        Site.Current is { Locales.Count: > 1 } ? $" ({Session.EditLocale.Value})" : string.Empty;

    /// <summary>Dispatches the full replacement node; undo puts back the node as it is now.</summary>
    protected Task Apply(TNode replacement, string label) =>
        Session.CurrentPageId is { } pageId
            ? Commands.Run(
                new ChangeNodeProps(pageId, replacement),
                new ChangeNodeProps(pageId, Node),
                label)
            : Task.CompletedTask;

    /// <summary>Dispatches a localized text edit for the current edit language; undo restores the old value.</summary>
    protected Task CommitText(string field, string oldValue, string newValue, string label)
    {
        if (Session.CurrentPageId is not { } pageId || oldValue == newValue)
        {
            return Task.CompletedTask;
        }

        var locale = Session.EditLocale.Value;
        return Commands.Run(
            new EditText(pageId, Node.Id, field, locale, newValue),
            new EditText(pageId, Node.Id, field, locale, oldValue),
            label);
    }
}
