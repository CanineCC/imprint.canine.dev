using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Imprint.EventSourcing;

public interface ICommandDispatcher
{
    Task<Result> Dispatch(ICommand command, CancellationToken ct = default);
}

/// <summary>
/// The single write path. Every state change in the system flows through here:
/// validate → handle (with bounded optimistic-concurrency retry) → synchronously
/// catch up projections, so the caller reads its own writes the moment Dispatch
/// returns.
/// </summary>
public sealed class CommandDispatcher(
    IServiceProvider services,
    EventMetadataProvider metadata,
    ProjectionEngine projections,
    ILogger<CommandDispatcher> logger) : ICommandDispatcher
{
    private const int MaxAttempts = 3;

    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public async Task<Result> Dispatch(ICommand command, CancellationToken ct = default)
    {
        if (command is IValidatableCommand validatable)
        {
            var errors = validatable.Validate().ToList();
            if (errors.Count > 0)
            {
                return Result.Fail([.. errors]);
            }
        }

        using var commandScope = metadata.BeginCommandScope();
        var invoker = Invokers.GetOrAdd(command.GetType(), HandlerInvoker.For);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // A scope per attempt: a retried command must re-resolve and re-load
                // everything — the previous attempt's aggregates are stale by definition.
                await using var scope = services.CreateAsyncScope();
                var result = await invoker.Invoke(scope.ServiceProvider, command, ct);
                if (result.Succeeded)
                {
                    await projections.CatchUp(ct);
                }

                return result;
            }
            catch (DomainException domainError)
            {
                return Result.Fail(domainError.Message);
            }
            catch (ConcurrencyException conflict) when (attempt < MaxAttempts)
            {
                logger.LogDebug(
                    "Concurrency conflict on {Stream}; re-deciding {Command} (attempt {Attempt}).",
                    conflict.StreamId, command.GetType().Name, attempt + 1);
            }
            catch (ConcurrencyException)
            {
                return Result.Fail("The content was changed by someone else while saving. Please try again.");
            }
        }
    }

    /// <summary>Bridges the non-generic dispatch call to the generic handler, cached per command type.</summary>
    private abstract class HandlerInvoker
    {
        public abstract Task<Result> Invoke(IServiceProvider scope, ICommand command, CancellationToken ct);

        public static HandlerInvoker For(Type commandType) =>
            (HandlerInvoker)Activator.CreateInstance(typeof(Typed<>).MakeGenericType(commandType))!;

        private sealed class Typed<TCommand> : HandlerInvoker where TCommand : ICommand
        {
            public override Task<Result> Invoke(IServiceProvider scope, ICommand command, CancellationToken ct) =>
                scope.GetRequiredService<ICommandHandler<TCommand>>().Handle((TCommand)command, ct);
        }
    }
}
