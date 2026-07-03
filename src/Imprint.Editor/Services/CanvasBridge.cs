using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.MoveNode;
using Imprint.Authoring.Projections;
using Microsoft.JSInterop;

namespace Imprint.Editor.Services;

public sealed record SelectionRect(double X, double Y, double Width, double Height);

/// <summary>Wire shape of a drag plan (docs/editor-ux.md §3). Ids travel in compact guid form.</summary>
public sealed record DragPlanDto(string DragLabel, IReadOnlyList<DragSlotDto> Slots);

public sealed record DragSlotDto(int SlotId, string ParentId, int Index, string AnchorId, string Edge, string Orientation);

/// <summary>
/// The [JSInvokable] surface of the canvas (counterpart of canvas-interop.js). JS
/// reports intents and geometry; every decision happens here: selection changes go to
/// the session, mutations become commands with pre-computed compensating inverses.
/// </summary>
public sealed class CanvasBridge(EditorSession session, CommandRunner commands) : IDisposable
{
    private SlotPlanner.DragPlan? _activePlan;

    // Inline-edit session state. _editOriginalValue is the value before the session
    // started and is kept for the WHOLE session so every commit (first + debounced
    // autosaves + final blur) shares one undo entry: undo reverts to the original,
    // redo replays the final value. _textCommitted flips on the first real commit.
    private string? _editOriginalValue;
    private bool _textCommitted;

    /// <summary>The canvas component (or a panel) opens the insert picker at these coordinates.</summary>
    public event Action<NodeId, int, double, double>? GapPickerRequested;

    /// <summary>Raised when the selection's overlay rect moved — positions the node toolbar.</summary>
    public event Action<SelectionRect?>? SelectionRectChanged;

    /// <summary>Set by the canvas component so the bridge can drive the JS module.</summary>
    public Func<string, string, Task>? EnterInlineEditInJs { get; set; }

    // ------------------------------------------------------------- selection

    [JSInvokable]
    public void ReportClick(string? nodeId) =>
        session.Select(nodeId is not null && NodeId.TryParse(nodeId, out var id) ? id : null);

    [JSInvokable]
    public async Task ReportDoubleClick(string nodeId)
    {
        if (NodeId.TryParse(nodeId, out var id))
        {
            session.Select(id);
            await TryStartInlineEdit(id);
        }
    }

    [JSInvokable]
    public void ReportSelectionRect(double x, double y, double width, double height) =>
        SelectionRectChanged?.Invoke(new SelectionRect(x, y, width, height));

    // ------------------------------------------------------------------ drag

    [JSInvokable]
    public Task<DragPlanDto?> BeginDrag(string nodeId)
    {
        _activePlan = null;
        if (session.CurrentPage is not { } page || !NodeId.TryParse(nodeId, out var id))
        {
            return Task.FromResult<DragPlanDto?>(null);
        }

        _activePlan = SlotPlanner.Plan(page.Tree, id);
        return Task.FromResult(_activePlan is null
            ? null
            : new DragPlanDto(
                _activePlan.DragLabel,
                [.. _activePlan.Slots.Select(slot => new DragSlotDto(
                    slot.SlotId,
                    slot.ParentId.IsRoot ? "" : slot.ParentId.Compact,
                    slot.Index,
                    slot.AnchorId.Compact,
                    slot.Edge switch
                    {
                        SlotPlanner.SlotEdge.Before => "before",
                        SlotPlanner.SlotEdge.After => "after",
                        _ => "into",
                    },
                    slot.Orientation == SlotPlanner.SlotOrientation.Horizontal ? "h" : "v"))]));
    }

    [JSInvokable]
    public async Task CompleteDrag(int slotId)
    {
        var plan = _activePlan;
        _activePlan = null;
        if (plan is null || session.CurrentPage is not { } page ||
            plan.Slots.FirstOrDefault(s => s.SlotId == slotId) is not { } slot)
        {
            return;
        }

        // The inverse is "move it back": current location, captured before dispatch,
        // already speaks after-removal indexes — the same frame MoveNode uses.
        var (oldParentId, oldIndex) = LocationOf(page, plan.NodeId);
        await commands.Run(
            new Authoring.Features.Pages.MoveNode.MoveNode(page.Id, plan.NodeId, slot.ParentId, slot.Index),
            new Authoring.Features.Pages.MoveNode.MoveNode(page.Id, plan.NodeId, oldParentId, oldIndex),
            "move");
    }

    [JSInvokable]
    public void CancelDrag() => _activePlan = null;

    // ------------------------------------------------------------------ keys

    [JSInvokable]
    public async Task ReportKey(string key, bool ctrl, bool alt, bool shift)
    {
        switch (key)
        {
            case "Escape":
                session.SelectParent();
                break;
            case "Delete" or "Backspace":
                await DeleteSelection();
                break;
            case "Enter" when session.Selection is { } id:
                await TryStartInlineEdit(id);
                break;
            case "z" when ctrl && shift:
            case "y" when ctrl:
                await commands.Redo();
                break;
            case "z" when ctrl:
                await commands.Undo();
                break;
            case "d" when ctrl:
                await DuplicateSelection();
                break;
            case "ArrowUp" or "ArrowDown" when alt:
                await NudgeSelection(key == "ArrowUp" ? -1 : +1);
                break;
            case "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight":
                NavigateSelection(key);
                break;
            case "/" when session.Selection is { } selected && session.CurrentPage is { } page:
                var (parentId, index) = LocationOf(page, selected);
                GapPickerRequested?.Invoke(parentId, index + 1, 0, 0);
                break;
        }
    }

    [JSInvokable]
    public void ReportGapClick(string parentId, int index, double x, double y)
    {
        var parent = parentId.Length == 0 ? NodeId.Root
            : NodeId.TryParse(parentId, out var id) ? id : NodeId.Root;
        GapPickerRequested?.Invoke(parent, index, x, y);
    }

    // ----------------------------------------------------------- inline edit

    public async Task TryStartInlineEdit(NodeId nodeId)
    {
        if (session.CurrentPage?.Tree.Find(nodeId) is not { } node)
        {
            return;
        }

        var mode = node switch
        {
            HeadingNode or ButtonNode => "plain",
            RichTextNode => "rich",
            _ => null,
        };
        if (mode is null || EnterInlineEditInJs is null)
        {
            return;
        }

        _editOriginalValue = CurrentTextOf(node);
        _textCommitted = false;
        session.BeginInlineEdit(nodeId);
        await EnterInlineEditInJs(nodeId.Compact, mode);
    }

    [JSInvokable]
    public async Task CommitText(string nodeId, string value)
    {
        if (NodeId.TryParse(nodeId, out var id))
        {
            await CommitTextCore(id, value);
        }
    }

    [JSInvokable]
    public async Task EndInlineEdit(string nodeId, bool committed, string value)
    {
        if (NodeId.TryParse(nodeId, out var id) && committed)
        {
            await CommitTextCore(id, value);
        }

        _editOriginalValue = null;
        session.EndInlineEdit();
    }

    private async Task CommitTextCore(NodeId nodeId, string value)
    {
        if (session.CurrentPage is not { } page || page.Tree.Find(nodeId) is not { } node)
        {
            return;
        }

        var field = FieldOf(node);
        if (field is null || CurrentTextOf(node) == value)
        {
            return;
        }

        var command = new Authoring.Features.Pages.EditText.EditText(
            page.Id, nodeId, field, session.EditLocale.Value, value);
        var inverse = new Authoring.Features.Pages.EditText.EditText(
            page.Id, nodeId, field, session.EditLocale.Value, _editOriginalValue ?? string.Empty);

        // One undo entry per edit session: the first commit pushes it (inverse = the
        // pre-session value), and every later autosave/blur amends its forward command
        // so redo replays the final text, not a stale mid-edit value.
        if (!_textCommitted)
        {
            _textCommitted = true;
            await commands.Run(command, inverse, "text edit");
        }
        else
        {
            await commands.Amend(command, inverse, "text edit");
        }
    }

    // ------------------------------------------------------------- mutations

    private async Task DeleteSelection()
    {
        if (session.Selection is not { } id || session.CurrentPage is not { } page ||
            page.Tree.Find(id) is not { } node)
        {
            return;
        }

        var (parentId, index) = LocationOf(page, id);
        session.Select(page.Tree.ParentOf(id)?.Id);
        await commands.Run(
            new Authoring.Features.Pages.RemoveNode.RemoveNode(page.Id, id),
            new Authoring.Features.Pages.AddNode.AddNode(page.Id, parentId, index, node),
            node.DisplayName.ToLowerInvariant() + " removal");
    }

    private async Task DuplicateSelection()
    {
        if (session.Selection is not { } id || session.CurrentPage is not { } page || !page.Tree.Contains(id))
        {
            return;
        }

        // We mint the copy id here so the inverse can remove exactly that node.
        var copyId = NodeId.New();
        if (await commands.Run(
                new Authoring.Features.Pages.DuplicateNode.DuplicateNode(page.Id, id, copyId),
                new Authoring.Features.Pages.RemoveNode.RemoveNode(page.Id, copyId),
                "duplicate"))
        {
            session.Select(copyId);
        }
    }

    private async Task NudgeSelection(int delta)
    {
        if (session.Selection is not { } id || session.CurrentPage is not { } page ||
            !page.Tree.Contains(id))
        {
            return;
        }

        var (parentId, index) = LocationOf(page, id);
        var newIndex = index + delta;
        if (newIndex < 0)
        {
            return;
        }

        await commands.Run(
            new Authoring.Features.Pages.MoveNode.MoveNode(page.Id, id, parentId, newIndex),
            new Authoring.Features.Pages.MoveNode.MoveNode(page.Id, id, parentId, index),
            "move");
    }

    private void NavigateSelection(string key)
    {
        var page = session.CurrentPage;
        if (page is null)
        {
            return;
        }

        if (session.Selection is not { } id || page.Tree.Find(id) is not { } node)
        {
            session.Select(page.Tree.Roots.Count > 0 ? page.Tree.Roots[0].Id : null);
            return;
        }

        switch (key)
        {
            case "ArrowLeft":
                session.SelectParent();
                break;
            case "ArrowRight" when node is IContainerNode { Children.Count: > 0 } container:
                session.Select(container.Children[0].Id);
                break;
            case "ArrowUp" or "ArrowDown":
                var siblings = page.Tree.ParentOf(id) is IContainerNode parent ? parent.Children : page.Tree.Roots;
                var at = siblings.IndexOf(id);
                var next = key == "ArrowUp" ? at - 1 : at + 1;
                if (next >= 0 && next < siblings.Count)
                {
                    session.Select(siblings[next].Id);
                }

                break;
        }
    }

    // --------------------------------------------------------------- helpers

    /// <summary>Location in the after-removal frame (= index among current siblings).</summary>
    private static (NodeId ParentId, int Index) LocationOf(Page page, NodeId id)
    {
        var parent = page.Tree.ParentOf(id);
        var siblings = parent is IContainerNode container ? container.Children : page.Tree.Roots;
        return (parent?.Id ?? NodeId.Root, siblings.IndexOf(id));
    }

    private string? CurrentTextOf(Node node) => node switch
    {
        HeadingNode heading => heading.Text.Get(session.EditLocale) ?? "",
        ButtonNode button => button.Label.Get(session.EditLocale) ?? "",
        RichTextNode richText => richText.Html.Get(session.EditLocale) ?? "",
        _ => null,
    };

    private static string? FieldOf(Node node) => node switch
    {
        HeadingNode => "text",
        ButtonNode => "label",
        RichTextNode => "html",
        _ => null,
    };

    public void Dispose()
    {
        GapPickerRequested = null;
        SelectionRectChanged = null;
    }
}
