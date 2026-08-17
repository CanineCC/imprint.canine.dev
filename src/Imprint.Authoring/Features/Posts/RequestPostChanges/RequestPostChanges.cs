using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.RequestPostChanges;

// The reason's presence is the aggregate's rule — it is what makes this a review rather than a veto.
public sealed record RequestPostChanges(PostId PostId, string Reason) : ICommand;
