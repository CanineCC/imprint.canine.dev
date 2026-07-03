namespace Imprint.Authoring.Domain.Widgets;

/// <summary>
/// The lifecycle of a widget submission. Only <see cref="Approved"/> submissions ever
/// publish runnable code to visitors — the whole point of the review gate.
/// </summary>
public enum WidgetSubmissionStatus
{
    Pending,
    Approved,
    Rejected,
    Withdrawn,
}
