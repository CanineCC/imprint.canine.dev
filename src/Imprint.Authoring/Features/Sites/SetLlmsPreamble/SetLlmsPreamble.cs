using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetLlmsPreamble;

// What llms.txt says about the site before the generated page index — the site's own
// account of what it IS, which a list of page titles cannot convey. Null or blank clears
// it, and the file falls back to the site name plus the home page's description.
public sealed record SetLlmsPreamble(SiteId SiteId, string? Preamble) : ICommand;
