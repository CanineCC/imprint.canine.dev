using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Projections;

/// <summary>
/// Drives the <see cref="WidgetRegistry"/> fold directly through the aggregate store
/// (appending exactly the events the slices would), so these assertions are about the
/// projection itself — independent of the feature handlers.
/// </summary>
public sealed class WidgetRegistryTests
{
    private const string Bundle = "export default class extends HTMLElement { connectedCallback() {} }";

    private static WidgetSubmission NewSubmission(WidgetSubmissionId id, string tag, string requestedBy = "editor@example.com") =>
        WidgetSubmission.Submit(
            id, tag, "Widget", "A description", "Loading…", "16 / 9", true,
            [new WidgetPropSpec("mode", "Mode", "choice", "dark", ["dark", "light"])],
            Bundle, requestedBy);

    private static async Task Append(AuthoringTestHost host, WidgetSubmission submission) =>
        await host.SaveAggregate(submission);

    [Fact]
    public async Task Approved_submission_appears_in_the_approved_view_with_its_bundle()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        var submission = NewSubmission(id, "x-alpha");
        submission.Approve("admin@example.com");
        await Append(host, submission);

        var registry = host.Get<WidgetRegistry>();
        Assert.Single(registry.Approved);
        Assert.True(registry.IsApprovedTag("x-alpha"));
        Assert.Equal(Bundle, registry.BundleOf("x-alpha"));
        Assert.Equal("x-alpha", Assert.Single(registry.ApprovedTags));
    }

    [Fact]
    public async Task Rejected_and_withdrawn_submissions_are_listed_but_never_approved()
    {
        await using var host = new AuthoringTestHost();

        var rejected = NewSubmission(WidgetSubmissionId.New(), "x-rejected");
        rejected.Reject("admin", "no");
        await Append(host, rejected);

        var withdrawn = NewSubmission(WidgetSubmissionId.New(), "x-withdrawn");
        withdrawn.Withdraw();
        await Append(host, withdrawn);

        var pending = NewSubmission(WidgetSubmissionId.New(), "x-pending");
        await Append(host, pending);

        var registry = host.Get<WidgetRegistry>();
        Assert.Equal(3, registry.Submissions.Count);
        Assert.Empty(registry.Approved);
        Assert.False(registry.IsApprovedTag("x-rejected"));
        Assert.False(registry.IsApprovedTag("x-withdrawn"));
        Assert.False(registry.IsApprovedTag("x-pending"));
        Assert.Null(registry.BundleOf("x-rejected"));
    }

    [Fact]
    public async Task Submissions_are_returned_in_submission_order()
    {
        await using var host = new AuthoringTestHost();
        var one = WidgetSubmissionId.New();
        var two = WidgetSubmissionId.New();
        var three = WidgetSubmissionId.New();
        await Append(host, NewSubmission(one, "x-one"));
        await Append(host, NewSubmission(two, "x-two"));
        await Append(host, NewSubmission(three, "x-three"));

        var order = host.Get<WidgetRegistry>().Submissions.Select(s => s.Id).ToList();

        Assert.Equal(new[] { one, two, three }, order);
    }

    [Fact]
    public async Task A_revised_submission_folds_its_new_tag_into_the_registry()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        var submission = NewSubmission(id, "x-old");
        submission.Reject("admin", "rename it");
        submission.Revise("x-new", "New", "d", "p", null, false, [], Bundle);
        await Append(host, submission);

        var stored = host.Get<WidgetRegistry>().Get(id)!;
        Assert.Equal("x-new", stored.Tag);
        Assert.Equal(WidgetSubmissionStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task The_registry_survives_a_full_projection_rebuild()
    {
        await using var host = new AuthoringTestHost();
        var id = WidgetSubmissionId.New();
        var submission = NewSubmission(id, "x-alpha");
        submission.Approve("admin@example.com");
        await Append(host, submission);

        // A rebuild resets and replays the whole log — derived state is disposable.
        await host.Get<ProjectionEngine>().Rebuild();

        var registry = host.Get<WidgetRegistry>();
        Assert.True(registry.IsApprovedTag("x-alpha"));
        Assert.Equal(Bundle, registry.BundleOf("x-alpha"));
    }
}
