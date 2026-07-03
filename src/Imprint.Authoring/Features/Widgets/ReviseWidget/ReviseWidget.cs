using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.ReviseWidget;

public sealed record ReviseWidget(
    WidgetSubmissionId WidgetSubmissionId,
    string Tag,
    string Name,
    string Description,
    string Placeholder,
    string? AspectRatio,
    bool Eager,
    IReadOnlyList<WidgetPropSpec> Props,
    string BundleSource) : ICommand;
