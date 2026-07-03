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
    /// <summary>A per-page failure (e.g. a slug collision) — recorded, never thrown.</summary>
    public sealed record PageError(PageId PageId, string Message);
}
