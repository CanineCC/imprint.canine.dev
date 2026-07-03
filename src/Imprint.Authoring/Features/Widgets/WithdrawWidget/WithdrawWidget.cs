using Imprint.Authoring.Domain.Widgets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Widgets.WithdrawWidget;

public sealed record WithdrawWidget(WidgetSubmissionId WidgetSubmissionId) : ICommand;
