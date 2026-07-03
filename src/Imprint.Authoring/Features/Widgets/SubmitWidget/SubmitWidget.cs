using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.SubmitWidget;

public sealed record SubmitWidget(
    WidgetSubmissionId WidgetSubmissionId,
    string Tag,
    string Name,
    string Description,
    string Placeholder,
    string? AspectRatio,
    bool Eager,
    IReadOnlyList<WidgetPropSpec> Props,
    string BundleSource,
    string RequestedBy) : ICommand;
