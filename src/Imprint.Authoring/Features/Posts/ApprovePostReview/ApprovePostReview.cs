using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ApprovePostReview;

/// <param name="PublishAt">The date the reviewer settled on; null approves the words and leaves
/// the timing open, which is a real answer ("yes, but not yet").</param>
public sealed record ApprovePostReview(PostId PostId, DateTimeOffset? PublishAt) : ICommand;
