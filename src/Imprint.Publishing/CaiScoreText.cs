using System.Globalization;

namespace Imprint.Publishing;

/// <summary>
/// ★ The one way a CAI score becomes text on the websites (owner, 12 November 2026; ADR-0027 in kennel.canine.dev): one
/// decimal, always — <c>97.6</c>, and <c>98.0</c> when it lands on a whole number — rounded half to even as the CAI
/// standard states (§7), the same tenth the product writes, never <c>-0.0</c>. IMP0001 fails the build when a score is
/// written any other way, and IMP0002 when one is carried under a name that hides it.
/// </summary>
internal static class CaiScoreText
{
    /// <summary>The score with one decimal: <c>72.4</c>, <c>70.0</c>.</summary>
    public static string Shown(double score)
    {
        var tenths = Tenths(score);
        return (tenths == 0 ? 0 : tenths).ToString("0.0", CultureInfo.InvariantCulture);
    }

    /// <summary>The shown tenth as a number (half to even), for comparing or subtracting shown figures.</summary>
    public static double Tenths(double score) => Math.Round(score, 1, MidpointRounding.ToEven);

    /// <summary>The size of a change between two scores as the reader sees them: the difference of the two shown figures,
    /// one decimal, no sign (62.0 to 97.6 is 35.6).</summary>
    public static string Change(double from, double to) => Shown(Math.Abs(Tenths(to) - Tenths(from)));
}

/// <summary>Marks a member, parameter or return that holds a CAI score under a name that does not say so, so IMP0001 can
/// follow it to where it becomes text.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Method)]
internal sealed class CaiScoreAttribute : Attribute;

/// <summary>The reviewed exemption: a number named like a score that is not one (a band line, a drawing's geometry), or a
/// member whose text no reader sees. The reason is required.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Method
    | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class NotACaiScoreAttribute(string reason) : Attribute
{
    /// <summary>Why this is not a score.</summary>
    public string Reason { get; } = reason;
}
