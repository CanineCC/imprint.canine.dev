using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetPostOrder;

public sealed record SetPostOrder(SiteId SiteId, IReadOnlyList<PostId> Posts) : ICommand;
