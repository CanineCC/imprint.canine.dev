namespace Imprint.Authoring.Domain.Pages;

/// <summary>
/// The one reader of imprint's own <c>asset:{guid}</c> href scheme — a link to an uploaded
/// file (a whitepaper PDF, a press kit) that survives publishing. Prose and buttons store
/// the asset reference rather than a media path because the two planes serve the same file
/// from different URLs: the editor from <c>/media/…</c> behind auth, the published site
/// from a hashed <c>/assets/…</c> file the publisher only ships when something references
/// it. A raw URL can be right on at most one plane; the reference is right on both.
/// <para>Shared by the validator and the renderer so the grammar they enforce and the
/// grammar they resolve cannot drift into a parser differential (see <see cref="PageHref"/>).</para>
/// </summary>
public static class AssetHref
{
    public const string Scheme = "asset:";

    /// <summary>True when <paramref name="href"/> is a well-formed asset reference.</summary>
    public static bool TryParse(string href, out AssetId assetId)
    {
        assetId = default;
        if (!href.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Guid.TryParse(href[Scheme.Length..], out var guid))
        {
            return false;
        }

        assetId = AssetId.From(guid);
        return true;
    }
}
