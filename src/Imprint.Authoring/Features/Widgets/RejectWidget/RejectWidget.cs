using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.RejectWidget;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The RejectWidget slice is agent B1's. Stubbed so the admin page can dispatch it. The
// rejecter (RejectedBy in the event) comes from command metadata in B1's handler. See caveats.

/// <summary>An admin rejects a pending submission with a reason shown back to the requester.</summary>
public sealed record RejectWidget(WidgetSubmissionId Id, string Reason) : ICommand;
