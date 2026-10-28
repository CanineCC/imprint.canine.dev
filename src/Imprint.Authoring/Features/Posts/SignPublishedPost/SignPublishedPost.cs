using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.SignPublishedPost;

/// <summary>Sign one live article where it stands (see <c>Post.SignPublished</c>).</summary>
public sealed record SignPublishedPost(PostId PostId) : ICommand;

/// <summary>Sign every live article on a site whose live words are unsigned; drafts and edited ones are left.</summary>
public sealed record SignPublishedPosts(SiteId SiteId) : ICommand;
