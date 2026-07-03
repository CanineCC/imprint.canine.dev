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

    private readonly Stack<UndoEntry> _undo = new();
    private readonly Stack<UndoEntry> _redo = new();

    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoLabel => _undo.TryPeek(out var entry) ? entry.Label : null;

    /// <summary>Runs a command; failures become toasts. Returns success.</summary>
    public async Task<bool> Run(ICommand command)
    {
        var result = await dispatcher.Dispatch(command);
        if (!result.Succeeded)
        {
            toasts.Error(result.ErrorMessage);
        }

        return result.Succeeded;
    }

    /// <summary>
    /// Runs an undoable command. The inverse is computed by the caller *before*
    /// dispatch, from current read-model state — the only moment the old values exist.
    /// </summary>
    public async Task<bool> Run(ICommand command, ICommand inverse, string label)
    {
        if (!await Run(command))
        {
            return false;
        }

        _undo.Push(new UndoEntry(label, command, inverse));
        _redo.Clear();
        Changed?.Invoke();
        return true;
    }

    public async Task Undo()
    {
        if (!_undo.TryPop(out var entry))
        {
            return;
        }

        if (await Run(entry.Inverse))
        {
            _redo.Push(entry);
            toasts.Show($"Undid {entry.Label}");
        }

        Changed?.Invoke();
    }

    public async Task Redo()
    {
        if (!_redo.TryPop(out var entry))
        {
            return;
        }

        if (await Run(entry.Command))
        {
            _undo.Push(entry);
        }

        Changed?.Invoke();
    }
}
