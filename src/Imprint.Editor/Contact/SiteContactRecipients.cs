using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Projections;

namespace Imprint.Editor.Contact;

/// <summary>
/// Finds the <c>recipients</c> prop of the submitting site's contact-form widget, live
/// from the read models at request time. The form posts a <c>site</c> field carrying
/// <c>window.location.hostname</c>; that host is matched against each site's environment
/// origins (<see cref="Site.Environments"/> BaseUrl hosts, www-insensitive) and, as a
/// courtesy, the site's name. The matched site's draft pages are then walked for the
/// first contact-form <see cref="WidgetNode"/> with a non-blank <c>recipients</c> value.
/// Reading DRAFT state is deliberate: the prop is private (never published), so the
/// draft IS its only home — editing it must not require a republish to take effect.
/// </summary>
public sealed class SiteContactRecipients(SiteOverview sites, PageDrafts drafts)
{
    public string? Find(string? submittedHost)
    {
        var host = submittedHost?.Trim();
        if (host is not { Length: > 0 })
        {
            return null;
        }

        // Accepted race: the read models mutate as editors work, exactly as they do under
        // every editor panel — a submission during an edit sees one side or the other.
        foreach (var site in sites.All)
        {
            if (!Matches(site, host))
            {
                continue;
            }

            foreach (var page in drafts.All)
            {
                if (page.SiteId != site.Id)
                {
                    continue;
                }

                foreach (var node in page.Tree.All())
                {
                    if (node is WidgetNode { Tag: "contact-form" } widget &&
                        widget.Props.Get("recipients") is { } recipients &&
                        !string.IsNullOrWhiteSpace(recipients))
                    {
                        return recipients;
                    }
                }
            }

            return null; // the matched site simply has no recipients prop set
        }

        return null;
    }

    internal static bool Matches(Site site, string host)
    {
        if (string.Equals(site.Name, host, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var environment in site.Environments)
        {
            if (environment.BaseUrl is { Length: > 0 } baseUrl &&
                Uri.TryCreate(baseUrl, UriKind.Absolute, out var origin) &&
                HostsEqual(origin.Host, host))
            {
                return true;
            }
        }

        return false;
    }

    // www.canine.dev and canine.dev are the same inbox: the marketing sites 301 the www
    // alias to the apex, but a form served from either must still find its site.
    private static bool HostsEqual(string a, string b) =>
        string.Equals(StripWww(a), StripWww(b), StringComparison.OrdinalIgnoreCase);

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
}
