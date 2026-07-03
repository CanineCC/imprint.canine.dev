using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Widgets;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The WidgetSubmission aggregate + its events + slices are owned by agent B1. This id
// is stubbed here ONLY so the merged-catalog / publisher / editor work in this worktree
// compiles and can address a submission. When B1 lands, this file collapses into B1's
// real Domain/Widgets. Shape matches domain-model.md's id convention exactly, so the
// reconciliation is a delete, not a rewrite. See caveats.

/// <summary>Identifies a <c>WidgetSubmission</c>. Stream: <c>widget-{id}</c>.</summary>
public readonly record struct WidgetSubmissionId(Guid Value) : IGuidId<WidgetSubmissionId>
{
    public static WidgetSubmissionId New() => new(Guid.NewGuid());
    public static WidgetSubmissionId From(Guid value) => new(value);
    public string Stream => $"widget-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}
