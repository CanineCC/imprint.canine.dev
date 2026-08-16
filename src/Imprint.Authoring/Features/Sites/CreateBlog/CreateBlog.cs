using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.CreateBlog;

/// <summary>
/// Create a blog: a site whose content is a dated stream of posts rather than a page
/// tree, addressed by its own origin.
///
/// <paramref name="PublishFolder"/> and <paramref name="PublicUrl"/> are the "and it
/// lives at blog.canine.dev" half of the thought, taken at creation because that is when
/// the author has it. Both are optional — a blog with neither is a blog you can write in
/// and point somewhere later — but a URL alone is refused: a deploy environment cannot
/// exist without a folder to write into, so accepting one would silently do nothing.
/// </summary>
public sealed record CreateBlog(
    SiteId SiteId,
    string Name,
    string DefaultLocale,
    string? PublishFolder = null,
    string? PublicUrl = null) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return "The blog name cannot be empty.";
        }
        else if (Name.Trim().Length > Site.MaxNameLength)
        {
            yield return $"The blog name must be {Site.MaxNameLength} characters or fewer.";
        }

        if (!Locale.TryCreate(DefaultLocale, out _))
        {
            yield return $"'{DefaultLocale}' is not a valid locale tag (expected e.g. 'en' or 'de-AT').";
        }

        if (!string.IsNullOrWhiteSpace(PublicUrl) && string.IsNullOrWhiteSpace(PublishFolder))
        {
            yield return "A public address needs a publish folder to write into — give both, or neither.";
        }
    }
}
