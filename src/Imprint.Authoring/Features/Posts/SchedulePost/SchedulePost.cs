using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SchedulePost;

/// <param name="PublishAt">Absolute instant, or null for "to be decided". Nothing publishes a
/// post whose date is unset — a missing date is a decision to wait, not a decision to go now.</param>
public sealed record SchedulePost(PostId PostId, DateTimeOffset? PublishAt) : ICommand;
