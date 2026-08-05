using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetLlmsExcludedPaths;

// Which path prefixes are published for search engines but kept out of llms.txt and
// llms-full.txt — each prefix and everything under it. Null or empty clears the policy.
// Never affects sitemap.xml: these pages exist to be indexed.
public sealed record SetLlmsExcludedPaths(SiteId SiteId, IReadOnlyList<string>? Paths) : ICommand;
