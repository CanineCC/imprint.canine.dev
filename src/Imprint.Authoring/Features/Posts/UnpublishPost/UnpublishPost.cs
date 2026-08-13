using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.UnpublishPost;

public sealed record UnpublishPost(PostId PostId) : ICommand;
