namespace Imprint.Authoring.Domain.Sites;

/// <summary>Who may post a site's articles to LinkedIn: the key an article names, a display name, and the LinkedIn
/// organization that posts (<c>urn:li:organization:{id}</c>: a company page, or a product's showcase page).</summary>
public sealed record LinkedInSender(string Key, string Name, string OrganizationUrn);
