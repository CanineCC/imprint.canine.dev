namespace Imprint.Publishing;

/// <summary>
/// "Something a page renders from, that does not live in this process, has changed."
/// </summary>
/// <remarks>
/// <para>Pages can depend on data fetched from another service at publish time (see
/// <c>WidgetDescriptor.Prerender</c>). Nothing in this process moves when that data does, so the
/// publisher would not otherwise know to look — the site would be as fresh as the last time somebody
/// edited a page, which for a price list is not fresh at all.</para>
/// <para>★ It carries NO detail: not which topic, not which page, not what changed. It only says
/// "look again". Everything downstream is content-addressed — a bake's hash is a page dependency —
/// so the publisher re-fetches, compares, and re-renders exactly the pages whose data actually
/// moved. That is what makes a duplicate signal free and a lost one survivable: the next signal, from
/// any source, converges on the same answer.</para>
/// <para>The hosted service debounces, so a burst of notifications is one publish.</para>
/// </remarks>
public sealed class ExternalContentSignal
{
    /// <summary>Raised when an external dependency may have changed.</summary>
    public event Action? Changed;

    /// <summary>Ask the publisher to look again. Safe to call from any thread, as often as you like.</summary>
    public void Raise() => Changed?.Invoke();
}
