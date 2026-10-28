namespace Imprint.Authoring.Domain.Sites;

/// <summary>Who may post to LinkedIn for a site: the key an article or update names, a display name, and the author:
/// a LinkedIn organization (<c>urn:li:organization:{id}</c>: a company page, or a product's showcase page) or a
/// member (<c>urn:li:person:{id}</c>, posting under their own name).</summary>
/// <remarks><see cref="OrganizationUrn"/> keeps its name although it may hold a person: it is serialized in every
/// stored <c>SiteLinkedInSendersChanged</c> event, so renaming it would break replay. Read it as "author URN".</remarks>
public sealed record LinkedInSender(string Key, string Name, string OrganizationUrn)
{
    public const string PersonPrefix = "urn:li:person:";

    /// <summary>A member posting under their own name, with their own connection, rather than a page.</summary>
    public bool IsPerson => OrganizationUrn.StartsWith(PersonPrefix, StringComparison.Ordinal);
}
