using Imprint.Authoring.Domain.Widgets;
using Imprint.Authoring.Projections;
using Imprint.Rendering;

namespace Imprint.Publishing;

/// <summary>
/// Maps an approved <see cref="WidgetSubmissionView"/> (authoring/domain shape, prop
/// <c>Type</c> is a validated string) into the delivery-side <see cref="WidgetDescriptor"/>
/// the render components understand. This is the one boundary where a submission's
/// <see cref="WidgetPropSpec"/> becomes a <see cref="WidgetProp"/> and the string type
/// becomes the <see cref="WidgetPropType"/> enum. Shared by the merged editor catalog and
/// the static publisher so the two see byte-identical descriptors for an approved widget.
/// </summary>
public static class ApprovedWidgetDescriptors
{
    public static WidgetDescriptor ToDescriptor(WidgetSubmissionView submission) => new()
    {
        Tag = submission.Tag,
        Name = submission.Name,
        Description = submission.Description,
        // Approved widgets have no file on disk: their bundle SOURCE is sourced from the
        // registry (WidgetView never reads Bundle; the publisher branches on the tag). A
        // non-empty value only satisfies the required member.
        Bundle = $"{submission.Tag}.js",
        AspectRatio = submission.AspectRatio,
        Placeholder = submission.Placeholder,
        Eager = submission.Eager,
        Props = [.. submission.Props.Select(ToProp)],
    };

    private static WidgetProp ToProp(WidgetPropSpec spec) => new()
    {
        Name = spec.Name,
        Label = spec.Label,
        Type = ToType(spec.Type),
        Default = spec.Default,
        Options = [.. spec.Options],
    };

    // The six known types are validated on submit (WidgetPropSpec.KnownTypes); an unknown
    // string can only reach here via a hand-edited stream, and Text is the safe fallback.
    private static WidgetPropType ToType(string type) => type switch
    {
        "number" => WidgetPropType.Number,
        "color" => WidgetPropType.Color,
        "url" => WidgetPropType.Url,
        "choice" => WidgetPropType.Choice,
        "toggle" => WidgetPropType.Toggle,
        _ => WidgetPropType.Text,
    };
}
