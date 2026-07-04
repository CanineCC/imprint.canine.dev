namespace Imprint.Publishing;

/// <summary>
/// The single writer lock over the published output folders. A publish pass and a
/// promotion — or two promotions — that touch the same folder cannot both be a
/// projection of the truth, so every writer takes this one gate: the background
/// auto-sync, on-demand <c>Publish to &lt;env&gt;</c>, and <c>Promote</c>. Deploys are
/// user-paced and rare, so one process-wide gate is simpler than a per-folder lock table
/// and costs nothing. Held only for the duration of a single pass or mirror.
/// </summary>
public sealed class PublishGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<T> RunExclusive<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RunExclusive(Func<Task> action, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }
}
