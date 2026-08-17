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

    /// <summary>Where the post stands with the reviewer. <see cref="PostReview.None"/> on a post
    /// nobody has submitted — which is every post on a site with no reviewer configured.</summary>
    public PostReview Review { get; private set; }

    /// <summary>
    /// When the post is meant to go live, absolute. Null means "to be decided": the reviewer can
    /// approve the words and leave the timing open, and nothing will publish until a date exists.
    /// </summary>
    public DateTimeOffset? PublishAt { get; private set; }

    /// <summary>Why the reviewer sent it back; cleared when it is submitted again.</summary>
    public string? ReviewNote { get; private set; }

    /// <summary>A date in the future — the post is waiting for its moment rather than for a person.</summary>
    public bool IsScheduled(DateTimeOffset now) => !IsPublished && PublishAt is { } at && at > now;

    /// <summary>Whether a reviewer's sign-off is on the CURRENT words (an edit lapses it).</summary>
    public bool IsApproved => Review is PostReview.Approved;

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

        // A no-op save must not lapse an approval: the editor autosaves on a timer, and a
        // reviewer's sign-off surviving until somebody actually TYPES is the difference between
        // a workflow and a nuisance.
        if (Body.Get(locale) == markdown)
        {
            return;
        }

        Raise(new PostBodyChanged(locale, markdown));

        // Sign-off is on words, not on a post. Editing after approval withdraws it — otherwise
        // "approved" could be obtained on a mild draft and spent on something else entirely.
        if (Review is PostReview.Approved && !IsPublished)
        {
            Raise(new PostApprovalLapsed());
        }
    }

    /// <summary>
    /// Sets (or clears) when the post should go live. Clearing is "to be decided", not "now":
    /// nothing publishes a post whose date is unset.
    /// </summary>
    public void SetPublishDate(DateTimeOffset? at)
    {
        EnsureNotDeleted();
        if (at == PublishAt)
        {
            return;
        }

        Raise(new PostPublishDateSet(at));
    }

    /// <summary>
    /// Hands the post to the reviewer, with a proposed date the reviewer may overrule.
    /// The body is vetted here with the same conversion the publisher demands: sending prose
    /// that cannot be rendered wastes the reviewer's pass, and they would have no way to tell.
    /// </summary>
    public void SubmitForReview(Locale locale, DateTimeOffset? proposedPublishAt, string? note = null)
    {
        EnsureNotDeleted();
        if (IsPublished)
        {
            throw new DomainException("This post is already live — unpublish it before sending it back for review.");
        }

        if (Review is PostReview.Pending)
        {
            throw new DomainException("This post is already with the reviewer.");
        }

        EnsureRenderable(locale);
        Raise(new PostSubmittedForReview(proposedPublishAt, Trimmed(note)));
    }

    /// <summary>
    /// The reviewer's sign-off, with the date THEY settled on — which may be the proposal, a
    /// different date, or none at all ("approved, timing to be decided").
    /// </summary>
    public void ApproveReview(DateTimeOffset? publishAt)
    {
        EnsureNotDeleted();
        if (Review is not PostReview.Pending)
        {
            throw new DomainException("Only a post that is waiting for review can be approved.");
        }

        Raise(new PostReviewApproved(publishAt));
    }

    /// <summary>Sends it back. The reason is required — a rejection nobody can act on is not a review.</summary>
    public void RequestChanges(string reason)
    {
        EnsureNotDeleted();
        if (Review is not PostReview.Pending)
        {
            throw new DomainException("Only a post that is waiting for review can be sent back.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Say what needs changing — the author has to be able to act on it.");
        }

        Raise(new PostChangesRequested(reason.Trim()));
    }

    /// <summary>
    /// Publishes the post, if its body in <paramref name="locale"/> can be represented.
    /// Idempotent: a second publish keeps the original date.
    /// </summary>
    public void Publish(Locale locale, DateTimeOffset at)
    {
        EnsureNotDeleted();

        // A post sitting with the reviewer cannot be published out from under them, whatever the
        // site's policy says — that much is the post's own business. Whether a review was
        // REQUIRED in the first place is the site's, and lives in the slice.
        if (Review is PostReview.Pending)
        {
            throw new DomainException("This post is with the reviewer — it cannot be published until they have answered.");
        }

        EnsureRenderable(locale);

        // Raised on EVERY publish, including a re-publish after an edit — the event is what
        // carries the new content to the published projection, so swallowing it would leave the
        // live post stale forever while the editor cheerfully showed "Published". The DATE still
        // does not move: the fold keeps the first one (see When), so re-publishing is an update
        // and not a new post.
        // A post that went out on a schedule is dated by the schedule, not by the moment a
        // polling worker happened to wake up: the agreed instant is what everyone was told, and
        // "09:00" reading as 09:00:23 in the feed is a small lie with no upside.
        Raise(new PostPublished(PublishAt is { } planned && planned <= at ? planned : at));
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
            case PostPublishDateSet e:
                PublishAt = e.PublishAt;
                break;
            case PostSubmittedForReview e:
                Review = PostReview.Pending;
                PublishAt = e.ProposedPublishAt;
                // The old rejection note goes with the old draft: it described words that have
                // since been rewritten, and leaving it up would read as a standing objection.
                ReviewNote = e.Note;
                break;
            case PostReviewApproved e:
                Review = PostReview.Approved;
                PublishAt = e.PublishAt;
                ReviewNote = null;
                break;
            case PostChangesRequested e:
                Review = PostReview.ChangesRequested;
                ReviewNote = e.Reason;
                break;
            case PostApprovalLapsed:
                Review = PostReview.None;
                break;
            case PostDeleted:
                IsDeleted = true;
                break;
            default:
                throw new InvalidOperationException($"Post cannot fold {@event.GetType().Name}.");
        }
    }

    // ---------------------------------------------------------------------- helpers

    /// <summary>
    /// The body must exist and must convert. Shared by publish and submit-for-review because
    /// they demand the same thing of the prose — the reviewer is reading what will ship.
    /// The conversion is run for its VERDICT, not its output: the nodes are rebuilt at render
    /// time from the markdown in the log, so nothing derived is stored here.
    /// </summary>
    private void EnsureRenderable(Locale locale)
    {
        var markdown = Body.Get(locale);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new DomainException("A post cannot be published while its body is empty.");
        }

        var conversion = MarkdownToNodes.Convert(markdown, locale);
        if (!conversion.Ok)
        {
            throw new DomainException(Describe(conversion.Problems));
        }
    }

    private static string? Trimmed(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

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
