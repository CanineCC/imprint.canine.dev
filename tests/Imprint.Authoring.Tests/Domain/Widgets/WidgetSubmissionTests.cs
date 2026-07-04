using System.Text;
using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Domain.Widgets.Events;
using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Widgets;

public sealed class WidgetSubmissionTests
{
    private static readonly WidgetSubmissionId Id = WidgetSubmissionId.New();
    private const string Bundle = "export default class extends HTMLElement { connectedCallback() {} }";
    private static readonly long BundleBytes = Encoding.UTF8.GetByteCount(Bundle);

    private static IReadOnlyList<WidgetPropSpec> Props() =>
    [
        new WidgetPropSpec("label", "Label", "text", "Hi", []),
        new WidgetPropSpec("theme", "Theme", "choice", "dark", ["dark", "light"]),
    ];

    // A valid submission with one field swappable per test.
    private static WidgetSubmission Submit(
        string tag = "x-countdown",
        string name = "Countdown",
        string? aspectRatio = "16 / 9",
        IReadOnlyList<WidgetPropSpec>? props = null,
        string? bundle = null,
        string requestedBy = "editor@example.com") =>
        WidgetSubmission.Submit(
            WidgetSubmissionId.New(), tag, name, "A description", "Loading…",
            aspectRatio, eager: true, props ?? Props(), bundle ?? Bundle, requestedBy);

    private static WidgetSubmitted Submitted(string tag = "x-countdown") =>
        new(Id, tag, "Countdown", "A description", "Loading…", "16 / 9", true, Props(), Bundle, BundleBytes, "editor@example.com");

    // ---------------------------------------------------------------- submit (happy)

    [Fact]
    public void Submit_valid_manifest_raises_widget_submitted_and_is_pending()
    {
        var submission = WidgetSubmission.Submit(
            Id, "x-countdown", "Countdown", "A description", "Loading…", "16 / 9", true, Props(), Bundle, "editor@example.com");

        var raised = Assert.Single(submission.UncommittedEvents);
        var e = Assert.IsType<WidgetSubmitted>(raised);
        Assert.Equal(Id, e.WidgetSubmissionId);
        Assert.Equal("x-countdown", e.Tag);
        // The aggregate computes and records the exact byte size it validated.
        Assert.Equal(BundleBytes, e.ByteSize);
        Assert.Equal("editor@example.com", e.RequestedBy);
        Assert.Equal(WidgetSubmissionStatus.Pending, submission.Status);
        Assert.Equal(2, submission.Props.Count);
    }

    [Fact]
    public void Submit_without_an_aspect_ratio_is_accepted()
    {
        var submission = Submit(aspectRatio: null);
        Assert.Single(submission.UncommittedEvents);
        Assert.Null(submission.AspectRatio);
    }

    [Fact]
    public void Submit_at_the_byte_cap_is_accepted()
    {
        var submission = Submit(bundle: new string('a', WidgetSubmission.MaxBundleBytes));
        Assert.Single(submission.UncommittedEvents);
    }

    // ---------------------------------------------------------------- submit (validation battery)

    [Theory]
    [InlineData("nohyphen")]
    [InlineData("-leading")]
    [InlineData("UPPER-CASE")]
    [InlineData("x")]
    [InlineData("has space")]
    public void Submit_with_an_invalid_tag_is_rejected(string tag)
    {
        var ex = Assert.Throws<DomainException>(() => Submit(tag: tag));
        Assert.Contains("custom-element tag", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_over_the_byte_cap_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => Submit(bundle: new string('a', WidgetSubmission.MaxBundleBytes + 1)));
        Assert.Contains("limit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_with_an_empty_bundle_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => Submit(bundle: ""));
        Assert.Contains("bundle source", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_with_an_unknown_prop_type_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Submit(props: [new WidgetPropSpec("mode", "Mode", "widget-magic", null, [])]));
        Assert.Contains("unknown type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_with_a_choice_prop_and_no_options_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Submit(props: [new WidgetPropSpec("mode", "Mode", "choice", null, [])]));
        Assert.Contains("at least one option", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_with_an_invalid_prop_name_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Submit(props: [new WidgetPropSpec("Bad Name", "Label", "text", null, [])]));
        Assert.Contains("property name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("onclick")]
    [InlineData("onmouseover")]
    [InlineData("onerror")]
    [InlineData("onfocus")]
    [InlineData("style")]
    [InlineData("data-island")]        // the island loader imports this value as a module URL
    [InlineData("data-island-eager")]
    public void Submit_with_a_reserved_attribute_prop_name_is_rejected(string propName)
    {
        // A prop name becomes an HTML attribute name verbatim. An on*/style name turns its
        // author value into a live event handler / inline style; a data-island* name shadows
        // the loader's own attribute so the browser imports an attacker-chosen module. None
        // may ever clear submission — that would bypass admin bundle review entirely.
        var ex = Assert.Throws<DomainException>(() =>
            Submit(props: [new WidgetPropSpec(propName, "Label", "text", null, [])]));
        Assert.Contains("property name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("on")]          // bare "on" is not an event handler
    [InlineData("only-item")]   // starts with "on" but has a hyphen — a plain data attribute
    [InlineData("on-sale")]
    public void Submit_with_a_prop_name_that_only_resembles_an_event_handler_is_allowed(string propName)
    {
        // The denial targets the real event-handler shape (on + letters), not any name that
        // happens to begin with "on" — those legitimate names must still validate.
        var submission = Submit(props: [new WidgetPropSpec(propName, "Label", "text", null, [])]);
        Assert.Equal(propName, Assert.Single(submission.Props).Name);
    }

    [Fact]
    public void Submit_with_more_than_twenty_props_is_rejected()
    {
        IReadOnlyList<WidgetPropSpec> tooMany =
            [.. Enumerable.Range(0, 21).Select(i => new WidgetPropSpec($"p{i}", "L", "text", null, []))];
        var ex = Assert.Throws<DomainException>(() => Submit(props: tooMany));
        Assert.Contains("at most 20", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("16x9")]
    [InlineData("wide")]
    [InlineData("16:9")]
    public void Submit_with_an_invalid_aspect_ratio_is_rejected(string ratio)
    {
        var ex = Assert.Throws<DomainException>(() => Submit(aspectRatio: ratio));
        Assert.Contains("aspect ratio", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_with_a_blank_name_is_rejected(string name)
    {
        var ex = Assert.Throws<DomainException>(() => Submit(name: name));
        Assert.Contains("needs a name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_without_a_requester_is_rejected(string requestedBy)
    {
        var ex = Assert.Throws<DomainException>(() => Submit(requestedBy: requestedBy));
        Assert.Contains("who requested", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- approve

    [Fact]
    public void Approve_a_pending_submission_raises_widget_approved()
    {
        var outcome = AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Approve("admin@example.com"));

        outcome.ThenRaised(new WidgetApproved("admin@example.com"));
        Assert.Equal(WidgetSubmissionStatus.Approved, outcome.Aggregate.Status);
        Assert.Equal("admin@example.com", outcome.Aggregate.ApprovedBy);
    }

    [Fact]
    public void Approve_without_an_approver_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Approve("  "))
            .ThenFails("who approved");

    [Fact]
    public void Approve_an_already_approved_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetApproved("admin"))
            .When(s => s.Approve("admin2"))
            .ThenFails("pending");

    [Fact]
    public void Approve_a_rejected_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetRejected("admin", "no"))
            .When(s => s.Approve("admin"))
            .ThenFails("pending");

    [Fact]
    public void Approve_a_withdrawn_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetWithdrawn())
            .When(s => s.Approve("admin"))
            .ThenFails("pending");

    // ---------------------------------------------------------------- reject

    [Fact]
    public void Reject_a_pending_submission_raises_widget_rejected()
    {
        var outcome = AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Reject("admin", "Uses eval"));

        outcome.ThenRaised(new WidgetRejected("admin", "Uses eval"));
        Assert.Equal(WidgetSubmissionStatus.Rejected, outcome.Aggregate.Status);
        Assert.Equal("Uses eval", outcome.Aggregate.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_without_a_reason_is_rejected(string reason) =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Reject("admin", reason))
            .ThenFails("needs a reason");

    [Fact]
    public void Reject_without_a_rejecter_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Reject(" ", "no"))
            .ThenFails("who rejected");

    [Fact]
    public void Reject_an_approved_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetApproved("admin"))
            .When(s => s.Reject("admin", "too late"))
            .ThenFails("pending");

    // ---------------------------------------------------------------- withdraw

    [Fact]
    public void Withdraw_a_pending_submission_raises_widget_withdrawn()
    {
        var outcome = AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Withdraw());

        outcome.ThenRaised(new WidgetWithdrawn());
        Assert.Equal(WidgetSubmissionStatus.Withdrawn, outcome.Aggregate.Status);
    }

    [Fact]
    public void Withdraw_an_approved_submission_is_allowed() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetApproved("admin"))
            .When(s => s.Withdraw())
            .ThenRaised(new WidgetWithdrawn());

    [Fact]
    public void Withdraw_twice_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetWithdrawn())
            .When(s => s.Withdraw())
            .ThenFails("already been withdrawn");

    // ---------------------------------------------------------------- revise

    [Fact]
    public void Revise_a_pending_submission_reopens_review_with_the_new_manifest()
    {
        var outcome = AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Revise("x-timer", "Timer", "d", "p", null, false, [], "export {}"));

        outcome.ThenRaised(new WidgetRevised(
            "x-timer", "Timer", "d", "p", null, false, [], "export {}", Encoding.UTF8.GetByteCount("export {}")));
        Assert.Equal(WidgetSubmissionStatus.Pending, outcome.Aggregate.Status);
        Assert.Equal("x-timer", outcome.Aggregate.Tag);
    }

    [Fact]
    public void Revise_a_rejected_submission_reopens_review_and_clears_the_rejection()
    {
        var outcome = AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetRejected("admin", "Uses eval"))
            .When(s => s.Revise("x-countdown", "Countdown", "d", "p", "16 / 9", true, Props(), Bundle));

        Assert.Equal(WidgetSubmissionStatus.Pending, outcome.Aggregate.Status);
        Assert.Null(outcome.Aggregate.RejectionReason);
        Assert.Null(outcome.Aggregate.RejectedBy);
    }

    [Fact]
    public void Revise_still_validates_the_manifest() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted())
            .When(s => s.Revise("nohyphen", "Timer", "d", "p", null, false, [], "export {}"))
            .ThenFails("custom-element tag");

    [Fact]
    public void Revise_an_approved_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetApproved("admin"))
            .When(s => s.Revise("x-countdown", "Countdown", "d", "p", null, false, [], "export {}"))
            .ThenFails("approved");

    [Fact]
    public void Revise_a_withdrawn_submission_is_rejected() =>
        AggregateSpec.For<WidgetSubmission>()
            .Given(Submitted(), new WidgetWithdrawn())
            .When(s => s.Revise("x-countdown", "Countdown", "d", "p", null, false, [], "export {}"))
            .ThenFails("withdrawn");
}
