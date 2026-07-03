using Imprint.Editor.Services;
using Imprint.EventSourcing;

namespace Imprint.Editor.Tests;

/// <summary>
/// The undo/redo compensating-command machinery is the flagship event-sourcing
/// showcase, and the audit found real corruption bugs in it — these pin the fixes:
/// redo is invalidated by any new mutation, a failing compensation is not lost, and a
/// multi-autosave edit session redoes to its final value.
/// </summary>
public sealed class CommandRunnerTests
{
    private sealed record Cmd(string Name) : ICommand;

    /// <summary>Records dispatched commands; can be told to fail specific ones.</summary>
    private sealed class FakeDispatcher : ICommandDispatcher
    {
        public List<ICommand> Dispatched { get; } = [];
        public Func<ICommand, bool> Succeeds { get; set; } = _ => true;

        public Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
        {
            if (!Succeeds(command))
            {
                return Task.FromResult(Result.Fail("nope"));
            }

            Dispatched.Add(command);
            return Task.FromResult(Result.Ok());
        }
    }

    private static (CommandRunner Runner, FakeDispatcher Dispatcher) NewRunner()
    {
        var dispatcher = new FakeDispatcher();
        return (new CommandRunner(dispatcher, new ToastService()), dispatcher);
    }

    [Fact]
    public async Task Undo_then_redo_replays_the_forward_command()
    {
        var (runner, dispatcher) = NewRunner();
        await runner.Run(new Cmd("do"), new Cmd("undo"), "action");

        await runner.Undo();
        Assert.Equal("undo", ((Cmd)dispatcher.Dispatched[^1]).Name);
        Assert.False(runner.CanUndo);
        Assert.True(runner.CanRedo);

        await runner.Redo();
        Assert.Equal("do", ((Cmd)dispatcher.Dispatched[^1]).Name);
        Assert.True(runner.CanUndo);
        Assert.False(runner.CanRedo);
    }

    [Fact]
    public async Task A_new_mutation_clears_the_redo_stack()
    {
        var (runner, _) = NewRunner();
        await runner.Run(new Cmd("a"), new Cmd("a'"), "a");
        await runner.Undo();
        Assert.True(runner.CanRedo);

        // A non-undoable mutation must still invalidate the now-divergent redo timeline.
        await runner.Run(new Cmd("b"));
        Assert.False(runner.CanRedo);
    }

    [Fact]
    public async Task A_failing_undo_keeps_the_entry_for_retry()
    {
        var (runner, dispatcher) = NewRunner();
        await runner.Run(new Cmd("do"), new Cmd("undo"), "action");

        // The compensation fails (e.g. a concurrent edit removed its target).
        dispatcher.Succeeds = c => ((Cmd)c).Name != "undo";
        await runner.Undo();
        Assert.True(runner.CanUndo);   // not lost
        Assert.False(runner.CanRedo);

        // Once the conflict clears, the same undo succeeds.
        dispatcher.Succeeds = _ => true;
        await runner.Undo();
        Assert.False(runner.CanUndo);
        Assert.True(runner.CanRedo);
    }

    [Fact]
    public async Task Amend_redoes_to_the_final_value_of_a_multi_commit_session()
    {
        var (runner, dispatcher) = NewRunner();

        // First commit pushes the entry (inverse pinned to the pre-edit value);
        // subsequent autosaves amend the forward command.
        await runner.Run(new Cmd("v1"), new Cmd("original"), "text edit");
        await runner.Amend(new Cmd("v2"), new Cmd("original"), "text edit");
        await runner.Amend(new Cmd("v3"), new Cmd("original"), "text edit");

        await runner.Undo();
        Assert.Equal("original", ((Cmd)dispatcher.Dispatched[^1]).Name);

        await runner.Redo();
        Assert.Equal("v3", ((Cmd)dispatcher.Dispatched[^1]).Name); // final value, not a stale v1/v2
    }

    [Fact]
    public async Task Amend_without_a_matching_session_on_top_starts_a_new_entry()
    {
        var (runner, _) = NewRunner();
        await runner.Run(new Cmd("other"), new Cmd("other'"), "move");

        // No "text edit" session on top → Amend behaves like a fresh undoable Run.
        await runner.Amend(new Cmd("v1"), new Cmd("orig"), "text edit");
        await runner.Undo(); // undoes the text edit
        await runner.Undo(); // undoes the move
        Assert.False(runner.CanUndo);
    }

    [Fact]
    public async Task A_failed_command_records_nothing()
    {
        var (runner, dispatcher) = NewRunner();
        dispatcher.Succeeds = _ => false;

        Assert.False(await runner.Run(new Cmd("x"), new Cmd("x'"), "x"));
        Assert.False(runner.CanUndo);
        Assert.Empty(dispatcher.Dispatched);
    }
}
