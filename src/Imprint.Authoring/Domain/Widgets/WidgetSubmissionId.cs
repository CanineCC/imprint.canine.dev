using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Widgets;

/// <summary>
/// Identifies a <see cref="WidgetSubmission"/>. It lives beside the aggregate (rather
/// than in <c>Domain/Ids.cs</c> with the other ids) so the whole widget-approval
/// concept — id, events, aggregate, prop spec — stays in one folder and this slice's
/// work never touches a shared file. Stream is <c>widget-{id:N}</c>, matching the
/// aggregate-per-stream convention.
/// </summary>
public readonly record struct WidgetSubmissionId(Guid Value) : IGuidId<WidgetSubmissionId>
{
    public static WidgetSubmissionId New() => new(Guid.NewGuid());
    public static WidgetSubmissionId From(Guid value) => new(value);
    public string Stream => $"widget-{Value:N}";
    public string Compact => Value.ToString("N");
    public override string ToString() => Compact;
}
