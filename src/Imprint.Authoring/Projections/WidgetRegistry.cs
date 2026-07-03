using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The WidgetRegistry read model is owned by agent B1, which folds widget.* events into
// it (proposal §"WidgetRegistry projection"). This stub provides the exact READ SURFACE
// the merged catalog, the publisher and the admin page code against — Approved /
// Submissions / ApprovedTags / IsApprovedTag / BundleOf — so that work compiles and is
// unit-tested now. Apply() is intentionally inert (no widget events exist in this
// worktree yet); Seed() is a temporary test/integration seam. When B1 lands its folding
// projection, delete this file and keep the read surface identical. See caveats.

/// <summary>
/// Approved + pending + rejected widget submissions, derived from the WidgetSubmission
/// streams. Live read model (fires <see cref="ReadModel.Changed"/> for the admin page).
/// </summary>
public sealed class WidgetRegistry : ReadModel
{
    private readonly Dictionary<WidgetSubmissionId, WidgetSubmissionView> _submissions = [];

    /// <summary>Every submission (newest first), for the admin review page.</summary>
    public IReadOnlyList<WidgetSubmissionView> Submissions =>
        [.. _submissions.Values.OrderByDescending(submission => submission.SubmittedAt)];

    /// <summary>The approved submissions only — the widgets the merged catalog adds to the built-ins.</summary>
    public IReadOnlyList<WidgetSubmissionView> Approved =>
        [.. _submissions.Values.Where(submission => submission.Status == WidgetStatus.Approved)];

    /// <summary>Tags of approved widgets, for collision checks.</summary>
    public IReadOnlySet<string> ApprovedTags =>
        Approved.Select(submission => submission.Tag).ToHashSet(StringComparer.Ordinal);

    public bool IsApprovedTag(string tag) =>
        Approved.Any(submission => string.Equals(submission.Tag, tag, StringComparison.Ordinal));

    /// <summary>The approved bundle source for a tag — the bytes the publisher writes. Null when not approved.</summary>
    public string? BundleOf(string tag) =>
        Approved.FirstOrDefault(submission => string.Equals(submission.Tag, tag, StringComparison.Ordinal))?.BundleSource;

    // B1's real projection folds widget.submitted/revised/approved/rejected/withdrawn
    // here. No such events exist in this worktree, so nothing is applied yet.
    public override void Apply(StoredEvent @event)
    {
    }

    public override void Reset() => _submissions.Clear();

    /// <summary>
    /// Temporary test/integration seam: pushes submissions in without an event stream.
    /// B1's folding projection replaces this entirely — do not build on it.
    /// </summary>
    public void Seed(params IReadOnlyList<WidgetSubmissionView> submissions)
    {
        foreach (var submission in submissions)
        {
            _submissions[submission.Id] = submission;
        }

        NotifyChanged();
    }
}
