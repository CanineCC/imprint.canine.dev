using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The publisher's widget-bundle sourcing: a used, approved (non-built-in) widget has no
/// file on disk — its reviewed source lives in the registry and must be written to
/// <c>widgets/{tag}.{hash}.js</c>, from where island hydration works exactly as for a
/// copied built-in bundle. (The submit → approve write path is B1's; here the registry is
/// seeded directly, standing in for the folding projection.)
/// </summary>
public sealed class ApprovedWidgetPublishTests
{
    private const string BundleSource =
        "export default class XLive extends HTMLElement { connectedCallback() { this.textContent = 'live'; } }\n";

    [Fact]
    public async Task Approved_widget_bundle_is_written_from_the_registry_source_and_the_page_hydrates_it()
    {
        await using var host = new PublishingTestHost();

        // No widget files on disk (the host's manifest is empty "[]"). The only source of
        // truth for x-live is the approved submission the registry folds from the log.
        await host.SubmitWidget(
            "x-live", BundleSource, "Live widget",
            props: [new WidgetPropSpec("text", "Text", "text", null, [])],
            approve: true);

        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode
            {
                Id = NodeId.New(),
                Tag = "x-live",
                Props = PropBag.Of([new KeyValuePair<string, string>("text", "hello")]),
            }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        await host.Publisher.Synchronize();

        // The bundle was written from the approved source, content-hashed.
        var bundle = Assert.Single(host.FilesMatching("widgets/x-live.", ".js"));
        Assert.Equal(BundleSource, host.ReadText(bundle));

        // The static page carries the custom element and hydrates it from that bundle.
        var html = host.ReadText("index.html");
        Assert.Contains("<x-live", html);
        Assert.Contains($"data-island=\"/{bundle}\"", html);
        Assert.Contains("text=\"hello\"", html);
    }

    [Fact]
    public async Task A_pending_submission_never_publishes_its_bundle()
    {
        await using var host = new PublishingTestHost();

        // Submitted but NOT approved: still pending, so its bundle must never publish.
        await host.SubmitWidget("x-live", BundleSource, "Live widget");

        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new WidgetNode { Id = NodeId.New(), Tag = "x-live" }),
        });
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        await host.Publisher.Synchronize();

        // Unapproved: no bundle, and the unknown tag is omitted from the static output.
        Assert.Empty(host.FilesMatching("widgets/"));
        Assert.DoesNotContain("<x-live", host.ReadText("index.html"));
    }
}
