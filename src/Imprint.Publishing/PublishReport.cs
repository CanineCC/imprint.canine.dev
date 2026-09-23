using Imprint.Authoring.Domain;

namespace Imprint.Publishing;

/// <summary>
/// The outcome of one <see cref="SitePublisher.Synchronize"/> pass. Counts are about
/// work actually performed — an up-to-date site synchronizes to all zeros, which is
/// exactly what the determinism guarantee promises.
/// </summary>
public sealed record PublishReport(
    int PagesRendered,
    int PagesRemoved,
    int FilesWritten,
    long BytesWritten,
    IReadOnlyList<PublishReport.PageError> Errors,
    DateTimeOffset CompletedAtUtc,
    TimeSpan Duration)
{
    /// <summary>
    /// How many widget fragments this publish could not fetch — each one kept its previous bake, or
    /// published a placeholder when there was nothing to keep.
    /// </summary>
    /// <remarks>
    /// ★★ A PUBLISHED PAGE IS A STATIC FILE, so a fragment that did not arrive is not a transient
    /// error — it is what every visitor sees until something publishes again. Keeping the last good
    /// bake stops the page going blank; this count is what stops it sitting there stale, because the
    /// hosted service retries while it is above zero. A publish that reached everything reports zero,
    /// which is the only state that ends the retry.
    /// </remarks>
    public int StaleBakes { get; init; }

    /// <summary>A per-page failure (e.g. a slug collision) — recorded, never thrown.</summary>
    public sealed record PageError(PageId PageId, string Message);
}
