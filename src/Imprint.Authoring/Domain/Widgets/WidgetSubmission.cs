using System.Text;
using System.Text.RegularExpressions;
using Imprint.Authoring.Domain.Widgets.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Widgets;

/// <summary>
/// An editor's request to add a widget — code that runs JavaScript on visitors' pages —
/// moving the trust gate into the app without removing it: nothing here runs anywhere
/// until an admin approves it. The submitted bundle source is carried in the events, so
/// the exact approved bytes are part of the immutable audit trail
/// (docs/proposals/theme-media-and-widget-approval.md §"Part 2").
/// </summary>
public sealed partial class WidgetSubmission : AggregateRoot
{
    // Widgets are small; storing their exact bytes in the log is only affordable because
    // they're capped. 512 KB matches the proposal.
    public const int MaxBundleBytes = 512 * 1024;
    private const int MaxProps = 20;

    public WidgetSubmissionId Id { get; private set; }
    public WidgetSubmissionStatus Status { get; private set; }
    public string Tag { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Placeholder { get; private set; } = string.Empty;
    public string? AspectRatio { get; private set; }
    public bool Eager { get; private set; }
    public IReadOnlyList<WidgetPropSpec> Props { get; private set; } = [];
    public string BundleSource { get; private set; } = string.Empty;
    public long ByteSize { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    public override string StreamId => Id.Stream;

    public static WidgetSubmission Submit(
        WidgetSubmissionId id,
        string tag,
        string name,
        string description,
        string placeholder,
        string? aspectRatio,
        bool eager,
        IReadOnlyList<WidgetPropSpec> props,
        string bundleSource,
        string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new DomainException("A widget submission must record who requested it.");
        }

        var byteSize = ValidateManifest(tag, name, aspectRatio, props, bundleSource);
        var submission = new WidgetSubmission();
        submission.Raise(new WidgetSubmitted(
            id, tag, name, description, placeholder, aspectRatio, eager,
            [.. props], bundleSource, byteSize, requestedBy));
        return submission;
    }

    /// <summary>Edit a pending or rejected submission and re-open it for review.</summary>
    public void Revise(
        string tag,
        string name,
        string description,
        string placeholder,
        string? aspectRatio,
        bool eager,
        IReadOnlyList<WidgetPropSpec> props,
        string bundleSource)
    {
        // Only a submission still in play may change: an approved widget's bytes are
        // frozen (they're already running on visitors' pages, and the log is the audit
        // trail), and a withdrawn one is done. Revising re-opens review from Pending.
        if (Status is not (WidgetSubmissionStatus.Pending or WidgetSubmissionStatus.Rejected))
        {
            throw new DomainException($"A {Describe(Status)} widget cannot be revised.");
        }

        var byteSize = ValidateManifest(tag, name, aspectRatio, props, bundleSource);
        Raise(new WidgetRevised(
            tag, name, description, placeholder, aspectRatio, eager, [.. props], bundleSource, byteSize));
    }

    public void Approve(string approvedBy)
    {
        if (Status is not WidgetSubmissionStatus.Pending)
        {
            throw new DomainException($"Only a pending widget can be approved; this one is {Describe(Status)}.");
        }

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new DomainException("An approval must record who approved it.");
        }

        Raise(new WidgetApproved(approvedBy));
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status is not WidgetSubmissionStatus.Pending)
        {
            throw new DomainException($"Only a pending widget can be rejected; this one is {Describe(Status)}.");
        }

        if (string.IsNullOrWhiteSpace(rejectedBy))
        {
            throw new DomainException("A rejection must record who rejected it.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rejection needs a reason so the requester knows what to fix.");
        }

        Raise(new WidgetRejected(rejectedBy, reason));
    }

    public void Withdraw()
    {
        // Withdrawal is allowed from any live status (including Approved — pulling a
        // widget out of circulation), just not twice.
        if (Status is WidgetSubmissionStatus.Withdrawn)
        {
            throw new DomainException("This widget submission has already been withdrawn.");
        }

        Raise(new WidgetWithdrawn());
    }

    protected override void When(object @event)
    {
        switch (@event)
        {
            case WidgetSubmitted e:
                Id = e.WidgetSubmissionId;
                Status = WidgetSubmissionStatus.Pending;
                ApplyManifest(e.Tag, e.Name, e.Description, e.Placeholder, e.AspectRatio, e.Eager, e.Props, e.BundleSource, e.ByteSize);
                RequestedBy = e.RequestedBy;
                break;
            case WidgetRevised e:
                Status = WidgetSubmissionStatus.Pending;
                ApplyManifest(e.Tag, e.Name, e.Description, e.Placeholder, e.AspectRatio, e.Eager, e.Props, e.BundleSource, e.ByteSize);
                // A fresh review starts clean: a prior rejection no longer applies.
                RejectedBy = null;
                RejectionReason = null;
                break;
            case WidgetApproved e:
                Status = WidgetSubmissionStatus.Approved;
                ApprovedBy = e.ApprovedBy;
                break;
            case WidgetRejected e:
                Status = WidgetSubmissionStatus.Rejected;
                RejectedBy = e.RejectedBy;
                RejectionReason = e.Reason;
                break;
            case WidgetWithdrawn:
                Status = WidgetSubmissionStatus.Withdrawn;
                break;
            default:
                throw new InvalidOperationException($"WidgetSubmission cannot fold unknown event {@event.GetType().Name}.");
        }
    }

    private void ApplyManifest(
        string tag, string name, string description, string placeholder,
        string? aspectRatio, bool eager, IReadOnlyList<WidgetPropSpec> props, string bundleSource, long byteSize)
    {
        Tag = tag;
        Name = name;
        Description = description;
        Placeholder = placeholder;
        AspectRatio = aspectRatio;
        Eager = eager;
        Props = props;
        BundleSource = bundleSource;
        ByteSize = byteSize;
    }

    // Returns the bundle's byte size so the caller records the exact value it validated.
    private static long ValidateManifest(
        string tag, string name, string? aspectRatio, IReadOnlyList<WidgetPropSpec> props, string bundleSource)
    {
        if (!IsValidTag(tag))
        {
            throw new DomainException(
                $"'{tag}' is not a valid custom-element tag: lower-case letters, digits and hyphens, with at least one hyphen (e.g. x-countdown).");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A widget needs a name.");
        }

        if (props.Count > MaxProps)
        {
            throw new DomainException($"A widget can declare at most {MaxProps} properties; this one declares {props.Count}.");
        }

        foreach (var prop in props)
        {
            if (!IsValidPropName(prop.Name))
            {
                throw new DomainException(
                    $"'{prop.Name}' is not a valid property name: lower-case letters, digits and hyphens, and never an event handler (on…) or 'style'.");
            }

            if (!WidgetPropSpec.KnownTypes.Contains(prop.Type))
            {
                throw new DomainException(
                    $"Property '{prop.Name}' has an unknown type '{prop.Type}'. Known types: {string.Join(", ", WidgetPropSpec.KnownTypes)}.");
            }

            // A choice with no options can never be given a value — reject it up front.
            if (prop.Type == "choice" && prop.Options.IsDefaultOrEmpty)
            {
                throw new DomainException($"The choice property '{prop.Name}' needs at least one option.");
            }
        }

        if (aspectRatio is not null && !AspectRatioSyntax().IsMatch(aspectRatio))
        {
            throw new DomainException(
                $"'{aspectRatio}' is not a valid aspect ratio — use digits, spaces, '/' and '.' (e.g. 16 / 9).");
        }

        if (string.IsNullOrEmpty(bundleSource))
        {
            throw new DomainException("A widget submission must include its bundle source.");
        }

        var byteSize = Encoding.UTF8.GetByteCount(bundleSource);
        if (byteSize > MaxBundleBytes)
        {
            throw new DomainException(
                $"The widget bundle is {byteSize:N0} bytes; the limit is {MaxBundleBytes:N0} ({MaxBundleBytes / 1024} KB).");
        }

        return byteSize;
    }

    // Duplicates WidgetManifest.IsValidTag / IsValidPropName from Imprint.Rendering: the
    // aggregate must not reference the delivery layer, so this custom-element-name rule is
    // copied here. Keep the two in sync — both also make the tag/prop safe to emit into
    // HTML unescaped. A prop name becomes an HTML attribute NAME verbatim, so an on*/style
    // name is rejected: it would turn an author-controlled value into a live event handler
    // or inline CSS on every visitor's page (the same denial SvgPublishGuard makes).
    private static bool IsValidTag(string tag) =>
        tag.Length is > 2 and <= 64 &&
        tag.Contains('-', StringComparison.Ordinal) &&
        char.IsAsciiLetterLower(tag[0]) &&
        tag.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    private static bool IsValidPropName(string name) =>
        name.Length is > 0 and <= 64 &&
        char.IsAsciiLetterLower(name[0]) &&
        name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-') &&
        !IsReservedAttributeName(name);

    // A prop name is reserved when the widget renderer emits an attribute of that name with
    // security-sensitive meaning and an author prop must not shadow it: `style` and the
    // whole `data-island*` namespace (data-island is imported as a module URL by the island
    // loader), plus every HTML event handler ("on" + an all-letter event name). Matching the
    // handler shape blocks every real handler without rejecting names that merely begin with
    // "on" (e.g. "only-item"). Mirrors WidgetManifest.IsReservedAttributeName.
    private static bool IsReservedAttributeName(string name)
    {
        if (name == "style" || name.StartsWith("data-island", StringComparison.Ordinal))
        {
            return true;
        }

        if (name.Length <= 2 || name[0] != 'o' || name[1] != 'n')
        {
            return false;
        }

        for (var i = 2; i < name.Length; i++)
        {
            if (!char.IsAsciiLetterLower(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string Describe(WidgetSubmissionStatus status) => status switch
    {
        WidgetSubmissionStatus.Pending => "pending",
        WidgetSubmissionStatus.Approved => "approved",
        WidgetSubmissionStatus.Rejected => "rejected",
        WidgetSubmissionStatus.Withdrawn => "withdrawn",
        _ => status.ToString().ToLowerInvariant(),
    };

    [GeneratedRegex(@"^[0-9 /.]+$")]
    private static partial Regex AspectRatioSyntax();
}
