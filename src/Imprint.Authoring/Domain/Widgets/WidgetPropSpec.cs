using System.Collections.Immutable;

namespace Imprint.Authoring.Domain.Widgets;

/// <summary>
/// A widget's declared property, as carried in a <c>WidgetSubmission</c>'s events. The
/// domain deliberately keeps <see cref="Type"/> a validated string rather than
/// depending on the delivery-side <c>WidgetPropType</c> enum — the catalog maps it to a
/// rendering descriptor at the boundary. Structural equality (records with a list need
/// help) so submission events round-trip and compare by value.
/// </summary>
public sealed record WidgetPropSpec(
    string Name,
    string Label,
    string Type,
    string? Default,
    ImmutableArray<string> Options)
{
    /// <summary>The property types the editor form and the manifest schema understand.</summary>
    public static readonly ImmutableArray<string> KnownTypes =
        ["text", "number", "color", "url", "choice", "toggle"];

    public bool Equals(WidgetPropSpec? other) =>
        other is not null &&
        Name == other.Name && Label == other.Label && Type == other.Type &&
        Default == other.Default && Options.SequenceEqual(other.Options);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(Label);
        hash.Add(Type);
        hash.Add(Default);
        foreach (var option in Options)
        {
            hash.Add(option);
        }

        return hash.ToHashCode();
    }
}
