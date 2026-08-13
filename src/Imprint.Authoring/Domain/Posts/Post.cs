using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Posts.Events;
using Imprint.Authoring.Markdown;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Posts;

/// <summary>
/// A blog entry, whose authored state is MARKDOWN rather than a node tree.
///
/// <para><b>Why markdown is the state.</b> Long-form prose is exactly what the structural
/// editor is worst at, and it is the one content shape people already have a good notation
/// for. Keeping the markdown as the aggregate's state means the node tree is a projection of
/// it — which is this system's whole thesis applied one level down — and it keeps versioning,
/// drafts, undo and translation coverage working with no new machinery, because they are all
/// projections over the same log.</para>
///
/// <para><b>Where representability is enforced: publishing, not editing.</b> A draft accepts
/// anything the author types. Someone mid-sentence has an unclosed <c>**</c> and a stray
/// backtick constantly, and an aggregate that refused those would make the editor unusable.
/// <see cref="Publish"/> is the gate — it is the moment the markdown has to become nodes, so
/// it is the moment that must be provable. The refusal names the line, because "it did not
/// convert" is not something an author can act on.</para>
/// </summary>
public sealed class Post : AggregateRoot
{
    public const int MaxBodyLength = 200_000;
    private const int MaxTitleLength = 200;
    private const int MaxMetaLength = 300;

    /// <summary>How many refused lines a publish error lists before it stops. A wall of forty
    /// errors is not more helpful than the first few, and the author fixes them top-down anyway.</summary>
    private const int MaxReportedProblems = 5;

    public PostId Id { get; private set; }
    public SiteId SiteId { get; private set; }
    public Slug Slug { get; private set; }
    public LocalizedText Title { get; private set; } = LocalizedText.Empty;
    public LocalizedText MetaTitle { get; private set; } = LocalizedText.Empty;
    public LocalizedText MetaDescription { get; private set; } = LocalizedText.Empty;

    /// <summary>The post's markdown source, per locale.</summary>
    public LocalizedText Body { get; private set; } = LocalizedText.Empty;

    public DateTimeOffset? PublishedAt { get; private set; }
    public bool IsPublished => PublishedAt is not null;
    public bool IsDeleted { get; private set; }

    public override string StreamId => Id.Stream;

    // ------------------------------------------------------------------ behaviors

    public static Post Create(PostId id, SiteId siteId, Slug slug, Locale initialLocale, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("A post needs a title.");
        }

        EnsureTitleLength(title);
        var post = new Post();
        post.Raise(new PostCreated(id, siteId, slug.Value, initialLocale, title));
        return post;
    }

    public void ChangeTitle(Locale locale, string title)
    {
        EnsureNotDeleted();
        EnsureTitleLength(title);
        Raise(new PostTitleChanged(locale, title));
    }

    public void ChangeSlug(Slug slug)
    {
        EnsureNotDeleted();
        if (slug == Slug)
        {
            return;   // nothing changed, so nothing happened
        }

        Raise(new PostSlugChanged(slug.Value));
    }

    public void ChangeMeta(Locale locale, string? metaTitle, string? metaDescription)
    {
        EnsureNotDeleted();
        if (metaTitle?.Length > MaxMetaLength || metaDescription?.Length > MaxMetaLength)
        {
            throw new DomainException($"Meta text cannot be longer than {MaxMetaLength} characters.");
        }

        Raise(new PostMetaChanged(locale, metaTitle, metaDescription));
    }

    /// <summary>Replaces the body for one locale. Deliberately permissive — see the class remarks.</summary>
    public void ChangeBody(Locale locale, string markdown)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(markdown);
        if (markdown.Length > MaxBodyLength)
        {
            throw new DomainException($"A post body cannot be longer than {MaxBodyLength:N0} characters.");
        }

        Raise(new PostBodyChanged(locale, markdown));
    }

    /// <summary>
    /// Publishes the post, if its body in <paramref name="locale"/> can be represented.
    /// Idempotent: a second publish keeps the original date.
    /// </summary>
    public void Publish(Locale locale, DateTimeOffset at)
    {
        EnsureNotDeleted();

        var markdown = Body.Get(locale);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new DomainException("A post cannot be published while its body is empty.");
        }

        // The conversion is run for its VERDICT, not its output: the nodes are rebuilt at
        // render time from the markdown in the log, so nothing derived is stored here.
        var conversion = MarkdownToNodes.Convert(markdown, locale);
        if (!conversion.Ok)
        {
            throw new DomainException(Describe(conversion.Problems));
        }

        // Raised on EVERY publish, including a re-publish after an edit — the event is what
        // carries the new content to the published projection, so swallowing it would leave the
        // live post stale forever while the editor cheerfully showed "Published". The DATE still
        // does not move: the fold keeps the first one (see When), so re-publishing is an update
        // and not a new post.
        Raise(new PostPublished(at));
    }

    public void Unpublish()
    {
        EnsureNotDeleted();
        if (!IsPublished)
        {
            return;
        }

        Raise(new PostUnpublished());
    }

    public void Delete()
    {
        EnsureNotDeleted();
        Raise(new PostDeleted());
    }

    // ----------------------------------------------------------------------- fold

    protected override void When(object @event)
    {
        switch (@event)
        {
            case PostCreated e:
                Id = e.PostId;
                SiteId = e.SiteId;
                Slug = ParseStoredSlug(e.Slug);
                Title = LocalizedText.Of(e.InitialLocale, e.Title);
                break;
            case PostTitleChanged e:
                Title = Title.With(e.Locale, e.Title);
                break;
            case PostSlugChanged e:
                Slug = ParseStoredSlug(e.Slug);
                break;
            case PostMetaChanged e:
                MetaTitle = MetaTitle.With(e.Locale, e.MetaTitle ?? string.Empty);
                MetaDescription = MetaDescription.With(e.Locale, e.MetaDescription ?? string.Empty);
                break;
            case PostBodyChanged e:
                Body = Body.With(e.Locale, e.Markdown);
                break;
            case PostPublished e:
                // First date wins. A reader sorts and cites by this, so it must not move because
                // somebody fixed a typo and pressed Publish again.
                PublishedAt ??= e.PublishedAt;
                break;
            case PostUnpublished:
                PublishedAt = null;
                break;
            case PostDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"Post cannot fold {@event.GetType().Name}.");
        }
    }

    // ---------------------------------------------------------------------- helpers

    private static string Describe(IReadOnlyList<MarkdownProblem> problems)
    {
        var listed = problems
            .Take(MaxReportedProblems)
            .Select(p => $"line {p.Line}: {p.Message}");
        var suffix = problems.Count > MaxReportedProblems ? $" (and {problems.Count - MaxReportedProblems} more)" : "";
        return "This post cannot be published until its body converts — " + string.Join("; ", listed) + suffix;
    }

    private static Slug ParseStoredSlug(string value) =>
        Slug.TryCreateNested(value, out var slug, out var error)
            ? slug
            // A stored slug was valid when it was written; if it no longer parses, the rules
            // changed under it and silently repairing would hide that from whoever must fix it.
            : throw new InvalidOperationException($"Stored slug '{value}' is not valid: {error}");

    private static void EnsureTitleLength(string title)
    {
        if (title.Length > MaxTitleLength)
        {
            throw new DomainException($"A title cannot be longer than {MaxTitleLength} characters.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("This post has been deleted.");
        }
    }
}
