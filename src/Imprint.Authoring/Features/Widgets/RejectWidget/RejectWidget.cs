using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.RejectWidget;

public sealed record RejectWidget(WidgetSubmissionId WidgetSubmissionId, string RejectedBy, string Reason) : ICommand;
