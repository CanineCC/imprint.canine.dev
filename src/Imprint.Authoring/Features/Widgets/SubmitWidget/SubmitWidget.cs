using System.Collections.Immutable;
using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.SubmitWidget;

// ── B1 PLACEHOLDER (widget request/approval) ──────────────────────────────────────
// The SubmitWidget slice (handler + real validation + WidgetSubmission behaviour) is
// agent B1's. This command record is stubbed so the editor "Request a widget" modal can
// construct and dispatch it. The actor (RequestedBy in the event) is taken from command
// metadata by B1's handler, so it is NOT a field here. Shape follows the proposal's
// widget.submitted manifest fields. See caveats.

/// <summary>An editor submits a widget for admin review. Runs nothing until approved.</summary>
public sealed record SubmitWidget(
    string Tag,
    string Name,
    string Description,
    string Placeholder,
    string? AspectRatio,
    bool Eager,
    ImmutableArray<WidgetPropSpec> Props,
    string BundleSource) : ICommand;
