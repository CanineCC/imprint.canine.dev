using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Widgets.Events;

// The WidgetSubmission aggregate's events — one file because the closed set *is* the
// submit → review → approve/reject/withdraw state machine (docs/proposals/…widget-approval).
// `widget.submitted` and `widget.revised` carry the widget's manifest and, in the event
// itself, the exact bundle source: keeping the approved bytes in the immutable log is
// the audit trail the feature exists to provide (cap 512 KB keeps that affordable).

[EventType("widget.submitted")]
public sealed record WidgetSubmitted(
    WidgetSubmissionId WidgetSubmissionId,
    string Tag,
    string Name,
    string Description,
    string Placeholder,
    string? AspectRatio,
    bool Eager,
    IReadOnlyList<WidgetPropSpec> Props,
    string BundleSource,
    long ByteSize,
    string RequestedBy)
{
    // A record holding a list compares by reference, but events must compare by value
    // (specs and round-trip tests rely on it) — same precedent as ImageVariantsGenerated
    // and ColumnsNode. Only the Props list needs the hand-written comparison.
    public bool Equals(WidgetSubmitted? other) =>
        other is not null &&
        WidgetSubmissionId.Equals(other.WidgetSubmissionId) &&
        Tag == other.Tag &&
        Name == other.Name &&
        Description == other.Description &&
        Placeholder == other.Placeholder &&
        AspectRatio == other.AspectRatio &&
        Eager == other.Eager &&
        BundleSource == other.BundleSource &&
        ByteSize == other.ByteSize &&
        RequestedBy == other.RequestedBy &&
        Props.SequenceEqual(other.Props);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(WidgetSubmissionId);
        hash.Add(Tag);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Placeholder);
        hash.Add(AspectRatio);
        hash.Add(Eager);
        hash.Add(BundleSource);
        hash.Add(ByteSize);
        hash.Add(RequestedBy);
        foreach (var prop in Props)
        {
            hash.Add(prop);
        }

        return hash.ToHashCode();
    }
}

[EventType("widget.revised")]
public sealed record WidgetRevised(
    string Tag,
    string Name,
    string Description,
    string Placeholder,
    string? AspectRatio,
    bool Eager,
    IReadOnlyList<WidgetPropSpec> Props,
    string BundleSource,
    long ByteSize)
{
    // Same list-carrying value-equality contract as WidgetSubmitted.
    public bool Equals(WidgetRevised? other) =>
        other is not null &&
        Tag == other.Tag &&
        Name == other.Name &&
        Description == other.Description &&
        Placeholder == other.Placeholder &&
        AspectRatio == other.AspectRatio &&
        Eager == other.Eager &&
        BundleSource == other.BundleSource &&
        ByteSize == other.ByteSize &&
        Props.SequenceEqual(other.Props);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tag);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Placeholder);
        hash.Add(AspectRatio);
        hash.Add(Eager);
        hash.Add(BundleSource);
        hash.Add(ByteSize);
        foreach (var prop in Props)
        {
            hash.Add(prop);
        }

        return hash.ToHashCode();
    }
}

[EventType("widget.approved")]
public sealed record WidgetApproved(string ApprovedBy);

[EventType("widget.rejected")]
public sealed record WidgetRejected(string RejectedBy, string Reason);

[EventType("widget.withdrawn")]
public sealed record WidgetWithdrawn;
