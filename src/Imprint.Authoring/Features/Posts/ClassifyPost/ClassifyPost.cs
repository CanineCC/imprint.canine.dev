using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Posts.ClassifyPost;

/// <param name="Kind">essay, guide, whitepaper or documentation (see ArticleClassification).</param>
/// <param name="Products">The products the article is about; may be empty.</param>
public sealed record ClassifyPost(PostId PostId, string Kind, IReadOnlyList<string> Products) : ICommand;
