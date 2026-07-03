using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.RenameSite;

// Name shape (empty, length) is validated by the aggregate, whose messages are
// already written for humans — no command-side duplicate.
public sealed record RenameSite(SiteId SiteId, string Name) : ICommand;
