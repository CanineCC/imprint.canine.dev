using System.Text;

namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The single source of truth for the CSS class a <see cref="SectionAppearance"/> emits:
/// <c>ip-ap-{kebab}</c>, kebab-cased from the enum name (<c>FeatureGrid</c> →
/// <c>feature-grid</c>). The renderer, the block seeder and the marketing stylesheet all
/// derive the class name here so they can never drift out of the shared contract.
/// <see cref="SectionAppearance.Plain"/> has no class (it is the structural default).
/// </summary>
public static class SectionAppearanceClass
{
    /// <summary>The full class (<c>ip-ap-…</c>) for an appearance, or null for <see cref="SectionAppearance.Plain"/>.</summary>
    public static string? For(SectionAppearance appearance) =>
        appearance is SectionAppearance.Plain ? null : $"ip-ap-{Kebab(appearance.ToString())}";

    /// <summary>The kebab-cased suffix (<c>feature-grid</c>) without the <c>ip-ap-</c> prefix.</summary>
    public static string Suffix(SectionAppearance appearance) => Kebab(appearance.ToString());

    private static string Kebab(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
