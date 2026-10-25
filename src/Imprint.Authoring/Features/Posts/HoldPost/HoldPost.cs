using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.HoldPost;

/// <param name="Reason">Why the post must not go live yet, or null to lift the hold.</param>
public sealed record HoldPost(PostId PostId, string? Reason) : ICommand;
