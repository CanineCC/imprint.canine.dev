namespace Imprint.EventSourcing;

/// <summary>Marker for command messages. Commands are records named as imperatives (<c>MoveNode</c>).</summary>
public interface ICommand;

/// <summary>
/// Optional data-shape validation (ranges, formats, required fields) run by the
/// dispatcher before the handler. Domain *invariants* do not belong here — they live
/// in the aggregate, where they can see current state.
/// </summary>
public interface IValidatableCommand : ICommand
{
    IEnumerable<string> Validate();
}

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// The outcome of a command: success, or human-readable errors. Domain errors are user
/// feedback, not exceptions — the editor shows <see cref="ErrorMessage"/> in a toast.
/// </summary>
public sealed record Result
{
    private Result(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }
    public string ErrorMessage => string.Join(" ", Errors);

    public static Result Ok() => new(true, []);
    public static Result Fail(params IReadOnlyList<string> errors) => new(false, errors);
}
