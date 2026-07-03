using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Domain.Widgets.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// The widget-approval read model, folded through the <see cref="WidgetSubmission"/>
/// aggregate (same pattern as <see cref="BlockLibrary"/>). Unlike the block library it
/// keeps <em>every</em> submission — the admin page lists rejected and withdrawn ones
/// too, and the whole point of the feature is the audit trail. <see cref="Approved"/> is
/// the subset whose bundles publish to visitors; slices consult
/// <see cref="IsApprovedTag"/> for tag collisions and the publisher reads
/// <see cref="BundleOf"/> for the exact approved source.
/// </summary>
public sealed class WidgetRegistry : ReadModel
{
    private readonly Dictionary<WidgetSubmissionId, WidgetSubmission> _submissions = [];

    // Preserves submission order (the dictionary doesn't) so the admin list is stable.
    private readonly List<WidgetSubmissionId> _order = [];

    public WidgetSubmission? Get(WidgetSubmissionId id) => _submissions.GetValueOrDefault(id);

    /// <summary>Every submission, in submission order, whatever its status — for the admin review page.</summary>
    public IReadOnlyList<WidgetSubmission> Submissions =>
        [.. _order.Select(id => _submissions[id])];

    /// <summary>Only approved submissions — the ones whose bundles reach a visitor's browser.</summary>
    public IReadOnlyList<WidgetSubmission> Approved =>
        [.. _order.Select(id => _submissions[id]).Where(s => s.Status is WidgetSubmissionStatus.Approved)];

    /// <summary>The set of approved tags — what slices check a new/approved tag against.</summary>
    public IReadOnlySet<string> ApprovedTags =>
        Approved.Select(s => s.Tag).ToHashSet(StringComparer.Ordinal);

    public bool IsApprovedTag(string tag) =>
        Approved.Any(s => string.Equals(s.Tag, tag, StringComparison.Ordinal));

    /// <summary>The approved bundle source for a tag, or null when no approved widget owns it.</summary>
    public string? BundleOf(string tag) =>
        Approved.FirstOrDefault(s => string.Equals(s.Tag, tag, StringComparison.Ordinal))?.BundleSource;

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "widget-") is not { } guid)
        {
            return;
        }

        var id = WidgetSubmissionId.From(guid);
        if (@event.Event is WidgetSubmitted)
        {
            // `widget.submitted` is a stream's first event; a submission is never removed.
            if (!_submissions.ContainsKey(id))
            {
                _order.Add(id);
            }

            var submission = new WidgetSubmission();
            submission.LoadFrom([@event.Event]);
            _submissions[id] = submission;
        }
        else if (_submissions.TryGetValue(id, out var submission))
        {
            submission.LoadFrom([@event.Event]);
        }
        else
        {
            throw new InvalidOperationException(
                $"Widget event {@event.StableId} for unknown submission {id} — corrupt sequence?");
        }

        NotifyChanged();
    }

    public override void Reset()
    {
        _submissions.Clear();
        _order.Clear();
    }
}
