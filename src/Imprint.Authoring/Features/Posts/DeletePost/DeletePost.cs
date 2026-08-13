using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.DeletePost;

public sealed record DeletePost(PostId PostId) : ICommand;
