using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.SetPageArticle;

/// <summary>
/// Declare (or clear) a page as an article for structured data: a named human author and a
/// publication date, or neither. The date is ISO (yyyy-MM-dd) because it is destined for a
/// datePublished field, where anything fuzzier is worse than nothing.
/// </summary>
public sealed record SetPageArticle(PageId PageId, string? Author, string? Published)
    : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (Published is not null && !DateOnly.TryParseExact(Published, "yyyy-MM-dd", out _))
        {
            yield return $"'{Published}' is not an ISO date (expected yyyy-MM-dd).";
        }
    }
}
