using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Features.Widgets.ApproveWidget;
using Imprint.Authoring.Features.Widgets.RejectWidget;
using Imprint.Authoring.Features.Widgets.ReviseWidget;
using Imprint.Authoring.Features.Widgets.SubmitWidget;
using Imprint.Authoring.Features.Widgets.WithdrawWidget;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Widgets;

public sealed class WidgetWorkflowTests
{
    private const string Bundle = "export default class extends HTMLElement { connectedCallback() {} }";

    private static SubmitWidget SubmitCmd(
        WidgetSubmissionId id, string tag = "x-countdown", string requestedBy = "editor@example.com") =>
        new(id, tag, "Countdown", "A live countdown.", "Loading…", "16 / 9", true,
            [new WidgetPropSpec("mode", "Mode", "choice", "dark", ["dark", "light"])],
            Bundle, requestedBy);

    // ---------------------------------------------------------------- submit → registry

    [Fact]
    public async Task SubmitWidget_records_a_pending_submission_that_is_not_yet_approved()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();

        await host.Ok(SubmitCmd(id));

        var registry = host.Get<WidgetRegistry>();
        var submission = Assert.Single(registry.Submissions);
        Assert.Equal(WidgetSubmissionStatus.Pending, submission.Status);
        Assert.Equal("editor@example.com", submission.RequestedBy);
        Assert.Empty(registry.Approved);
        Assert.False(registry.IsApprovedTag("x-countdown"));
        Assert.Null(registry.BundleOf("x-countdown"));
    }

    // ---------------------------------------------------------------- approve

    [Fact]
    public async Task ApproveWidget_publishes_the_tag_and_bundle_to_the_approved_view()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(id));

        await host.Ok(new ApproveWidget(id, "admin@example.com"));

        var registry = host.Get<WidgetRegistry>();
        var approved = Assert.Single(registry.Approved);
        Assert.Equal("x-countdown", approved.Tag);
        Assert.Equal("admin@example.com", approved.ApprovedBy);
        Assert.True(registry.IsApprovedTag("x-countdown"));
        Assert.Equal(Bundle, registry.BundleOf("x-countdown"));
        Assert.Contains("x-countdown", registry.ApprovedTags);
    }

    // ---------------------------------------------------------------- reject / withdraw drop from Approved

    [Fact]
    public async Task RejectWidget_keeps_the_submission_but_never_approves_it()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(id));

        await host.Ok(new RejectWidget(id, "admin@example.com", "Uses eval — please remove."));

        var registry = host.Get<WidgetRegistry>();
        var submission = Assert.Single(registry.Submissions);
        Assert.Equal(WidgetSubmissionStatus.Rejected, submission.Status);
        Assert.Equal("Uses eval — please remove.", submission.RejectionReason);
        Assert.Empty(registry.Approved);
    }

    [Fact]
    public async Task WithdrawWidget_drops_an_approved_widget_from_the_approved_view()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(id));
        await host.Ok(new ApproveWidget(id, "admin@example.com"));

        await host.Ok(new WithdrawWidget(id));

        var registry = host.Get<WidgetRegistry>();
        Assert.Equal(WidgetSubmissionStatus.Withdrawn, registry.Get(id)!.Status);
        Assert.Empty(registry.Approved);
        Assert.False(registry.IsApprovedTag("x-countdown"));
    }

    // ---------------------------------------------------------------- revise

    [Fact]
    public async Task ReviseWidget_reopens_a_rejected_submission_for_review()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(id));
        await host.Ok(new RejectWidget(id, "admin@example.com", "nope"));

        await host.Ok(new ReviseWidget(
            id, "x-countdown", "Countdown 2", "Revised.", "Loading…", null, false, [],
            "export default class extends HTMLElement { connectedCallback() { /* v2 */ } }"));

        var submission = host.Get<WidgetRegistry>().Get(id)!;
        Assert.Equal(WidgetSubmissionStatus.Pending, submission.Status);
        Assert.Equal("Countdown 2", submission.Name);
        Assert.Null(submission.RejectionReason);
    }

    // ---------------------------------------------------------------- tag collisions (documented race)

    [Fact]
    public async Task SubmitWidget_is_rejected_when_the_tag_is_already_approved()
    {
        await using var host = new AuthoringTestHost();
        var first = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(first, "x-shared"));
        await host.Ok(new ApproveWidget(first, "admin@example.com"));

        var error = await host.Fails(SubmitCmd(WidgetSubmissionId.New(), "x-shared"));

        Assert.Contains("already used by an approved widget", error);
    }

    [Fact]
    public async Task ApproveWidget_re_checks_the_collision_so_the_second_approval_of_a_shared_tag_fails()
    {
        await using var host = new AuthoringTestHost();
        var a = WidgetSubmissionId.New();
        var b = WidgetSubmissionId.New();
        // Two pending submissions may share a tag — the gate is at approval.
        await host.Ok(SubmitCmd(a, "x-shared"));
        await host.Ok(SubmitCmd(b, "x-shared"));

        await host.Ok(new ApproveWidget(a, "admin@example.com"));
        var error = await host.Fails(new ApproveWidget(b, "admin@example.com"));

        Assert.Contains("approved for another widget", error);
        Assert.Equal(WidgetSubmissionStatus.Pending, host.Get<WidgetRegistry>().Get(b)!.Status);
    }

    [Fact]
    public async Task ReviseWidget_is_rejected_when_the_new_tag_belongs_to_an_approved_widget()
    {
        await using var host = new AuthoringTestHost();
        var approved = WidgetSubmissionId.New();
        var pending = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(approved, "x-alpha"));
        await host.Ok(new ApproveWidget(approved, "admin@example.com"));
        await host.Ok(SubmitCmd(pending, "x-beta"));

        var error = await host.Fails(new ReviseWidget(
            pending, "x-alpha", "B", "d", "p", null, false, [], Bundle));

        Assert.Contains("already used by an approved widget", error);
    }

    // ---------------------------------------------------------------- state machine surfaces through the slice

    [Fact]
    public async Task ApproveWidget_on_a_withdrawn_submission_surfaces_the_aggregate_error()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        await host.Ok(SubmitCmd(id));
        await host.Ok(new WithdrawWidget(id));

        var error = await host.Fails(new ApproveWidget(id, "admin@example.com"));

        Assert.Contains("Only a pending widget can be approved", error);
    }

    [Fact]
    public async Task SubmitWidget_with_an_invalid_manifest_surfaces_the_aggregate_error()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(SubmitCmd(WidgetSubmissionId.New(), "nohyphen"));

        Assert.Contains("custom-element tag", error);
        Assert.Empty(host.Get<WidgetRegistry>().Submissions);
    }
}
