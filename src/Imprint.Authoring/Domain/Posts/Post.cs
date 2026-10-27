using System.Security.Cryptography;
using System.Text;
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

    /// <summary>Why the post must not go live yet; null when nothing holds it. See <see cref="SetHold"/>.</summary>
    public string? Hold { get; private set; }

    /// <summary>How many times the body has been changed: the revision a sign-off names.</summary>
    public int BodyRevision { get; private set; }

    /// <summary>The body revision the standing approval covers; null when nothing is approved, or
    /// when the approval was recorded before approvals were signed.</summary>
    public int? ApprovedRevision { get; private set; }

    /// <summary>The sha256 of the words the standing approval covers (see <see cref="BodySha256"/>).</summary>
    public string? ApprovedSha256 { get; private set; }

    /// <summary>The version the last publish recorded: 1 for the first text, +1 for each changed text
    /// published since. Null on a post never published. Kept across an unpublish, so a republish of the
    /// same words is the same version.</summary>
    public int? PublishedVersion { get; private set; }

    /// <summary>The sha256 (<see cref="BodySha256"/>) of the words the last publish covered; null before
    /// versions were recorded.</summary>
    public string? PublishedSha256 { get; private set; }

    /// <summary>LinkedIn's limit on a post's text.</summary>
    public const int MaxLinkedInLength = 3_000;

    /// <summary>The LinkedIn post's text; null when the article posts nothing.</summary>
    public string? LinkedInText { get; private set; }

    /// <summary>The key of the site's LinkedIn sender that posts it; null with no text.</summary>
    public string? LinkedInSender { get; private set; }

    /// <summary>The private publication-control notes; null when there are none. Never published.</summary>
    public string? ControlNotes { get; private set; }

    /// <summary>What kind of article this is (see <see cref="ArticleClassification.Kinds"/>); null on a
    /// post nobody has classified, which is every post written before articles had kinds.</summary>
    public string? Kind { get; private set; }

    /// <summary>The products the article is about, in canonical order; empty when none is named.</summary>
    public IReadOnlyList<string> Products { get; private set; } = [];

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

        Raise(new PostReviewApproved(
            publishAt, BodyRevision, BodySha256(Body), ControlNotesSha256(ControlNotes), LinkedInSha256(LinkedInSender, LinkedInText)));
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

        if (Hold is { } held)
        {
            throw new DomainException($"This post is on hold ({held}). Lift the hold before it can go live.");
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
        var sha = BodySha256(Body);
        var version = PublishedVersion is not { } last ? 1
            : sha == PublishedSha256 ? last
            : last + 1;
        Raise(new PostPublished(PublishAt is { } planned && planned <= at ? planned : at, version, sha));
    }

    /// <summary>
    /// Holds the post with a reason, or lifts the hold with null. A hold says nothing about the words,
    /// so it neither needs nor lapses an approval: it only stops the post going live until it is lifted.
    /// </summary>
    public void SetHold(string? reason)
    {
        EnsureNotDeleted();
        var normalized = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalized?.Length > MaxMetaLength)
        {
            throw new DomainException($"A hold reason cannot be longer than {MaxMetaLength} characters.");
        }

        if (normalized == Hold)
        {
            return;
        }

        Raise(new PostHoldSet(normalized));
    }

    /// <summary>
    /// Says what kind of article this is and which products it is about. Both change what a reader
    /// sees (the template and index switch on the kind, the accent on the products), so changing them
    /// after approval withdraws the approval, exactly as an edit to the words does.
    /// </summary>
    public void Classify(string kind, IEnumerable<string> products)
    {
        EnsureNotDeleted();
        var classified = new PostClassified(
            ArticleClassification.NormalizeKind(kind), ArticleClassification.NormalizeProducts(products));
        if (classified.Kind == Kind && classified.Products.SequenceEqual(Products))
        {
            return;
        }

        Raise(classified);
        if (Review is PostReview.Approved && !IsPublished)
        {
            Raise(new PostApprovalLapsed());
        }
    }

    /// <summary>
    /// Sets (or clears, with blank) the private publication-control notes: where each figure comes
    /// from, what was checked, by whom. They are never published, but the signer signs them with the
    /// words, so changing them after approval withdraws the approval.
    /// </summary>
    public void SetControlNotes(string? notes)
    {
        EnsureNotDeleted();
        var normalized = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalized?.Length > MaxBodyLength)
        {
            throw new DomainException($"Control notes cannot be longer than {MaxBodyLength:N0} characters.");
        }

        if (normalized == ControlNotes)
        {
            return;
        }

        Raise(new PostControlNotesChanged(normalized));
        if (Review is PostReview.Approved && !IsPublished)
        {
            Raise(new PostApprovalLapsed());
        }
    }

    /// <summary>
    /// Sets the LinkedIn post that goes out with the article, or clears it with blank text. The text is public
    /// under the sender's name, so it is signed with the words, and changing it (or the sender) after approval
    /// withdraws the approval. Whether the sender exists on the site is the slice's question.
    /// </summary>
    public void SetLinkedIn(string? text, string? sender)
    {
        EnsureNotDeleted();
        var normalized = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var who = normalized is null || string.IsNullOrWhiteSpace(sender) ? null : sender.Trim().ToLowerInvariant();
        if (normalized is not null && who is null)
        {
            throw new DomainException("A LinkedIn post needs a sender: the company page or a product page set up on the site.");
        }

        if (normalized?.Length > MaxLinkedInLength)
        {
            throw new DomainException($"A LinkedIn post can be at most {MaxLinkedInLength:N0} characters.");
        }

        if (normalized == LinkedInText && who == LinkedInSender)
        {
            return;
        }

        Raise(new PostLinkedInChanged(normalized, who));
        if (Review is PostReview.Approved && !IsPublished)
        {
            Raise(new PostApprovalLapsed());
        }
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
                BodyRevision++;
                break;
            case PostPublished e:
                // First date wins. A reader sorts and cites by this, so it must not move because
                // somebody fixed a typo and pressed Publish again.
                PublishedAt ??= e.PublishedAt;
                // A publish from before versions were recorded was version one of words nobody can
                // recompute; with no hash, the next publish of any text is the next version.
                PublishedVersion = e.Version ?? PublishedVersion ?? 1;
                PublishedSha256 = e.Sha256;
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
                ApprovedRevision = e.Revision;
                ApprovedSha256 = e.Sha256;
                break;
            case PostChangesRequested e:
                Review = PostReview.ChangesRequested;
                ReviewNote = e.Reason;
                break;
            case PostApprovalLapsed:
                Review = PostReview.None;
                ApprovedRevision = null;
                ApprovedSha256 = null;
                break;
            case PostHoldSet e:
                Hold = e.Reason;
                break;
            case PostLinkedInChanged e:
                LinkedInText = e.Text;
                LinkedInSender = e.Sender;
                break;
            case PostControlNotesChanged e:
                ControlNotes = e.Notes;
                break;
            case PostClassified e:
                Kind = e.Kind;
                Products = e.Products;
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
    /// The hash a sign-off records, in a form anyone can recompute from the log: SHA-256 over the
    /// UTF-8 of each locale's body in locale order, each written as <c>{locale}\n{markdown}\0</c>,
    /// as lowercase hex. A single-locale post is therefore <c>sha256("en\n" + markdown + "\0")</c>.
    /// Changing this form makes every recorded approval unverifiable, so it is pinned by a test.
    /// </summary>
    public static string BodySha256(LocalizedText body)
    {
        var canonical = new StringBuilder();
        foreach (var (locale, markdown) in body.Values)
        {
            canonical.Append(locale.Value).Append('\n').Append(markdown).Append('\0');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    /// <summary>The sha256 of <c>{sender}\n{text}</c> as UTF-8, lowercase hex; null when the article posts nothing.</summary>
    public static string? LinkedInSha256(string? sender, string? text) =>
        text is null ? null : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{sender}\n{text}")));

    /// <summary>The sha256 of the control notes as UTF-8, lowercase hex; null when there are none.</summary>
    public static string? ControlNotesSha256(string? notes) =>
        notes is null ? null : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(notes)));

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
