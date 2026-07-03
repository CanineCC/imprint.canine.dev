using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.ApproveWidget;

public sealed record ApproveWidget(WidgetSubmissionId WidgetSubmissionId, string ApprovedBy) : ICommand;
