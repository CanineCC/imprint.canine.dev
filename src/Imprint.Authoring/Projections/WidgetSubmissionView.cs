using Imprint.Authoring.Domain.Widgets;

namespace Imprint.Authoring.Projections;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The read-model DTO the WidgetRegistry exposes for a single submission. B1's projection
// folds widget.* events into instances of this; the merged catalog, the publisher and
// the admin page consume it. Kept deliberately faithful to the proposal's manifest +
// audit fields so B1's real projection can produce the identical shape. See caveats.

/// <summary>Everything the admin review page and the merged catalog need about one submission.</summary>
public sealed record WidgetSubmissionView
{
    public required WidgetSubmissionId Id { get; init; }
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string Placeholder { get; init; } = "";
    public string? AspectRatio { get; init; }
    public bool Eager { get; init; }

    /// <summary>The submitted prop declarations, in domain form (string <c>Type</c>).</summary>
    public IReadOnlyList<WidgetPropSpec> Props { get; init; } = [];

    /// <summary>The exact ES-module source as submitted/approved — part of the audit trail.</summary>
    public string BundleSource { get; init; } = "";

    public long ByteSize { get; init; }
    public required string RequestedBy { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }

    public WidgetStatus Status { get; init; } = WidgetStatus.Pending;

    /// <summary>The approver or rejecter; null while pending/withdrawn.</summary>
    public string? DecidedBy { get; init; }

    /// <summary>Present only for a rejection.</summary>
    public string? RejectionReason { get; init; }

    /// <summary>When the approve/reject/withdraw happened; null while pending.</summary>
    public DateTimeOffset? DecidedAt { get; init; }
}
