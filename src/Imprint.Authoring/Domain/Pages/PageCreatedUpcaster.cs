using Imprint.Authoring.Domain.Pages.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// Normalizes a legacy <c>page.created</c> that carries no site: a stream written before
/// pages were explicitly bound to a site (or hand-authored without the field) deserializes
/// with <see cref="PageCreated.SiteId"/> = the empty Guid, and this rebinds it to
/// <see cref="SiteId.Default"/> so a single-site install's content lands under one real
/// site instead of a phantom <c>site-000…0</c>. Every <c>page.created</c> the current code
/// writes already carries a real SiteId, so this is a pure pass-through for them — the
/// invariant that a fresh install behaves identically to today holds by construction.
/// </summary>
public sealed class PageCreatedUpcaster : IEventUpcaster
{
    public string EventName => "page.created";

    public object Upcast(object @event) =>
        @event is PageCreated { SiteId.IsEmpty: true } legacy
            ? legacy with { SiteId = SiteId.Default }
            : @event;
}
