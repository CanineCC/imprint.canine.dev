using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Domain.Widgets.Events;

namespace Imprint.Authoring.Tests.Domain.Widgets;

public sealed class WidgetEventSamples : IEventSampleProvider
{
    // A realistic prop set so the samples exercise every prop shape: a plain text prop
    // with a default, a choice prop carrying its options (the list inside the list), and
    // a toggle. That way the round-trip battery proves the WidgetPropSpec value-equality
    // and the events' SequenceEqual-on-Props actually hold.
    private static IReadOnlyList<WidgetPropSpec> Props() =>
    [
        new WidgetPropSpec("label", "Label", "text", "Hello", []),
        new WidgetPropSpec("theme", "Theme", "choice", "dark", ["dark", "light"]),
        new WidgetPropSpec("eager", "Eager", "toggle", "false", []),
    ];

    public IEnumerable<object> Samples =>
    [
        new WidgetSubmitted(
            WidgetSubmissionId.New(), "x-countdown", "Countdown", "A live countdown timer.",
            "Loading countdown…", "16 / 9", Eager: true, Props(),
            "export default class extends HTMLElement { connectedCallback() {} }", ByteSize: 512, "editor@example.com"),
        new WidgetRevised(
            "x-countdown", "Countdown", "A live countdown timer, revised.",
            "Loading…", AspectRatio: null, Eager: false, Props(),
            "export default class extends HTMLElement { connectedCallback() { /* v2 */ } }", ByteSize: 640),
        new WidgetApproved("admin@example.com"),
        new WidgetRejected("admin@example.com", "Uses eval() — please remove it and resubmit."),
        new WidgetWithdrawn(),
    ];
}
