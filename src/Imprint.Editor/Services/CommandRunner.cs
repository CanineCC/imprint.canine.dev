using Imprint.EventSourcing;

namespace Imprint.Editor.Services;

public sealed record Toast(string Message, bool IsError, string? UndoLabel = null, Func<Task>? UndoAction = null);

/// <summary>Per-circuit toast stream; the shell renders it.</summary>
public sealed class ToastService
{
    public event Action<Toast>? Shown;

    public void Show(string message) => Shown?.Invoke(new Toast(message, IsError: false));
    public void Error(string message) => Shown?.Invoke(new Toast(message, IsError: true));
    public void WithUndo(string message, Func<Task> undo) =>
        Shown?.Invoke(new Toast(message, IsError: false, "Undo", undo));
}

/// <summary>
/// The editor's single write path: dispatch, surface domain errors as toasts, and —
/// the event-sourcing showcase — maintain undo/redo as *compensating commands*.
/// Nothing is ever rolled back; undoing appends the inverse decision, exactly like a
/// human would. History stays honest.
/// </summary>
public sealed class CommandRunner(ICommandDispatcher dispatcher, ToastService toasts)
{
    private sealed record UndoEntry(string Label, ICommand Command, ICommand Inverse);

    // Lists, not Stacks, so the top entry can be amended in place (see Amend).
    private readonly List<UndoEntry> _undo = [];
    private readonly List<UndoEntry> _redo = [];

    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoLabel => _undo.Count > 0 ? _undo[^1].Label : null;

    /// <summary>
    /// Runs a mutating command that isn't (yet) undoable. It still clears the redo
    /// stack: any new mutation makes the redo timeline meaningless, and leaving a stale
    /// redo target would let a later Ctrl+Y replay an inverse against a page it was
    /// never computed for.
    /// </summary>
    public async Task<bool> Run(ICommand command)
    {
        if (!await Dispatch(command))
        {
            return false;
        }

        if (_redo.Count > 0)
        {
            _redo.Clear();
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Runs an undoable command. The inverse is computed by the caller *before*
    /// dispatch, from current read-model state — the only moment the old values exist.
    /// </summary>
    public async Task<bool> Run(ICommand command, ICommand inverse, string label)
    {
        if (!await Dispatch(command))
        {
            return false;
        }

        _undo.Add(new UndoEntry(label, command, inverse));
        _redo.Clear();
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Amends the forward command of the current top undo entry, for a follow-up edit
    /// in the same session (the debounced autosaves after the first commit of an inline
    /// edit). The inverse stays pinned to the pre-session value, so one Ctrl+Z reverts
    /// the whole session and Ctrl+Y replays the FINAL value — not a stale mid-edit one.
    /// Falls back to a plain undoable entry if there is no matching session on top.
    /// </summary>
    public async Task<bool> Amend(ICommand command, ICommand inverse, string label)
    {
        if (_undo.Count == 0 || _undo[^1].Label != label)
        {
            return await Run(command, inverse, label);
        }

        if (!await Dispatch(command))
        {
            return false;
        }

        _undo[^1] = _undo[^1] with { Command = command };
        _redo.Clear();
        Changed?.Invoke();
        return true;
    }

    public async Task Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        // Peek, then pop only on success: if the inverse now fails (a concurrent edit
        // from another tab removed the target), the step stays on the stack to retry
        // rather than vanishing and desyncing the whole history.
        var entry = _undo[^1];
        if (await Dispatch(entry.Inverse))
        {
            _undo.RemoveAt(_undo.Count - 1);
            _redo.Add(entry);
            toasts.Show($"Undid {entry.Label}");
        }

        Changed?.Invoke();
    }

    public async Task Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        var entry = _redo[^1];
        if (await Dispatch(entry.Command))
        {
            _redo.RemoveAt(_redo.Count - 1);
            _undo.Add(entry);
        }

        Changed?.Invoke();
    }

    /// <summary>Dispatch + surface domain errors as toasts. Does not touch the undo/redo stacks.</summary>
    private async Task<bool> Dispatch(ICommand command)
    {
        var result = await dispatcher.Dispatch(command);
        if (!result.Succeeded)
        {
            toasts.Error(result.ErrorMessage);
        }

        return result.Succeeded;
    }
}
