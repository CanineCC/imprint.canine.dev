namespace Imprint.Authoring.Domain.Widgets;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The WidgetSubmission lifecycle is B1's. Stubbed here so the admin page and registry
// read model can name a submission's state. Values mirror the proposal §"WidgetSubmission
// aggregate" verbatim. See caveats.

/// <summary>Lifecycle state of a widget submission.</summary>
public enum WidgetStatus
{
    Pending,
    Approved,
    Rejected,
    Withdrawn,
}
