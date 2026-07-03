namespace Imprint.Publishing;

/// <summary>
/// The editor-facing view of the publisher: the last report, nothing more. The editor
/// status bar reads <see cref="Last"/> and subscribes to <see cref="Changed"/> (fired
/// from the publisher's thread; UI code marshals via InvokeAsync, same contract as the
/// read models).
/// </summary>
public sealed class PublisherStatus
{
    public PublishReport? Last { get; private set; }

    public event Action? Changed;

    internal void Record(PublishReport report)
    {
        Last = report;
        Changed?.Invoke();
    }
}
