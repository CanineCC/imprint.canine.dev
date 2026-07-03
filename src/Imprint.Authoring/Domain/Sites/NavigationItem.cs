namespace Imprint.Authoring.Domain.Sites;

/// <summary>
/// One navigation entry. The label override is optional on purpose: without it the
/// rendered label is the target page's title, so renaming a page keeps the navigation
/// in sync for free.
/// </summary>
public sealed record NavigationItem(PageId PageId, LocalizedText? LabelOverride);
