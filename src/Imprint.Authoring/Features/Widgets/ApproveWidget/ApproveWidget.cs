using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.ApproveWidget;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The ApproveWidget slice is agent B1's (it also re-checks tag collision at approve time
// — the documented race in the proposal). Stubbed so the admin page can dispatch it. The
// approver (ApprovedBy in the event) comes from command metadata in B1's handler. See caveats.

/// <summary>An admin approves a pending submission — its bundle now publishes to visitors.</summary>
public sealed record ApproveWidget(WidgetSubmissionId Id) : ICommand;
